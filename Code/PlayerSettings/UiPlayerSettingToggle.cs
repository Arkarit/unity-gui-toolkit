using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	public class UiPlayerSettingToggle : UiPlayerSettingBase
	{

		[SerializeField]
		protected UiToggle m_toggle;

		public UiToggle UiToggle => m_toggle;
		public override Toggle Toggle => m_toggle.Toggle; 

		public override object Value
		{
			get => base.Value;
			set
			{
				base.Value = value;
				m_toggle.IsOn = GetValue<bool>();
			}
		}

		protected override void OnEnable()
		{
			base.OnEnable();
			m_toggle.OnValueChanged.AddListener(OnValueChanged);
		}

		protected override void OnDisable()
		{
			m_toggle.OnValueChanged.RemoveListener(OnValueChanged);
			base.OnDisable();
		}

		protected virtual void OnValueChanged( bool _value )
		{
			base.Value = _value;
		}

	}
}