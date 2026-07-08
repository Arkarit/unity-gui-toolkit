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
	}

	/// <summary>
	/// A set of UI sound effects (clip + per-sound volume), referenced by
	/// <see cref="UiToolkitConfiguration"/>. Kept as its own asset so each game can
	/// supply its own content — the toolkit ships the playback mechanism
	/// (<see cref="UiSound"/>), not the clips. To add a new sound, add a value to
	/// <see cref="EUiSoundType"/> and an entry in the inspector; no code changes to
	/// this class are required.
	/// </summary>
	[CreateAssetMenu(fileName = nameof(UiSoundConfig), menuName = StringConstants.CREATE_SOUND_CONFIG)]
	public class UiSoundConfig : ScriptableObject
	{
		[Serializable]
		public class Entry
		{
			public EUiSoundType Type = EUiSoundType.None;
			public AudioClip Clip;
			[Range(0f, 1f)] public float Volume = 1f;
		}

		[Tooltip("Master volume [0..1] applied on top of every entry's own volume.")]
		[SerializeField, Range(0f, 1f)] private float m_masterVolume = 1f;

		[Tooltip("One entry per UI sound. The first entry for a given type wins; entries with type None or no clip are ignored.")]
		[SerializeField] private Entry[] m_entries = new Entry[0];

		private Dictionary<EUiSoundType, Entry> m_lookup;

		public float MasterVolume => m_masterVolume;

		/// <summary>The clip mapped to <paramref name="_type"/>, or null if unmapped.</summary>
		public AudioClip GetClip( EUiSoundType _type )
		{
			var entry = GetEntry(_type);
			return entry != null ? entry.Clip : null;
		}

		/// <summary>Effective volume for a type (entry volume × master volume), or 0 if unmapped.</summary>
		public float GetVolume( EUiSoundType _type )
		{
			var entry = GetEntry(_type);
			return entry != null ? Mathf.Clamp01(entry.Volume * m_masterVolume) : 0f;
		}

		private Entry GetEntry( EUiSoundType _type )
		{
			if (_type == EUiSoundType.None)
				return null;

			EnsureLookup();
			return m_lookup.TryGetValue(_type, out var entry) ? entry : null;
		}

		private void EnsureLookup()
		{
			if (m_lookup != null)
				return;

			m_lookup = new Dictionary<EUiSoundType, Entry>();
			foreach (var entry in m_entries)
			{
				if (entry == null || entry.Type == EUiSoundType.None || entry.Clip == null)
					continue;
				if (!m_lookup.ContainsKey(entry.Type))
					m_lookup.Add(entry.Type, entry);
			}
		}

		// Rebuild the lookup after inspector edits / domain reloads.
		private void OnEnable() => m_lookup = null;
		private void OnValidate() => m_lookup = null;
	}
}
