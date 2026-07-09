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
