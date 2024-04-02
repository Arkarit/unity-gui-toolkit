using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit
{
	[RequireComponent(typeof(RectTransform))]
	public class UiLinkSize : MonoBehaviour
	{
		[SerializeField] protected RectTransform m_linkTo;
		[SerializeField] protected bool m_linkX = true;
		[SerializeField] protected bool m_linkY = true;
		[SerializeField] protected bool m_linkWidth = true;
		[SerializeField] protected bool m_linkHeight = true;

		public RectTransform RectTransform => (RectTransform) transform;
		private Coroutine m_coroutine;

		private Vector2 m_oldPos;
		private Vector2 m_oldSize;

		private void OnEnable()
		{
			AlignSize();
		}

		// Shitty Unity has NO event driven OnTransformChanged()
		// https://forum.unity.com/threads/more-transform-callbacks.263448/
		private void Update()
		{
			if (!m_linkTo.hasChanged)
				return;
			m_linkTo.hasChanged = false;
			AlignSize();
		}

		private void AlignSize()
		{
			if (m_linkX || m_linkY )
			{
				if (m_linkTo.anchoredPosition != m_oldPos)
				{
					m_oldPos = m_linkTo.anchoredPosition;
 					Vector2 pos = RectTransform.anchoredPosition;
					Vector2 transformed = SwitchToRectTransform(m_linkTo, RectTransform);

					if (m_linkX)
						pos.x = transformed.x;
					if (m_linkY)
						pos.y = transformed.y;
					RectTransform.anchoredPosition = pos;
				}
			}

			if (m_linkWidth || m_linkHeight)
			{
				if (m_linkTo.sizeDelta != m_oldSize)
				{
					m_oldSize = m_linkTo.sizeDelta;
					Vector2 size = RectTransform.sizeDelta;
					if (m_linkWidth)
						size.x = m_linkTo.sizeDelta.x;
					if (m_linkHeight)
						size.y = m_linkTo.sizeDelta.y;
					RectTransform.sizeDelta = size;
				}
			}
		}

		private Vector2 SwitchToRectTransform( RectTransform _from, RectTransform _to )
		{
			Vector2 localPoint;
			Vector2 fromPivotDerivedOffset = new Vector2(_from.rect.width * _from.pivot.x + _from.rect.xMin, _from.rect.height * _from.pivot.y + _from.rect.yMin);
			Vector2 screenP = RectTransformUtility.WorldToScreenPoint(null, _from.position);
			screenP += fromPivotDerivedOffset;
			RectTransformUtility.ScreenPointToLocalPointInRectangle(_to, screenP, null, out localPoint);
			Vector2 pivotDerivedOffset = new Vector2(_to.rect.width * _to.pivot.x + _to.rect.xMin, _to.rect.height * _to.pivot.y + _to.rect.yMin);
			return _to.anchoredPosition + localPoint - pivotDerivedOffset;
		}

	}
}