using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace GuiToolkit
{
	/// <summary>
	/// Work in progress.
	/// Generic auto layout element. Currently only supports TMP vertical preferred height.
	/// Extended functionality is added only when backed by real use cases.
	/// </summary>
	[DisallowMultipleComponent]
	public class UiAutoLayoutElement : UIBehaviour, ILayoutElement, ILayoutSelfController
	{
		public enum SizeSource
		{
			Unspecified,
			TmpPreferred,
		}

		[SerializeField] private int m_layoutPriority = 1;

		[SerializeField] private TMP_Text m_tmpText;
		[SerializeField] private SizeSource m_heightSource = SizeSource.Unspecified;

		private bool m_isDirty = true;

		private float m_preferredHeight;

		public int layoutPriority => m_layoutPriority;

		public float minWidth => -1;
		public float preferredWidth => -1;
		public float flexibleWidth => -1;
		public float flexibleHeight => -1;
		public float minHeight => -1;

		public float preferredHeight
		{
			get
			{
				EnsureCache();
				return m_preferredHeight;
			}
		}

		public void CalculateLayoutInputHorizontal() => EnsureCache();

		public void CalculateLayoutInputVertical() => EnsureCache();

		public void SetLayoutHorizontal()
		{
		}

		public void SetLayoutVertical()
		{
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

		private void OnTmpTextChanged( Object _obj )
		{
			if (m_tmpText == null)
			{
				return;
			}

			if (!ReferenceEquals(_obj, m_tmpText))
			{
				return;
			}

			SetDirty();
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
			m_preferredHeight = ResolveHeight(m_heightSource);
		}

		private float ResolveHeight( SizeSource _source )
		{
			if (_source == SizeSource.Unspecified)
			{
				return -1f;
			}

			if (_source == SizeSource.TmpPreferred)
			{
				return ResolveTmpHeight();
			}

			return -1f;
		}

		private float ResolveTmpHeight()
		{
			if (m_tmpText == null)
			{
				return -1f;
			}

			float width = m_tmpText.rectTransform.rect.width;
			if (width <= 0f)
			{
				return -1f;
			}

			Vector2 preferred = m_tmpText.GetPreferredValues(width, 0f);
			return preferred.y;
		}
	}
}
