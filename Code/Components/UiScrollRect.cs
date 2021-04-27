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

		public void ScrollToChild( RectTransform _child )
		{
			Rect viewport = ScrollRect.viewport.GetScreenRect();
			Rect child = _child.GetScreenRect();

			if (ScrollRect.horizontal)
			{
				float val = GetScrollValue(viewport.x, viewport.width, child.x, child.width);
				Vector2 v = new Vector2(val, 0);
				Vector2 v2 = ScrollRect.viewport.InverseTransformVector(v);
				Debug.Log(v2.x);
				Vector3 cSharpIsCircumstancial = ScrollRect.content.position;
				cSharpIsCircumstancial.x += v.x;
				ScrollRect.content.position = cSharpIsCircumstancial;
			}
		}

		private float GetScrollValue(float _xViewport, float _wViewport, float _xChild, float _wChild)
		{
			if (_xChild < _xViewport)
				return _xViewport - _xChild;

			if (_xChild + _wChild > _xViewport + _wViewport)
				return (_xViewport + _wViewport) - (_xChild + _wChild);

			return 0;
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