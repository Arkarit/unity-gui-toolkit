using System;
using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit
{
	[ExecuteAlways]
	public class UiDistortGroup : MonoBehaviour
	{

		public enum EDirection
		{
			Horizontal,
			Vertical,
		}

		[SerializeField]
		protected EDirection m_direction;

		private List<UiDistort> m_elements;

		private void OnTransformChildrenChanged()
		{
			CollectElements();
			PositionElements();
		}

		private void OnEnable()
		{
			CollectElements();
			PositionElements();
		}

		private void CollectElements()
		{
			m_elements.Clear();
			UiDistort[] elements = GetComponentsInChildren<UiDistort>();
			foreach (var element in elements)
				m_elements.Add(element);
		}

		private void PositionElements()
		{
			for (int i=0; i<m_elements.Count; i++)
			{
				if (i == 0)
				{
					m_elements[i].enabled = true;
					if (m_direction == EDirection.Horizontal)
					{
						m_elements[i].MirrorHorizontal = false;
					}
					else
					{
						m_elements[i].MirrorVertical = false;
					}
				}
				else if (i == m_elements.Count-1)
				{
					m_elements[i].enabled = true;
					if (m_direction == EDirection.Horizontal)
					{
						m_elements[i].MirrorHorizontal = true;
					}
					else
					{
						m_elements[i].MirrorVertical = true;
					}
				}
				else
				{
					m_elements[i].enabled = false;
				}
			}
		}

		private void OnValidate()
		{
			CollectElements();
			PositionElements();
		}

	}
}