using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GuiToolkit
{
	public class UiTextContainer : UiThing
	{
		protected UiTMPTranslator m_translator;
		protected TextMeshProUGUI m_tmpText;
		protected Text m_text;
		protected bool m_initialized = false;
		public string Text
		{
			get
			{
				InitIfNecessary();
				if (m_translator && Application.isPlaying)
					return m_translator.Text;
				if (m_tmpText)
					return m_tmpText.text;
				if (m_text)
					return m_text.text;
				return "";
			}

			set
			{
				if (value == null)
					return;
Debug.Log($"Setting text to '{value}'" );
				InitIfNecessary();
				if (m_translator && Application.isPlaying)
					m_translator.Text = value;
				else if (m_tmpText)
					m_tmpText.text = value;
				else if (m_text)
					m_text.text = value;
				else
					Debug.LogError($"No button text found for Button '{gameObject.name}', can not set string '{value}'");
			}
		}

		public Color TextColor
		{
			get
			{
				InitIfNecessary();
				if (m_tmpText)
					return m_tmpText.color;
				if (m_text)
					return m_text.color;
				return Color.black;
			}

			set
			{
				if (value == null)
					return;

				InitIfNecessary();
				if (m_tmpText)
					m_tmpText.color = value;
				else if (m_text)
					m_text.color = value;
				else
					Debug.LogError($"No button text found for Button '{gameObject.name}', can not set color '{value}'");
			}
		}

		public UnityEngine.Object TextComponent
		{
			get
			{
				InitIfNecessary();
				if (m_translator)
					return m_translator;
				if (m_tmpText)
					return m_tmpText;
				if (m_text)
					return m_text;
				return null;
			}
		}

		protected override void Awake()
		{
			base.Awake();
			InitIfNecessary();
		}

		protected virtual void Init() { }

		protected void InitIfNecessary()
		{
			if (m_initialized)
				return;

			m_translator = GetComponentInChildren<UiTMPTranslator>();
			m_tmpText = GetComponentInChildren<TextMeshProUGUI>();
			m_text = GetComponentInChildren<Text>();

			Init();
		}
	}

#if UNITY_EDITOR
	[CustomEditor(typeof(UiTextContainer))]
	public class UiTextContainerEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			UiTextContainer thisUiTextContainer = (UiTextContainer)target;

			UnityEngine.Object textComponent = thisUiTextContainer.TextComponent;
			if (textComponent != null)
			{
				string text = thisUiTextContainer.Text;
				string newText = EditorGUILayout.TextField("Text:", text);
				if (newText != text)
				{
					Undo.RecordObject(textComponent, "Text change");
					thisUiTextContainer.Text = newText;
				}
			}
			if (textComponent != null)
			{
				Color color = thisUiTextContainer.TextColor;
				Color newColor = EditorGUILayout.ColorField("Text Color:", color);
				if (newColor != color)
				{
					Undo.RecordObject(textComponent, "Text color change");
					thisUiTextContainer.TextColor = newColor;
				}
			}
		}

	}
#endif
}