using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit
{
	/// <summary>
	/// A per-child MonoBehaviour companion for UiRadialLayoutGroup that supplies an individual Angle used in the
	/// group's per-element-angle mode, marking the parent layout group dirty when the angle changes.
	/// </summary>
	public class UiRadialLayoutElement : MonoBehaviour
	{
		[SerializeField] protected float m_angle = 10;

		private UiRadialLayoutGroup m_layoutGroup;
		public UiRadialLayoutGroup LayoutGroup
		{
			get
			{
				if (m_layoutGroup == null)
					m_layoutGroup = GetComponentInParent<UiRadialLayoutGroup>();
				return m_layoutGroup;
			}
		}

		public float Angle
		{
			get
			{
				return m_angle;
			}
			set
			{
				m_angle = value;
				SetLayoutGroupDirty();
			}
		}

		private void OnValidate()
		{
			SetLayoutGroupDirty();
		}

		private void SetLayoutGroupDirty()
		{
			var layoutGroup = LayoutGroup;
			if (layoutGroup != null)
				layoutGroup.SetDirty();
		}
	}
}