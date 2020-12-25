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
	public abstract class UiButtonBase : UiTextContainer, IPointerDownHandler, IPointerUpHandler
	{
		[Tooltip("Simple animation (optional)")]
		[SerializeField] protected UiSimpleAnimation m_simpleAnimation;
		[Tooltip("Audio source (optional)")]
		[SerializeField] protected AudioSource m_audioSource;
		[Tooltip("Button Image. Mandatory if you want to use the 'Color' property or the 'Enabled' property.")]
		public UiImage m_uiImage;
		public bool m_enabled = true;

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
				OnEnabledChanged(m_enabled);
				if (!m_enabled && m_simpleAnimation)
					m_simpleAnimation.Stop(false);
				if (m_uiImage != null)
					m_uiImage.Enabled = value;
			}
		}

		public Color Color
		{
			get
			{
				if (m_uiImage == null)
					return Color.white;

				return m_uiImage.Color;
			}
			set
			{
				if (m_uiImage == null)
				{
					Debug.LogError("Attempt to set button color, but UI image was not set");
					return;
				}

				m_uiImage.Color = value;
			}
		}

		public void SetSimpleGradientColors(Color _leftOrTop, Color _rightOrBottom)
		{
			if (m_uiImage == null)
			{
				Debug.LogError("Attempt to set simple gradient colors, but simple gradient was not set");
				return;
			}
			m_uiImage.SetSimpleGradientColors(_leftOrTop, _rightOrBottom);
		}

		public (Color leftOrTop, Color rightOrBottom) GetSimpleGradientColors()
		{
			if (m_uiImage == null)
				return (leftOrTop:Color.white, rightOrBottom:Color.white);
			return m_uiImage.GetSimpleGradientColors();
		}

		protected virtual void OnEnabledChanged(bool _enabled) {}

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

#if UNITY_EDITOR
		private void OnValidate()
		{
			OnEnabledChanged(m_enabled);
		}
#endif

	}


#if UNITY_EDITOR
	[CustomEditor(typeof(UiButtonBase))]
	public class UiButtonBaseEditor : UiTextContainerEditor
	{
		protected SerializedProperty m_simpleAnimationProp;
		protected SerializedProperty m_audioSourceProp;
		protected SerializedProperty m_uiImageProp;
		protected SerializedProperty m_enabledProp;

		static private bool m_toolsVisible;

		public virtual void OnEnable()
		{
			m_simpleAnimationProp = serializedObject.FindProperty("m_simpleAnimation");
			m_audioSourceProp = serializedObject.FindProperty("m_audioSource");
			m_uiImageProp = serializedObject.FindProperty("m_uiImage");
			m_enabledProp = serializedObject.FindProperty("m_enabled");
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			UiButtonBase thisButtonBase = (UiButtonBase)target;

			EditorGUILayout.PropertyField(m_uiImageProp);
			EditorGUILayout.PropertyField(m_simpleAnimationProp);
			EditorGUILayout.PropertyField(m_audioSourceProp);

			serializedObject.ApplyModifiedProperties();
		}

	}
#endif

}