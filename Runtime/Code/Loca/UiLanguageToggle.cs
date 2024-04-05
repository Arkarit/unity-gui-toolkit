using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GuiToolkit
{
	public class UiLanguageToggle : UiToggle
	{
		[SerializeField]
		private Image m_flagImage;

		[SerializeField]
		private string m_languageToken;

		public string Language
		{
			get => m_languageToken;
#if UNITY_EDITOR
			set
			{
				m_languageToken = value;
				OnValidate();
			}
#endif
		}

		protected override void OnEnable()
		{
			base.OnEnable();
			bool isActive = LocaManager.Instance.Language == Language;
			if (isActive)
				SetDelayed(true);

			base.OnValueChanged.AddListener(this.OnValueChanged);
		}

		protected override void OnDisable()
		{
			base.OnDisable();
			base.OnValueChanged.RemoveListener(this.OnValueChanged);
		}

		private void OnValueChanged( bool _active )
		{
			if (_active)
			{
				LocaManager.Instance.ChangeLanguage(m_languageToken);
			}
		}

#if UNITY_EDITOR
		private void OnValidate()
		{
			SetNationalFlag();
		}
#endif
		private void SetNationalFlag()
		{
			m_flagImage.sprite = Resources.Load<Sprite>("Flags/" + m_languageToken );
		}

	}
	#if UNITY_EDITOR
	[CustomEditor(typeof(UiLanguageToggle))]
	public class UiLanguageToggleEditor : UiToggleEditor
	{
		protected SerializedProperty m_flagImageProp;
		protected SerializedProperty m_languageTokenProp;

		public override void OnEnable()
		{
			base.OnEnable();
			m_flagImageProp = serializedObject.FindProperty("m_flagImage");
			m_languageTokenProp = serializedObject.FindProperty("m_languageToken");
		}

		public override void OnInspectorGUI()
		{
			UiLanguageToggle thisUiLanguageToggle = (UiLanguageToggle)target;
			base.OnInspectorGUI();
			EditorGUILayout.PropertyField(m_flagImageProp);
			EditorGUILayout.PropertyField(m_languageTokenProp);
			serializedObject.ApplyModifiedProperties();

			if (EditorUiUtility.LanguagePopup("Select available language:", thisUiLanguageToggle.Language,
				    out string newLanguage))
			{
				thisUiLanguageToggle.Language = newLanguage;
				EditorUtility.SetDirty(thisUiLanguageToggle);
			}
		}
	}
#endif

}