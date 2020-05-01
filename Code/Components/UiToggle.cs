using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GuiToolkit
{
	[RequireComponent(typeof(Toggle))]
	public class UiToggle: UiButtonBase
	{
		private Toggle m_toggle;

		public Toggle Toggle
		{
			get
			{
				InitIfNecessary();
				return Toggle;
			}
		}

		public Toggle.ToggleEvent OnValueChanged => Toggle.onValueChanged;

		protected override void Init()
		{
			base.Init();

			m_toggle = GetComponent<Toggle>();
		}

		protected override void OnEnabledChanged(bool _enabled)
		{
			base.OnEnabledChanged(_enabled);
			InitIfNecessary();
			m_toggle.interactable = _enabled;
		}

		private void OnValidate()
		{
			m_toggle = GetComponent<Toggle>();
		}
	}
}