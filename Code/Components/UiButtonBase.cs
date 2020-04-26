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
	public abstract class UiButtonBase : UiThing, IPointerDownHandler, IPointerUpHandler
	{
		[Tooltip("Simple animation (optional)")]
		public UiSimpleAnimation m_simpleAnimation;
		[Tooltip("Audio source (optional)")]
		public AudioSource m_audioSource;
		[Tooltip("Background Image. Mandatory if you want to use the 'Color' property or the 'Enabled' property.")]
		public Image m_backgroundImage;
		[Tooltip("Simple Gradient. Mandatory if you want to use the 'SimpleGradientColors' getters+setters.")]
		public UiGradientSimple m_backgroundGradientSimple;
		public bool m_enabled = true;

		public Material m_normalMaterial;
		public Material m_disabledMaterial;
		
		private TextMeshProUGUI m_tmpText;
		private Text m_text;
		private bool m_initialized = false;

		public bool Enabled
		{
			get
			{
				return m_enabled;
			}
			set
			{
				if (m_enabled == value)
					return;
				m_enabled = value;
				SetMaterialByEnabled();
				OnEnabledChanged(m_enabled);
				if (!m_enabled && m_simpleAnimation)
					m_simpleAnimation.Stop(false);
			}
		}

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

		public Color Color
		{
			get
			{
				if (m_backgroundImage == null)
					return Color.white;

				return m_backgroundImage.color;
			}
			set
			{
				if (m_backgroundImage == null)
				{
					Debug.LogError("Attempt to set button color, but background image was not set");
					return;
				}

				m_backgroundImage.color = value;
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

		public void SetSimpleGradientColors(Color _leftOrTop, Color _rightOrBottom)
		{
			if (m_backgroundGradientSimple == null)
			{
				Debug.LogError("Attempt to set simple gradient colors, but simple gradient was not set");
				return;
			}
			m_backgroundGradientSimple.SetColors(_leftOrTop, _rightOrBottom);
		}

		public (Color leftOrTop, Color rightOrBottom) GetSimpleGradientColors()
		{
			if (m_backgroundGradientSimple == null)
				return (leftOrTop:Color.white, rightOrBottom:Color.white);
			return m_backgroundGradientSimple.GetColors();
		}

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

		protected virtual void Init() { }
		protected virtual void OnEnabledChanged(bool _enabled) {}

		protected override void Awake()
		{
			base.Awake();
			InitIfNecessary();
		}

		public virtual void OnPointerDown( PointerEventData eventData )
		{
			if (!m_enabled)
				return;

			if (m_simpleAnimation != null)
				m_simpleAnimation.Play();
			if (m_audioSource != null)
				m_audioSource.Play();
		}

		public virtual void OnPointerUp( PointerEventData eventData )
		{
			if (!m_enabled)
				return;

			if (m_simpleAnimation != null)
				m_simpleAnimation.Play(true);
		}

		protected void InitIfNecessary()
		{
			if (m_initialized)
				return;

			m_tmpText = GetComponentInChildren<TextMeshProUGUI>();
			m_text = GetComponentInChildren<Text>();
			SetMaterialByEnabled();

			Init();
		}

#if UNITY_EDITOR
		private void OnValidate()
		{
			SetMaterialByEnabled();
			OnEnabledChanged(m_enabled);
		}
#endif

		private void SetMaterialByEnabled()
		{
			if (m_backgroundImage && m_normalMaterial && m_disabledMaterial)
			{
				m_backgroundImage.material = m_enabled ? m_normalMaterial : m_disabledMaterial;
			}
		}

	}


#if UNITY_EDITOR
	[CustomEditor(typeof(UiButtonBase))]
	public class UiButtonBaseEditor : Editor
	{
		protected SerializedProperty m_simpleAnimationProp;
		protected SerializedProperty m_audioSourceProp;
		protected SerializedProperty m_backgroundImageProp;
		protected SerializedProperty m_backgroundGradientSimpleProp;
		protected SerializedProperty m_normalMaterialProp;
		protected SerializedProperty m_disabledMaterialProp;
		protected SerializedProperty m_enabledProp;

		static private bool m_toolsVisible;

		public virtual void OnEnable()
		{
			m_simpleAnimationProp = serializedObject.FindProperty("m_simpleAnimation");
			m_audioSourceProp = serializedObject.FindProperty("m_audioSource");
			m_backgroundImageProp = serializedObject.FindProperty("m_backgroundImage");
			m_backgroundGradientSimpleProp = serializedObject.FindProperty("m_backgroundGradientSimple");
			m_normalMaterialProp = serializedObject.FindProperty("m_normalMaterial");
			m_disabledMaterialProp = serializedObject.FindProperty("m_disabledMaterial");
			m_enabledProp = serializedObject.FindProperty("m_enabled");
		}

		public override void OnInspectorGUI()
		{
			UiButtonBase thisButtonBase = (UiButtonBase)target;

			UnityEngine.Object textComponent = thisButtonBase.TextComponent;
			if (textComponent != null)
			{
				string text = thisButtonBase.Text;
				string newText = EditorGUILayout.TextField("Text:", text);
				if (newText != text)
				{
					Undo.RecordObject(textComponent, "Text change");
					thisButtonBase.Text = newText;
				}
			}

			EditorGUILayout.PropertyField(m_backgroundGradientSimpleProp);
			EditorGUILayout.PropertyField(m_backgroundImageProp);

			serializedObject.ApplyModifiedProperties();

			if (m_backgroundImageProp.objectReferenceValue != null)
			{
				EditorGUILayout.PropertyField(m_normalMaterialProp);
				EditorGUILayout.PropertyField(m_disabledMaterialProp);
				EditorGUILayout.PropertyField(m_enabledProp);

				Image backgroundImage = (Image) m_backgroundImageProp.objectReferenceValue;
				Color color = backgroundImage.color;
				Color newColor = EditorGUILayout.ColorField("Color:", color);
				if (newColor != color)
				{
					Undo.RecordObject(backgroundImage, "Background color change");
					thisButtonBase.Color = newColor;
				}
			}

			if (m_backgroundGradientSimpleProp.objectReferenceValue != null)
			{
				UiGradientSimple gradientSimple = (UiGradientSimple) m_backgroundGradientSimpleProp.objectReferenceValue;
				var colors = gradientSimple.GetColors();
				Color newColorLeftOrTop = EditorGUILayout.ColorField("Color left or top:", colors.leftOrTop);
				Color newColorRightOrBottom = EditorGUILayout.ColorField("Color right or bottom:", colors.rightOrBottom);
				if (newColorLeftOrTop != colors.leftOrTop || newColorRightOrBottom != colors.rightOrBottom)
				{
					Undo.RecordObject(gradientSimple, "Simple gradient colors change");
					thisButtonBase.SetSimpleGradientColors(newColorLeftOrTop, newColorRightOrBottom);
				}
			}

			if (textComponent != null)
			{
				Color color = thisButtonBase.TextColor;
				Color newColor = EditorGUILayout.ColorField("Text Color:", color);
				if (newColor != color)
				{
					Undo.RecordObject(textComponent, "Text color change");
					thisButtonBase.TextColor = newColor;
				}
			}

			EditorGUILayout.PropertyField(m_simpleAnimationProp);
			EditorGUILayout.PropertyField(m_audioSourceProp);

			serializedObject.ApplyModifiedProperties();
		}

	}
#endif

}