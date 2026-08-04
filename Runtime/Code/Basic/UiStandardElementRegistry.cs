using System;
using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit
{
	/// <summary>
	/// GENERATED runtime lookup from an <see cref="EStandardElement"/> (or a Custom id) to the winning
	/// prefab, resolved with client prefabs/variants out-ranking the toolkit defaults. It is (re)built by
	/// the editor-side catalog generator, which scans <see cref="UiStandardElement"/> markers across the
	/// project.
	///
	/// DO NOT edit by hand — the inspector is read-only and any manual change is overwritten on the next
	/// generate (<c>Gui Toolkit → AI → Generate Screen Catalog</c>). This asset is the runtime face of the
	/// same source of truth the JSON catalog exposes to the authoring AI.
	/// </summary>
	public class UiStandardElementRegistry : ScriptableObject
	{
		[Serializable]
		public class Entry
		{
			public EStandardElement element = EStandardElement.None;

			[Tooltip("Identity string when element == Custom.")]
			public string customId = "";

			public GameObject prefab;

			[Tooltip("True when the winning prefab is a toolkit/library asset (no client override exists).")]
			public bool fromLibrary;

			[Tooltip("True for an internal sub-part (resolvable, but hidden from the screen-authoring vocabulary).")]
			public bool isInternal;

			/// <summary>The lookup key: the enum name, or the custom id when element == Custom.</summary>
			public string Key => element == EStandardElement.Custom ? customId : element.ToString();
		}

		[ReadOnly]
		[SerializeField] private List<Entry> m_entries = new();

		[NonSerialized] private Dictionary<string, GameObject> m_byKey;

		public IReadOnlyList<Entry> Entries => m_entries;

		/// <summary>Resolves a built-in standard element to its winning prefab, or null if none is registered.</summary>
		public GameObject Resolve( EStandardElement _element )
		{
			if (_element == EStandardElement.None || _element == EStandardElement.Custom)
				return null;
			return Resolve(_element.ToString());
		}

		/// <summary>Resolves any standard element (built-in enum name or Custom id) to its prefab, or null.</summary>
		public GameObject Resolve( string _key )
		{
			if (string.IsNullOrEmpty(_key))
				return null;
			EnsureCache();
			return m_byKey.TryGetValue(_key, out var prefab) ? prefab : null;
		}

		/// <summary>Convenience: resolves and returns the requested component on the winning prefab root.</summary>
		public T Resolve<T>( EStandardElement _element ) where T : Component
		{
			var go = Resolve(_element);
			return go != null ? go.GetComponent<T>() : null;
		}

		private void EnsureCache()
		{
			if (m_byKey != null)
				return;

			m_byKey = new Dictionary<string, GameObject>(StringComparer.Ordinal);
			foreach (var entry in m_entries)
			{
				if (entry == null || entry.prefab == null)
					continue;
				string key = entry.Key;
				if (!string.IsNullOrEmpty(key))
					m_byKey[key] = entry.prefab;
			}
		}

#if UNITY_EDITOR
		/// <summary>Editor-only: replaces the whole entry set (used by the generator). Invalidates the cache.</summary>
		public void EditorSetEntries( List<Entry> _entries )
		{
			m_entries = _entries ?? new List<Entry>();
			m_byKey = null;
		}
#endif
	}
}
