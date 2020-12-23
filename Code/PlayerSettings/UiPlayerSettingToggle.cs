using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace GuiToolkit
{
	public class UiPlayerSettingToggle : UiPlayerSettingBase
	{

		[SerializeField]
		protected UiToggle m_toggle;

		public bool IsRadio => m_toggle.Toggle.group != null;

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