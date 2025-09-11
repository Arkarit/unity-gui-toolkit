using System;
using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit.AssetHandling
{
	[CreateAssetMenu(fileName = nameof(UiPanelConfig), menuName = StringConstants.UI_PANEL_CONFIG)]
	public class UiPanelConfig : ScriptableObject
	{
		[Serializable]
		public class CategoryEntry
		{
			public string Category;
			public List<CanonicalAssetRef> PanelEntries = new();
		}

		[SerializeField] private List<CategoryEntry> m_categories = new();

		private Dictionary<string, CanonicalAssetRef> m_panelEntryByTypeName;

		public List<CategoryEntry> Categories => m_categories;

		public bool TryGetAssetKeyByType( Type _type, out CanonicalAssetKey _assetKey )
		{
			_assetKey = default;
			if (_type == null)
				return false;

			InitIfNecessary();

			if (!m_panelEntryByTypeName.TryGetValue(_type.Name, out var entry))
				return false;

			if (string.IsNullOrEmpty(entry.PanelId))
				return false;

			var provider = AssetManager.GetAssetProviderOrThrow(entry.PanelId);

			// Panels are GameObjects
			_assetKey = new CanonicalAssetKey(provider, entry.PanelId, typeof(GameObject));
			return true;
		}

		public CanonicalAssetKey GetAssetKeyByType( Type _type )
		{
			if (TryGetAssetKeyByType(_type, out var key))
				return key;

			throw new InvalidOperationException($"Panel type '{_type?.Name}' not found or unmapped in UiPanelConfig '{name}'.");
		}

		private void InitIfNecessary()
		{
			if (m_panelEntryByTypeName != null)
				return;

			m_panelEntryByTypeName = new Dictionary<string, CanonicalAssetRef>(StringComparer.Ordinal);
			foreach (var category in m_categories)
			{
				foreach (var entry in category.PanelEntries)
				{
					if (!string.IsNullOrEmpty(entry.Type) && !m_panelEntryByTypeName.ContainsKey(entry.Type))
						m_panelEntryByTypeName.Add(entry.Type, entry);
				}
			}
		}
	}
}
