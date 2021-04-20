using UnityEditor;
using UnityEngine;
namespace GuiToolkit
{
	[CustomPropertyDrawer(typeof(OrientationDependentDefinition))]
	public class OrientationDependentDefinitionDrawer : PropertyDrawer
	{
		public override void OnGUI( Rect _position, SerializedProperty _property, GUIContent _label )
		{
			EditorGUI.BeginProperty(_position, _label, _property);
			_position = EditorGUI.PrefixLabel(_position, GUIUtility.GetControlID(FocusType.Passive), new GUIContent(" "));
			var indent = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;
			var targetRect = new Rect(_position.x, _position.y, _position.width, _position.height);
			EditorGUI.PropertyField(targetRect, _property.FindPropertyRelative("Target"), GUIContent.none);
			EditorGUI.indentLevel = indent;
			EditorGUI.EndProperty();
		}
	}
}