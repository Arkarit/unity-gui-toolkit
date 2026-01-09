using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace GuiToolkit
{
	[DisallowMultipleComponent]
	public class UiAutoLayoutElement : UIBehaviour, ILayoutElement, ILayoutSelfController
	{
		public enum SizeSource
		{
			Unspecified,
			Manual,
			RectMin,
			RectPreferred,
			TmpMin,
			TmpPreferred,
		}

		[SerializeField] private int m_layoutPriority = 1;

		[SerializeField] private RectTransform m_sourceRect;
		[SerializeField] private TMP_Text m_tmpText;
		[SerializeField] private SizeSource m_widthSource = SizeSource.Unspecified;
		[SerializeField] private SizeSource m_heightSource = SizeSource.Unspecified;

		[SerializeField] private bool m_useManualMinWidth = false;
		[SerializeField] private float m_manualMinWidth = -1f;
		[SerializeField] private bool m_useManualPreferredWidth = false;
		[SerializeField] private float m_manualPreferredWidth = -1f;
		[SerializeField] private bool m_useManualFlexibleWidth = false;
		[SerializeField] private float m_manualFlexibleWidth = -1f;

		[SerializeField] private bool m_useManualMinHeight = false;
		[SerializeField] private float m_manualMinHeight = -1f;
		[SerializeField] private bool m_useManualPreferredHeight = false;
		[SerializeField] private float m_manualPreferredHeight = -1f;
		[SerializeField] private bool m_useManualFlexibleHeight = false;
		[SerializeField] private float m_manualFlexibleHeight = -1f;

		[SerializeField] private float m_paddingLeft = 0f;
		[SerializeField] private float m_paddingRight = 0f;
		[SerializeField] private float m_paddingTop = 0f;
		[SerializeField] private float m_paddingBottom = 0f;

		private bool m_isDirty = true;

		private float m_minWidth = -1f;
		private float m_preferredWidth = -1f;
		private float m_flexibleWidth = -1f;

		private float m_minHeight = -1f;
		private float m_preferredHeight = -1f;
		private float m_flexibleHeight = -1f;

		public int layoutPriority => m_layoutPriority;

		public float minWidth
		{
			get
			{
				EnsureCache();
				return m_minWidth;
			}
		}

		public float preferredWidth
		{
			get
			{
				EnsureCache();
				return m_preferredWidth;
			}
		}

		public float flexibleWidth
		{
			get
			{
				EnsureCache();
				return m_flexibleWidth;
			}
		}

		public float minHeight
		{
			get
			{
				EnsureCache();
				return m_minHeight;
			}
		}

		public float preferredHeight
		{
			get
			{
				EnsureCache();
				return m_preferredHeight;
			}
		}

		public float flexibleHeight
		{
			get
			{
				EnsureCache();
				return m_flexibleHeight;
			}
		}

		public void CalculateLayoutInputHorizontal()
		{
			EnsureCache();
		}

		public void CalculateLayoutInputVertical()
		{
			EnsureCache();
		}

		public void SetLayoutHorizontal()
		{
		}

		public void SetLayoutVertical()
		{
		}

		protected override void OnEnable()
		{
			base.OnEnable();
			SetDirty();
		}

		protected override void OnDisable()
		{
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
			SetDirty();
		}
#endif

		public void SetDirty()
		{
			m_isDirty = true;

			if (!IsActive())
			{
				return;
			}

			LayoutRebuilder.MarkLayoutForRebuild((RectTransform)transform);
		}

		private void EnsureCache()
		{
			if (!m_isDirty)
			{
				return;
			}

			m_isDirty = false;

			float padX = m_paddingLeft + m_paddingRight;
			float padY = m_paddingTop + m_paddingBottom;

			m_minWidth = ResolveWidth(m_widthSource, _preferMin: true);
			m_preferredWidth = ResolveWidth(m_widthSource, _preferMin: false);
			m_flexibleWidth = ResolveFlexible(_useManual: m_useManualFlexibleWidth, _manual: m_manualFlexibleWidth);

			m_minHeight = ResolveHeight(m_heightSource, _preferMin: true);
			m_preferredHeight = ResolveHeight(m_heightSource, _preferMin: false);
			m_flexibleHeight = ResolveFlexible(_useManual: m_useManualFlexibleHeight, _manual: m_manualFlexibleHeight);

			if (m_minWidth >= 0f)
			{
				m_minWidth += padX;
			}

			if (m_preferredWidth >= 0f)
			{
				m_preferredWidth += padX;
			}

			if (m_minHeight >= 0f)
			{
				m_minHeight += padY;
			}

			if (m_preferredHeight >= 0f)
			{
				m_preferredHeight += padY;
			}
		}

		private float ResolveWidth( SizeSource _source, bool _preferMin )
		{
			if (_source == SizeSource.Unspecified)
			{
				return -1f;
			}

			if (_source == SizeSource.Manual)
			{
				if (_preferMin)
				{
					return ResolveManual(_useManual: m_useManualMinWidth, _manual: m_manualMinWidth);
				}

				return ResolveManual(_useManual: m_useManualPreferredWidth, _manual: m_manualPreferredWidth);
			}

			if (_source == SizeSource.RectMin)
			{
				return ResolveRectWidth(_min: true);
			}

			if (_source == SizeSource.RectPreferred)
			{
				return ResolveRectWidth(_min: false);
			}

			if (_source == SizeSource.TmpMin)
			{
				return ResolveTmpWidth(_min: true);
			}

			if (_source == SizeSource.TmpPreferred)
			{
				return ResolveTmpWidth(_min: false);
			}

			return -1f;
		}

		private float ResolveHeight( SizeSource _source, bool _preferMin )
		{
			if (_source == SizeSource.Unspecified)
			{
				return -1f;
			}

			if (_source == SizeSource.Manual)
			{
				if (_preferMin)
				{
					return ResolveManual(_useManual: m_useManualMinHeight, _manual: m_manualMinHeight);
				}

				return ResolveManual(_useManual: m_useManualPreferredHeight, _manual: m_manualPreferredHeight);
			}

			if (_source == SizeSource.RectMin)
			{
				return ResolveRectHeight(_min: true);
			}

			if (_source == SizeSource.RectPreferred)
			{
				return ResolveRectHeight(_min: false);
			}

			if (_source == SizeSource.TmpMin)
			{
				return ResolveTmpHeight(_min: true);
			}

			if (_source == SizeSource.TmpPreferred)
			{
				return ResolveTmpHeight(_min: false);
			}

			return -1f;
		}

		private static float ResolveManual( bool _useManual, float _manual )
		{
			if (!_useManual)
			{
				return -1f;
			}

			return Mathf.Max(0f, _manual);
		}

		private static float ResolveFlexible( bool _useManual, float _manual )
		{
			if (!_useManual)
			{
				return -1f;
			}

			return Mathf.Max(0f, _manual);
		}

		private float ResolveRectWidth( bool _min )
		{
			RectTransform source = m_sourceRect != null ? m_sourceRect : (RectTransform)transform;

			if (_min)
			{
				return LayoutUtility.GetMinWidth(source);
			}

			return LayoutUtility.GetPreferredWidth(source);
		}

		private float ResolveRectHeight( bool _min )
		{
			RectTransform source = m_sourceRect != null ? m_sourceRect : (RectTransform)transform;

			if (_min)
			{
				return LayoutUtility.GetMinHeight(source);
			}

			return LayoutUtility.GetPreferredHeight(source);
		}

		private float ResolveTmpWidth( bool _min )
		{
			if (m_tmpText == null)
			{
				return -1f;
			}

			m_tmpText.ForceMeshUpdate();

			Vector2 preferred = m_tmpText.GetPreferredValues(float.PositiveInfinity, 0f);

			if (_min)
			{
				return preferred.x;
			}

			return preferred.x;
		}

		private float ResolveTmpHeight( bool _min )
		{
			if (m_tmpText == null)
			{
				return -1f;
			}

			m_tmpText.ForceMeshUpdate();

			float width = GetTmpConstraintWidth(m_tmpText);
			Vector2 preferred = m_tmpText.GetPreferredValues(width, 0f);

			if (_min)
			{
				return preferred.y;
			}

			return preferred.y;
		}

		private static float GetTmpConstraintWidth( TMP_Text _tmpText )
		{
			RectTransform rt = _tmpText.rectTransform;

			float width = rt.rect.width;
			if (width > 0f)
			{
				return width;
			}

			float fallback = LayoutUtility.GetPreferredWidth(rt);
			if (fallback > 0f)
			{
				return fallback;
			}

			return 1000f;
		}
	}
}
