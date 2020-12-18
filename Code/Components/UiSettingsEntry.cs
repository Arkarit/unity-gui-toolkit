using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace GuiToolkit
{
	public class UiSettingsEntry : UiThing
	{
		[SerializeField]
		protected TMP_Text m_text;

		[SerializeField]
		protected UiToggle m_toggle;

		protected override void OnEnable()
		{
			base.OnEnable();
			m_toggle.OnValueChanged.AddListener(OnValueChanged);
		}

		private void OnValueChanged( bool _val )
		{
			Debug.Log($"TODO: Evaluate toggle '{gameObject.name}': {_val}");
		}

		protected override void OnDisable()
		{
			m_toggle.OnValueChanged.RemoveListener(OnValueChanged);
			base.OnDisable();
		}
	}
}