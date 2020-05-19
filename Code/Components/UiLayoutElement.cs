using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GuiToolkit
{
	[Serializable]
	public class SizeProperty
	{
		public enum SizePolicy
		{
			Fixed,
			Flexible,
		}

		[SerializeField]
		private float m_sizeA;

		public float GetSize()
		{
			return m_sizeA;
		}
	}

	[RequireComponent(typeof(RectTransform))]
	public class UiLayoutElement : MonoBehaviour
	{
		[SerializeField]
		protected SizeProperty m_width;
		[SerializeField]
		protected SizeProperty m_height;

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