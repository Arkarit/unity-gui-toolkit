using System;
using System.Collections.Generic;
using System.Data;
using UnityEngine;

namespace GuiToolkit
{
	[Serializable]
	public abstract class ApplicableValueBase
	{
		public bool IsApplicable = false;
		public abstract string Skin { get; set; }
		public abstract void RemoveSkin(string _skinName);
		public abstract void AddSkin(string _skinName, object _defaultValue);
		public abstract object ValueObj { get; }
	}

	[Serializable]
	public class ApplicableValue<T> : ApplicableValueBase
	{
		[SerializeReference] protected List<T> m_values = new();
		[SerializeField] protected List<string> m_skins = new();
		[SerializeField] protected int m_index = -1;

		public override object ValueObj => Value;

		public T Value
		{
			get
			{
				if (m_index >= 0 && m_index < m_values.Count)
				{
					return m_values[m_index];
				}

				return default;
			}
			set
			{
				if (m_index < 0 || m_index >= m_values.Count)
					return;

				m_values[m_index] = value;
			}
		}

		public override string Skin
		{
			get
			{
				return m_index < 0 || m_index >= m_values.Count ? null : m_skins[m_index];
			}
			set
			{
				if (value == null)
				{
					m_index = -1;
					return;
				}

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

		public override void RemoveSkin(string _skinName)
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

		public override void AddSkin(string _skinName, object _defaultValue)
		{
			if (m_skins.Contains(_skinName))
				throw new DuplicateNameException($"Skin name '{_skinName}' already defined");

			m_skins.Add(_skinName);
			m_values.Add((T) _defaultValue);
			if (m_index == -1)
				m_index = 0;
		}

	}
}
