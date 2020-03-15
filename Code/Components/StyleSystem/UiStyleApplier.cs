using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit
{
	[ExecuteAlways]
	public class UiStyleApplier : MonoBehaviour
	{
		[SerializeField]
		private UiStyle[] m_styles;

		[SerializeField]
		private int m_currentStyleIndex;

		private int m_oldStyleIndex = -1;

		public int CurrentStyleIndex
		{
			get
			{
				return m_currentStyleIndex;
			}
			set
			{
				m_currentStyleIndex = value;
				if (m_currentStyleIndex < 0)
					m_currentStyleIndex = 0;
				SetCurrentStyle();
			}
		}

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

		// C# programmers don't have friends
		// In a real programming language I'd just make UiStyleSwitcher a friend of UiStyleApplier.
		// With C# we have to use this ugly workaround
		internal void SetCurrentStyleIndexWithoutApply(int _index)
		{
			m_currentStyleIndex = _index;
		}


	}
}
