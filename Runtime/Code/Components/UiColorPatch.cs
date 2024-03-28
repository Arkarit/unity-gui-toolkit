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

		public Action<UiColorPatch> OnSelected;
		public Action<UiColorPatch> OnDeselected;

		public Color Color
		{
			get { return m_color; }
			set
			{
				m_color = value;
				SetValues();
			}
		}

		public Toggle Toggle
		{
			get { return m_toggle; }
		}

		public bool Selected
		{
			get { return m_toggle.isOn; }
			set { m_toggle.isOn = value; }
		}

		protected virtual void Start()
		{
			m_toggle.onValueChanged.AddListener(OnToggleChanged);
			SetValues();
		}

		protected virtual void OnToggleChanged( bool _selected )
		{
			m_selectionFrame.SetActive(_selected);
			if (_selected)
				OnSelected?.Invoke(this);
			else
				OnDeselected?.Invoke(this);
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
