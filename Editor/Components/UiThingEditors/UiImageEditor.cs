using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit.Editor
{
	[CustomEditor(typeof(UiImage))]
	public class UiImageEditor : UiThingEditor
	{
		protected SerializedProperty m_imageProp;
		protected SerializedProperty m_gradientSimpleProp;
		protected SerializedProperty m_supportDisabledMaterialProp;
		protected SerializedProperty m_normalMaterialProp;
		protected SerializedProperty m_disabledMaterialProp;

		private static readonly HashSet<string> m_excludedProperties = new()
		{
			"m_supportDisabledMaterial",
			"m_normalMaterial",
			"m_disabledMaterial"
		};

		protected override HashSet<string> excludedProperties => m_excludedProperties;

		protected override void OnEnable()
		{
			base.OnEnable();
			m_imageProp = serializedObject.FindProperty("m_image");
			m_gradientSimpleProp = serializedObject.FindProperty("m_gradientSimple");
			m_supportDisabledMaterialProp = serializedObject.FindProperty("m_supportDisabledMaterial");
			m_normalMaterialProp = serializedObject.FindProperty("m_normalMaterial");
			m_disabledMaterialProp = serializedObject.FindProperty("m_disabledMaterial");
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			UiImage thisImage = (UiImage)target;
			if (m_imageProp.objectReferenceValue != null)
			{
				EditorGUILayout.PropertyField(m_supportDisabledMaterialProp);
				if (m_supportDisabledMaterialProp.boolValue)
				{
					EditorGUILayout.PropertyField(m_normalMaterialProp);
					EditorGUILayout.PropertyField(m_disabledMaterialProp);
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

}