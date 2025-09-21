using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using EditorGUILayout = UnityEditor.EditorGUILayout;

namespace GuiToolkit.Style.Editor
{
	public class UiSkinHSVDialog : EditorWindow
	{
		[Range(0, 1)]
		[SerializeField] private float m_hue = 0;
		[Range(0,2)]
		[SerializeField] private float m_value = 1;
		[Range(0,2)]
		[SerializeField] private float m_saturation = 1;
		
		
		public UiSkin Skin {get; set;}
		public UiStyleConfig StyleConfig {get; set;}
		
		private List<ApplicableValue<Color>> m_colorValues;
		private List<Color> m_colors;
		
		private void OnGUI()
		{
			if (m_colorValues == null)
				CollectColors();
			
			var serializedObject = new SerializedObject(this);
			var hueProp = serializedObject.FindProperty("m_hue");
			var valueProp = serializedObject.FindProperty("m_value");
			var saturationProp = serializedObject.FindProperty("m_saturation");
			EditorGUI.BeginChangeCheck();
			
			if (Skin == null)
				EditorGUILayout.LabelField("Skin is null!");
			else
				EditorGUILayout.LabelField($"Skin: {Skin.Alias}");
				
			EditorGUILayout.PropertyField(hueProp);
			EditorGUILayout.PropertyField(valueProp);
			EditorGUILayout.PropertyField(saturationProp);
			
			if (GUILayout.Button("Reset"))
				Clear(true);
			
			if (EditorGUI.EndChangeCheck())
				Apply();
			
			serializedObject.ApplyModifiedProperties();
		}

		private void OnDisable()
		{
			Clear();
		}

		private void CollectColors()
		{
			if (Skin == null)
			{
				UiLog.LogError("Skin is null!");
				return;
			}

			m_colorValues = new List<ApplicableValue<Color>>();
			m_colors = new List<Color>();
			
			foreach (var style in Skin.Styles)
			{
				foreach (var value in style.Values)
				{
					if (!value.IsApplicable)
						continue;
					
					var valueObj = value.RawValueObj;
					if (valueObj.GetType() != typeof(Color))
						continue;
					
					m_colorValues.Add((ApplicableValue<Color>) value);
					m_colors.Add((Color) value.RawValueObj);
				}
			}
		}

		public void Clear(bool revertColors = false)
		{
			if (revertColors && m_colorValues != null)
			{
				for (int i=0; i<m_colorValues.Count; i++)
					m_colorValues[i].Value = m_colors[i];
				
				EditorApplication.delayCall += () => UiEventDefinitions.EvSkinChanged.InvokeAlways(0);
				
				if (StyleConfig)
					EditorGeneralUtility.SetDirty(StyleConfig);
			}
			
			m_hue = 0;
			m_saturation = 1;
			m_value = 1;
			m_colorValues = null;
			m_colors = null;
		}
		
		private void Apply()
		{
			if (m_colorValues == null)
				return;

			for (int i=0; i<m_colorValues.Count; i++)
			{
				var inColor = m_colors[i];
				Color.RGBToHSV(inColor, out float h, out float s, out float v);
				h = (h + m_hue) % 1.0f;
				s = Mathf.Clamp01(s * m_saturation);
				v = Mathf.Clamp01(v * m_value);
				var outColor = Color.HSVToRGB(h, s, v);
				outColor.a = inColor.a;
				m_colorValues[i].RawValue = outColor;
			}
			
			if (StyleConfig)
				EditorGeneralUtility.SetDirty(StyleConfig);
			EditorApplication.delayCall += () => UiEventDefinitions.EvSkinChanged.InvokeAlways(0);
		}

		public static UiSkinHSVDialog GetWindow()
		{
			var window = GetWindow<UiSkinHSVDialog>();
			
			window.Clear();
			window.titleContent = new GUIContent("HSV");
			window.Focus();
			window.Repaint();
			return window;
		}
	}
}
