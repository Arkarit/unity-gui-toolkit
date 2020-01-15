using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GuiToolkit
{
	[ExecuteAlways]
	public class UiDistortGroup : MonoBehaviour
	{
		[SerializeField]
		protected EDirection m_direction;

		private List<UiDistort> m_elements;
		private List<UiDistort> m_secondaryElements;

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
			int numElements = m_elements.Count;

			if (numElements == 0)
				return;

			for (int i=0; i<numElements; i++)
			{
				m_elements[i].SetMirror( 0 );
				m_elements[i].enabled = true;

				if (i > 0 && i < numElements-1)
					m_elements[i].enabled = false;
				else if (i == numElements-1 && numElements > 1)
					m_elements[i].SetMirror(m_direction);
			}
		}

		private void OnValidate()
		{
			CollectElements();
			PositionElements();
		}
	}
}