using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GuiToolkit
{
	/// <summary>
	/// Global UI sound. A single, self-bootstrapping listener that plays the
	/// configured UI sounds. Currently it plays the <see cref="EUiSoundType.Click"/>
	/// sound whenever the user presses any button — both GuiToolkit
	/// <see cref="UiButtonBase"/>s and plain UGUI <see cref="Button"/>s — with zero
	/// per-button setup. Further sound types (hover, ...) can be added to
	/// <see cref="EUiSoundType"/> / <see cref="UiSoundConfig"/> and triggered here.
	///
	/// Detection: on pointer-down we raycast the UI at the pointer position via the
	/// current <see cref="EventSystem"/> and, if the frontmost hit resolves to an
	/// interactable button, play the click sound. We fire on pointer-DOWN because
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
				s_instance.PlayInternal(_type);
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
			if (hit != null && IsInteractableButton(hit))
				PlayInternal(EUiSoundType.Click);
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

		// Recognizes both button families and honors their respective enabled /
		// interactable state so disabled buttons stay silent. UiButtonBase is NOT a
		// UGUI Selectable, so it has to be probed separately.
		private static bool IsInteractableButton( GameObject _hit )
		{
			var uiButton = _hit.GetComponentInParent<UiButtonBase>();
			if (uiButton != null)
				return uiButton.EnabledInHierarchy && uiButton.isActiveAndEnabled;

			var button = _hit.GetComponentInParent<Button>();
			if (button != null)
				return button.IsInteractable();

			return false;
		}

		private void PlayInternal( EUiSoundType _type )
		{
			if (m_config == null)
				return;

			var clip = m_config.GetClip(_type);
			if (clip == null)
				return;

			if (MutedProvider != null && MutedProvider())
				return;

			var volume = m_config.GetVolume(_type);
			if (VolumeProvider != null)
				volume *= VolumeProvider();

			volume = Mathf.Clamp01(volume);
			if (volume <= 0f)
				return;

			m_audioSource.PlayOneShot(clip, volume);
		}
	}
}
