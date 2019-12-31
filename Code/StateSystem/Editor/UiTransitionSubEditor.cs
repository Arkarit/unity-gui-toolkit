#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.UiStateSystem
{
	public static class UiTransitionSubEditor
	{

		public static bool DisplayTransitionList( UiStateMachine _stateMachine, SerializedProperty _list, bool _showComponents )
		{
			GUILayout.Label("Transitions:", EditorStyles.boldLabel);
			List<string> stateNames = _stateMachine.StateNames;
			bool result = false;

			for (int i = 0; i < _list.arraySize; i++)
			{
				SerializedProperty transitionProp = _list.GetArrayElementAtIndex(i);
				if (transitionProp.objectReferenceValue == null)
					transitionProp.objectReferenceValue = OdinSerializer.SerializedScriptableObject.CreateInstance<UiTransition>();

				UiTransition transition = transitionProp.objectReferenceValue as UiTransition;
				transition.hideFlags = _showComponents ? HideFlags.None : HideFlags.HideInInspector;

				// Note: transitionProp.FindPropertyRelative simply does not work. 
				// Workaround: Create temporary SerializedObject
				// https://answers.unity.com/questions/543010/odd-behavior-of-findpropertyrelative.html
				SerializedObject serObj = new SerializedObject(transitionProp.objectReferenceValue);


				// UiTransition serialized properties
				SerializedProperty stateMachineProp = serObj.FindProperty("m_stateMachine");
				SerializedProperty fromProp = serObj.FindProperty("m_from");
				SerializedProperty toProp = serObj.FindProperty("m_to");
				SerializedProperty triggerProp = serObj.FindProperty("m_trigger");
				SerializedProperty durationProp = serObj.FindProperty("m_duration");
				SerializedProperty easeProp = serObj.FindProperty("m_ease");

				// enter the matching state machine
				stateMachineProp.objectReferenceValue = _stateMachine;

				// Display fields
				EditorStyles.popup.fixedHeight = UiEditorUtility.MEDIUM_POPUP_HEIGHT;
				DisplayStateNamePopup("From:", fromProp, ref stateNames, true);
				DisplayStateNamePopup("To:", toProp, ref stateNames, false);
				EditorStyles.popup.fixedHeight = UiEditorUtility.NORMAL_POPUP_HEIGHT;
//TODO animation curve
//				EaseBase.DisplayEase(easeProp);
				UiEditorUtility.PropertyField("Duration", durationProp, true);
				// UiEditorUtility.PropertyField("Trigger", triggerProp, true);

				serObj.ApplyModifiedProperties();

				UiEditorUtility.Button(" ", "Delete", delegate
				{
					result = true;
					Undo.DestroyObjectImmediate(transition);
					UiEditorUtility.RemoveArrayElementAtIndex(_list, i);
				});
				GUILayout.Space(-3);
				UiEditorUtility.Button(" ", "Test", delegate
				{
					if (!string.IsNullOrEmpty(fromProp.stringValue))
						_stateMachine.ApplyInstant(fromProp.stringValue);
					_stateMachine.State = toProp.stringValue;
					EditorUpdater.StartUpdating(_stateMachine);
				}, UiEditorUtility.MEDIUM_POPUP_HEIGHT);

				GUILayout.Space(UiEditorUtility.LARGE_SPACE_HEIGHT);
			}


			GUILayout.Space(UiEditorUtility.SMALL_SPACE_HEIGHT);
			UiEditorUtility.Button("New Transition:", "Create", delegate
			{
				int newTransitionIdx = _list.arraySize++;
				_list.GetArrayElementAtIndex(newTransitionIdx).objectReferenceValue = OdinSerializer.SerializedScriptableObject.CreateInstance<UiTransition>();
			});
			UiEditorUtility.Button("All Transitions", "Clear", delegate
			{
				result = true;
				for (int i = 0; i < _list.arraySize; i++)
				{
					UiTransition transition = _list.GetArrayElementAtIndex(i).objectReferenceValue as UiTransition;
					Undo.DestroyObjectImmediate(transition);
				}
				_list.arraySize = 0;
			});

			return result;
		}

		private static void DisplayStateNamePopup( string _prefixLabel, SerializedProperty _prop, ref List<string> _stateNames, bool _allowAny )
		{
			int numStates = _stateNames.Count;
			int anyOffset = _allowAny ? 1 : 0;
			string[] options = new string[numStates + anyOffset];
			if (_allowAny)
				options[0] = "(Any)";

			int selected = 0;

			for (int i = 0; i < numStates; i++)
			{
				options[i + anyOffset] = _stateNames[i];
				if (_stateNames[i] == _prop.stringValue)
					selected = i + anyOffset;
			}

			EditorGUILayout.BeginHorizontal();
			GUILayout.Label(_prefixLabel, GUILayout.Width(UiEditorUtility.PREFIX_WIDTH));
			selected = EditorGUILayout.Popup(selected, options, GUILayout.Height(EditorStyles.popup.fixedHeight - 5));
			if (_allowAny && selected == 0)
				_prop.stringValue = "";
			else
				_prop.stringValue = _stateNames[selected - anyOffset];
			EditorGUILayout.EndHorizontal();
		}

	}

}

#endif

