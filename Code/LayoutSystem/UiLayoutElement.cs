using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace GuiToolkit.Layout
{
	[RequireComponent(typeof(RectTransform))]
	public class UiLayoutElement : MonoBehaviour
	{
//		[Header("Horizontal")]
		[SerializeField]
		protected TransformPolicy m_width;

//		[Header("Vertical")]
		[SerializeField]
		protected TransformPolicy m_height;

		private UiLayout m_parentLayout;

		public RectTransform RectTransform => transform as RectTransform;


		public virtual float PreferredWidth => m_width.PreferredSize;
		public virtual float PreferredHeight => m_height.PreferredSize;
		public virtual TransformPolicy WidthPolicy => m_width;
		public virtual TransformPolicy HeightPolicy => m_height;

		public virtual bool VisibleInLayout {get; set;}

		public TransformPolicy GetTransformPolicy(EAxis2D _axis)
		{
			return _axis == EAxis2D.Horizontal ? m_width : m_height;
		}

		public TransformPolicy GetTransformPolicy(bool _isHorizontal)
		{
			return _isHorizontal ? m_width : m_height;
		}

		private void MakeParentDirty()
		{
			if (m_parentLayout == null)
				return;

			m_parentLayout.SetDirty();
		}

		private void OnEnable()
		{
			if (m_parentLayout == null)
				m_parentLayout = GetComponentInParent<UiLayout>();

			MakeParentDirty();
			return;
		}

		protected void OnDisable()
		{
			MakeParentDirty();
		}

	}
}