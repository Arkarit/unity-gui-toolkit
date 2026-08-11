using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace GuiToolkit
{
	/// <summary>
	/// Sizes its own RectTransform from the measured size of ANOTHER object, plus padding, clamped to an
	/// optional maximum. That maximum is the piece uGUI is missing: ContentSizeFitter has no upper bound,
	/// and LayoutElement can only state values, not bound them.
	///
	/// The resolved size is always reported as an <see cref="ILayoutElement"/>. Whether it is also written
	/// to the own RectTransform depends on the direct parent: a layout group that controls that axis stays
	/// in charge, so the two never fight over the same rect.
	///
	/// Why this does not oscillate on a wrapping TextMeshPro text: the reference is measured WITHOUT a
	/// width constraint, so the measurement cannot depend on the size this component is about to produce.
	/// At the maximum the width is therefore a decision, not a measurement, and the wrapping happens
	/// inside it. The height may then follow that wrapping, because it is measured against the width that
	/// has already been applied - uGUI finishes the entire horizontal pass before starting the vertical
	/// one (see LayoutRebuilder.Rebuild).
	/// </summary>
	[DisallowMultipleComponent]
	[RequireComponent(typeof(RectTransform))]
	public class UiReferencedSizeFitter : UIBehaviour, ILayoutElement, ILayoutSelfController
	{
		public enum EFitMode
		{
			Unconstrained,
			MinSize,
			PreferredSize,
		}

		/// <summary>
		/// The object being measured. Deliberately independent of this RectTransform - it may be a child
		/// (the usual case: a button's own label), a sibling, or something entirely elsewhere.
		/// </summary>
		[SerializeField] private RectTransform m_reference;

		[SerializeField] private EFitMode m_horizontalFit = EFitMode.Unconstrained;
		[SerializeField] private EFitMode m_verticalFit = EFitMode.Unconstrained;

		[SerializeField] private float m_paddingLeft;
		[SerializeField] private float m_paddingRight;
		[SerializeField] private float m_paddingTop;
		[SerializeField] private float m_paddingBottom;

		// Negative means "no maximum", the same way LayoutElement expresses "no value".
		[SerializeField] private float m_maxWidth = -1f;
		[SerializeField] private float m_maxHeight = -1f;

		// Has to stay above LayoutElement's 1: LayoutUtility takes the MAXIMUM among the components of
		// highest priority, so an equal priority would let an unclamped LayoutElement value win and the
		// maximum above would quietly do nothing.
		[SerializeField] private int m_layoutPriority = 2;

		private bool m_isApplying;
		private bool m_hasWarnedAboutCycle;

		private float m_width = -1f;
		private float m_height = -1f;

		private TMP_Text m_referenceText;
		private RectTransform m_referenceTextResolvedFor;

		private RectTransform m_rectTransform;

		private static readonly List<Component> s_ignorers = new List<Component>();

		public RectTransform Reference
		{
			get => m_reference;
			set { m_reference = value; SetDirty(); }
		}

		public EFitMode HorizontalFit
		{
			get => m_horizontalFit;
			set { m_horizontalFit = value; SetDirty(); }
		}

		public EFitMode VerticalFit
		{
			get => m_verticalFit;
			set { m_verticalFit = value; SetDirty(); }
		}

		public float MaxWidth
		{
			get => m_maxWidth;
			set { m_maxWidth = value; SetDirty(); }
		}

		public float MaxHeight
		{
			get => m_maxHeight;
			set { m_maxHeight = value; SetDirty(); }
		}

		private RectTransform RectTransform =>
			m_rectTransform != null ? m_rectTransform : m_rectTransform = (RectTransform)transform;

		public int layoutPriority => m_layoutPriority;

		// The resolved size is reported as the preferred one regardless of which measurement produced it:
		// the fit mode says HOW the reference was measured, not what we want to be. Reporting a minimum
		// as well would stop a parent from ever squeezing us, which is the parent's call to make.
		public float minWidth => -1f;
		public float minHeight => -1f;
		public float flexibleWidth => -1f;
		public float flexibleHeight => -1f;

		public float preferredWidth => m_horizontalFit == EFitMode.Unconstrained ? -1f : m_width;
		public float preferredHeight => m_verticalFit == EFitMode.Unconstrained ? -1f : m_height;

		public void CalculateLayoutInputHorizontal()
		{
			m_width = ResolveWidth();
		}

		public void CalculateLayoutInputVertical()
		{
			m_height = ResolveHeight();
		}

		public void SetLayoutHorizontal()
		{
			Apply(RectTransform.Axis.Horizontal, m_horizontalFit, m_width);
		}

		public void SetLayoutVertical()
		{
			Apply(RectTransform.Axis.Vertical, m_verticalFit, m_height);
		}

		public void SetDirty()
		{
			// Our own write to the rect comes back as OnRectTransformDimensionsChange. Queueing another
			// rebuild from it is how this turns into an endless loop.
			if (m_isApplying)
				return;

			if (!IsActive())
				return;

			// No need to guard against repeat calls: MarkLayoutForRebuild drops a rect that is already
			// queued for this frame.
			LayoutRebuilder.MarkLayoutForRebuild(RectTransform);
		}

		protected override void OnEnable()
		{
			base.OnEnable();
			TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTmpTextChanged);
			SetDirty();
		}

		protected override void OnDisable()
		{
			TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTmpTextChanged);
			SetDirty();
			base.OnDisable();
		}

		protected override void OnRectTransformDimensionsChange()
		{
			SetDirty();
		}

		protected virtual void OnTransformChildrenChanged()
		{
			SetDirty();
		}

		protected override void OnDidApplyAnimationProperties()
		{
			SetDirty();
		}

#if UNITY_EDITOR
		protected override void OnValidate()
		{
			base.OnValidate();
			m_referenceTextResolvedFor = null;
			m_hasWarnedAboutCycle = false;
			SetDirty();
		}
#endif

		/// <summary>
		/// True when the direct parent's layout group drives this axis, in which case this component only
		/// reports and does not write. Public so the inspector can say so instead of leaving the reader
		/// wondering why nothing happens.
		/// </summary>
		public bool IsControlledByParent( RectTransform.Axis _axis )
		{
			// ignoreLayout takes us out of the parent's hands entirely, so we are free to size ourselves.
			s_ignorers.Clear();
			GetComponents(typeof(ILayoutIgnorer), s_ignorers);
			for (int i = 0; i < s_ignorers.Count; i++)
			{
				if (s_ignorers[i] is Behaviour behaviour && !behaviour.enabled)
					continue;
				if (((ILayoutIgnorer)s_ignorers[i]).ignoreLayout)
					return false;
			}

			var parent = transform.parent;
			if (parent == null)
				return false;

			var group = parent.GetComponent<LayoutGroup>();
			if (group == null || !group.enabled)
				return false;

			// Covers HorizontalLayoutGroup, VerticalLayoutGroup and UiHorizontalOrVerticalLayoutGroup.
			if (group is HorizontalOrVerticalLayoutGroup horizontalOrVertical)
			{
				return _axis == RectTransform.Axis.Horizontal
					? horizontalOrVertical.childControlWidth
					: horizontalOrVertical.childControlHeight;
			}

			// GridLayoutGroup drives both axes. Anything unknown is assumed to as well: guessing wrong in
			// that direction costs a size we did not set, guessing wrong the other way means two
			// components writing the same rect.
			return true;
		}

		private void Apply( RectTransform.Axis _axis, EFitMode _mode, float _size )
		{
			if (_mode == EFitMode.Unconstrained || _size < 0f)
				return;

			// A parent that controls this axis works from our reported size. Writing here as well would
			// overwrite the arithmetic it just did for our siblings.
			if (IsControlledByParent(_axis))
				return;

			m_isApplying = true;
			try
			{
				RectTransform.SetSizeWithCurrentAnchors(_axis, _size);
			}
			finally
			{
				m_isApplying = false;
			}
		}

		private float ResolveWidth()
		{
			if (m_horizontalFit == EFitMode.Unconstrained)
				return -1f;

			float content = MeasureWidth(m_horizontalFit);
			if (content < 0f)
				return -1f;

			float result = content + m_paddingLeft + m_paddingRight;
			if (m_maxWidth >= 0f)
				result = Mathf.Min(result, m_maxWidth);

			return result;
		}

		private float ResolveHeight()
		{
			if (m_verticalFit == EFitMode.Unconstrained)
				return -1f;

			float content = MeasureHeight(m_verticalFit);
			if (content < 0f)
				return -1f;

			float result = content + m_paddingTop + m_paddingBottom;
			if (m_maxHeight >= 0f)
				result = Mathf.Min(result, m_maxHeight);

			return result;
		}

		private float MeasureWidth( EFitMode _mode )
		{
			if (!IsReferenceUsable())
				return -1f;

			var text = ResolveReferenceText();
			if (text != null)
			{
				if (_mode == EFitMode.MinSize)
					return MeasureWidestToken(text);

				// Infinite on both axes: nothing can wrap, so the answer is the single-line ideal width and
				// it cannot depend on the size we are about to set. That is what keeps this from oscillating.
				return text.GetPreferredValues(text.text, Mathf.Infinity, Mathf.Infinity).x;
			}

			return _mode == EFitMode.MinSize
				? LayoutUtility.GetMinWidth(m_reference)
				: LayoutUtility.GetPreferredWidth(m_reference);
		}

		private float MeasureHeight( EFitMode _mode )
		{
			if (!IsReferenceUsable())
				return -1f;

			var text = ResolveReferenceText();
			if (text != null)
			{
				float available = AvailableTextWidth();
				if (available <= 0f)
					return -1f;

				return text.GetPreferredValues(text.text, available, Mathf.Infinity).y;
			}

			return _mode == EFitMode.MinSize
				? LayoutUtility.GetMinHeight(m_reference)
				: LayoutUtility.GetPreferredHeight(m_reference);
		}

		/// <summary>
		/// The narrowest width the text can take without a word being broken.
		///
		/// TMP offers nothing usable here: its own ILayoutElement.minWidth is 0 no matter how wide the rect
		/// is, and probing GetPreferredValues at zero width breaks inside words - measured 21.3 for a text
		/// whose widest word is 218.7. So the widest token is measured directly.
		///
		/// Approximation: this splits on whitespace and therefore does not know TMP's own break
		/// opportunities (hyphens, CJK). Good enough for labels, and it only runs while the layout is dirty.
		/// </summary>
		private float MeasureWidestToken( TMP_Text _text )
		{
			string s = _text.text;
			if (string.IsNullOrEmpty(s))
				return 0f;

			float widest = 0f;
			int start = -1;

			for (int i = 0; i <= s.Length; i++)
			{
				bool isBreak = i == s.Length || char.IsWhiteSpace(s[i]);
				if (!isBreak)
				{
					if (start < 0)
						start = i;
					continue;
				}

				if (start < 0)
					continue;

				float width = _text.GetPreferredValues(s.Substring(start, i - start), Mathf.Infinity, Mathf.Infinity).x;
				if (width > widest)
					widest = width;

				start = -1;
			}

			return widest;
		}

		/// <summary>
		/// The width the text has to lay out in. When this component drives the width, the horizontal pass
		/// has already run and our own rect holds the clamped value - measuring the height against it is
		/// what lets the height follow a wrap without the width ever depending on it. When it does not,
		/// the reference's own width is the answer.
		/// </summary>
		private float AvailableTextWidth()
		{
			if (m_horizontalFit != EFitMode.Unconstrained)
				return RectTransform.rect.width - m_paddingLeft - m_paddingRight;

			return m_reference.rect.width;
		}

		private bool IsReferenceUsable()
		{
			if (m_reference == null)
				return false;

			// A descendant is fine and is the usual case; an ancestor is not, because its size would be
			// derived from ours.
			if (m_reference == RectTransform || IsAncestor(m_reference))
			{
				if (!m_hasWarnedAboutCycle)
				{
					m_hasWarnedAboutCycle = true;
					UiLog.LogWarning(
						$"{nameof(UiReferencedSizeFitter)}: the reference must not be this object or one of its " +
						"ancestors - its size would be derived from ours. Ignoring it.", this);
				}
				return false;
			}

			return true;
		}

		private bool IsAncestor( Transform _candidate )
		{
			for (var t = transform.parent; t != null; t = t.parent)
			{
				if (t == _candidate)
					return true;
			}
			return false;
		}

		private TMP_Text ResolveReferenceText()
		{
			if (m_reference == null)
			{
				m_referenceText = null;
				m_referenceTextResolvedFor = null;
				return null;
			}

			if (m_referenceTextResolvedFor != m_reference)
			{
				m_referenceTextResolvedFor = m_reference;
				m_referenceText = m_reference.GetComponent<TMP_Text>();
			}

			return m_referenceText;
		}

		private void OnTmpTextChanged( Object _obj )
		{
			// The reference may live outside our hierarchy, where its own layout-dirty flag never reaches
			// us. This callback is the only way we hear about it.
			var text = ResolveReferenceText();
			if (text == null || !ReferenceEquals(_obj, text))
				return;

			SetDirty();
		}
	}
}
