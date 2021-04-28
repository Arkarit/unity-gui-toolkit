using DigitalRuby.Tween;
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

		[Tooltip("This ensures, that a Tab child in an UiScrollRect is displayed fully, if it is only partially visible when selected. (Recommended)")]
		[SerializeField] protected bool m_ensureTabVisibility = true;

		[Tooltip("Duration of tween to make child visible. 0 means instant.")]
		[SerializeField] protected float m_ensureTabVisibilityDuration = 0.3f;

		[Tooltip("Padding (Fraction of child width). If a child isn't the first or the last child, it makes sense to also display the neighbor tabs. This distance is used for that.")]
		[SerializeField] protected float m_ensureTabVisibilityPadding = 0.4f;


		private ScrollRect m_scrollRect;
		private Vector3Tween m_ensureTabVisibilityTween = null;

		protected bool IsEnsureTabVisibilityAnimated => m_ensureTabVisibility && !Mathf.Approximately(m_ensureTabVisibilityDuration, 0);

		public ScrollRect ScrollRect
		{
			get
			{
				if (m_scrollRect == null)
					m_scrollRect = GetComponent<ScrollRect>();
				return m_scrollRect;
			}
		}

		public void EnsureTabVisibility( UiTab _child )
		{
			if (!m_ensureTabVisibility)
				return;

			if (IsEnsureTabVisibilityAnimated && m_ensureTabVisibilityTween != null)
			{
				TweenFactory.RemoveTween(m_ensureTabVisibilityTween, TweenStopBehavior.Complete);
				m_ensureTabVisibilityTween = null;
			}

			float padding = m_ensureTabVisibilityPadding;
			RectTransform child = _child.RectTransform;
			int siblingIndex = child.GetSiblingIndex();

			if (siblingIndex == 0 || siblingIndex == ScrollRect.content.childCount - 1)
				padding = 0;

			Rect viewportRect = ScrollRect.viewport.GetScreenRect();
			Rect childRect = child.GetScreenRect();

			Vector3 startPos = ScrollRect.content.position;
			Vector3 pos = startPos;

			if (ScrollRect.horizontal)
			{
				float val = GetScrollValue(viewportRect.x, viewportRect.width, childRect.x, childRect.width);
				if (Mathf.Approximately(val, 0))
					return;

				padding *= childRect.width;
				val += padding * Mathf.Sign(val);
				pos.x += val;
			}
			else
			{
				float val = GetScrollValue(viewportRect.y, viewportRect.height, childRect.y, childRect.height);
				if (Mathf.Approximately(val, 0))
					return;

				padding *= childRect.height;
					val += padding * Mathf.Sign(val);
				pos.y += val;
			}

			if (!IsEnsureTabVisibilityAnimated)
			{
				ScrollRect.content.position = pos;
				return;
			}

			System.Action<ITween<Vector3>> updateBar = ( t ) =>
			{
				ScrollRect.content.position = t.CurrentValue;
			};

			m_ensureTabVisibilityTween = gameObject.Tween("makeVisible", startPos, pos, m_ensureTabVisibilityDuration, TweenScaleFunctions.QuadraticEaseInOut, updateBar );
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