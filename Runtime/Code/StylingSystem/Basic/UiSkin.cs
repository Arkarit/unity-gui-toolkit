using System;
using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit.Style
{
	[Serializable]
	public class UiSkin
	{
		[SerializeField] private string m_name;
		[SerializeField] [SerializeReference] private List<UiAbstractStyleBase> m_styles = new();

		private readonly Dictionary<Type, UiAbstractStyleBase> m_styleByClass = new ();

		public string Name => m_name;
		public List<UiAbstractStyleBase> Styles => m_styles;

		public UiAbstractStyleBase StyleByMonoBehaviourClass(Type monoBehaviourClass)
		{
			if (m_styleByClass.TryGetValue(monoBehaviourClass, out UiAbstractStyleBase result))
			{
				return result;
			}

			BuildDictionaries();

			if (m_styleByClass.TryGetValue(monoBehaviourClass, out result))
			{
				return result;
			}

			return null;
		}

		private void BuildDictionaries()
		{
			m_styleByClass.Clear();
			foreach (var style in m_styles)
			{
				m_styleByClass.Add(style.SupportedMonoBehaviour, style);
			}
		}
	}
}