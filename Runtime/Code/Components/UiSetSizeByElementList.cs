using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit
{
	/// \brief Set vertical and / or horizontal size of a rect transform by the sum of a list of RectTransforms
	/// This is handy in cases where nested layout groups get too complicated.
	///
	/// \attention This can only be done via Update(), which is expensive. So don't overuse please.

	[RequireComponent(typeof(RectTransform))]
	public class UiSetSizeByElementList : MonoBehaviour
	{
		[SerializeField] private List<RectTransform> m_rectTransforms;
		[SerializeField] protected bool m_horizontal;
		[SerializeField] protected bool m_vertical;
		[SerializeField] protected float m_widthOffset;
		[SerializeField] protected float m_heightOffset;

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

		private void Update()
		{
			if (!m_horizontal && !m_vertical)
				return;

			float width = m_widthOffset;
			float height = m_heightOffset;

			foreach (var rectTransform in m_rectTransforms)
			{
				if (!rectTransform.gameObject.activeInHierarchy)
					continue;

				var callbackRect = rectTransform.rect;
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