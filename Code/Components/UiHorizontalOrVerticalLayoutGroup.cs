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

		public bool Vertical
		{
			get => m_vertical;
			set => m_vertical = value;
		}

		protected UiHorizontalOrVerticalLayoutGroup() {}

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
		}

		/// <summary>
		/// Called by the layout system. Also see ILayoutElement
		/// </summary>
		public override void SetLayoutVertical()
		{
			SetChildrenAlongAxis(1, m_vertical);
		}

		private void OnValidate()
		{
			LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform) transform);
		}
	}
}
