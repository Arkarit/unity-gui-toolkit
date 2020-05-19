using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace GuiToolkit
{
	[Serializable]
	public class LayoutElementSizeProperty
	{
		public enum SizePolicy
		{
			Fixed,
			Flexible,
			Master,
		}

		[SerializeField]
		private float m_minimumSize;
		[SerializeField]
		[FormerlySerializedAs("m_sizeA")]
		private float m_preferredSize;
		[SerializeField]
		private float m_maximumSize;

		public float GetSize()
		{
			return m_preferredSize;
		}
	}

	[RequireComponent(typeof(RectTransform))]
	public class UiLayoutElement : MonoBehaviour
	{
		[SerializeField]
		protected LayoutElementSizeProperty m_width;
		[SerializeField]
		protected LayoutElementSizeProperty m_height;

		private UiLayout m_parentLayout;

		public RectTransform RectTransform => transform as RectTransform;


		public virtual float Width => m_width.GetSize();
		public virtual float Height => m_height.GetSize();
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