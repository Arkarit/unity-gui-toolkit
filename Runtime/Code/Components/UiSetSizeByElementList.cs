using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit
{
	[RequireComponent(typeof(RectTransform))]
	public class UiSetSizeByElementList : MonoBehaviour
	{
		[SerializeField] private List<UiSizeChangeCallback> m_callbackComponents;
		[SerializeField] protected bool m_horizontal;
		[SerializeField] protected bool m_vertical;

		private RectTransform m_rectTransform;

		public RectTransform RectTransform
		{
			get
			{
				if (m_rectTransform == null)
					m_rectTransform = GetComponent<RectTransform>();

				return m_rectTransform;
			}
		}

		private void OnEnable()
		{
			foreach (var callbackComponent in m_callbackComponents)
				callbackComponent.OnSizeChanged.AddListener(OnSizeChanged);
		}

		private void OnDisable()
		{
			foreach (var callbackComponent in m_callbackComponents)
				callbackComponent.OnSizeChanged.RemoveListener(OnSizeChanged);
		}

		private void OnSizeChanged(float _, float __) => UpdateSize();

		private void UpdateSize()
		{
			if (!m_horizontal && !m_vertical)
				return;

			float width = 0;
			float height = 0;

			foreach (var callbackComponent in m_callbackComponents)
			{
				var callbackRect = callbackComponent.RectTransform.rect;
				width += callbackRect.width;
				height += callbackRect.height;
			}

			if (m_horizontal)
				RectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);

			if (m_vertical)
				RectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
		}
	}
}