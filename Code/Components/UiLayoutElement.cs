using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GuiToolkit
{
	[Serializable]
	public class SizeProperty
	{
		public enum ExpansionPolicy
		{
			Fixed,
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
		private SizeProperty m_width;
		[SerializeField]
		private SizeProperty m_height;

		private UiLayout m_parentLayout;

		public RectTransform RectTransform => transform as RectTransform;

		public float GetWidth()
		{
			return m_width.GetSize();
		}

		public float GetHeight()
		{
			return m_height.GetSize();
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