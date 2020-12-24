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
	public class UiPlayerSettingLanguageToggle : UiPlayerSettingToggle
	{
		[SerializeField]
		private Image m_image;

		[SerializeField]
		private string m_language;

		public string Language
		{
			get => m_language;
			set
			{
				m_language = value;
				SetToggleByLanguage();
				SetNationalFlag();
			}
		}

		public override string Icon
		{
			get => base.Icon;
			set
			{
				base.Icon = value;
				SetNationalFlag();
			}
		}

		protected override void OnEnable()
		{
			base.OnEnable();
			SetToggleByLanguage();
		}

		protected override void OnValueChanged( bool _active )
		{
			if (_active)
				UiMain.Instance.LocaManager.ChangeLanguage(m_language);
		}

#if UNITY_EDITOR
		public void OnValidate()
		{
			SetNationalFlag();
		}
#endif

		private void SetToggleByLanguage()
		{
			bool isActive = UiMain.Instance.LocaManager.Language == m_language;
			m_toggle.SetDelayed(isActive);
		}

		private void SetNationalFlag()
		{
			if (!string.IsNullOrEmpty(Icon))
			{
				m_image.sprite = Resources.Load<Sprite>(Icon);
				if (m_image.sprite != null)
					return;

				Debug.LogError($"Sprite '{Icon}' not found!");
			}
			m_image.sprite = Resources.Load<Sprite>("Flags/" + m_language );
		}
	}

#if UNITY_EDITOR
	[CustomEditor(typeof(UiPlayerSettingLanguageToggle))]
	public class UiSettingsEntryLanguageEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			UiPlayerSettingLanguageToggle thisUiSettingsEntry = (UiPlayerSettingLanguageToggle)target;
			DrawDefaultInspector();
			if ( UiEditorUtility.LanguagePopup("Select available language:", thisUiSettingsEntry.Language, out string newLanguage ))
				thisUiSettingsEntry.Language = newLanguage;
		}
	}
#endif
}