using System;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

namespace GuiToolkit
{
	[CustomEditor(typeof(UiOrientationDependentSwitcher))]
	public class UiOrientationDependentSwitcherEditor : Editor
	{
		private const string TemplateParentName = "_orientationTemplates";

		protected SerializedProperty m_definitionsProp;
		protected SerializedProperty m_componentsProp;
		protected SerializedProperty m_autoUpdateOnEnableProp;
		protected SerializedProperty m_visibleInLandscapeProp;
		protected SerializedProperty m_visibleInPortraitProp;

		private void OnEnable()
		{
			m_definitionsProp = serializedObject.FindProperty("m_definitions");
			m_componentsProp = serializedObject.FindProperty("m_components");
			m_autoUpdateOnEnableProp = serializedObject.FindProperty("m_autoUpdateOnEnable");
			m_visibleInLandscapeProp = serializedObject.FindProperty("m_visibleInLandscape");
			m_visibleInPortraitProp = serializedObject.FindProperty("m_visibleInPortrait");
		}

		public override void OnInspectorGUI()
		{
			var thisUiResolutionDependentSwitcher = (UiOrientationDependentSwitcher) target;
			EnsureTemplatesExist(thisUiResolutionDependentSwitcher);

			EditorGUILayout.PropertyField(m_autoUpdateOnEnableProp);
			EditorGUILayout.PropertyField(m_visibleInLandscapeProp, true);
			EditorGUILayout.PropertyField(m_visibleInPortraitProp, true);
			EditorGUILayout.PropertyField(m_definitionsProp, true);
			EditorGUILayout.PropertyField(m_componentsProp, true);

			serializedObject.ApplyModifiedProperties();

			GUILayout.Space(UiEditorUtility.LARGE_SPACE_HEIGHT);

			if (GUILayout.Button("Apply"))
			{
				bool isLandscape = Screen.width >= Screen.height;
				//Debug.Log($"isLandscape: {isLandscape}");

				foreach (var definition in thisUiResolutionDependentSwitcher.Definitions)
				{
					Component target = isLandscape ? definition.DeprecatedTemplateLandscape : definition.DeprecatedTemplatePortrait;
					Component source = definition.Target;
					if (source == null || target == null)
						continue;

					//Debug.Log($"Copy {source} ('{source.transform.GetPath()}') to {target} ('{target.transform.GetPath()}') ");

					Undo.RegisterCompleteObjectUndo(target, "Apply Resolution dependent components");
					target.CopyFrom(source);
				}
				Canvas.ForceUpdateCanvases();
			}
		}

		private void EnsureTemplatesExist( UiOrientationDependentSwitcher _thisUiResolutionDependentSwitcher )
		{
			Transform thisTransform = _thisUiResolutionDependentSwitcher.transform;
			Transform templateParent = thisTransform.Find(TemplateParentName);
			if (templateParent == null)
			{
				GameObject templateParentGo = new GameObject(TemplateParentName);
				templateParent = templateParentGo.transform;

				templateParent.transform.SetParent(thisTransform);
				templateParent.SetAsFirstSibling();

				templateParentGo.SetActive(false);
				LayoutElement le = templateParent.GetOrCreateComponent<LayoutElement>();
				le.ignoreLayout = true;
			}

			foreach (var definition in _thisUiResolutionDependentSwitcher.Definitions)
			{
			}

		}
	}
}