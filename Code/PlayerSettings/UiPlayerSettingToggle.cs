using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace GuiToolkit
{
	public class UiPlayerSettingToggle : UiPlayerSettingBase<bool>
	{

		[SerializeField]
		protected UiToggle m_toggle;

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