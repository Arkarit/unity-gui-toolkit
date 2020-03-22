using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	public class UiLayout : LayoutGroup
	{
		public enum EFitMode
		{
			Fix,
			ShrinkWrap,
		}

		[SerializeField]
		protected int m_maxHorizontal = Constants.INVALID;

		[SerializeField]
		protected int m_maxVertical = 1;

		[SerializeField]
		protected EFitMode m_selfSizeHorizontal = EFitMode.Fix;

		[SerializeField]
		protected EFitMode m_selfSizeVertical = EFitMode.Fix;

        [SerializeField]
		protected float m_spacing = 0;


        public int MaxHorizontal { get { return m_maxHorizontal; } set { SetProperty(ref m_maxHorizontal, value); } }
        public int MaxVertical { get { return m_maxVertical; } set { SetProperty(ref m_maxVertical, value); } }
        public float Spacing { get { return m_spacing; } set { SetProperty(ref m_spacing, value); } }
        public EFitMode SelfSizeHorizontal { get { return m_selfSizeHorizontal; } set { SetProperty(ref m_selfSizeHorizontal, value); } }
        public EFitMode SelfSizeVertical { get { return m_selfSizeVertical; } set { SetProperty(ref m_selfSizeVertical, value); } }

		public override void CalculateLayoutInputVertical()
		{
		}

		public override void SetLayoutHorizontal()
		{
		}

		public override void SetLayoutVertical()
		{
		}

	}
}