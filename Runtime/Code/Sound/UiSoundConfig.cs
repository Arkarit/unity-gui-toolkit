using System;
using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit
{
	/// <summary>
	/// Identifies a UI sound effect. Extend as the toolkit grows (hover, toggle,
	/// open, close, ...). Keep <see cref="None"/> at 0 so unset entries are inert.
	/// </summary>
	public enum EUiSoundType
	{
		None = 0,
		Click,
		Toggle,
		PanelOpen,
		PanelClose,
	}

	/// <summary>
	/// A set of UI sound effects, referenced by <see cref="UiToolkitConfiguration"/>.
	/// Kept as its own asset so each game can supply its own content — the toolkit
	/// ships the playback mechanism (<see cref="UiSound"/>), not the clips.
	///
	/// To add a new sound, add a value to <see cref="EUiSoundType"/> and an entry in
	/// the inspector; no code changes to this class are required. Several entries may
	/// share the same <see cref="Entry.Type"/> — one is then chosen per play, weighted
	/// by <see cref="Entry.Weight"/>, to avoid repetitive "machine-gun" playback.
	/// </summary>
	[CreateAssetMenu(fileName = nameof(UiSoundConfig), menuName = StringConstants.CREATE_SOUND_CONFIG)]
	public class UiSoundConfig : ScriptableObject
	{
		/// <summary>
		/// A playable sound definition: a clip with its own volume and pitch (fixed or
		/// randomized per play). Shared between the config table (see <see cref="Entry"/>)
		/// and per-control overrides (<see cref="UiSoundOverride"/>) so both use the same
		/// inspector and the same runtime pitch logic.
		/// </summary>
		[Serializable]
		public class SoundDef
		{
			public AudioClip Clip;
			[Range(0f, 1f)] public float Volume = 1f;

			[Tooltip("When enabled, the pitch is randomized in [PitchMin..PitchMax] on every play; otherwise the fixed Pitch is used. (Pitch is Unity's AudioSource pitch: 1 = original, <1 lower/slower, >1 higher/faster.)")]
			public bool RandomPitch = false;

			[Tooltip("Fixed playback pitch (used when RandomPitch is off). 1 = original pitch.")]
			[Range(-3f, 3f)] public float Pitch = 1f;

			[Tooltip("Lower bound of the randomized pitch (used when RandomPitch is on).")]
			[Range(-3f, 3f)] public float PitchMin = 0.95f;

			[Tooltip("Upper bound of the randomized pitch (used when RandomPitch is on).")]
			[Range(-3f, 3f)] public float PitchMax = 1.05f;

			/// <summary>Resolves the pitch for one play: a fresh random value in range, or the fixed pitch.</summary>
			public float ResolvePitch() =>
				RandomPitch ? UnityEngine.Random.Range(Mathf.Min(PitchMin, PitchMax), Mathf.Max(PitchMin, PitchMax)) : Pitch;
		}

		/// <summary>
		/// A <see cref="SoundDef"/> tagged with a type (and weight) for the config table.
		/// Several entries may share a <see cref="Type"/>; one is picked per play,
		/// weighted by <see cref="Weight"/>.
		/// </summary>
		[Serializable]
		public class Entry : SoundDef
		{
			public EUiSoundType Type = EUiSoundType.None;

			[Tooltip("Relative likelihood of being chosen when several entries share the same Type. Ignored for a lone entry; must be > 0 to be pickable.")]
			[Min(0f)] public float Weight = 1f;
		}

		[Tooltip("Master volume [0..1] applied on top of every entry's own volume.")]
		[SerializeField, Range(0f, 1f)] private float m_masterVolume = 1f;

		[Tooltip("One or more entries per UI sound. Entries sharing a Type are chosen at random (weighted by Weight). Entries with type None or no clip are ignored.")]
		[SerializeField] private Entry[] m_entries = new Entry[0];

		[Tooltip("When enabled, UiSound logs every decision to the console: which sound played, how loud, when, what triggered it, and why a sound was skipped.")]
		[SerializeField] private bool m_debugLog = false;

		private Dictionary<EUiSoundType, List<Entry>> m_lookup;

		public float MasterVolume => m_masterVolume;
		public bool DebugLog => m_debugLog;

		/// <summary>
		/// Picks one entry for <paramref name="_type"/> — the single mapped entry, or a
		/// weighted-random one when several share the type — or null if unmapped.
		/// </summary>
		public Entry Resolve( EUiSoundType _type )
		{
			if (_type == EUiSoundType.None)
				return null;

			EnsureLookup();
			if (!m_lookup.TryGetValue(_type, out var list) || list.Count == 0)
				return null;

			return list.Count == 1 ? list[0] : PickWeighted(list);
		}

		private static Entry PickWeighted( List<Entry> _list )
		{
			float total = 0f;
			foreach (var entry in _list)
				total += Mathf.Max(0f, entry.Weight);

			// All weights non-positive → fall back to a uniform pick.
			if (total <= 0f)
				return _list[UnityEngine.Random.Range(0, _list.Count)];

			float r = UnityEngine.Random.Range(0f, total);
			Entry last = null;
			foreach (var entry in _list)
			{
				float weight = Mathf.Max(0f, entry.Weight);
				if (weight <= 0f)
					continue;

				last = entry;
				r -= weight;
				if (r < 0f)
					return entry;
			}

			// Float rounding safety net: return the last positive-weight entry.
			return last;
		}

		private void EnsureLookup()
		{
			if (m_lookup != null)
				return;

			m_lookup = new Dictionary<EUiSoundType, List<Entry>>();
			foreach (var entry in m_entries)
			{
				if (entry == null || entry.Type == EUiSoundType.None || entry.Clip == null)
					continue;

				if (!m_lookup.TryGetValue(entry.Type, out var list))
				{
					list = new List<Entry>();
					m_lookup.Add(entry.Type, list);
				}
				list.Add(entry);
			}
		}

		// Rebuild the lookup after inspector edits / domain reloads.
		private void OnEnable() => m_lookup = null;
		private void OnValidate() => m_lookup = null;
	}
}
