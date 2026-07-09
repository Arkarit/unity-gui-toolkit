using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	/// <summary>
	/// Inspector for a single <see cref="UiSoundConfig.Entry"/>. Shows only the pitch
	/// fields that apply (fixed <c>Pitch</c> vs. <c>PitchMin</c>/<c>PitchMax</c>,
	/// switched by <c>RandomPitch</c>) and adds a Play button that previews the clip in
	/// the editor with the entry's volume (× master volume) and resolved pitch — the
	/// same values <see cref="UiSound"/> would use at runtime.
	/// </summary>
	[CustomPropertyDrawer(typeof(UiSoundConfig.Entry))]
	public class UiSoundConfigEntryDrawer : AbstractPropertyDrawer<UiSoundConfig.Entry>
	{
		private const float Gap = 2f;

		protected override void OnInspectorGUI()
		{
			var typeProp     = Property.FindPropertyRelative(nameof(UiSoundConfig.Entry.Type));
			var clipProp     = Property.FindPropertyRelative(nameof(UiSoundConfig.Entry.Clip));
			var volumeProp   = Property.FindPropertyRelative(nameof(UiSoundConfig.Entry.Volume));
			var randomProp   = Property.FindPropertyRelative(nameof(UiSoundConfig.Entry.RandomPitch));
			var pitchProp    = Property.FindPropertyRelative(nameof(UiSoundConfig.Entry.Pitch));
			var pitchMinProp = Property.FindPropertyRelative(nameof(UiSoundConfig.Entry.PitchMin));
			var pitchMaxProp = Property.FindPropertyRelative(nameof(UiSoundConfig.Entry.PitchMax));

			PropertyField(typeProp);
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

			Space(Gap);

			bool hasClip = clipProp.objectReferenceValue != null;
			using (new EditorGUI.DisabledScope(!hasClip))
			{
				if (Button(hasClip ? "▶ Play" : "▶ Play (no clip)"))
					PlayPreview();
			}
		}

		// Faithful preview: reuse the runtime entry (volume × master, resolved pitch) so a
		// randomized range is re-rolled on every press, exactly as it would be in game.
		private void PlayPreview()
		{
			var entry = Property.boxedValue as UiSoundConfig.Entry;
			if (entry == null || entry.Clip == null)
				return;

			var config = Property.serializedObject.targetObject as UiSoundConfig;
			float master = config != null ? config.MasterVolume : 1f;

			SoundPreview.Play(entry.Clip, Mathf.Clamp01(entry.Volume * master), entry.ResolvePitch());
		}

		/// <summary>
		/// Editor-only audio preview through a single reusable hidden AudioSource. Kept
		/// as one persistent source (not per-play temporaries) so there is nothing to
		/// clean up; it is discarded automatically on domain reload.
		/// </summary>
		private static class SoundPreview
		{
			private static AudioSource s_source;

			public static void Play( AudioClip _clip, float _volume, float _pitch )
			{
				if (_clip == null)
					return;

				EnsureSource();
				s_source.Stop();
				s_source.pitch = _pitch;
				s_source.PlayOneShot(_clip, _volume);
			}

			private static void EnsureSource()
			{
				if (s_source != null)
					return;

				var go = new GameObject("UiSoundPreview") { hideFlags = HideFlags.HideAndDontSave };
				s_source = go.AddComponent<AudioSource>();
				s_source.playOnAwake = false;
			}
		}
	}
}
