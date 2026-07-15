using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	/// <summary>
	/// Opt-in editor helper that keeps the editor's audio alive while working on sounds.
	/// Toggle it via the menu (<see cref="StringConstants.EDITOR_AUDIO_RECOVERY_MENU_NAME"/>);
	/// it is OFF by default and the choice is persisted per project in EditorPrefs.
	///
	/// When enabled it handles two failure modes seen after the editor loses audio focus:
	/// <list type="number">
	/// <item>Editor audio ends up muted (<see cref="EditorUtility.audioMasterMute"/>) —
	/// un-muted on focus regain.</item>
	/// <item>The audio output device is lost when another application holds it for a while
	/// (or the default device changes): every volume/mute value still looks fine, yet the
	/// engine stays silent. The audio system is reset to re-acquire the device — both on
	/// the device-changed callback and, as a fallback, on focus regain (edit mode only,
	/// since a reset during play mode would restart all playing sounds).</item>
	/// </list>
	///
	/// Why opt-in: un-muting on focus regain fights anyone who deliberately keeps the Game
	/// view's "Mute Audio" on — their mute would silently clear itself after every task
	/// switch. So unless you are actually working on sounds, leave it off (the default).
	///
	/// Diagnostic logging is gated by the project sound config's Debug flag.
	/// </summary>
	[InitializeOnLoad]
	[EditorAware]
	internal static class EditorAudioRecovery
	{
		private static bool s_enabled = false;

		private static readonly string PrefsKey =
			UnityEditor.PlayerSettings.productName + "." + nameof(EditorAudioRecovery) + ".active";

		static EditorAudioRecovery()
		{
			// Only EditorPrefs is touched at load; the audio/config side effects happen
			// solely while enabled, in the event handlers.
			IsEnabled = EditorPrefs.GetBool(PrefsKey);
		}

		[MenuItem(StringConstants.EDITOR_AUDIO_RECOVERY_MENU_NAME, priority = Constants.EDITOR_AUDIO_RECOVERY_MENU_PRIORITY)]
		private static void Toggle()
		{
			IsEnabled = !IsEnabled;
		}

		private static bool IsEnabled
		{
			get => s_enabled;
			set
			{
				if (value == s_enabled)
					return;

				s_enabled = value;

				Menu.SetChecked(StringConstants.EDITOR_AUDIO_RECOVERY_MENU_NAME, s_enabled);

				if (s_enabled)
				{
					EditorApplication.focusChanged += OnFocusChanged;
					AudioSettings.OnAudioConfigurationChanged += OnAudioConfigurationChanged;
				}
				else
				{
					EditorApplication.focusChanged -= OnFocusChanged;
					AudioSettings.OnAudioConfigurationChanged -= OnAudioConfigurationChanged;
				}

				EditorPrefs.SetBool(PrefsKey, s_enabled);
			}
		}

		private static void OnFocusChanged( bool _focused )
		{
			if (!_focused)
				return;

			bool muteBefore = EditorUtility.audioMasterMute;
			EditorUtility.audioMasterMute = false;

			// Re-acquire the output device in case another app grabbed it while we were away.
			// Edit mode only — a reset during play mode would restart every playing sound.
			bool reset = !EditorApplication.isPlaying;
			if (reset)
				AudioSettings.Reset(AudioSettings.GetConfiguration());

			if (IsDebugLogEnabled())
				Debug.Log($"[EditorAudioRecovery] focus regained: audioMasterMute {muteBefore} -> {EditorUtility.audioMasterMute}, audioReset={reset}, AudioListener.volume={AudioListener.volume:F2}, pause={AudioListener.pause}");
		}

		private static void OnAudioConfigurationChanged( bool _deviceWasChanged )
		{
			if (IsDebugLogEnabled())
				Debug.Log($"[EditorAudioRecovery] OnAudioConfigurationChanged(deviceWasChanged={_deviceWasChanged})");

			// Guard against recursion: Reset() itself raises this callback with false.
			if (_deviceWasChanged)
				AudioSettings.Reset(AudioSettings.GetConfiguration());
		}

		// Gate diagnostics on the project sound config's Debug flag.
		private static bool IsDebugLogEnabled()
		{
			var config = UiToolkitConfiguration.Instance;
			return config != null && config.UiSoundConfig != null && config.UiSoundConfig.DebugLog;
		}
	}
}
