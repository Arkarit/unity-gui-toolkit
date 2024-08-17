using System;
using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit.Style
{
	[Serializable]
	public class UiSkin
	{
		[SerializeField] private string m_name;
		[NonReorderable][SerializeReference] private List<UiAbstractStyleBase> m_styles = new();

		private readonly Dictionary<int, UiAbstractStyleBase> m_styleByKey = new ();

		public UiSkin() {}
		public UiSkin(string _name) => m_name = _name;

		public string Name => m_name;
		public List<UiAbstractStyleBase> Styles => m_styles;

		public UiSkin Clone()
		{
			List<UiAbstractStyleBase> clonedStyles = new List<UiAbstractStyleBase>();
			foreach (var style in m_styles)
				clonedStyles.Add(style.Clone());

			var result = new UiSkin()
			{
				m_name = m_name + "(Clone)",
				m_styles = clonedStyles,
			};

			result.Init();
			return result;
		}

		public void Init()
		{
			foreach (var style in m_styles)
				style.Init();

			BuildDictionaries();
		}

		public UiAbstractStyleBase StyleByKey(int _key)
		{
#if UNITY_EDITOR
			if (!Application.isPlaying)
				BuildDictionaries();
#endif

			if (m_styleByKey.TryGetValue(_key, out UiAbstractStyleBase result))
			{
				return result;
			}

			return null;
		}

		public void DeleteStyle(UiAbstractStyleBase _style)
		{
			for (int i = 0; i < m_styles.Count; i++)
			{
				if (m_styles[i].Key == _style.Key)
				{
					m_styles.RemoveAt(i);
					break;
				}
			}

			BuildDictionaries();
		}

		private void BuildDictionaries()
		{
			m_styleByKey.Clear();
			foreach (var style in m_styles)
			{
				m_styleByKey.Add(style.Key, style);
			}
		}
	}
}