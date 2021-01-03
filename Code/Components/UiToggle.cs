using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

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
				return m_toggle;
			}
		}

		public Toggle.ToggleEvent OnValueChanged => Toggle.onValueChanged;

		public bool IsOn
		{
			get => Toggle.isOn;
			set => Toggle.isOn = value;
		}
		public void SetDelayed(bool _value) => StartCoroutine(SetDelayedCoroutine(_value));

		protected IEnumerator SetDelayedCoroutine(bool _value)
		{
			yield return 0;
			Toggle.isOn = _value;
		}

		protected override void Init()
		{
			base.Init();

			m_toggle = GetComponent<Toggle>();
		}

		protected override void OnEnabledInHierarchyChanged(bool _enabled)
		{
			base.OnEnabledInHierarchyChanged(_enabled);
			InitIfNecessary();
			m_toggle.interactable = _enabled;
		}

		private void OnValidate()
		{
			m_toggle = GetComponent<Toggle>();
		}
	}
#if UNITY_EDITOR
	[CustomEditor(typeof(UiToggle))]
	public class UiToggleEditor : UiButtonBaseEditor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
		}
	}
#endif
}