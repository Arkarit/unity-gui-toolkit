#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.UiStateSystem
{
	public static class UiTransitionSubEditor
	{
		// Static transfer variables are disgusting - but we are forced to use it since Unity provides no way
		// to transfer additional data from an editor to a drawer
		public static UiStateMachine StateMachineCurrentlyEdited { get; set; }
		public static bool DestroyCurrentElement { get; set; }

		public static bool DisplayTransitionList( UiStateMachine _stateMachine, SerializedProperty _list, bool _showComponents )
		{
			GUILayout.Label("Transitions:", EditorStyles.boldLabel);
			List<string> stateNames = _stateMachine.StateNames;
			bool result = false;
			StateMachineCurrentlyEdited = _stateMachine;

			for (int i = 0; i < _list.arraySize; i++)
			{
				SerializedProperty transitionProp = _list.GetArrayElementAtIndex(i);

				DestroyCurrentElement = false;
				EditorGUILayout.PropertyField(transitionProp, true);
				if (DestroyCurrentElement)
				{
					UiEditorUtility.RemoveArrayElementAtIndex(_list, i);
					return true;
				}

				/*
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
				*/
			}


			GUILayout.Space(UiEditorUtility.SMALL_SPACE_HEIGHT);
			UiEditorUtility.Button("New Transition:", "Create", delegate
			{
				result = true;
				_list.arraySize++;
				//				int newTransitionIdx = _list.arraySize++;
				//				_list.GetArrayElementAtIndex(newTransitionIdx).objectReferenceValue = OdinSerializer.SerializedScriptableObject.CreateInstance<UiTransition>();
			});
			UiEditorUtility.Button("All Transitions", "Clear", delegate
			{
				result = true;
				_list.arraySize = 0;
			});

			StateMachineCurrentlyEdited = null;
			return result;
		}

	}

	// IngredientDrawer
	[CustomPropertyDrawer(typeof(UiTransition))]
	public class UiTransitionDrawer : PropertyDrawer
	{
		static private float LINE_HEIGHT = 22;
		static private float SPACING = 15;

		// Draw the property inside the given rect
		public override void OnGUI( Rect _position, SerializedProperty _property, GUIContent _label )
		{
			// If we are drawn by the default inspector, we display the default drawer.
			UiStateMachine thisStateMachine = UiTransitionSubEditor.StateMachineCurrentlyEdited;
			if (thisStateMachine == null)
			{
				EditorGUI.PropertyField(_position, _property, _label, true);
				return;
			}

			// Using BeginProperty / EndProperty on the parent property means that
			// prefab override logic works on the entire property.
			EditorGUI.BeginProperty(_position, _label, _property);

			// UiTransition serialized properties
			SerializedProperty stateMachineProp = _property.FindPropertyRelative("m_stateMachine");
			SerializedProperty fromProp = _property.FindPropertyRelative("m_from");
			SerializedProperty toProp = _property.FindPropertyRelative("m_to");
			SerializedProperty durationProp = _property.FindPropertyRelative("m_duration");
			SerializedProperty animationCurveProp = _property.FindPropertyRelative("m_animationCurve");

			stateMachineProp.objectReferenceValue = UiTransitionSubEditor.StateMachineCurrentlyEdited;

			Rect fromRect = new Rect(_position.x, _position.y, _position.width, LINE_HEIGHT);
			Rect toRect = new Rect(_position.x, _position.y + LINE_HEIGHT, _position.width, LINE_HEIGHT);
			Rect durationRect = new Rect(_position.x, _position.y + LINE_HEIGHT * 2, _position.width, LINE_HEIGHT);
			Rect animationCurveRect = new Rect(_position.x, _position.y + LINE_HEIGHT * 3, _position.width, LINE_HEIGHT);
			float buttonWidth = (_position.width - EditorGUIUtility.labelWidth) / 2;
			Rect deleteButtonRect = new Rect(_position.x + EditorGUIUtility.labelWidth, _position.y + LINE_HEIGHT * 4, buttonWidth, LINE_HEIGHT);
			Rect testButtonRect = new Rect(_position.x + EditorGUIUtility.labelWidth + buttonWidth, _position.y + LINE_HEIGHT * 4, buttonWidth, LINE_HEIGHT);

			DisplayStateNamePopup(fromRect, "From:", fromProp, thisStateMachine.StateNames, true);
			DisplayStateNamePopup(toRect, "To:", toProp, thisStateMachine.StateNames, false);
			EditorGUI.PropertyField(durationRect, durationProp, new GUIContent("Duration:"));
			EditorGUI.PropertyField(animationCurveRect, animationCurveProp, new GUIContent("Curve (norm.):"));
			if (GUI.Button(deleteButtonRect, "Delete"))
			{
				UiTransitionSubEditor.DestroyCurrentElement = true;
			}
			if (GUI.Button(testButtonRect, "Test"))
			{
				if (!string.IsNullOrEmpty(fromProp.stringValue))
					thisStateMachine.ApplyInstant(fromProp.stringValue);
				thisStateMachine.State = toProp.stringValue;
				EditorUpdater.StartUpdating(thisStateMachine);
			}

			EditorGUI.EndProperty();
		}

		private static void DisplayStateNamePopup( Rect _rect, string _prefixLabel, SerializedProperty _prop, List<string> _stateNames, bool _allowAny )
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
			selected = EditorGUI.Popup(_rect, _prefixLabel, selected, options);
			if (_allowAny && selected == 0)
				_prop.stringValue = "";
			else
				_prop.stringValue = _stateNames[selected - anyOffset];
		}

		public override float GetPropertyHeight( SerializedProperty property, GUIContent label )
		{
			if (UiTransitionSubEditor.StateMachineCurrentlyEdited == null)
				return EditorGUI.GetPropertyHeight(property);
			return LINE_HEIGHT * 5 + SPACING;
		}

	}

}

#endif

