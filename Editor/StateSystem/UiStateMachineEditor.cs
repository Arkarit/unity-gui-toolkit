#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit.UiStateSystem.Editor
{

	[CustomEditor(typeof(UiStateMachine))]
	//	[CanEditMultipleObjects]
	public class UiStateMachineEditor : UnityEditor.Editor
	{

		public SerializedProperty m_supportProp;
		public SerializedProperty m_gameObjectsProp;
		public SerializedProperty m_layoutRootsToUpdateProp;
		public SerializedProperty m_stateNamesProp;
		public SerializedProperty m_statesProp;
		public SerializedProperty m_currentStateNameProp;
		public SerializedProperty m_transitionsProp;
		public SerializedProperty m_pristineProp;
		public SerializedProperty m_subStateMachinesProp;
		public SerializedProperty m_autoSetStateOnEnableProp;
		public SerializedProperty m_stateIdxOnEnableProp;
		private static bool s_drawDefaultInspector = false;
		private string m_newStateName;
		private UiStateMachine m_thisStateMachine;

		public void OnEnable()
		{
			m_supportProp = serializedObject.FindProperty("m_propertySupport");
			m_gameObjectsProp = serializedObject.FindProperty("m_gameObjects");
			m_layoutRootsToUpdateProp = serializedObject.FindProperty("m_layoutRootsToUpdate");
			m_stateNamesProp = serializedObject.FindProperty("m_stateNames");
			m_statesProp = serializedObject.FindProperty("m_states");
			m_currentStateNameProp = serializedObject.FindProperty("m_currentStateName");
			m_transitionsProp = serializedObject.FindProperty("m_transitions");
			m_pristineProp = serializedObject.FindProperty("m_pristine");
			m_subStateMachinesProp = serializedObject.FindProperty("m_subStateMachines");
			m_autoSetStateOnEnableProp = serializedObject.FindProperty("m_autoSetStateOnEnable");
			m_stateIdxOnEnableProp = serializedObject.FindProperty("m_stateIdxOnEnable");
		}

		public override void OnInspectorGUI()
		{
			GUILayout.Label("Debug", EditorStyles.boldLabel);
			s_drawDefaultInspector = GUILayout.Toggle(s_drawDefaultInspector, "Show Debug Data");
			GUILayout.Space(EditorUiUtility.LARGE_SPACE_HEIGHT);
			if (s_drawDefaultInspector)
				DrawDefaultInspector();

			m_thisStateMachine = (UiStateMachine)target;

			SetDefaultValuesIfNecessary();

			EditorStyles.popup.fixedHeight = EditorUiUtility.NORMAL_POPUP_HEIGHT;

			GUILayout.Label("General", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(m_autoSetStateOnEnableProp);
			if (m_autoSetStateOnEnableProp.boolValue)
			{
				// TODO: state name popup instead of simple int field
				EditorGUILayout.PropertyField(m_stateIdxOnEnableProp);
			}
			
			GUILayout.Space(EditorUiUtility.LARGE_SPACE_HEIGHT);
			
			DisplaySupportFlags();
			DisplayGameObjects();
			EditorGUILayout.PropertyField(m_layoutRootsToUpdateProp);
			DisplayStates();
			bool exitGui = DisplayTransitionList();

			GUILayout.Space(EditorUiUtility.LARGE_SPACE_HEIGHT);

			TidyUpAndApply(serializedObject, m_thisStateMachine, exitGui);
		}

		private void SetDefaultValuesIfNecessary()
		{
			if (m_pristineProp.boolValue)
			{
				m_pristineProp.boolValue = false;
				if (m_gameObjectsProp.arraySize == 0)
				{
					m_supportProp.longValue = (long)EStatePropertySupport.All;
					FillWithChildren(m_thisStateMachine.gameObject, true, false);
					CreateState("Default");
				}
			}
		}

		private void DisplaySupportFlags()
		{
			int width = (Screen.width - 80) / 3;

			GUILayout.Space(EditorUiUtility.LARGE_SPACE_HEIGHT);
			GUILayout.Label("Support for", EditorStyles.boldLabel);
			EStatePropertySupport support = (EStatePropertySupport)m_supportProp.longValue;

			EditorGUILayout.BeginHorizontal();
			DisplaySupportFlag(EStatePropertySupport.SizePosition, "Size/Position", ref support, width);
			DisplaySupportFlag(EStatePropertySupport.Rotation, "Z Rotation", ref support, width);
			DisplaySupportFlag(EStatePropertySupport.Scale, "Scale", ref support, width);
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal();
			DisplaySupportFlag(EStatePropertySupport.Alpha, "Alpha", ref support, width);
			DisplaySupportFlag(EStatePropertySupport.Interactable, "Interactable", ref support, width);
			DisplaySupportFlag(EStatePropertySupport.Active, "Active", ref support, width);
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal();
			DisplaySupportFlag(EStatePropertySupport.PreferredWidth, "Preferred Width", ref support, width);
			DisplaySupportFlag(EStatePropertySupport.PreferredHeight, "Preferred Height", ref support, width);
			EditorGUILayout.EndHorizontal();

			m_supportProp.longValue = (long)support;
		}

		private void DisplaySupportFlag(EStatePropertySupport _flag, string name, ref EStatePropertySupport _values, int _width)
		{
			bool isSet = (_values & _flag) != 0;
			bool newSet = GUILayout.Toggle(isSet, name, GUILayout.Width(_width));
			if (newSet)
				_values |= _flag;
			else
				_values &= ~_flag;
		}

		private void DisplayGameObjects()
		{
			GUILayout.Space(EditorUiUtility.LARGE_SPACE_HEIGHT);
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

			GUILayout.Space(EditorUiUtility.LARGE_SPACE_HEIGHT);

			EditorGUILayout.PropertyField(m_subStateMachinesProp, new GUIContent("SubStateMachines"), true);

			if (fillWithChildren || fillWithSelf)
			{
				FillWithChildren(m_thisStateMachine.gameObject, fillWithSelf, fillWithChildren);
			}
		}

		private void DisplayClearAllStatesButton()
		{
			EditorUiUtility.Button("All States", "Clear", delegate
			{
				m_currentStateNameProp.stringValue = "";
				m_stateNamesProp.arraySize = 0;
			});
		}

		private void DisplayStates()
		{
			GUILayout.Space(EditorUiUtility.LARGE_SPACE_HEIGHT);
			GUILayout.Label("State selection", EditorStyles.boldLabel);

			DisplayCurrentStatePopup();
			DisplayRecordButton();
			DisplayDeleteStateButton();
			GUILayout.Space(EditorUiUtility.LARGE_SPACE_HEIGHT);
			GUILayout.Label("States", EditorStyles.boldLabel);
			DisplayNewOrRenameStateField();
			GUILayout.Space(EditorUiUtility.SMALL_SPACE_HEIGHT);
			DisplayClearAllStatesButton();
			GUILayout.Space(EditorUiUtility.SMALL_SPACE_HEIGHT);
			DisplayCopyStateValuesPopup();
		}

		private void DisplayDeleteStateButton()
		{
			if (string.IsNullOrEmpty(m_currentStateNameProp.stringValue))
				return;

			EditorUiUtility.Button(" ", "Delete", delegate
			{
				// Note: we only delete the state name here.
				// The matching states are deleted by TidyUpAndApply()
				int selected = m_thisStateMachine.GetStateNameIndex(m_currentStateNameProp.stringValue);
				if (selected < 0)
					return;
				m_stateNamesProp.DeleteArrayElementAtIndex(selected);
				m_currentStateNameProp.stringValue = m_stateNamesProp.arraySize > 0 ? m_stateNamesProp.GetArrayElementAtIndex(0).stringValue : "";
				TidyUpAndApply(serializedObject, m_thisStateMachine);
				m_thisStateMachine.ApplyInstant();
			});
		}

		private void DisplayCurrentStatePopup()
		{
			DisplayStatePopup("Current State", true, (prev, curr) =>
			{
				m_currentStateNameProp.stringValue = m_thisStateMachine.StateNames[curr];
				serializedObject.ApplyModifiedProperties();
				m_thisStateMachine.ApplyInstant();
			});
		}

		private void DisplayCopyStateValuesPopup()
		{
			DisplayStatePopup("Copy values from", false, (prev, curr) =>
			{
				string nameToCopyTo = m_thisStateMachine.StateNames[prev];
				string nameToCopyFrom = m_thisStateMachine.StateNames[curr];
				for (int i=0; i<m_statesProp.arraySize; i++)
				{
					var stateProp = m_statesProp.GetArrayElementAtIndex(i);
					var state = stateProp.boxedValue as UiState;
					if (state == null)
						continue;
					
					if (state.Name != nameToCopyTo)
						continue;
					
					for (int j=0; j<m_thisStateMachine.States.Count; j++)
					{
						var otherState = m_thisStateMachine.States[j];
						if (otherState.Name != nameToCopyFrom)
							continue;
						
						if (otherState.GameObject != state.GameObject)
							continue;
						
						otherState.SetSerializedProperty(stateProp, false);
						break;
					}
				}
				
				serializedObject.ApplyModifiedProperties();
				m_thisStateMachine.ApplyInstant();
			});
		}

		private void DisplayStatePopup(string _label, bool _largeHeight, Action<int,int> onSelect)
		{
			int selected = m_thisStateMachine.GetStateNameIndex(m_currentStateNameProp.stringValue);
			int oldSelected = selected;

			if (selected < 0 && m_stateNamesProp.arraySize > 0)
				selected = 0;

			EditorGUILayout.BeginHorizontal();
			GUILayout.Label(_label, GUILayout.Width(EditorGUIUtility.labelWidth));
			if (selected >= 0)
			{
				int numCurrentStates = m_stateNamesProp.arraySize;
				string[] options = new string[numCurrentStates];
				for (int i = 0; i < numCurrentStates; i++)
				{
					SerializedProperty sp = m_stateNamesProp.GetArrayElementAtIndex(i);
					options[i] = sp.stringValue;
				}

				if (_largeHeight)
					EditorStyles.popup.fixedHeight = EditorUiUtility.LARGE_POPUP_HEIGHT;
				
				selected = EditorGUILayout.Popup(selected, options, GUILayout.Height(EditorUiUtility.LARGE_POPUP_HEIGHT - 5));
				
				if (_largeHeight)
					EditorStyles.popup.fixedHeight = EditorUiUtility.NORMAL_POPUP_HEIGHT;

				if (oldSelected != selected && !Event.current.shift)
					onSelect(oldSelected, selected);
			}
			else
			{
				GUILayout.Label("(No state created yet)", EditorUiUtility.Italic,
					GUILayout.Height(EditorUiUtility.LARGE_POPUP_HEIGHT - 5));
			}

			EditorGUILayout.EndHorizontal();
		}

		private void DisplayRecordButton()
		{
			if (string.IsNullOrEmpty(m_currentStateNameProp.stringValue))
				return;

			EditorGUILayout.BeginHorizontal();
			GUILayout.Label(" ", GUILayout.Width(EditorGUIUtility.labelWidth));
			if (GUILayout.Button("Record", GUILayout.Height(EditorUiUtility.LARGE_BUTTON_HEIGHT)))
			{
				m_thisStateMachine.Record();
			}
			EditorGUILayout.EndHorizontal();
		}

		private void DisplayNewOrRenameStateField()
		{
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("New State(s)", GUILayout.Width(EditorGUIUtility.labelWidth));
			m_newStateName = GUILayout.TextField(m_newStateName);
			EditorGUILayout.EndHorizontal();

			EditorUiUtility.Button(" ", "Create", delegate { CreateState(); });
			EditorUiUtility.Button(" ", "Rename", delegate { RenameState(); });
		}

		private bool DisplayTransitionList()
		{
			GUILayout.Space(EditorUiUtility.LARGE_SPACE_HEIGHT);
			return UiTransitionSubEditor.DisplayTransitionList(m_thisStateMachine, m_transitionsProp, s_drawDefaultInspector);
		}

		private void FillWithChildren(GameObject _gameObject, bool _includeSelf, bool _includeChildren)
		{
			Debug.Assert(_includeSelf || _includeChildren);
			if (!_includeChildren)
			{
				m_gameObjectsProp.arraySize = 1;
				m_gameObjectsProp.GetArrayElementAtIndex(0).objectReferenceValue = _gameObject;
				return;
			}

			HashSet<Transform> exclusionSet = m_thisStateMachine.GetSubStateMachineExclusionSet();

			List<GameObject> children = new List<GameObject>();
			FillWithChildrenRecursive(children, exclusionSet, _gameObject.transform, _includeSelf);

			int numChildren = children.Count;
			m_gameObjectsProp.arraySize = numChildren;
			for (int i = 0; i < numChildren; i++)
				m_gameObjectsProp.GetArrayElementAtIndex(i).objectReferenceValue = children[i];
		}

		private void FillWithChildrenRecursive(List<GameObject> _children, HashSet<Transform> _exclusionList, Transform _transform, bool _include)
		{
			if (_include)
			{
				if (!_exclusionList.Contains(_transform) && _transform is RectTransform)
					_children.Add(_transform.gameObject);
			}

			foreach (Transform child in _transform)
				FillWithChildrenRecursive(_children, _exclusionList, child, true);
		}

		private void CreateState()
		{
			var stateNames = m_newStateName.Split(',');
			if (stateNames.Length > 1)
			{
				for (int i = 0; i < stateNames.Length; i++)
				{
					var state = stateNames[i];
					if (state == null)
						continue;
					
					state = state.Trim();
					if (state == string.Empty)
						continue;
					
					CreateState(state);
				}
			}
			else
			{
				CreateState(m_newStateName);
			}
			
			m_newStateName = "";
		}

		private void RenameState()
		{
			RenameState(m_newStateName);
			m_newStateName = "";
		}

		private void CreateState(string _stateName)
		{
			UiStateMachine thisStateMachine = (UiStateMachine)target;

			if (string.IsNullOrWhiteSpace(_stateName))
			{
				EditorUtility.DisplayDialog("Can not create state", "UIState name is empty", "OK");
				return;
			}

			if (thisStateMachine.StateNames != null)
			{
				foreach (var stateName in thisStateMachine.StateNames)
				{
					if (stateName == _stateName)
					{
						EditorUtility.DisplayDialog("Can not create state", "UIState '" + stateName + "' already exists", "OK");
						return;
					}
				}
			}

			int selected = m_stateNamesProp.arraySize++;
			m_stateNamesProp.GetArrayElementAtIndex(m_stateNamesProp.arraySize - 1).stringValue = _stateName;
			serializedObject.ApplyModifiedProperties();

			m_currentStateNameProp.stringValue = thisStateMachine.GetStateName(selected);
		}

		private void RenameState(string _newStateName)
		{
			UiStateMachine thisStateMachine = (UiStateMachine)target;

			if (string.IsNullOrWhiteSpace(_newStateName))
			{
				EditorUtility.DisplayDialog("Can not rename state", "UIState name is empty", "OK");
				return;
			}

			if (thisStateMachine.StateNames != null)
			{
				foreach (var stateName in thisStateMachine.StateNames)
				{
					if (stateName == _newStateName)
					{
						EditorUtility.DisplayDialog("Can not rename state", "UIState '" + stateName + "' already exists", "OK");
						return;
					}
				}
			}

			var currentStateName = thisStateMachine.State;
			int numStateNames = m_stateNamesProp.arraySize;
			for (int i = 0; i < numStateNames; i++)
			{
				string s = m_stateNamesProp.GetArrayElementAtIndex(i).stringValue;
				if (s == currentStateName)
				{
					m_stateNamesProp.GetArrayElementAtIndex(i).stringValue = _newStateName;
					break;
				}
			}
			
			int numStates = m_statesProp.arraySize;
			for (int i = 0; i < numStates; i++)
			{
				SerializedProperty stateProp = m_statesProp.GetArrayElementAtIndex(i);
				RenamePropIfMatches(stateProp, "m_name", currentStateName, _newStateName);
			}

			int numTransitions = m_transitionsProp.arraySize;
			for (int i = 0; i < numTransitions; i++)
			{
				SerializedProperty transitionProp = m_transitionsProp.GetArrayElementAtIndex(i);
				
				RenamePropIfMatches(transitionProp, "m_from", currentStateName, _newStateName);
				RenamePropIfMatches(transitionProp, "m_to", currentStateName, _newStateName);
			}

			m_currentStateNameProp.stringValue = _newStateName;
		}
		
		private static void RenamePropIfMatches(SerializedProperty prop, string _propName, string _nameToCheck, string _newName)
		{
			SerializedProperty subProp = prop.FindPropertyRelative(_propName);
			string name = subProp.stringValue;
			if (name == _nameToCheck)
				subProp.stringValue = _newName;
		}

		private static void TidyUpAndApply
		(
			SerializedObject _serializedObject, 
			UiStateMachine _stateMachine, 
			bool _exitGui = false
		)
		{
			SerializedProperty gameObjectsProp = _serializedObject.FindProperty("m_gameObjects");
			SerializedProperty stateNamesProp = _serializedObject.FindProperty("m_stateNames");
			SerializedProperty statesProp = _serializedObject.FindProperty("m_states");
			SerializedProperty subStateMachinesProp = _serializedObject.FindProperty("m_subStateMachines");
			
			_exitGui |= RemoveDeletedGameObjects(gameObjectsProp);
			_exitGui |= RemoveObsoleteStates(_stateMachine, stateNamesProp, statesProp);
			UpdateStates(statesProp);
			CreateMissingStates(_serializedObject, _stateMachine, stateNamesProp, statesProp, gameObjectsProp);
			
			int subStateMachineCount = subStateMachinesProp.arraySize;
			for (int i=0; i<subStateMachineCount; i++)
				TidyUpAndApplyRecursive(subStateMachinesProp.GetArrayElementAtIndex(i), ref _exitGui);
			
			_serializedObject.ApplyModifiedProperties();
			if (_exitGui)
				EditorGUIUtility.ExitGUI();
		}

		private static void TidyUpAndApplyRecursive 
		(
			SerializedProperty _stateMachineProp, 
			ref bool _exitGui
		)
		{
			UiStateMachine _stateMachine = (UiStateMachine) _stateMachineProp.objectReferenceValue;
			if (_stateMachine == null)
				return;
			
			SerializedObject serializedObject = new SerializedObject(_stateMachine);
			
			SerializedProperty gameObjectsProp = serializedObject.FindProperty("m_gameObjects");
			SerializedProperty stateNamesProp = serializedObject.FindProperty("m_stateNames");
			SerializedProperty statesProp = serializedObject.FindProperty("m_states");
			SerializedProperty subStateMachinesProp = serializedObject.FindProperty("m_subStateMachines");
			
			_exitGui |= RemoveDeletedGameObjects(gameObjectsProp);
			_exitGui |= RemoveObsoleteStates(_stateMachine, stateNamesProp, statesProp);
			UpdateStates(statesProp);
			CreateMissingStates(serializedObject, _stateMachine, stateNamesProp, statesProp, gameObjectsProp);
			
			int subStateMachineCount = subStateMachinesProp.arraySize;
			for (int i=0; i<subStateMachineCount; i++)
				TidyUpAndApplyRecursive(subStateMachinesProp.GetArrayElementAtIndex(i), ref _exitGui);
			
			serializedObject.ApplyModifiedProperties();
		}

		private static bool RemoveDeletedGameObjects(SerializedProperty _gameObjectsProp)
		{
			bool result = false;
			for (int i = 0; i < _gameObjectsProp.arraySize; i++)
			{
				GameObject go = _gameObjectsProp.GetArrayElementAtIndex(i).objectReferenceValue as GameObject;
				if (go == null)
				{
					EditorGeneralUtility.RemoveArrayElementAtIndex(_gameObjectsProp, i);
					result = true;
				}
			}
			return result;
		}

		private static bool RemoveObsoleteStates
		(
			UiStateMachine _stateMachine, 
			SerializedProperty _stateNamesProp, 
			SerializedProperty _statesProp
		)
		{
			bool result = false;
			HashSet<string> existingStates = new HashSet<string>();

			for (int i = 0; i < _stateNamesProp.arraySize; i++)
				existingStates.Add(_stateNamesProp.GetArrayElementAtIndex(i).stringValue);

			for (int i = 0; i < _statesProp.arraySize; i++)
			{
				SerializedProperty stateProp = _statesProp.GetArrayElementAtIndex(i);
				SerializedProperty stateGoProp = stateProp.FindPropertyRelative("m_gameObject");
				SerializedProperty stateNameProp = stateProp.FindPropertyRelative("m_name");
				var stateGo = stateGoProp.objectReferenceValue as GameObject;
				var stateName = stateNameProp.stringValue;

				if (stateGo == null || !_stateMachine.GameObjects.Contains(stateGo) || !existingStates.Contains(stateName))
				{
					EditorGeneralUtility.RemoveArrayElementAtIndex(_statesProp, i);
					result = true;
				}
			}
			
			return result;
		}

		private static void UpdateStates(SerializedProperty _statesProp)
		{
			for (int i = 0; i < _statesProp.arraySize; i++)
			{
				SerializedProperty stateProp = _statesProp.GetArrayElementAtIndex(i);
				SerializedProperty goProp = stateProp.FindPropertyRelative("m_gameObject");
				SerializedProperty rectTransformProp = stateProp.FindPropertyRelative("m_rectTransform");
				SerializedProperty layoutElementProp = stateProp.FindPropertyRelative("m_layoutElement");
				SerializedProperty canvasGroupProp = stateProp.FindPropertyRelative("m_canvasGroup");

				GameObject go = (GameObject)goProp.objectReferenceValue;
				if (go == null)
					continue;

				rectTransformProp.objectReferenceValue = go.transform as RectTransform;
				layoutElementProp.objectReferenceValue = go.GetComponent<LayoutElement>();
				canvasGroupProp.objectReferenceValue = go.GetComponent<CanvasGroup>();
			}
		}

		private static void CreateMissingStates
		(
			SerializedObject _serializedObject, 
			UiStateMachine _stateMachine, 
			SerializedProperty _stateNamesProp, 
			SerializedProperty _statesProp, 
			SerializedProperty _gameObjectsProp
		)
		{
			if (_statesProp.arraySize == _stateNamesProp.arraySize * _gameObjectsProp.arraySize)
				return;

			UiState[] states = new UiState[_statesProp.arraySize];
			for (int i = 0; i < states.Length; i++)
				states[i] = new UiState(_statesProp.GetArrayElementAtIndex(i));

			GameObject[] gameObjects = new GameObject[_gameObjectsProp.arraySize];
			for (int i = 0; i < gameObjects.Length; i++)
				gameObjects[i] = _gameObjectsProp.GetArrayElementAtIndex(i).objectReferenceValue as GameObject;

			int numStateNames = _stateNamesProp.arraySize;
			for (int i = 0; i < numStateNames; i++)
			{
				string stateName = _stateNamesProp.GetArrayElementAtIndex(i).stringValue;
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
						int newElemIdx = _statesProp.arraySize++;
						UiState newState = new UiState();
						newState.SetBasicValues(stateName, _stateMachine, gameObject);
						_serializedObject.ApplyModifiedProperties();
						newState.Record();
						newState.SetSerializedProperty(_statesProp.GetArrayElementAtIndex(newElemIdx));
					}
				}
			}
		}
	}
}
#endif
