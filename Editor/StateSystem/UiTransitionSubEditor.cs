#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.UiStateSystem.Editor
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

				// UiTransition serialized properties
				SerializedProperty stateMachineProp = transitionProp.FindPropertyRelative("m_stateMachine");
				SerializedProperty fromProp = transitionProp.FindPropertyRelative("m_from");
				SerializedProperty toProp = transitionProp.FindPropertyRelative("m_to");
				SerializedProperty durationProp = transitionProp.FindPropertyRelative("m_duration");
				SerializedProperty delayProp = transitionProp.FindPropertyRelative("m_delay");
				SerializedProperty curveProp = transitionProp.FindPropertyRelative("m_animationCurve");

				// enter the matching state machine
				stateMachineProp.objectReferenceValue = _stateMachine;

				// Display fields
				EditorStyles.popup.fixedHeight = EditorUiUtility.MEDIUM_POPUP_HEIGHT;
				DisplayStateNamePopup("From:", fromProp, ref stateNames, true);
				DisplayStateNamePopup("To:", toProp, ref stateNames, false);
				EditorStyles.popup.fixedHeight = EditorUiUtility.NORMAL_POPUP_HEIGHT;
				EditorUiUtility.PropertyField("Duration", durationProp, true);
				EditorUiUtility.PropertyField("Delay", delayProp, true);
				EditorUiUtility.PropertyField("Curve", curveProp, true);

				EditorUiUtility.Button(" ", "Delete", delegate
				{
					result = true;
					EditorUiUtility.RemoveArrayElementAtIndex(_list, i);
					WorkaroundAnimationCurveRedrawProblem(i, _list);
				});

				GUILayout.Space(-3);
				EditorUiUtility.Button(" ", "Test", delegate
				{
					if (!string.IsNullOrEmpty(fromProp.stringValue))
						_stateMachine.ApplyInstant(fromProp.stringValue);
					_stateMachine.State = toProp.stringValue;
				}, EditorUiUtility.MEDIUM_POPUP_HEIGHT);

				GUILayout.Space(EditorUiUtility.LARGE_SPACE_HEIGHT);
			}


			GUILayout.Space(EditorUiUtility.SMALL_SPACE_HEIGHT);
			EditorUiUtility.Button("New Transition:", "Create", delegate
			{
				int newTransitionIdx = _list.arraySize++;
			});
			EditorUiUtility.Button("All Transitions", "Clear", delegate
			{
				result = true;
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
			GUILayout.Label(_prefixLabel, GUILayout.Width(EditorGUIUtility.labelWidth));
			selected = EditorGUILayout.Popup(selected, options, GUILayout.Height(EditorStyles.popup.fixedHeight - 5));
			if (_allowAny && selected == 0)
				_prop.stringValue = "";
			else
				_prop.stringValue = _stateNames[selected - anyOffset];
			EditorGUILayout.EndHorizontal();
		}

		// Unity has a redraw? cache? problem regarding deleted list elements which contain an animation curve.
		// For unknown reasons, the wrong (deleted) curve is displayed.
		// The usual countermeasures (setting objects dirty etc) didn't help.
		// Cloning the moved curves helps.
		private static void WorkaroundAnimationCurveRedrawProblem( int _idx, SerializedProperty _list )
		{
			for (int i = _idx; i < _list.arraySize; i++)
			{
				SerializedProperty transitionProp = _list.GetArrayElementAtIndex(i);
				SerializedProperty curveProp = transitionProp.FindPropertyRelative("m_animationCurve");

				AnimationCurve curve = curveProp.animationCurveValue;
				if (curve != null)
				{
					AnimationCurve cloned = new AnimationCurve();
					cloned.keys = curve.keys;
					curveProp.animationCurveValue = cloned;
				}
			}
		}

	}

}

#endif

