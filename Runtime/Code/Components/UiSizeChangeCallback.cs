using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit
{
	[RequireComponent(typeof(RectTransform))]
	public class UiSizeChangeCallback : MonoBehaviour
	{
		public CEvent<float, float> OnSizeChanged = new();

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
			StartCoroutine(DelayedInvokeEvent());
		}

		IEnumerator DelayedInvokeEvent()
		{
			yield return 0;
			InvokeEvent();
		}

		private void OnDisable()
		{
			InvokeEvent();
		}

		public void OnRectTransformDimensionsChange()
		{
			InvokeEvent();
		}

		private void InvokeEvent()
		{
			bool isEnabled = gameObject.activeInHierarchy && enabled;

			if (isEnabled)
			{
				var rect = RectTransform.rect;
				OnSizeChanged.Invoke(rect.width, rect.height);
				return;
			}

			OnSizeChanged.Invoke(0,0);
		}
	}
}
