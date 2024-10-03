using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

namespace GuiToolkit.Editor
{
	[CustomEditor(typeof(UiOrientationDependentSwitcher))]
	public class UiOrientationDependentSwitcherEditor : UnityEditor.Editor
	{
		private const string TemplateParentName = "_orientationTemplates";

		protected SerializedProperty m_definitionsProp;
		protected SerializedProperty m_autoUpdateOnEnableProp;
		protected SerializedProperty m_visibleInLandscapeProp;
		protected SerializedProperty m_visibleInPortraitProp;

		private void OnEnable()
		{
			m_definitionsProp = serializedObject.FindProperty("m_definitions");
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

			serializedObject.ApplyModifiedProperties();

			GUILayout.Space(EditorUiUtility.LARGE_SPACE_HEIGHT);

			if (GUILayout.Button("Apply"))
			{
				EScreenOrientation orientation = UiUtility.GetCurrentScreenOrientation();
				int orientationIdx = (int) orientation;
				//Debug.Log($"orientation: {orientation} UiUtility.ScreenWidth():{UiUtility.ScreenWidth()} UiUtility.ScreenHeight():{UiUtility.ScreenHeight()}");

				foreach (var definition in thisUiResolutionDependentSwitcher.Definitions)
				{
					Component target = definition.OrientationTemplates[orientationIdx];
					Component source = definition.Target;
					if (source == null || target == null)
						continue;

					//Debug.Log($"Copy {source} ('{source.transform.GetPath()}') to {target} ('{target.transform.GetPath()}') ");

					Undo.RegisterCompleteObjectUndo(target, "Apply Resolution dependent components");
					source.CopyTo(target);
				}
				Canvas.ForceUpdateCanvases();
			}
		}

		private void EnsureTemplatesExist( UiOrientationDependentSwitcher _thisUiResolutionDependentSwitcher )
		{
			Transform thisTransform = _thisUiResolutionDependentSwitcher.transform;
			Transform templateParent = CreateHolder(thisTransform, TemplateParentName, 0, true);

			int orientationCount = (int)EScreenOrientation.Count;
			Transform[] subParents = new Transform[orientationCount];
			for (EScreenOrientation screenOrientation = EScreenOrientation.Landscape; screenOrientation < EScreenOrientation.Count; screenOrientation++)
			{
				int idx = (int) screenOrientation;
				string name = idx.ToString();
				subParents[idx] = CreateHolder(templateParent, name, idx);
			}

			foreach (var definition in _thisUiResolutionDependentSwitcher.Definitions)
			{
				if (definition.OrientationTemplates == null)
					definition.OrientationTemplates = new Component[orientationCount];

				if (definition.OrientationTemplates.Length != orientationCount)
				{
					Component[] orientations = new Component[orientationCount];
					for (int i=0; i<orientations.Length && i<definition.OrientationTemplates.Length; i++)
						orientations[i] = definition.OrientationTemplates[i];
					definition.OrientationTemplates = orientations;
				}

				for (int i=0; i<definition.OrientationTemplates.Length; i++)
				{
					if (definition.OrientationTemplates[i] == null || definition.OrientationTemplates[i].GetType() != definition.Target.GetType())
						definition.OrientationTemplates[i] = CreateOrientationTemplate(subParents[i], definition.Target);
				}
			}
		}

		private Component CreateOrientationTemplate( Transform _parent, Component _target )
		{
			string orientationName = _target.gameObject.name + "_" + _target.GetType().ToString();
			//TODO Unique name
			Transform t = CreateHolder(_parent, orientationName);
			Component result = t.GetOrCreateComponent(_target.GetType());
			_target.CopyTo(result);
			return result;
		}

		private Transform CreateHolder(Transform _parent, string _name, int _siblingIndex = -1, bool _addLayoutElement = false )
		{
			Transform result = _parent.Find(_name);
			if (result == null)
			{
				GameObject templateParentGo = new GameObject(_name);
				result = templateParentGo.transform;

				result.transform.SetParent(_parent);
				if (_siblingIndex != -1)
					result.SetSiblingIndex(_siblingIndex);

				templateParentGo.SetActive(false);

				if (_addLayoutElement)
				{
					LayoutElement le = result.GetOrCreateComponent<LayoutElement>();
					le.ignoreLayout = true;
				}
			}

			return result;
		}
	}
}