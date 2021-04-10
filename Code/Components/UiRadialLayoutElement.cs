using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit
{
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