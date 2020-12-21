using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GuiToolkit
{
	public class UiSettingsEntryLanguage : UiSettingsEntryToggle
	{
		[SerializeField]
		private Image m_image;

		[SerializeField]
		private string m_language;

		public string Language
		{
			get => m_language;
#if UNITY_EDITOR
			set
			{
				m_language = value;
				OnValidate();
			}
#endif
		}

		protected override void OnEnable()
		{
			base.OnEnable();

			bool isActive = UiMain.LocaManager.Language == m_language;

			StoredValue = isActive;

			if (isActive)
				StartCoroutine(SetToggleDelayed(true));
		}

		protected override void OnValueChanged( bool _active )
		{
			if (_active)
			{
				UiMain.LocaManager.ChangeLanguage(m_language);
				StoredValue = true;
				return;
			}

			StoredValue = false;
		}

#if UNITY_EDITOR
		public void OnValidate()
		{
			UiEditorUtility.SetNationalFlagByLanguage(m_image, m_language);
		}
#endif
	}

#if UNITY_EDITOR
	[CustomEditor(typeof(UiSettingsEntryLanguage))]
	public class UiSettingsEntryLanguageEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			UiSettingsEntryLanguage thisUiSettingsEntry = (UiSettingsEntryLanguage)target;
			DrawDefaultInspector();
			if ( UiEditorUtility.LanguagePopup("Select available language:", thisUiSettingsEntry.Language, out string newLanguage ))
				thisUiSettingsEntry.Language = newLanguage;
		}
	}
#endif
}