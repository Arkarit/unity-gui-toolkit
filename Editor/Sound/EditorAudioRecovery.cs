using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	/// <summary>
	/// Keeps the Unity editor's audio alive for sound previews. Handles two failure modes
	/// seen after the editor loses audio focus:
	/// <list type="number">
	/// <item>Editor audio ends up muted (<see cref="EditorUtility.audioMasterMute"/>) —
	/// un-muted on focus regain.</item>
	/// <item>The audio output device is lost when another application holds it for a while
	/// (or the default device changes): every volume/mute value still looks fine, yet the
	/// engine stays silent. The audio system is reset to re-acquire the device — both on
	/// the device-changed callback and, as a fallback, on focus regain (edit mode only,
	/// since a reset during play mode would restart all playing sounds).</item>
	/// </list>
	/// Diagnostic logging is gated by the project sound config's Debug flag.
	/// </summary>
	[InitializeOnLoad]
	[EditorAware]
	internal static class EditorAudioRecovery
	{
		static EditorAudioRecovery()
		{
			AssetReadyGate.WhenReady(Init);
		}

		private static void Init()
		{
			EditorApplication.focusChanged -= OnFocusChanged;
			EditorApplication.focusChanged += OnFocusChanged;
			AudioSettings.OnAudioConfigurationChanged -= OnAudioConfigurationChanged;
			AudioSettings.OnAudioConfigurationChanged += OnAudioConfigurationChanged;
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
