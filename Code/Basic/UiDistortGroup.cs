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

		private readonly List<UiDistortBase> m_elements = new List<UiDistortBase>();
		private readonly List<UiDistortBase> m_secondaryElements = new List<UiDistortBase>();
		private readonly Dictionary<GameObject,int> m_done = new Dictionary<GameObject, int>();
		private bool m_hasSecondary;

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
			m_secondaryElements.Clear();
			m_done.Clear();

			UiDistortBase[] elements = GetComponentsInChildren<UiDistortBase>(true);
			foreach (var element in elements)
			{
				GameObject go = element.gameObject;

				if (!m_done.ContainsKey(go))
					m_done.Add(go, 0);

				int prevElementsOnGo = m_done[element.gameObject];
				if (prevElementsOnGo <= 1)
				{
					if (prevElementsOnGo == 0)
						m_elements.Add(element);
					else
						m_secondaryElements.Add(element);
				}

				m_done[go]++;
			}

			m_hasSecondary = m_elements.Count != 0 && m_elements.Count == m_secondaryElements.Count;
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
				if (m_hasSecondary)
					m_secondaryElements[i].enabled = false;

				if (i > 0 && i < numElements-1)
				{
					m_elements[i].enabled = false;
					if (m_hasSecondary)
						m_secondaryElements[i].enabled = true;
				}
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