using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GuiToolkit
{
	public class UiLanguageToggle : UiToggle
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
			OnValueChanged.AddListener(OnValueChangedListener);
		}

		protected override void OnDisable()
		{
			base.OnDisable();
			OnValueChanged.RemoveListener(OnValueChangedListener);
		}

		private void OnValueChangedListener( bool _active )
		{
			if (_active)
			{
				UiMain.LocaManager.ChangeLanguage(m_language);
			}
		}

#if UNITY_EDITOR
		private void OnValidate()
		{
			UiEditorUtility.SetNationalFlagByLanguage(m_image, m_language);
		}
#endif
	}
	#if UNITY_EDITOR
	[CustomEditor(typeof(UiLanguageToggle))]
	public class UiLanguageToggleEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			UiLanguageToggle thisUiLanguageToggle = (UiLanguageToggle)target;
			DrawDefaultInspector();
			if ( UiEditorUtility.LanguagePopup("Select available language:", thisUiLanguageToggle.Language, out string newLanguage ))
				thisUiLanguageToggle.Language = newLanguage;
		}
	}
#endif

}