using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	/// <summary>
	/// Layout group which can be switched between horizontal and vertical
	/// </summary>
	[RequireComponent(typeof(RectTransform))]
	public class UiHorizontalOrVerticalLayoutGroup : HorizontalOrVerticalLayoutGroup
	{
		[SerializeField] protected bool m_vertical;
		[SerializeField] protected bool m_reverseOrder;

		public bool Vertical
		{
			get => m_vertical;
			set => m_vertical = value;
		}

		public bool ReverseOrder
		{
			get => m_reverseOrder;
			set => m_reverseOrder = value;
		}

		protected UiHorizontalOrVerticalLayoutGroup() { }

		/// <summary>
		/// Called by the layout system. Also see ILayoutElement
		/// </summary>
		public override void CalculateLayoutInputHorizontal()
		{
			base.CalculateLayoutInputHorizontal();
			CalcAlongAxis(0, m_vertical);
		}

		/// <summary>
		/// Called by the layout system. Also see ILayoutElement
		/// </summary>
		public override void CalculateLayoutInputVertical()
		{
			CalcAlongAxis(1, m_vertical);
		}

		/// <summary>
		/// Called by the layout system. Also see ILayoutElement
		/// </summary>
		public override void SetLayoutHorizontal()
		{
			SetChildrenAlongAxis(0, m_vertical);
			RevertIfNecessary(false);
		}

		/// <summary>
		/// Called by the layout system. Also see ILayoutElement
		/// </summary>
		public override void SetLayoutVertical()
		{
			SetChildrenAlongAxis(1, m_vertical);
			RevertIfNecessary(true);
		}

		private void RevertIfNecessary(bool _vertical)
		{
			if (!m_reverseOrder || _vertical != m_vertical)
				return;

			float parentWidth = rectTransform.rect.width;
			float parentHeight = rectTransform.rect.height;

			for (int i = 0; i < rectChildren.Count; i++)
			{
				RectTransform child = rectChildren[i];
				Vector2 pos = child.anchoredPosition;
				Vector2 size = child.rect.size;

				if (m_vertical)
					pos.y = -parentHeight - pos.y - m_Padding.top + m_Padding.bottom;
				else
					pos.x = parentWidth - pos.x + m_Padding.left - m_Padding.right;

				child.anchoredPosition = pos;
			}
		}

#if UNITY_EDITOR
		protected override void OnValidate()
		{
			base.OnValidate();
			LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)transform);
		}
#endif
	}
}
