using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit
{
	public class UiStyleSwitcher : MonoBehaviour
	{
		[SerializeField]
		private List<UiStyleApplier> m_styleAppliers;

		[SerializeField]
		private int m_currentStyleIndex;

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
				SetCurrentStyle(true);
			}
		}

		protected int NumStyleAppliers => m_styleAppliers == null ? 0 : m_styleAppliers.Count;

		protected void Awake()
		{
			SetCurrentStyle(false);
		}


		private void SetCurrentStyle(bool _withApply)
		{
			foreach (var applier in m_styleAppliers)
			{
				if (_withApply)
					applier.CurrentStyleIndex = m_currentStyleIndex;
				else
					applier.SetCurrentStyleIndexWithoutApply(m_currentStyleIndex);
			}
		}
	}
}