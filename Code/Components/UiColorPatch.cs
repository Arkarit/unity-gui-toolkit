using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{

	[ExecuteAlways]
	public class UiColorPatch : UiThing
	{
		[SerializeField]
		protected Graphic m_graphic;

		[SerializeField]
		protected Color m_color;

		[SerializeField]
		protected Toggle m_toggle;

		[SerializeField]
		protected GameObject m_selectionFrame;

		protected virtual void Start()
		{
			if (m_toggle != null)
				m_toggle.onValueChanged.AddListener(OnToggleChanged);
			SetValues();
		}

		protected virtual void OnToggleChanged( bool _selected )
		{
			m_selectionFrame.SetActive(_selected);
		}

		private void SetValues()
		{
			if (m_graphic != null)
				m_graphic.color = m_color;
		}

#if UNITY_EDITOR
		protected virtual void OnValidate()
		{
			SetValues();
		}
#endif
	}

}
