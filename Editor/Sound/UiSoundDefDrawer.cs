using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	/// <summary>
	/// Inspector for a <see cref="UiSoundConfig.SoundDef"/> and its subclasses (i.e.
	/// <see cref="UiSoundConfig.Entry"/> and custom override sounds). Draws the
	/// Entry-only Type/Weight fields only when present, shows only the applicable pitch
	/// controls (fixed <c>Pitch</c> vs. <c>PitchMin</c>/<c>PitchMax</c>, switched by
	/// <c>RandomPitch</c>), and adds a Play button that previews the clip in the editor
	/// with the resolved volume (× master) and pitch — the same values
	/// <see cref="UiSound"/> would use at runtime.
	/// </summary>
	[CustomPropertyDrawer(typeof(UiSoundConfig.SoundDef), true)]
	[EditorAware]
	public class UiSoundDefDrawer : AbstractPropertyDrawer<UiSoundConfig.SoundDef>
	{
		private const float Gap = 2f;

		protected override void OnInspectorGUI()
		{
			// Entry-only fields — absent for a plain SoundDef (e.g. a custom override sound).
			var typeProp   = Property.FindPropertyRelative(nameof(UiSoundConfig.Entry.Type));
			var weightProp = Property.FindPropertyRelative(nameof(UiSoundConfig.Entry.Weight));

			var clipProp     = Property.FindPropertyRelative(nameof(UiSoundConfig.SoundDef.Clip));
			var volumeProp   = Property.FindPropertyRelative(nameof(UiSoundConfig.SoundDef.Volume));
			var randomProp   = Property.FindPropertyRelative(nameof(UiSoundConfig.SoundDef.RandomPitch));
			var pitchProp    = Property.FindPropertyRelative(nameof(UiSoundConfig.SoundDef.Pitch));
			var pitchMinProp = Property.FindPropertyRelative(nameof(UiSoundConfig.SoundDef.PitchMin));
			var pitchMaxProp = Property.FindPropertyRelative(nameof(UiSoundConfig.SoundDef.PitchMax));

			var duckProp        = Property.FindPropertyRelative(nameof(UiSoundConfig.SoundDef.DuckMusic));
			var duckVolumeProp  = Property.FindPropertyRelative(nameof(UiSoundConfig.SoundDef.DuckVolume));
			var duckAttackProp  = Property.FindPropertyRelative(nameof(UiSoundConfig.SoundDef.DuckAttack));
			var duckReleaseProp = Property.FindPropertyRelative(nameof(UiSoundConfig.SoundDef.DuckRelease));

			var identifierProp = Property.FindPropertyRelative(nameof(UiSoundConfig.SoundDef.Identifier));
			var priorityProp   = Property.FindPropertyRelative(nameof(UiSoundConfig.SoundDef.Priority));
			var singleProp     = Property.FindPropertyRelative(nameof(UiSoundConfig.SoundDef.Single));

			if (typeProp != null)
				PropertyField(typeProp);
			if (weightProp != null)
				PropertyField(weightProp);

			PropertyField(clipProp);
			PropertyField(volumeProp);
			PropertyField(randomProp);

			// Show only the relevant pitch controls (absolute XOR random range).
			if (randomProp.boolValue)
			{
				PropertyField(pitchMinProp);
				PropertyField(pitchMaxProp);
			}
			else
			{
				PropertyField(pitchProp);
			}

			// Music ducking — show the amount/timing only when enabled.
			PropertyField(duckProp);
			if (duckProp.boolValue)
			{
				PropertyField(duckVolumeProp);
				PropertyField(duckAttackProp);
				PropertyField(duckReleaseProp);
			}

			// Channel grouping — Priority/Single only mean something with an Identifier.
			PropertyField(identifierProp);
			if (!string.IsNullOrEmpty(identifierProp.stringValue))
			{
				PropertyField(priorityProp);
				PropertyField(singleProp);
			}

			Space(Gap);

			bool hasClip = clipProp.objectReferenceValue != null;
			using (new EditorGUI.DisabledScope(!hasClip))
			{
				if (Button(hasClip ? "▶ Play" : "▶ Play (no clip)"))
					PlayPreview();
			}
		}

		// Faithful preview: reuse the runtime sound def (volume × master, resolved pitch)
		// so a randomized range is re-rolled on every press, exactly as in game.
		private void PlayPreview()
		{
			var def = Property.boxedValue as UiSoundConfig.SoundDef;
			if (def == null || def.Clip == null)
				return;

			var config = Property.serializedObject.targetObject as UiSoundConfig;
			float master = config != null ? config.MasterVolume : 1f;

			SoundPreview.Play(def.Clip, Mathf.Clamp01(def.Volume * master), def.ResolvePitch());
		}

		/// <summary>
		/// Editor-only audio preview through a single reusable hidden AudioSource. The
		/// object is actively destroyed before every domain reload / play-mode change (see
		/// RegisterCleanup), so it never accumulates and never leaks into play mode.
		///
		/// The editor plays through a real AudioSource, which is only audible when an
		/// AudioListener is active in the open scene. Scenes edited without a live
		/// listener (e.g. the UI, and thus its listener, is only instantiated at runtime)
		/// would be silent — so the preview object carries its own listener and enables it
		/// only when the scene has none, avoiding the "more than one audio listener"
		/// warning when one is already present.
		/// </summary>
		private static class SoundPreview
		{
			private const string PreviewObjectName = "UiSoundPreview";

			private static AudioSource s_source;
			private static AudioListener s_listener;

			// HideAndDontSave objects survive domain reloads (our static refs do not), so
			// without active cleanup the preview would leak a new AudioListener on every
			// reload — the cause of the "N audio listeners" warning. Destroy every preview
			// object now (clearing leaks from earlier sessions) and before each reload /
			// play-mode change, so at most one ever exists and none survive into play mode.
			[InitializeOnLoadMethod]
			private static void RegisterCleanup()
			{
				DestroyPreviewObjects();
				AssemblyReloadEvents.beforeAssemblyReload -= DestroyPreviewObjects;
				AssemblyReloadEvents.beforeAssemblyReload += DestroyPreviewObjects;
				EditorApplication.playModeStateChanged -= OnPlayModeChanged;
				EditorApplication.playModeStateChanged += OnPlayModeChanged;
			}

			private static void OnPlayModeChanged( PlayModeStateChange _change ) => DestroyPreviewObjects();

			private static void DestroyPreviewObjects()
			{
				foreach (var source in Resources.FindObjectsOfTypeAll<AudioSource>())
				{
					if (source.gameObject.name == PreviewObjectName && !EditorUtility.IsPersistent(source))
						Object.DestroyImmediate(source.gameObject);
				}
				s_source = null;
				s_listener = null;
			}

			public static void Play( AudioClip _clip, float _volume, float _pitch )
			{
				if (_clip == null)
					return;

				// Unity's editor audio (Game view "Mute Audio") tends to mute itself after
				// the editor loses audio focus, silently killing the preview. Force it off at
				// the moment of playback so Play always sounds, even mid-session.
				bool muteBefore = EditorUtility.audioMasterMute;
				EditorUtility.audioMasterMute = false;

				EnsureObjects();
				s_listener.enabled = !HasOtherActiveListener();
				s_source.Stop();
				s_source.pitch = _pitch;
				s_source.PlayOneShot(_clip, _volume);

				if (IsDebugLogEnabled())
				{
					Debug.Log(
						$"[UiSoundPreview] Play '{_clip.name}'\n" +
						$"  requested: volume={_volume:F2}, pitch={_pitch:F2}\n" +
						$"  EditorUtility.audioMasterMute: {muteBefore} -> {EditorUtility.audioMasterMute}\n" +
						$"  AudioListener.volume={AudioListener.volume:F2}, AudioListener.pause={AudioListener.pause}\n" +
						$"  preview AudioSource: enabled={s_source.enabled}, mute={s_source.mute}, volume={s_source.volume:F2}, pitch={s_source.pitch:F2}, isPlaying={s_source.isPlaying}\n" +
						$"  preview AudioListener: enabled={s_listener.enabled}, activeAndEnabled={s_listener.isActiveAndEnabled}\n" +
						$"  active AudioListeners in scene: {CountActiveListeners()}");
				}
			}

			// Gate diagnostics on the project sound config's Debug flag.
			private static bool IsDebugLogEnabled()
			{
				var config = UiToolkitConfiguration.Instance;
				return config != null && config.UiSoundConfig != null && config.UiSoundConfig.DebugLog;
			}

			private static int CountActiveListeners()
			{
				int count = 0;
				foreach (var listener in Resources.FindObjectsOfTypeAll<AudioListener>())
				{
					if (EditorUtility.IsPersistent(listener))
						continue;
					if (listener.isActiveAndEnabled)
						count++;
				}
				return count;
			}

			private static bool HasOtherActiveListener()
			{
				// FindObjectsOfTypeAll (unlike FindObjectsByType) also sees hidden / no-scene
				// listeners, so the preview correctly defers to any real listener present.
				foreach (var listener in Resources.FindObjectsOfTypeAll<AudioListener>())
				{
					if (listener == s_listener || EditorUtility.IsPersistent(listener))
						continue;
					if (listener.isActiveAndEnabled)
						return true;
				}
				return false;
			}

			private static void EnsureObjects()
			{
				if (s_source != null)
					return;

				var go = new GameObject(PreviewObjectName) { hideFlags = HideFlags.HideAndDontSave };
				s_source = go.AddComponent<AudioSource>();
				s_source.playOnAwake = false;
				s_listener = go.AddComponent<AudioListener>();
				s_listener.enabled = false;
			}
		}
	}
}
