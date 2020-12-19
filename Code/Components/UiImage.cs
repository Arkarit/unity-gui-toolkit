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
	public class UiImage : UiThing
	{
		[SerializeField] protected Image m_image;
		[Tooltip("Simple Gradient. Mandatory if you want to use the 'SimpleGradientColors' getters+setters.")]
		[SerializeField] protected UiGradientSimple m_gradientSimple;
		[SerializeField] protected bool m_enabled = true;
		[SerializeField] protected bool m_supportDisabledMaterial = true;
		[SerializeField] protected Material m_normalMaterial;
		[SerializeField] protected Material m_disabledMaterial;
		
		private bool m_initialized = false;

		public Image Image => m_image;

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
			}
		}

		public Color Color
		{
			get
			{
				if (m_image == null)
					return Color.white;

				return m_image.color;
			}
			set
			{
				if (m_image == null)
				{
					Debug.LogError("Attempt to set button color, but background image was not set");
					return;
				}

				m_image.color = value;
			}
		}

		public void SetSimpleGradientColors(Color _leftOrTop, Color _rightOrBottom)
		{
			if (m_gradientSimple == null)
			{
				Debug.LogError("Attempt to set simple gradient colors, but simple gradient was not set");
				return;
			}
			m_gradientSimple.SetColors(_leftOrTop, _rightOrBottom);
		}

		public (Color leftOrTop, Color rightOrBottom) GetSimpleGradientColors()
		{
			if (m_gradientSimple == null)
				return (leftOrTop:Color.white, rightOrBottom:Color.white);
			return m_gradientSimple.GetColors();
		}

		protected override void OnEnable()
		{
			base.OnEnable();
			SetMaterialByEnabled();
		}

		protected virtual void Init() { }
		protected virtual void OnEnabledChanged(bool _enabled) {}

#if UNITY_EDITOR
		private void OnValidate()
		{
			SetMaterialByEnabled();
			OnEnabledChanged(m_enabled);
		}
#endif

		private void SetMaterialByEnabled()
		{
			if (!m_supportDisabledMaterial)
				return;

			if (m_image && m_normalMaterial && m_disabledMaterial)
			{
				m_image.material = m_enabled ? m_normalMaterial : m_disabledMaterial;
			}
		}
	}


#if UNITY_EDITOR
	[CustomEditor(typeof(UiImage))]
	public class UiImageEditor : Editor
	{
		protected SerializedProperty m_imageProp;
		protected SerializedProperty m_gradientSimpleProp;
		protected SerializedProperty m_supportDisabledMaterialProp;
		protected SerializedProperty m_normalMaterialProp;
		protected SerializedProperty m_disabledMaterialProp;
		protected SerializedProperty m_enabledProp;

		static private bool m_toolsVisible;

		public virtual void OnEnable()
		{
			m_imageProp = serializedObject.FindProperty("m_image");
			m_gradientSimpleProp = serializedObject.FindProperty("m_gradientSimple");
			m_supportDisabledMaterialProp = serializedObject.FindProperty("m_supportDisabledMaterial");
			m_normalMaterialProp = serializedObject.FindProperty("m_normalMaterial");
			m_disabledMaterialProp = serializedObject.FindProperty("m_disabledMaterial");
			m_enabledProp = serializedObject.FindProperty("m_enabled");
		}

		public override void OnInspectorGUI()
		{
			UiImage thisImage = (UiImage)target;

			EditorGUILayout.PropertyField(m_gradientSimpleProp);
			EditorGUILayout.PropertyField(m_imageProp);

			serializedObject.ApplyModifiedProperties();

			if (m_imageProp.objectReferenceValue != null)
			{
				EditorGUILayout.PropertyField(m_supportDisabledMaterialProp);
				if (m_supportDisabledMaterialProp.boolValue)
				{
					EditorGUILayout.PropertyField(m_normalMaterialProp);
					EditorGUILayout.PropertyField(m_disabledMaterialProp);
					EditorGUILayout.PropertyField(m_enabledProp);
				}

				Image backgroundImage = (Image) m_imageProp.objectReferenceValue;
				Color color = backgroundImage.color;
				Color newColor = EditorGUILayout.ColorField("Color:", color);
				if (newColor != color)
				{
					Undo.RecordObject(backgroundImage, "Background color change");
					thisImage.Color = newColor;
				}
			}

			if (m_gradientSimpleProp.objectReferenceValue != null)
			{
				UiGradientSimple gradientSimple = (UiGradientSimple) m_gradientSimpleProp.objectReferenceValue;
				var colors = gradientSimple.GetColors();
				Color newColorLeftOrTop = EditorGUILayout.ColorField("Color left or top:", colors.leftOrTop);
				Color newColorRightOrBottom = EditorGUILayout.ColorField("Color right or bottom:", colors.rightOrBottom);
				if (newColorLeftOrTop != colors.leftOrTop || newColorRightOrBottom != colors.rightOrBottom)
				{
					Undo.RecordObject(gradientSimple, "Simple gradient colors change");
					thisImage.SetSimpleGradientColors(newColorLeftOrTop, newColorRightOrBottom);
				}
			}

			serializedObject.ApplyModifiedProperties();
		}

	}
#endif

}