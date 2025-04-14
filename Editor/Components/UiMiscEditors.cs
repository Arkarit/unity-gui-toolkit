using System.Collections.Generic;
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

}