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
	public class UiButtonBase : UiThing, IPointerDownHandler, IPointerUpHandler
	{
		[Tooltip("Simple animation (optional)")]
		public UiSimpleAnimation m_simpleAnimation;
		[Tooltip("Audio source (optional)")]
		public AudioSource m_audioSource;
		
		private TextMeshProUGUI m_tmpText;
		private Text m_text;
		private bool m_initialized = false;

		public string Text
		{
			get
			{
				InitIfNecessary();
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

				InitIfNecessary();
				if (m_tmpText)
					m_tmpText.text = value;
				else if (m_text)
					m_text.text = value;
				else
					Debug.LogError($"No button text found for Button '{gameObject.name}', can not set string '{value}'");
			}
		}

#if UNITY_EDITOR
		public UnityEngine.Object TextComponent
		{
			get
			{
				InitIfNecessary();
				if (m_tmpText)
					return m_tmpText;
				if (m_text)
					return m_text;
				return null;
			}
		}
#endif

		protected virtual void Init() { }

		protected override void Awake()
		{
			base.Awake();
			InitIfNecessary();
		}

		public virtual void OnPointerDown( PointerEventData eventData )
		{
			if (m_simpleAnimation != null)
				m_simpleAnimation.Play();
			if (m_audioSource != null)
				m_audioSource.Play();
		}

		public virtual void OnPointerUp( PointerEventData eventData )
		{
			if (m_simpleAnimation != null)
				m_simpleAnimation.Play(true);
		}

		protected void InitIfNecessary()
		{
			if (m_initialized)
				return;

			m_tmpText = GetComponentInChildren<TextMeshProUGUI>();
			m_text = GetComponentInChildren<Text>();

			Init();
		}
	}


#if UNITY_EDITOR
	[CustomEditor(typeof(UiButtonBase))]
	public class UiButtonBaseEditor : Editor
	{
		protected SerializedProperty m_simpleAnimationProp;
		protected SerializedProperty m_audioSourceProp;

		static private bool m_toolsVisible;

		public virtual void OnEnable()
		{
			m_simpleAnimationProp = serializedObject.FindProperty("m_simpleAnimation");
			m_audioSourceProp = serializedObject.FindProperty("m_audioSource");
		}

		public override void OnInspectorGUI()
		{
			UiButtonBase thisButtonBase = (UiButtonBase)target;

			string text = thisButtonBase.Text;

			UnityEngine.Object textComponent = thisButtonBase.TextComponent;
			if (textComponent != null)
			{
				string newText = EditorGUILayout.TextField("Text:", text);
				if (newText != text)
				{
					Undo.RecordObject(textComponent, "Text change");
					thisButtonBase.Text = newText;
				}
			}

			EditorGUILayout.PropertyField(m_simpleAnimationProp);
			EditorGUILayout.PropertyField(m_audioSourceProp);

			serializedObject.ApplyModifiedProperties();
		}

	}
#endif

}