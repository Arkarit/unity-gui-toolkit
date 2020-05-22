using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace GuiToolkit
{
	public class UiProgressBarRectTransform : UiProgressBarBase
	{
		[SerializeField]
		[FormerlySerializedAs("m_direction")]
		EAxis2D m_axis;

		[SerializeField]
		private RectTransform m_referenceTransform;

		[SerializeField]
		private RectTransform m_barTransform;


		protected override void DisplayProgress( float _progressNormalized )
		{
			Vector2 sizeDelta = m_referenceTransform.sizeDelta;
			if (m_axis == EAxis2D.Horizontal)
				sizeDelta.x *= _progressNormalized;
			else
				sizeDelta.y *= _progressNormalized;
			m_barTransform.sizeDelta = sizeDelta;
		}
	}
}