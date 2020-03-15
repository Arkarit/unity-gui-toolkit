using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit
{
	[ExecuteAlways]
	public class UiStyleApply : MonoBehaviour
	{
		[SerializeField]
		private UiStyle[] m_styles;

		[SerializeField]
		private int m_currentStyleIndex;

		private int m_oldStyleIndex = -1;

		// Start is called before the first frame update
		void Start()
		{
			SetCurrentStyle();
		}

		private void OnValidate()
		{
			SetCurrentStyle();
		}

		private void SetCurrentStyle()
		{
			if (m_currentStyleIndex == m_oldStyleIndex)
				return;

			m_oldStyleIndex = m_currentStyleIndex;

			if (m_styles != null && m_styles.Length > 0)
			{
				if (m_currentStyleIndex >= m_styles.Length)
					m_currentStyleIndex = 0;

				if (m_styles[m_currentStyleIndex] != null)
					m_styles[m_currentStyleIndex].Apply(gameObject);
			}
		}


	}
}
