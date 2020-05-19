using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace GuiToolkit.Layout
{
	[RequireComponent(typeof(RectTransform))]
	public class UiLayoutElement : MonoBehaviour
	{
//		[Header("Horizontal")]
		[SerializeField]
		protected UiLayoutElementTransformPolicy m_width;

//		[Header("Vertical")]
		[SerializeField]
		protected UiLayoutElementTransformPolicy m_height;

		private UiLayout m_parentLayout;

		public RectTransform RectTransform => transform as RectTransform;


		public virtual float PreferredWidth => m_width.GetPreferredSize();
		public virtual float PreferredHeight => m_height.GetPreferredSize();
		public virtual bool VisibleInLayout {get; set;}

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