using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace GuiToolkit
{
	[RequireComponent(typeof(RectTransform))]
	public class UiLayoutElement : MonoBehaviour
	{
		[SerializeField]
		protected UiLayoutElementTransformPolicy m_width;
		[SerializeField]
		protected UiLayoutElementTransformPolicy m_height;

		private UiLayout m_parentLayout;

		public RectTransform RectTransform => transform as RectTransform;


		public virtual float Width => m_width.GetPreferredSize();
		public virtual float Height => m_height.GetPreferredSize();
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