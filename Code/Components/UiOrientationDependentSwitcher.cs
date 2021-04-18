using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GuiToolkit
{
	[Serializable]
	public struct OrientationDependentDefinition
	{
		public Component Target;
		public Component TemplateLandscape;
		public Component TemplatePortrait;
	}

	[RequireComponent(typeof(RectTransform))]
	[ExecuteAlways]
	public class UiOrientationDependentSwitcher : UiThing
	{
		[SerializeField] protected OrientationDependentDefinition[] m_definitions = new OrientationDependentDefinition[0];
		[SerializeField] protected bool m_autoUpdateOnEnable = true;

		public OrientationDependentDefinition[] Definitions => m_definitions;

		protected override bool NeedsOnScreenOrientationCallback => true;

		public virtual void UpdateElements()
		{
			if (!enabled)
				return;

			bool isLandscape = Screen.width >= Screen.height;
//Debug.Log($"isLandscape: {isLandscape}");

			foreach( var definition in m_definitions )
			{
				Component source = isLandscape ? definition.TemplateLandscape : definition.TemplatePortrait;
//Debug.Log($"Copy {source} to {definition.Target}");

				definition.Target.CopyFrom(source);
			}
		}

		protected override void OnEnable()
		{
			base.OnEnable();

			if (m_autoUpdateOnEnable)
				UpdateElements();
		}

		protected override void OnScreenOrientationChanged( EScreenOrientation _oldScreenOrientation, EScreenOrientation _newScreenOrientation )
		{
			UpdateElements();
		}

	}

#if UNITY_EDITOR
	/// \addtogroup Editor Code
	/// UiMaterialCloner is quite fragile regarding its options and thus needs a special
	/// treatment in the editor
	[CustomEditor(typeof(UiOrientationDependentSwitcher))]
	public class UiUiOrientationDependentSwitcherEditor : Editor
	{
		protected SerializedProperty m_definitionsProperty;
		protected SerializedProperty m_autoUpdateOnEnableProperty;

		private void OnEnable()
		{
			m_definitionsProperty = serializedObject.FindProperty("m_definitions");
			m_autoUpdateOnEnableProperty = serializedObject.FindProperty("m_autoUpdateOnEnable");
		}

		public override void OnInspectorGUI()
		{
			var thisUiResolutionDependentSwitcher = (UiOrientationDependentSwitcher) target;

			EditorGUILayout.PropertyField(m_autoUpdateOnEnableProperty);
			EditorGUILayout.PropertyField(m_definitionsProperty, true);

			serializedObject.ApplyModifiedProperties();

			GUILayout.Space(UiEditorUtility.LARGE_SPACE_HEIGHT);

			if (GUILayout.Button("Apply"))
			{
				bool isLandscape = Screen.width >= Screen.height;
//Debug.Log($"isLandscape: {isLandscape}");

				foreach (var definition in thisUiResolutionDependentSwitcher.Definitions)
				{
					Component target = isLandscape ? definition.TemplateLandscape : definition.TemplatePortrait;
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
	}
#endif

}