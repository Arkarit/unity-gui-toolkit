using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	public class UiPlayerSettingRadio : UiPlayerSettingBase
	{

		[SerializeField]
		protected UiToggle m_toggle;

		public UiToggle UiToggle => m_toggle;
		public override Toggle Toggle => m_toggle.Toggle; 

		protected string SubKey => m_subKey;

		public override object Value
		{
			get
			{
				if (m_playerSetting == null)
					return false;
				return (string) base.Value == SubKey;
			}
			set
			{
				base.Value = value;
				SetToggleByMatchingSubkey();
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

		protected virtual void OnValueChanged( bool _active )
		{
			if (_active && Initialized)
				Value = SubKey;
		}

		protected void SetToggleByMatchingSubkey()
		{
			m_toggle.Toggle.SetIsOnWithoutNotify((bool) Value);
		}

	}
}