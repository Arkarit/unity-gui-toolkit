using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	[RequireComponent(typeof(ScrollRect))]
	public class UiScrollRect : MonoBehaviour
	{
		public bool m_useInitialNormalizedPosition;
		public Vector2 m_initialNormalizedPosition = new Vector2(0,1);

		private ScrollRect m_scrollRect;

		public ScrollRect ScrollRect
		{
			get
			{
				if (m_scrollRect == null)
					m_scrollRect = GetComponent<ScrollRect>();
				return m_scrollRect;
			}
		}

		private void OnEnable()
		{
			if (m_useInitialNormalizedPosition)
				StartCoroutine(DelayedScrollToTop());
		}

		private IEnumerator DelayedScrollToTop()
		{
			yield return 0;
			ScrollRect.normalizedPosition = m_initialNormalizedPosition;
		}
	}
}