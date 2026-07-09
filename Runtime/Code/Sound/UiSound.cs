using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GuiToolkit
{
	/// <summary>
	/// Global UI sound. A single, self-bootstrapping listener that plays the
	/// configured UI sounds when the user presses a control — the
	/// <see cref="EUiSoundType.Click"/> sound for buttons (GuiToolkit
	/// <see cref="UiButtonBase"/>s and plain UGUI <see cref="Button"/>s) and the
	/// <see cref="EUiSoundType.Toggle"/> sound for toggles (GuiToolkit
	/// <see cref="UiToggle"/>s and plain UGUI <see cref="Toggle"/>s) — with zero
	/// per-control setup. Further sound types (hover, ...) can be added to
	/// <see cref="EUiSoundType"/> / <see cref="UiSoundConfig"/> and triggered here.
	///
	/// Detection: on pointer-down we raycast the UI at the pointer position via the
	/// current <see cref="EventSystem"/> and, if the frontmost hit resolves to an
	/// interactable control, play the matching sound. We fire on pointer-DOWN because
	/// <see cref="UiButtonBase"/> triggers its own action on PointerDown, which
	/// keeps the sound in sync with the toolkit's buttons. The detector is
	/// independent of the active UI input module.
	///
	/// Content lives in a <see cref="UiSoundConfig"/> asset supplied by the host
	/// (referenced from <see cref="UiToolkitConfiguration"/>). The host also owns
	/// mute/volume: assign <see cref="MutedProvider"/> and/or
	/// <see cref="VolumeProvider"/> to route UI sound through the game's own sound
	/// settings. Both are optional — when unset, sounds play at the configured
	/// volume.
	/// </summary>
	public class UiSound : MonoBehaviour
	{
		/// When assigned and it returns true, all UI sounds are suppressed.
		public static Func<bool> MutedProvider;

		/// When assigned, scales the UI sound volume; expected range [0..1].
		public static Func<float> VolumeProvider;

		private static UiSound s_instance;
		private static readonly IInputProxy s_fallbackInput = new UnityInputProxy();

		private AudioSource m_audioSource;
		private UiSoundConfig m_config;
		private readonly List<RaycastResult> m_raycastResults = new List<RaycastResult>();

		// Prefer the host-configured proxy (so games swapping input backends stay
		// consistent), fall back to the plain Unity proxy before settings init.
		private static IInputProxy InputProxy =>
			PlayerSettings.Instance != null ? PlayerSettings.Instance.InputProxy : s_fallbackInput;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static void Bootstrap()
		{
			if (s_instance != null)
				return;

			var go = new GameObject(nameof(UiSound));
			DontDestroyOnLoad(go);
			go.AddComponent<UiSound>();
		}

		/// <summary>Overrides the sound set at runtime (otherwise taken from UiToolkitConfiguration).</summary>
		public static void SetConfig( UiSoundConfig _config )
		{
			if (s_instance != null)
				s_instance.m_config = _config;
		}

		/// <summary>Plays the configured button-click sound on demand, honoring mute/volume.</summary>
		public static void PlayClick() => Play(EUiSoundType.Click);

		/// <summary>Plays a configured UI sound on demand, honoring mute/volume.</summary>
		public static void Play( EUiSoundType _type )
		{
			if (s_instance != null)
				s_instance.PlayType(_type, "Play() call");
		}

		private void Awake()
		{
			if (s_instance != null && s_instance != this)
			{
				Destroy(gameObject);
				return;
			}
			s_instance = this;

			m_audioSource = gameObject.AddComponent<AudioSource>();
			m_audioSource.playOnAwake = false;

			var config = UiToolkitConfiguration.Instance;
			if (config != null)
				m_config = config.UiSoundConfig;
		}

		private void OnDestroy()
		{
			if (s_instance == this)
				s_instance = null;
		}

		private void Update()
		{
			var eventSystem = EventSystem.current;
			if (eventSystem == null)
				return;

			// KeyCode.Mouse0 covers the editor mouse and, via Unity's
			// simulate-mouse-with-touches, the primary touch on device — the same
			// path the toolkit's own drag handling relies on.
			if (!InputProxy.GetKeyDown(KeyCode.Mouse0))
				return;

			var hit = RaycastTopObject(eventSystem, InputProxy.MousePosition);
			if (hit == null)
				return;

			var soundType = ResolveSoundType(hit, out bool interactable, out string trigger);
			if (soundType == EUiSoundType.None)
				return;

			if (!interactable)
			{
				if (m_config != null && m_config.DebugLog)
					UiLog.Log($"{soundType} suppressed — control not interactable (trigger: {trigger})", this, nameof(UiSound));
				return;
			}

			// A per-control override (on the hit or any parent) can mute, redirect, or
			// replace the automatic sound.
			var over = hit.GetComponentInParent<UiSoundOverride>();
			if (over != null)
			{
				switch (over.Mode)
				{
					case UiSoundOverride.EMode.Suppress:
						if (m_config != null && m_config.DebugLog)
							UiLog.Log($"{soundType} suppressed — UiSoundOverride on '{over.name}' (trigger: {trigger})", this, nameof(UiSound));
						return;
					case UiSoundOverride.EMode.Redirect:
						PlayType(over.RedirectType, $"{trigger} → redirected by '{over.name}'");
						return;
					case UiSoundOverride.EMode.Custom:
						PlaySound(over.CustomSound, "Custom", $"{trigger} → custom on '{over.name}'");
						return;
				}
			}

			PlayType(soundType, trigger);
		}

		private GameObject RaycastTopObject( EventSystem _eventSystem, Vector2 _screenPosition )
		{
			var pointerData = new PointerEventData(_eventSystem) { position = _screenPosition };
			m_raycastResults.Clear();
			_eventSystem.RaycastAll(pointerData, m_raycastResults);

			// RaycastAll returns hits front-to-back, so [0] is what actually
			// receives the press (respecting raycast blockers / modal overlays).
			return m_raycastResults.Count > 0 ? m_raycastResults[0].gameObject : null;
		}

		// Classifies the pressed control into a sound type + trigger description, and
		// reports whether it is currently interactable. Order matters: UiToggle is-a
		// UiButtonBase (and carries a UGUI Toggle), so toggles are resolved before the
		// generic button checks. UiButtonBase is NOT a UGUI Selectable, so it is
		// probed separately from Button / Toggle.
		private static EUiSoundType ResolveSoundType( GameObject _hit, out bool _interactable, out string _trigger )
		{
			var uiToggle = _hit.GetComponentInParent<UiToggle>();
			if (uiToggle != null)
			{
				_trigger = nameof(UiToggle) + " found";
				_interactable = IsEnabled(uiToggle);
				return EUiSoundType.Toggle;
			}

			var toggle = _hit.GetComponentInParent<Toggle>();
			if (toggle != null)
			{
				_trigger = nameof(Toggle) + " found";
				_interactable = toggle.IsInteractable();
				return EUiSoundType.Toggle;
			}

			var uiButton = _hit.GetComponentInParent<UiButtonBase>();
			if (uiButton != null)
			{
				_trigger = nameof(UiButtonBase) + " found";
				_interactable = IsEnabled(uiButton);
				return EUiSoundType.Click;
			}

			var button = _hit.GetComponentInParent<Button>();
			if (button != null)
			{
				_trigger = nameof(Button) + " found";
				_interactable = button.IsInteractable();
				return EUiSoundType.Click;
			}

			_trigger = null;
			_interactable = false;
			return EUiSoundType.None;
		}

		private static bool IsEnabled( UiButtonBase _uiButton ) =>
			_uiButton.EnabledInHierarchy && _uiButton.isActiveAndEnabled;

		// Resolves one entry for the type from the config (weighted-random when several
		// share it) and plays it. A single resolution per call keeps clip, volume and
		// pitch consistent — they all come from the same chosen entry.
		private void PlayType( EUiSoundType _type, string _trigger )
		{
			if (m_config == null)
				return;

			var def = m_config.Resolve(_type);
			if (def == null)
			{
				if (m_config.DebugLog)
					UiLog.Log($"{_type} skipped — no entry configured (trigger: {_trigger})", this, nameof(UiSound));
				return;
			}

			PlaySound(def, _type.ToString(), _trigger);
		}

		// Plays a single sound definition, honoring master volume + mute/volume
		// providers. Used for both config entries and custom overrides.
		private void PlaySound( UiSoundConfig.SoundDef _def, string _what, string _trigger )
		{
			bool debug = m_config != null && m_config.DebugLog;

			if (_def == null || _def.Clip == null)
			{
				if (debug)
					UiLog.Log($"{_what} skipped — no clip (trigger: {_trigger})", this, nameof(UiSound));
				return;
			}

			if (MutedProvider != null && MutedProvider())
			{
				if (debug)
					UiLog.Log($"{_what} skipped — muted (trigger: {_trigger}, clip '{_def.Clip.name}')", this, nameof(UiSound));
				return;
			}

			float master = m_config != null ? m_config.MasterVolume : 1f;
			var volume = _def.Volume * master;
			if (VolumeProvider != null)
				volume *= VolumeProvider();

			volume = Mathf.Clamp01(volume);
			if (volume <= 0f)
			{
				if (debug)
					UiLog.Log($"{_what} skipped — volume 0 (trigger: {_trigger}, clip '{_def.Clip.name}')", this, nameof(UiSound));
				return;
			}

			// Pitch is an AudioSource property (PlayOneShot takes no pitch), so set it
			// on the shared source right before playing. Resolved per call so a
			// randomized range yields a fresh value each time.
			var pitch = _def.ResolvePitch();
			m_audioSource.pitch = pitch;

			m_audioSource.PlayOneShot(_def.Clip, volume);

			if (debug)
				UiLog.Log($"{_what} played — clip '{_def.Clip.name}', volume {volume:F2}, pitch {pitch:F2}, t={Time.unscaledTime:F2}s frame {Time.frameCount} (trigger: {_trigger})", this, nameof(UiSound));
		}
	}
}
