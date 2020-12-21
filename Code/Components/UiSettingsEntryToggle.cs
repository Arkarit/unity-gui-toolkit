using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace GuiToolkit
{
	public class UiSettingsEntryToggle : UiSettingsEntryBase<bool>
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

		protected IEnumerator SetToggleDelayed(bool _value)
		{
			yield return 0;
			m_toggle.Toggle.isOn = _value;
		}

	}
}