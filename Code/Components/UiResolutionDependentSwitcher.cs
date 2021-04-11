using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GuiToolkit
{
	[Serializable]
	public struct ResolutionDependentDefinition
	{
		public Component Target;
		public Component TemplateLandscape;
		public Component TemplatePortrait;
	}

	[RequireComponent(typeof(RectTransform))]
	[ExecuteAlways]
	public class UiResolutionDependentSwitcher : MonoBehaviour
	{
		[SerializeField] protected ResolutionDependentDefinition[] m_definitions = new ResolutionDependentDefinition[0];

		public ResolutionDependentDefinition[] Definitions => m_definitions;

		protected virtual void Start()
		{
			UpdateElements();
		}

		protected virtual void OnRectTransformDimensionsChange()
		{
			UpdateElements();
		}

		protected virtual void UpdateElements()
		{
			bool isLandscape = Screen.width >= Screen.height;

			foreach( var definition in m_definitions )
			{
				definition.Target.CopyFrom(isLandscape ? definition.TemplateLandscape : definition.TemplatePortrait);
			}
		}
	}

#if UNITY_EDITOR
	/// \addtogroup Editor Code
	/// UiMaterialCloner is quite fragile regarding its options and thus needs a special
	/// treatment in the editor
	[CustomEditor(typeof(UiResolutionDependentSwitcher))]
	public class UiResolutionDependentSwitcherEditor : Editor
	{
		protected SerializedProperty m_definitionsProperty;

		private void OnEnable()
		{
			m_definitionsProperty = serializedObject.FindProperty("m_definitions");
		}

		public override void OnInspectorGUI()
		{
			var thisUiResolutionDependentSwitcher = (UiResolutionDependentSwitcher) target;

			EditorGUILayout.PropertyField(m_definitionsProperty, true);

			serializedObject.ApplyModifiedProperties();

			GUILayout.Space(UiEditorUtility.LARGE_SPACE_HEIGHT);

			if (GUILayout.Button("Apply"))
			{
				bool isLandscape = Screen.width >= Screen.height;

				Undo.SetCurrentGroupName("Apply Resolution dependent components");
				foreach (var definition in thisUiResolutionDependentSwitcher.Definitions)
				{
					Component target = isLandscape ? definition.TemplateLandscape : definition.TemplatePortrait;
					Component source = definition.Target;
					if (source == null || target == null)
						continue;

					Undo.RegisterCompleteObjectUndo(target, "");
					target.CopyFrom(source);
				}
				Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
			}
		}
	}
#endif

}