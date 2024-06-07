using System;
using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit
{
	// Just a stub to satisfy PropertyDrawer
	[Serializable]
	public abstract class ApplicableValueBase
	{
		public bool IsApplicable = false;
		public abstract object ValueObj { get;}
	}
	
	[Serializable]
	public class ApplicableValue<T> : ApplicableValueBase
	{
		[SerializeField] private readonly List<T> m_values = new();
		[SerializeField] private int m_index = 0;

		public override object ValueObj
		{
			get
			{
				if (m_index >= 0 && m_index < m_values.Count)
				{
					return m_values[m_index];
				}
				return null;
			}
		}

		public T Value
		{
			get => (T)ValueObj;
			set
			{
				if (m_index < 0 || m_index >= m_values.Count)
					return;

				m_values[m_index] = value;
			}
		}
	}
}
