using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	[RequireComponent(typeof(ScrollRect))]
	public class UiScrollRect : MonoBehaviour
	{
		[SerializeField] protected bool m_useInitialNormalizedPosition;
		[SerializeField] protected Vector2 m_initialNormalizedPosition = new Vector2(0,1);

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

		protected virtual void OnEnable()
		{
			if (m_useInitialNormalizedPosition)
				StartCoroutine(DelayedScrollToTop());
		}

		protected IEnumerator DelayedScrollToTop()
		{
			yield return 0;
			ScrollRect.normalizedPosition = m_initialNormalizedPosition;
		}
	}
}