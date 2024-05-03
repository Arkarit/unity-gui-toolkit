using System;
using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit.Style
{
	[Serializable]
	public class UiSkin
	{
		[SerializeField] private string m_name;
		[SerializeReference] private List<UiAbstractStyleBase> m_styles = new();

		private readonly Dictionary<int, UiAbstractStyleBase> m_styleByKey = new ();
		private bool m_dictionaryBuilt = false;

		public string Name => m_name;
		public List<UiAbstractStyleBase> Styles => m_styles;

		public UiAbstractStyleBase StyleByKey(int _key)
		{
			BuildDictionariesIfNecessary();

			if (m_styleByKey.TryGetValue(_key, out UiAbstractStyleBase result))
			{
				return result;
			}

			return null;
		}

		private void BuildDictionariesIfNecessary()
		{
			if (Application.isPlaying && m_dictionaryBuilt)
				return;

			m_styleByKey.Clear();
			foreach (var style in m_styles)
			{
				m_styleByKey.Add(style.Key, style);
			}

			m_dictionaryBuilt = true;
		}
	}
}