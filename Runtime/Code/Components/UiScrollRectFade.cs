using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	/// <summary>
	/// Automatic edge fade overlays for a ScrollRect: each edge graphic (typically an Image with a gradient, placed
	/// over the viewport edge, raycast target off) is visible only while the content continues beyond that edge.
	/// At the top of the content the top overlay is hidden, at the bottom the bottom one, in between both are shown.
	/// If the content fits into the viewport, all overlays are hidden. Works for vertical and horizontal scrolling.
	/// The overlays are faded via CanvasRenderer alpha, so their own colors (and gradient modifiers) stay untouched.
	/// </summary>
	[RequireComponent(typeof(ScrollRect))]
	public class UiScrollRectFade : UiThing
	{
		[Tooltip("Overlay shown while there is more content above the viewport. Optional.")]
		[SerializeField][Optional] protected Graphic m_top;

		[Tooltip("Overlay shown while there is more content below the viewport. Optional.")]
		[SerializeField][Optional] protected Graphic m_bottom;

		[Tooltip("Overlay shown while there is more content left of the viewport. Optional.")]
		[SerializeField][Optional] protected Graphic m_left;

		[Tooltip("Overlay shown while there is more content right of the viewport. Optional.")]
		[SerializeField][Optional] protected Graphic m_right;

		[Tooltip("Scroll distance (in pixels) over which an overlay fades in or out. 0 switches instantly.")]
		[SerializeField] protected float m_fadeDistance = 40;

		[Tooltip("Content overflow (in pixels) below which the content counts as fitting; avoids flicker from rounding.")]
		[SerializeField] protected float m_overflowTolerance = 1;

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

		protected override void OnEnable()
		{
			base.OnEnable();
			UpdateOverlays();
		}

		// Polled on purpose: content size changes (items added/removed, layout rebuilds) do not raise
		// ScrollRect.onValueChanged, and four float comparisons per frame are cheaper than tracking all causes.
		protected virtual void LateUpdate()
		{
			UpdateOverlays();
		}

		public void UpdateOverlays()
		{
			ScrollRect sr = ScrollRect;
			RectTransform content = sr.content;
			RectTransform viewport = sr.viewport != null ? sr.viewport : sr.transform as RectTransform;

			if (content == null || viewport == null)
			{
				SetAlpha(m_top, 0);
				SetAlpha(m_bottom, 0);
				SetAlpha(m_left, 0);
				SetAlpha(m_right, 0);
				return;
			}

			float overflowV = sr.vertical ? content.rect.height - viewport.rect.height : 0;
			float overflowH = sr.horizontal ? content.rect.width - viewport.rect.width : 0;

			if (overflowV > m_overflowTolerance)
			{
				float pos = sr.verticalNormalizedPosition; // 1 = at top
				SetAlpha(m_top, Fade((1 - pos) * overflowV));
				SetAlpha(m_bottom, Fade(pos * overflowV));
			}
			else
			{
				SetAlpha(m_top, 0);
				SetAlpha(m_bottom, 0);
			}

			if (overflowH > m_overflowTolerance)
			{
				float pos = sr.horizontalNormalizedPosition; // 0 = at left
				SetAlpha(m_left, Fade(pos * overflowH));
				SetAlpha(m_right, Fade((1 - pos) * overflowH));
			}
			else
			{
				SetAlpha(m_left, 0);
				SetAlpha(m_right, 0);
			}
		}

		// _distance: hidden content in pixels beyond the edge. Negative values (elastic overscroll) count as none.
		private float Fade( float _distance )
		{
			if (_distance <= 0)
				return 0;
			if (m_fadeDistance <= 0)
				return 1;
			return Mathf.Clamp01(_distance / m_fadeDistance);
		}

		private static void SetAlpha( Graphic _graphic, float _alpha )
		{
			if (_graphic == null)
				return;

			_graphic.canvasRenderer.SetAlpha(_alpha);
		}
	}
}
