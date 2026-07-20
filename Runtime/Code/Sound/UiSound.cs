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

		// A single playing (or fading-out) sound on its own AudioSource. One sound per
		// voice keeps each individually stoppable/fadeable, which PlayOneShot on a shared
		// source cannot do — required for the interrupt-and-fade rule below.
		private class Voice
		{
			public AudioSource Source;
			public string Identifier;
			public int Priority;

			// While fading out (after being interrupted by a higher-priority sound), the
			// voice no longer counts as "live" for the competition rules.
			public bool FadingOut;
			public float FadeElapsed;
			public float FadeDuration;
			public float FadeFromVolume;
		}

		private readonly List<Voice> m_voices = new List<Voice>();
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

			// Seed the pool with one voice; more are added on demand when sounds overlap.
			AddVoice();

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
			UpdateVoices();

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

			// Priority / Single competition, scoped to sounds sharing a (non-empty)
			// Identifier. Ungrouped sounds (empty Identifier) skip this entirely and always
			// play — the toolkit's classic overlapping behavior.
			string identifier = _def.Identifier;
			if (!string.IsNullOrEmpty(identifier))
			{
				int highestLivePriority = int.MinValue;
				bool equalPriorityLive = false;
				foreach (var voice in m_voices)
				{
					if (!IsLive(voice) || voice.Identifier != identifier)
						continue;
					if (voice.Priority > highestLivePriority)
						highestLivePriority = voice.Priority;
					if (voice.Priority == _def.Priority)
						equalPriorityLive = true;
				}

				// Rule: a higher-priority sound of this Identifier is playing → skip this one.
				if (highestLivePriority > _def.Priority)
				{
					if (debug)
						UiLog.Log($"{_what} skipped — lower priority ({_def.Priority}) than a running '{identifier}' sound ({highestLivePriority}) (trigger: {_trigger})", this, nameof(UiSound));
					return;
				}

				// Rule: Single skips this sound when an equal-priority one of this Identifier is playing.
				if (_def.Single && equalPriorityLive)
				{
					if (debug)
						UiLog.Log($"{_what} skipped — Single, equal-priority '{identifier}' sound already playing (trigger: {_trigger})", this, nameof(UiSound));
					return;
				}

				// Rule: interrupt (fade out) any running lower-priority sound of this Identifier.
				foreach (var voice in m_voices)
				{
					if (IsLive(voice) && voice.Identifier == identifier && voice.Priority < _def.Priority)
						StartFadeOut(voice);
				}
			}

			// Pitch is an AudioSource property, resolved per call so a randomized range
			// yields a fresh value each time.
			var pitch = _def.ResolvePitch();

			var target = GetFreeVoice();
			target.Identifier = identifier;
			target.Priority = _def.Priority;
			target.FadingOut = false;

			var source = target.Source;
			source.clip = _def.Clip;
			source.loop = false;
			source.pitch = pitch;
			source.volume = volume;
			source.Play();

			// Prominent UI sounds may duck the background music for their duration (no-op
			// unless the def opts in via DuckMusic). Same entry point client SFX use.
			UiMusic.Duck(_def, pitch);

			if (debug)
				UiLog.Log($"{_what} played — clip '{_def.Clip.name}', volume {volume:F2}, pitch {pitch:F2}, id '{identifier}' prio {_def.Priority}, t={Time.unscaledTime:F2}s frame {Time.frameCount} (trigger: {_trigger})", this, nameof(UiSound));
		}

		// A voice is "live" (counts for the competition rules) while it is audibly playing
		// and not already fading out after being interrupted.
		private static bool IsLive( Voice _voice ) =>
			_voice.Source != null && _voice.Source.isPlaying && !_voice.FadingOut;

		// Returns an idle voice (finished and not fading), or grows the pool by one.
		private Voice GetFreeVoice()
		{
			foreach (var voice in m_voices)
			{
				if (!voice.FadingOut && (voice.Source == null || !voice.Source.isPlaying))
					return voice;
			}
			return AddVoice();
		}

		private Voice AddVoice()
		{
			var source = gameObject.AddComponent<AudioSource>();
			source.playOnAwake = false;
			var voice = new Voice { Source = source };
			m_voices.Add(voice);
			return voice;
		}

		// Begins a quick volume fade-out on a voice; when it reaches zero the source is
		// stopped and the voice is freed for reuse. A zero (or negative) fade stops at once.
		private void StartFadeOut( Voice _voice )
		{
			float fade = m_config != null ? m_config.InterruptFade : 0f;
			if (fade <= 0f)
			{
				if (_voice.Source != null)
					_voice.Source.Stop();
				_voice.FadingOut = false;
				return;
			}

			_voice.FadingOut = true;
			_voice.FadeElapsed = 0f;
			_voice.FadeDuration = fade;
			_voice.FadeFromVolume = _voice.Source != null ? _voice.Source.volume : 0f;
		}

		// Advances active fade-outs on unscaled time (so a paused game still fades), and
		// stops each voice once faded so it becomes reusable.
		private void UpdateVoices()
		{
			foreach (var voice in m_voices)
			{
				if (!voice.FadingOut)
					continue;

				voice.FadeElapsed += Time.unscaledDeltaTime;
				float t = voice.FadeDuration > 0f ? Mathf.Clamp01(voice.FadeElapsed / voice.FadeDuration) : 1f;

				if (voice.Source != null)
					voice.Source.volume = Mathf.Lerp(voice.FadeFromVolume, 0f, t);

				if (t >= 1f)
				{
					if (voice.Source != null)
						voice.Source.Stop();
					voice.FadingOut = false;
				}
			}
		}
	}
}
