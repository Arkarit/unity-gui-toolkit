using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit
{
	public class UiProgressBarRectTransform : UiProgressBarBase
	{
		[SerializeField]
		EDirection m_direction;

		[SerializeField]
		private RectTransform m_referenceTransform;

		[SerializeField]
		private RectTransform m_barTransform;


		protected override void DisplayProgress( float _progressNormalized )
		{
			Vector2 sizeDelta = m_referenceTransform.sizeDelta;
			if (m_direction == EDirection.Horizontal)
				sizeDelta.x *= _progressNormalized;
			else
				sizeDelta.y *= _progressNormalized;
			m_barTransform.sizeDelta = sizeDelta;
		}
	}
}