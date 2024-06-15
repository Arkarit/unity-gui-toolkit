using System;
using System.Collections.Generic;
using System.Data;
using UnityEngine;

namespace GuiToolkit
{
	[Serializable]
	public abstract class ApplicableValueBase
	{
		[SerializeReference] protected List<object> m_values = new();
		[SerializeField] protected List<string> m_skins = new();
		[SerializeField] protected int m_index = -1;

		public bool IsApplicable = false;
		public object ValueObj
		{
			get
			{
				if (m_index >= 0 && m_index < m_values.Count)
				{
					return m_values[m_index];
				}

				return null;
			}
			set
			{
				if (m_index < 0 || m_index >= m_values.Count)
					return;

				m_values[m_index] = value;
			}
		}

		public string Skin
		{
			get
			{
				return m_index < 0 || m_index >= m_values.Count ? null : m_skins[m_index];
			}
			set
			{
				for (int i = 0; i < m_skins.Count; i++)
				{
					if (m_skins[i] == value)
					{
						m_index = i;
						return;
					}
				}

				throw new ArgumentException($"Attempt to set unknown skin '{value}'");
			}
		}

		public void RemoveSkin(string _skinName)
		{
			for (int i = 0; i < m_skins.Count; i++)
			{
				if (m_skins[i] == _skinName)
				{
					m_values.RemoveAt(i);
					m_skins.RemoveAt(i);
					if (m_index >= m_values.Count)
						m_index = m_values.Count - 1;

					return;
				}
			}
		}

		public void AddSkin(string _skinName, object _defaultValue)
		{
			if (m_skins.Contains(_skinName))
				throw new DuplicateNameException($"Skin name '{_skinName}' already defined");

			m_skins.Add(_skinName);
			m_values.Add( _defaultValue == null ? null : ObjectUtility.SafeClone(_defaultValue));
			if (m_index == -1)
				m_index = 0;
		}
	}

	[Serializable]
	public class ApplicableValue<T> : ApplicableValueBase
	{
		public T Value
		{
			get => (T) ValueObj;
			set => ValueObj = value;
		}
	}
}
