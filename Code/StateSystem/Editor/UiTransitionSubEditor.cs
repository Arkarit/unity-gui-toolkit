#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.UiStateSystem
{
	public static class UiTransitionSubEditor
	{
		// A static transfer variable is disgusting - but we are forced to use it since Unity provides no way
		// to transfer additional data from an editor to a drawer
		public static UiStateMachine StateMachineCurrentlyEdited { get; set; }

		public static bool DisplayTransitionList( UiStateMachine _stateMachine, SerializedProperty _list, bool _showComponents )
		{
			GUILayout.Label("Transitions:", EditorStyles.boldLabel);
			List<string> stateNames = _stateMachine.StateNames;
			bool result = false;
			StateMachineCurrentlyEdited = _stateMachine;

			for (int i = 0; i < _list.arraySize; i++)
			{
				SerializedProperty transitionProp = _list.GetArrayElementAtIndex(i);

				EditorGUILayout.PropertyField(transitionProp, true);
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

	// IngredientDrawer
	[CustomPropertyDrawer(typeof(UiTransition))]
	public class UiTransitionDrawer : PropertyDrawer
	{
		// Draw the property inside the given rect
		public override void OnGUI( Rect _position, SerializedProperty _property, GUIContent _label )
		{
			// If we are drawn by the default inspector, we display the default drawer.
			UiStateMachine thisStateMachine = UiTransitionSubEditor.StateMachineCurrentlyEdited;
			if( thisStateMachine == null)
			{
				EditorGUI.PropertyField(_position, _property, _label, true);
				return;
			}

			// Using BeginProperty / EndProperty on the parent property means that
			// prefab override logic works on the entire property.
			EditorGUI.BeginProperty(_position, _label, _property);

			// Draw label
//			_position = EditorGUI.PrefixLabel(_position, GUIUtility.GetControlID(FocusType.Passive), _label);

			// Don't make child fields be indented
			var indent = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;

			// UiTransition serialized properties
			SerializedProperty stateMachineProp = _property.FindPropertyRelative("m_stateMachine");
			SerializedProperty fromProp = _property.FindPropertyRelative("m_from");
			SerializedProperty toProp = _property.FindPropertyRelative("m_to");
			SerializedProperty durationProp = _property.FindPropertyRelative("m_duration");
			SerializedProperty easeProp = _property.FindPropertyRelative("m_ease");

			stateMachineProp.objectReferenceValue = UiTransitionSubEditor.StateMachineCurrentlyEdited;

			Rect fromRect = new Rect(_position.x, _position.y, _position.width, _position.height);
			Rect toRect = new Rect(_position.x, _position.y + EditorGUIUtility.singleLineHeight, _position.width, _position.height);

			DisplayStateNamePopup(fromRect, "From:", fromProp, thisStateMachine.StateNames, true);
			DisplayStateNamePopup(toRect, "To:", toProp, thisStateMachine.StateNames, false);
/*
			// Calculate rects
			var amountRect = new Rect(_position.x, _position.y, 30, _position.height);
			var unitRect = new Rect(_position.x + 35, _position.y, 50, _position.height);
			var nameRect = new Rect(_position.x + 90, _position.y, _position.width - 90, _position.height);

			// Draw fields - passs GUIContent.none to each so they are drawn without labels
			EditorGUI.PropertyField(amountRect, _property.FindPropertyRelative("amount"), GUIContent.none);
			EditorGUI.PropertyField(unitRect, _property.FindPropertyRelative("unit"), GUIContent.none);
			EditorGUI.PropertyField(nameRect, _property.FindPropertyRelative("name"), GUIContent.none);
*/
			// Set indent back to what it was
			EditorGUI.indentLevel = indent;

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
			_rect.height = EditorGUIUtility.singleLineHeight;
			EditorGUI.PrefixLabel(_rect, new GUIContent( _prefixLabel ));
			_rect.x += EditorGUIUtility.labelWidth;
			_rect.width -= EditorGUIUtility.labelWidth;
			
			selected = EditorGUI.Popup( _rect, selected, options );

Debug.Log(_rect);
/*
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label(_prefixLabel, GUILayout.Width(UiEditorUtility.PREFIX_WIDTH));
			selected = EditorGUILayout.Popup(selected, options, GUILayout.Height(EditorStyles.popup.fixedHeight - 5));
			if (_allowAny && selected == 0)
				_prop.stringValue = "";
			else
				_prop.stringValue = _stateNames[selected - anyOffset];
			EditorGUILayout.EndHorizontal();
*/
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			if( UiTransitionSubEditor.StateMachineCurrentlyEdited == null)
				return EditorGUI.GetPropertyHeight(property);
			return EditorGUIUtility.singleLineHeight * 10;
		}

	}

}

#endif

