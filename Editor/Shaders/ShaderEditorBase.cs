using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace GuiToolkit.Editor
{
	/// <summary>
	/// A base class for shader editors.
	/// Simplifies handling of shaders by supporting
	/// - easy displaying of properties
	/// - conditional foldouts
	/// - conditional checkboxes
	/// </summary>
	public abstract class ShaderEditorBase : ShaderGUI
	{
		private const int LARGE_SPACE = 10;
		private const int SMALL_SPACE = 4;

		protected enum CommonBlendModes
		{
			Indeterminate,
			Transparent,
			TransparentPremultiplied,
			Additive,
			AdditiveSoft,
			Multiplicative,
			MultiplicativeX2
		}

		private delegate bool ConditionalDelegate(MaterialProperty _property, bool _isSet, string _label);
		private MaterialEditor m_materialEditor;
		private MaterialProperty[] m_materialProperties;

		protected MaterialEditor MaterialEditor => m_materialEditor;
		protected MaterialProperty[] MaterialProperties => m_materialProperties;
		protected Material Material => m_materialEditor != null ? (Material)m_materialEditor.target : null;
		protected virtual string PropNameSrcBlend => "_SrcBlend";
		protected virtual string PropNameDstBlend => "_DstBlend";

		protected virtual bool DisplayDefaultUi => false;

		protected abstract void OnGUI();

		// Please don't override this.
		// Use OnGUI() without parameters please.
		// (In a real programming language it would of course be possible to degrade
		// the method definition to private, which would make this comment unnecessary)
		public override void OnGUI(MaterialEditor _materialEditor, MaterialProperty[] _properties)
		{
			m_materialEditor = _materialEditor;
			m_materialProperties = _properties;

			OnGUI();

			if (DisplayDefaultUi)
				base.OnGUI(_materialEditor, _properties);

			m_materialEditor = null;
			m_materialProperties = null;
		}
		
		protected void WarnIfImageClamped(string _texturePropertyName)
		{
			var prop = FindProperty(_texturePropertyName, MaterialProperties);
			if (prop == null)
			{
				Debug.LogError($"Unknown texture property '{_texturePropertyName}'");
				return;
			}
			
			var texture = prop.textureValue;
			if (texture == null)
				return;
			
			if (texture.wrapModeU == TextureWrapMode.Clamp || texture.wrapModeV == TextureWrapMode.Clamp)
			{
				EditorGUILayout.HelpBox($"Texture '{texture.name}' mustn't be clamped for the shader to work. Please adjust wrap mode in texture import settings.", MessageType.Error);
			}
		}
		
		protected void GeneralClampWarning()
		{
			EditorGUILayout.HelpBox("Please ensure that the texture used in your graphic component (image etc) is not clamped! (this can not be checked here, so this is a general info box)", MessageType.Info);
			SmallSpace();
		}

		protected void DisplayBlendingHelper()
		{
			var srcBlendProp = FindProperty(PropNameSrcBlend, MaterialProperties);
			BlendMode src = (BlendMode)srcBlendProp.floatValue;
			var dstBlendProp = FindProperty(PropNameDstBlend, MaterialProperties);
			BlendMode dst = (BlendMode)dstBlendProp.floatValue;

			var bm = CommonBlendModes.Indeterminate;

			if (src == BlendMode.SrcAlpha && dst == BlendMode.OneMinusSrcAlpha)
				bm = CommonBlendModes.Transparent;
			else if (src == BlendMode.One && dst == BlendMode.OneMinusSrcAlpha)
				bm = CommonBlendModes.TransparentPremultiplied;
			else if (src == BlendMode.One && dst == BlendMode.One)
				bm = CommonBlendModes.Additive;
			else if (src == BlendMode.OneMinusDstColor && dst == BlendMode.One)
				bm = CommonBlendModes.AdditiveSoft;
			else if (src == BlendMode.DstColor && dst == BlendMode.Zero)
				bm = CommonBlendModes.Multiplicative;
			else if (src == BlendMode.DstColor && dst == BlendMode.SrcColor)
				bm = CommonBlendModes.MultiplicativeX2;

			SmallSpace();
			bm = (CommonBlendModes)EditorGUILayout.EnumPopup("Set Source/Destination by common blend mode:", bm);

			switch (bm)
			{
				default:
				case CommonBlendModes.Indeterminate:
					break;
				case CommonBlendModes.Transparent:
					srcBlendProp.floatValue = (float)BlendMode.SrcAlpha;
					dstBlendProp.floatValue = (float)BlendMode.OneMinusSrcAlpha;
					break;
				case CommonBlendModes.TransparentPremultiplied:
					srcBlendProp.floatValue = (float)BlendMode.One;
					dstBlendProp.floatValue = (float)BlendMode.OneMinusSrcAlpha;
					break;
				case CommonBlendModes.Additive:
					srcBlendProp.floatValue = (float)BlendMode.One;
					dstBlendProp.floatValue = (float)BlendMode.One;
					break;
				case CommonBlendModes.AdditiveSoft:
					srcBlendProp.floatValue = (float)BlendMode.OneMinusDstColor;
					dstBlendProp.floatValue = (float)BlendMode.One;
					break;
				case CommonBlendModes.Multiplicative:
					srcBlendProp.floatValue = (float)BlendMode.DstColor;
					dstBlendProp.floatValue = (float)BlendMode.Zero;
					break;
				case CommonBlendModes.MultiplicativeX2:
					srcBlendProp.floatValue = (float)BlendMode.DstColor;
					dstBlendProp.floatValue = (float)BlendMode.SrcColor;
					break;
			}
		}

		protected void LargeSpace() => EditorGUILayout.Space(LARGE_SPACE);
		protected void SmallSpace() => EditorGUILayout.Space(SMALL_SPACE);
		protected void Line(int height = 1)
		{
			Rect rect = EditorGUILayout.GetControlRect(false, height);
			rect.height = height;
			EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));
		}

		protected MaterialProperty DisplayProperty(string _name, string _toolTip = null)
		{
			var result = FindProperty(_name, MaterialProperties);
			if (result == null)
			{
				Debug.LogError($"Unknown Property '{_name}'");
				return null;
			}

			MaterialEditor.ShaderProperty(result, new GUIContent(result.displayName, _toolTip));
			return result;
		}

		protected void SetKeyword(string _keyword, bool _value)
		{
			if (_value)
			{
				Material.EnableKeyword(_keyword);
				return;
			}

			Material.DisableKeyword(_keyword);
		}

		protected void Foldout(string _name, Action _onDisplayExpanded = null)
		{
			Conditional(_name, _onDisplayExpanded, (property, set, label) =>
			{
				var style = EditorStyles.foldout;
				var oldFontStyle = style.fontStyle;
				style.fontStyle = FontStyle.Bold;
				var result = EditorGUILayout.Foldout(set, label, style);
				style.fontStyle = oldFontStyle;
				return result;
			});
		}

		protected void KeywordToggle(string _name, Action _onToggleChecked = null)
		{
			Conditional(_name, _onToggleChecked, (property, set, label) =>
			{
				MaterialEditor.ShaderProperty(property, label);
				return property.floatValue != 0;
			});
		}

		protected void KeywordToggle(string _name, List<string> _radioStrings, Action _onToggleChecked = null)
		{
			Conditional(_name, _onToggleChecked, (property, set, label) =>
			{
				var oldValue = property.floatValue;
				MaterialEditor.ShaderProperty(property, label);
				bool wasJustSet = oldValue == 0 && property.floatValue != 0;
				if (wasJustSet)
					foreach (var radio in _radioStrings)
						if (radio != _name)
							SwitchOff(radio);

				return property.floatValue != 0;
			});
		}

		private void SwitchOff(string _name)
		{
			var property = FindProperty(_name, MaterialProperties);
			if (property == null)
			{
				Debug.LogError($"Unknown Radio Property '{_name}'");
				return;
			}

			property.floatValue = 0;
			var keywordName = property.name.Substring(1);
			SetKeyword(keywordName, false);
		}

		private void Conditional(string _name, Action _onDisplay, ConditionalDelegate _conditionalDelegate)
		{
			var property = FindProperty(_name, MaterialProperties);
			if (property == null)
			{
				Debug.LogError($"Unknown Foldout Property '{_name}'");
				return;
			}

			var isSet = property.floatValue != 0;
			isSet = _conditionalDelegate(property, isSet, property.displayName);
			property.floatValue = isSet ? 1 : 0;

			if (isSet)
			{
				EditorGUI.indentLevel++;
				_onDisplay?.Invoke();
				EditorGUI.indentLevel--;
			}
		}
	}
}
