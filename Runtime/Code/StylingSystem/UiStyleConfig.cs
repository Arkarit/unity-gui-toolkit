using System;
using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit.Style
{
	[ExecuteAlways]
	public class UiStyleConfig : MonoBehaviour
	{
		[SerializeField] private List<string> m_skins = new ();
		[SerializeField] private int m_index = -1;

		public List<string> Skins => m_skins;
		public int Index
		{
			get => m_index;
			set
			{
				if (value == -1 && m_skins.Count == 0)
				{
					m_index = -1;
					return;
				}

				if (value < 0 || value >= m_skins.Count)
					throw new ArgumentOutOfRangeException("");

				m_index = value;
				// Event
			}
		}

		public string Skin
		{
			get
			{
				if (m_index < 0)
					return null;

				return m_skins[m_index];
			}

			set
			{
				if (value == null)
					throw new ArgumentNullException("Can not set empty skin");

				for (int i=0; i<m_skins.Count; i++)
				{
					var skin = m_skins[i];
					if (skin == value)
					{
						m_index = i;
						// Event
						return;
					}
				}

				throw new ArgumentException($"Can not set unknown skin '{value}'; please try to AddSkin() first");
			}
		}

		public void AddSkin(string skinName)
		{
			m_skins.Add(skinName);
			// Event
		}

		public void RemoveCurrentSkin()
		{
			if (m_index < 0)
				return;

			m_skins.RemoveAt(m_index);
			if (m_index >= m_skins.Count)
				m_index = m_skins.Count - 1;

			// Event
		}

		public static UiStyleConfig Instance => UiToolkitConfiguration.Instance.m_styleConfig;

	}
}