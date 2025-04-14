using UnityEditor;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

namespace GuiToolkit.Editor
{

	[CustomEditor(typeof(UiTextContainer))]
	public class UiTextContainerEditor : UiThingEditor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

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
		}
	}


	[CustomEditor(typeof(UiTextContainerDisableable))]
	public class UiTextContainerDisableableEditor : UiTextContainerEditor
	{
		protected SerializedProperty m_disabledBrightnessProp;
		protected SerializedProperty m_disabledDesaturationStrengthProp;
		protected SerializedProperty m_disabledAlphaProp;

		protected override void OnEnable()
		{
			base.OnEnable();
			m_disabledBrightnessProp = serializedObject.FindProperty("m_disabledBrightness");
			m_disabledDesaturationStrengthProp = serializedObject.FindProperty("m_disabledDesaturationStrength");
			m_disabledAlphaProp = serializedObject.FindProperty("m_disabledAlpha");
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			UiTextContainerDisableable thisUiTextContainerDisableable = (UiTextContainerDisableable)target;

			EditorGUI.BeginChangeCheck();

			EditorGUILayout.PropertyField(m_disabledAlphaProp);
			EditorGUILayout.PropertyField(m_disabledDesaturationStrengthProp);
			EditorGUILayout.PropertyField(m_disabledBrightnessProp);

			bool changed = EditorGUI.EndChangeCheck();

			serializedObject.ApplyModifiedProperties();

			thisUiTextContainerDisableable.SetColorMembersIfNecessary(changed);
		}
	}


	[CustomEditor(typeof(UiButtonBase))]
	public class UiButtonBaseEditor : UiTextContainerEditor
	{
		protected SerializedProperty m_simpleAnimationProp;
		protected SerializedProperty m_audioSourceProp;
		protected SerializedProperty m_uiImageProp;

		static private bool m_toolsVisible;

		protected override void OnEnable()
		{
			base.OnEnable();
			m_simpleAnimationProp = serializedObject.FindProperty("m_simpleAnimation");
			m_audioSourceProp = serializedObject.FindProperty("m_audioSource");
			m_uiImageProp = serializedObject.FindProperty("m_uiImage");
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

	[CustomEditor(typeof(UiTabChapter))]
	public class UiTabChapterEditor : UiTextContainerEditor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			DrawDefaultInspector();
		}
	}

	/// \addtogroup Editor Code
	/// Ui3DObjectEditor can have several circumstances, under which it is technically impossible
	/// to work. This editor's purpose is to show some warning if these issues occur.
	[CustomEditor(typeof(Ui3DObject))]
	public class Ui3DObjectEditor : UiThingEditor
	{
		protected SerializedProperty m_zSizeProp;
		protected SerializedProperty m_zSizeFactorProp;

		protected override void OnEnable()
		{
			base.OnEnable();
			m_zSizeProp = serializedObject.FindProperty("m_zSize");
			m_zSizeFactorProp = serializedObject.FindProperty("m_zSizeFactor");
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			Ui3DObject thisUi3DObject = (Ui3DObject)target;
			GameObject go = thisUi3DObject.gameObject;
			MaterialCloner materialCloner = go.GetComponent<MaterialCloner>();

			if (materialCloner == null)
				return;

			if (EditorGameObjectUtility.InfoBoxIfPrefab(go))
				return;

			bool error = false;
			Material material = materialCloner.ClonedMaterial;

			if (!material.HasProperty(Ui3DObject.s_propOffset) || !material.HasProperty(Ui3DObject.s_propScale))
			{
				error = true;
				EditorGUILayout.HelpBox("Ui3DObject needs a material with _Offset and _Scale property (for scaling the mesh) support to work.\n" + 
					"You can assign UI_3D.mat (which supports this feature) to the MaterialCloner on this game object.\n" + 
					"Or, you can examine Ui3D.shader how it's done. ", MessageType.Warning);
			}

			if (materialCloner.IsSharedMaterial)
			{
				if (!material.enableInstancing)
				{
					error = true;
					EditorGUILayout.HelpBox("If 'Share Material between instances' is selected in the MaterialCloner script on this game object," + 
						"Ui3DObject needs a material, which has 'GPU Instancing' enabled. Otherwise scaling will not work properly.", MessageType.Warning);
				}
			}

			if (error)
				return;

			EditorGUI.BeginChangeCheck();

			EditorGUILayout.PropertyField(m_zSizeProp);
			EditorGUILayout.PropertyField(m_zSizeFactorProp);

			if (EditorGUI.EndChangeCheck())
			{
				thisUi3DObject.SetDirty();
				serializedObject.ApplyModifiedProperties();
			}
		}
	}

	[CustomEditor(typeof(UiButton))]
	public class UiButtonEditor : UiButtonBaseEditor
	{
		protected SerializedProperty m_simpleWiggleAnimationProp;

		protected override void OnEnable()
		{
			base.OnEnable();
			m_simpleWiggleAnimationProp = serializedObject.FindProperty("m_simpleWiggleAnimation");
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			UiButton thisButton = (UiButton)target;

			EditorGUILayout.PropertyField(m_simpleWiggleAnimationProp);

			serializedObject.ApplyModifiedProperties();
		}
	}

	[CustomEditor(typeof(UiLanguageToggle))]
	public class UiLanguageToggleEditor : UiThingEditor
	{
		protected SerializedProperty m_flagImageProp;
		protected SerializedProperty m_languageTokenProp;

		protected override void OnEnable()
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

			if (EditorLocaUtility.LanguagePopup("Select available language:", thisUiLanguageToggle.Language,
				    out string newLanguage))
			{
				thisUiLanguageToggle.Language = newLanguage;
				EditorUtility.SetDirty(thisUiLanguageToggle);
			}
		}
	}

	[CustomEditor(typeof(UiTab))]
	public class UiTabEditor : UiThingEditor
	{
		protected SerializedProperty m_ensureVisibilityInScrollRectProp;

		protected override void OnEnable()
		{
			base.OnEnable();
			m_ensureVisibilityInScrollRectProp = serializedObject.FindProperty("m_ensureVisibilityInScrollRect");
		}

		public override void OnInspectorGUI()
		{
			EditorGUILayout.PropertyField(m_ensureVisibilityInScrollRectProp);
			base.OnInspectorGUI();
		}
	}

	[CustomEditor(typeof(UiImage))]
	public class UiImageEditor : UiThingEditor
	{
		protected SerializedProperty m_imageProp;
		protected SerializedProperty m_gradientSimpleProp;
		protected SerializedProperty m_supportDisabledMaterialProp;
		protected SerializedProperty m_normalMaterialProp;
		protected SerializedProperty m_disabledMaterialProp;

		static private bool m_toolsVisible;

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