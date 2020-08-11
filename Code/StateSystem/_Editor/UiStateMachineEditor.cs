#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit.UiStateSystem
{

	[CustomEditor(typeof(UiStateMachine))]
	//	[CanEditMultipleObjects]
	public class UiStateMachineEditor : Editor
	{

		public SerializedProperty m_supportProp;
		public SerializedProperty m_gameObjectsProp;
		public SerializedProperty m_stateNamesProp;
		public SerializedProperty m_statesProp;
		public SerializedProperty m_currentStateNameProp;
		public SerializedProperty m_transitionsProp;
		public SerializedProperty m_pristineProp;
		public SerializedProperty m_subStateMachinesProp;
		private static bool s_drawDefaultInspector = false;
		private string m_newStateName;

		public void OnEnable()
		{
			m_supportProp = serializedObject.FindProperty("m_propertySupport");
			m_gameObjectsProp = serializedObject.FindProperty("m_gameObjects");
			m_stateNamesProp = serializedObject.FindProperty("m_stateNames");
			m_statesProp = serializedObject.FindProperty("m_states");
			m_currentStateNameProp = serializedObject.FindProperty("m_currentStateName");
			m_transitionsProp = serializedObject.FindProperty("m_transitions");
			m_pristineProp = serializedObject.FindProperty("m_pristine");
			m_subStateMachinesProp = serializedObject.FindProperty("m_subStateMachines");
		}

		public override void OnInspectorGUI()
		{
			s_drawDefaultInspector = GUILayout.Toggle(s_drawDefaultInspector, "Show Debug Data");
			if (s_drawDefaultInspector)
				DrawDefaultInspector();

			UiStateMachine thisStateMachine = (UiStateMachine)target;

			SetDefaultValuesIfNecessary(thisStateMachine);

			EditorStyles.popup.fixedHeight = UiEditorUtility.NORMAL_POPUP_HEIGHT;

			DisplaySupportFlags();
			DisplayGameObjects(thisStateMachine);
			DisplayStates(thisStateMachine);
			bool exitGui = DisplayTransitionList(thisStateMachine);

			GUILayout.Space(UiEditorUtility.LARGE_SPACE_HEIGHT);

			TidyUpAndApply(exitGui);
		}

		private void SetDefaultValuesIfNecessary( UiStateMachine _stateMachine )
		{
			if (m_pristineProp.boolValue)
			{
				m_pristineProp.boolValue = false;
				if (m_gameObjectsProp.arraySize == 0)
				{
					m_supportProp.longValue = (long)EStatePropertySupport.All;
					FillWithChildren(_stateMachine, _stateMachine.gameObject, true, false);
					CreateState("Default");
				}
			}
		}

		private void DisplaySupportFlags()
		{
			int width = (Screen.width - 80) / 3;

			GUILayout.Space(UiEditorUtility.LARGE_SPACE_HEIGHT);
			GUILayout.Label("Support for", EditorStyles.boldLabel);
			EStatePropertySupport support = (EStatePropertySupport)m_supportProp.longValue;

			EditorGUILayout.BeginHorizontal();
			DisplaySupportFlag(EStatePropertySupport.SizePosition, "Size/Position", ref support, width);
			DisplaySupportFlag(EStatePropertySupport.Rotation, "Z Rotation", ref support, width);
			DisplaySupportFlag(EStatePropertySupport.Scale, "Scale", ref support, width);
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal();
			DisplaySupportFlag(EStatePropertySupport.Alpha, "Alpha", ref support, width);
			DisplaySupportFlag(EStatePropertySupport.Active, "Active", ref support, width);
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal();
			DisplaySupportFlag(EStatePropertySupport.PreferredWidth, "Preferred Width", ref support, width);
			DisplaySupportFlag(EStatePropertySupport.PreferredHeight, "Preferred Height", ref support, width);
			EditorGUILayout.EndHorizontal();

			m_supportProp.longValue = (long)support;
		}

		private void DisplaySupportFlag( EStatePropertySupport _flag, string name, ref EStatePropertySupport _values, int _width )
		{
			bool isSet = (_values & _flag) != 0;
			bool newSet = GUILayout.Toggle(isSet, name, GUILayout.Width(_width));
			if (newSet)
				_values |= _flag;
			else
				_values &= ~_flag;
		}

		private void DisplayGameObjects( UiStateMachine _stateMachine )
		{
			GUILayout.Space(UiEditorUtility.LARGE_SPACE_HEIGHT);
			GUILayout.Label("State Game Objects", EditorStyles.boldLabel);

			bool fillWithChildren = false;
			bool fillWithSelf = false;

			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("Fill", GUILayout.Width(EditorGUIUtility.labelWidth));
			if (GUILayout.Button("Self"))
				fillWithSelf = true;
			if (GUILayout.Button("Children"))
				fillWithChildren = true;
			if (GUILayout.Button("Self+Children"))
				fillWithSelf = fillWithChildren = true;
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.PropertyField(m_gameObjectsProp, new GUIContent("Game Objects"), true);

			GUILayout.Space(UiEditorUtility.LARGE_SPACE_HEIGHT);

			EditorGUILayout.PropertyField(m_subStateMachinesProp, new GUIContent("SubStateMachines"), true);

			if (fillWithChildren || fillWithSelf)
			{
				FillWithChildren(_stateMachine, _stateMachine.gameObject, fillWithSelf, fillWithChildren);
			}
		}

		private void DisplayClearAllStatesButton()
		{
			UiEditorUtility.Button("All States", "Clear", delegate
			{
				m_currentStateNameProp.stringValue = "";
				m_stateNamesProp.arraySize = 0;
			});
		}

		private void DisplayStates( UiStateMachine _stateMachine )
		{
			GUILayout.Space(UiEditorUtility.LARGE_SPACE_HEIGHT);
			GUILayout.Label("State selection", EditorStyles.boldLabel);

			DisplayCurrentStatePopup(_stateMachine);
			DisplayRecordButton(_stateMachine);
			DisplayDeleteStateButton(_stateMachine);
			GUILayout.Space(UiEditorUtility.LARGE_SPACE_HEIGHT);
			GUILayout.Label("States", EditorStyles.boldLabel);
			DisplayNewStateField();
			GUILayout.Space(UiEditorUtility.SMALL_SPACE_HEIGHT);
			DisplayClearAllStatesButton();
		}

		private void DisplayDeleteStateButton( UiStateMachine _stateMachine )
		{
			if (string.IsNullOrEmpty(m_currentStateNameProp.stringValue))
				return;

			UiEditorUtility.Button(" ", "Delete", delegate
			{
				// Note: we only delete the state name here.
				// The matching states are deleted by TidyUpAndApply()
				int selected = _stateMachine.GetStateNameIndex(m_currentStateNameProp.stringValue);
				if (selected < 0)
					return;
				m_stateNamesProp.DeleteArrayElementAtIndex(selected);
				m_currentStateNameProp.stringValue = m_stateNamesProp.arraySize > 0 ? m_stateNamesProp.GetArrayElementAtIndex(0).stringValue : "";
				TidyUpAndApply();
				_stateMachine.ApplyInstant();
			});
		}

		private void DisplayCurrentStatePopup( UiStateMachine _stateMachine )
		{
			int selected = _stateMachine.GetStateNameIndex(m_currentStateNameProp.stringValue);
			int oldSelected = selected;

			if (selected < 0 && m_stateNamesProp.arraySize > 0)
				selected = 0;

			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("Current State", GUILayout.Width(EditorGUIUtility.labelWidth));
			if (selected >= 0)
			{
				int numCurrentStates = m_stateNamesProp.arraySize;
				string[] options = new string[numCurrentStates];
				for (int i = 0; i < numCurrentStates; i++)
				{
					SerializedProperty sp = m_stateNamesProp.GetArrayElementAtIndex(i);
					options[i] = sp.stringValue;
				}
				EditorStyles.popup.fixedHeight = UiEditorUtility.LARGE_POPUP_HEIGHT;
				selected = EditorGUILayout.Popup(selected, options, GUILayout.Height(UiEditorUtility.LARGE_POPUP_HEIGHT - 5));
				EditorStyles.popup.fixedHeight = UiEditorUtility.NORMAL_POPUP_HEIGHT;

				m_currentStateNameProp.stringValue = _stateMachine.StateNames[selected];
				if (oldSelected != selected && !Event.current.shift)
				{
					serializedObject.ApplyModifiedProperties();
					_stateMachine.ApplyInstant();
				}
			}
			else
			{
				GUILayout.Label("(No state created yet)", UiEditorUtility.Italic, GUILayout.Height(UiEditorUtility.LARGE_POPUP_HEIGHT - 5));
			}
			EditorGUILayout.EndHorizontal();
		}

		private void DisplayRecordButton( UiStateMachine _stateMachine )
		{
			if (string.IsNullOrEmpty(m_currentStateNameProp.stringValue))
				return;

			EditorGUILayout.BeginHorizontal();
			GUILayout.Label(" ", GUILayout.Width(EditorGUIUtility.labelWidth));
			if (GUILayout.Button("Record", GUILayout.Height(UiEditorUtility.LARGE_BUTTON_HEIGHT)))
			{
				_stateMachine.Record();
			}
			EditorGUILayout.EndHorizontal();
		}

		private void DisplayNewStateField()
		{
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("New State", GUILayout.Width(EditorGUIUtility.labelWidth));
			m_newStateName = GUILayout.TextField(m_newStateName);
			EditorGUILayout.EndHorizontal();

			UiEditorUtility.Button(" ", "Create", delegate { CreateState(); });
		}


		private bool DisplayTransitionList( UiStateMachine _stateMachine )
		{
			GUILayout.Space(UiEditorUtility.LARGE_SPACE_HEIGHT);
			return UiTransitionSubEditor.DisplayTransitionList(_stateMachine, m_transitionsProp, s_drawDefaultInspector);
		}

		private void FillWithChildren( UiStateMachine _stateMachine, GameObject _gameObject, bool _includeSelf, bool _includeChildren )
		{
			Debug.Assert(_includeSelf || _includeChildren);
			if (!_includeChildren)
			{
				m_gameObjectsProp.arraySize = 1;
				m_gameObjectsProp.GetArrayElementAtIndex(0).objectReferenceValue = _gameObject;
				return;
			}

			HashSet<Transform> exclusionSet = _stateMachine.GetSubStateMachineExclusionSet();

			List<GameObject> children = new List<GameObject>();
			FillWithChildrenRecursive(children, exclusionSet, _gameObject.transform, _includeSelf);

			int numChildren = children.Count;
			m_gameObjectsProp.arraySize = numChildren;
			for (int i = 0; i < numChildren; i++)
				m_gameObjectsProp.GetArrayElementAtIndex(i).objectReferenceValue = children[i];
		}

		private void FillWithChildrenRecursive( List<GameObject> _children, HashSet<Transform> _exclusionList, Transform _transform, bool _include )
		{

			if (_include)
			{
				if (!_exclusionList.Contains( _transform ))
					_children.Add(_transform.gameObject);
			}

			foreach( Transform child in _transform)
				FillWithChildrenRecursive(_children, _exclusionList, child, true );
		}

		private void CreateState()
		{
			CreateState(m_newStateName);
			m_newStateName = "";
		}

		private void CreateState( string _stateName )
		{
			UiStateMachine thisStateMachine = (UiStateMachine)target;

			if (string.IsNullOrWhiteSpace(_stateName))
			{
				EditorUtility.DisplayDialog("Can not create state", "UiState name is empty", "OK");
				return;
			}

			if (thisStateMachine.StateNames != null)
			{
				foreach (var stateName in thisStateMachine.StateNames)
				{
					if (stateName == _stateName)
					{
						EditorUtility.DisplayDialog("Can not create state", "UiState '" + stateName + "' already exists", "OK");
						return;
					}
				}
			}

			int selected = m_stateNamesProp.arraySize++;
			m_stateNamesProp.GetArrayElementAtIndex(m_stateNamesProp.arraySize - 1).stringValue = _stateName;
			serializedObject.ApplyModifiedProperties();

			m_currentStateNameProp.stringValue = thisStateMachine.GetStateName(selected);
		}

		private void TidyUpAndApply(bool _exitGui = false)
		{
			_exitGui |= RemoveDeletedGameObjects();
			_exitGui |= RemoveObsoleteStates();
			UpdateStates();
			CreateMissingStates();
			serializedObject.ApplyModifiedProperties();
			if (_exitGui)
				EditorGUIUtility.ExitGUI();
		}

		private bool RemoveDeletedGameObjects()
		{
			bool result = false;
			for (int i = 0; i < m_gameObjectsProp.arraySize; i++)
			{
				GameObject go = m_gameObjectsProp.GetArrayElementAtIndex(i).objectReferenceValue as GameObject;
				if (go == null)
				{
					UiEditorUtility.RemoveArrayElementAtIndex(m_gameObjectsProp, i);
					result = true;
				}
			}
			return result;
		}

		private bool RemoveObsoleteStates()
		{
			bool result = false;
			HashSet<string> existingStates = new HashSet<string>();

			for (int i = 0; i < m_stateNamesProp.arraySize; i++)
				existingStates.Add(m_stateNamesProp.GetArrayElementAtIndex(i).stringValue);

			for (int i = 0; i < m_statesProp.arraySize; i++)
			{
				SerializedProperty stateProp = m_statesProp.GetArrayElementAtIndex(i);
				SerializedProperty goProp = stateProp.FindPropertyRelative("m_gameObject");
				SerializedProperty nameProp = stateProp.FindPropertyRelative("m_name");

				if (goProp.objectReferenceValue == null || !existingStates.Contains(nameProp.stringValue))
				{
					UiEditorUtility.RemoveArrayElementAtIndex(m_statesProp, i);
					result = true;
				}
			}
			return result;
		}

		private void UpdateStates()
		{
			for (int i = 0; i < m_statesProp.arraySize; i++)
			{
				SerializedProperty stateProp = m_statesProp.GetArrayElementAtIndex(i);
				SerializedProperty goProp = stateProp.FindPropertyRelative("m_gameObject");
				SerializedProperty rectTransformProp = stateProp.FindPropertyRelative("m_rectTransform");
				SerializedProperty layoutElementProp = stateProp.FindPropertyRelative("m_layoutElement");
				SerializedProperty canvasGroupProp = stateProp.FindPropertyRelative("m_canvasGroup");

				GameObject go = (GameObject) goProp.objectReferenceValue;
				if (go == null)
					continue;

				rectTransformProp.objectReferenceValue = go.transform as RectTransform;
				layoutElementProp.objectReferenceValue = go.GetComponent<LayoutElement>();
				canvasGroupProp.objectReferenceValue = go.GetComponent<CanvasGroup>();
			}
		}


		private void CreateMissingStates()
		{
			if (m_statesProp.arraySize == m_stateNamesProp.arraySize * m_gameObjectsProp.arraySize)
				return;

			UiStateMachine thisStateMachine = (UiStateMachine)target;


			UiState[] states = new UiState[m_statesProp.arraySize];
			for (int i = 0; i < states.Length; i++)
				states[i] = new UiState( m_statesProp.GetArrayElementAtIndex(i));

			GameObject[] gameObjects = new GameObject[m_gameObjectsProp.arraySize];
			for (int i = 0; i < gameObjects.Length; i++)
				gameObjects[i] = m_gameObjectsProp.GetArrayElementAtIndex(i).objectReferenceValue as GameObject;

			int numStateNames = m_stateNamesProp.arraySize;
			for (int i = 0; i < numStateNames; i++)
			{
				string stateName = m_stateNamesProp.GetArrayElementAtIndex(i).stringValue;
				foreach (var gameObject in gameObjects)
				{
					bool found = false;
					foreach (var state in states)
					{
						if (state.Name == stateName && state.GameObject == gameObject)
						{
							found = true;
							break;
						}
					}

					if (!found)
					{
						int newElemIdx = m_statesProp.arraySize++;
						UiState newState = new UiState();
						newState.SetBasicValues(stateName, thisStateMachine, gameObject);
						serializedObject.ApplyModifiedProperties();
						newState.Record();
						newState.SetSerializedProperty(m_statesProp.GetArrayElementAtIndex(newElemIdx));
					}
				}
			}
		}

	}
}

#endif //UNITY_EDITOR
