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
		public bool IsRadio => m_toggle.Toggle.group != null;

		public bool Value
		{
			get => m_toggle.Toggle.isOn;
			set => m_toggle.Toggle.isOn = value;
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

		protected virtual void OnValueChanged( bool _val )
		{
			Debug.Log($"TODO: Evaluate toggle '{gameObject.name}': {_val}");
		}

	}
}