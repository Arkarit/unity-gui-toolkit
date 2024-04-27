using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	[CustomPropertyDrawer(typeof(ApplicableValueBase), true)]
	public class ApplicableValueBaseDrawer : PropertyDrawer
	{
		private const int ToggleWidth = 25;

		public override void OnGUI( Rect _position, SerializedProperty _property, GUIContent _label )
		{
			EditorGUI.BeginProperty(_position, _label, _property);

			var isApplicableProp = _property.FindPropertyRelative("IsApplicable");
			var valueProp = _property.FindPropertyRelative("Value");

			EditorGUI.BeginChangeCheck();
			var newIsApplicable = EditorGUI.Toggle(new Rect(_position.x, _position.y, ToggleWidth, _position.height), isApplicableProp.boolValue);
			if (EditorGUI.EndChangeCheck())
			{
				isApplicableProp.boolValue = newIsApplicable;
			}

			EditorGUI.LabelField(new Rect(_position.x + ToggleWidth, _position.y, EditorGUIUtility.labelWidth - ToggleWidth, _position.height), _label);

			using (new EditorGUI.DisabledScope(newIsApplicable == false))
			{
				EditorGUI.PropertyField(
					new Rect(_position.x + EditorGUIUtility.labelWidth - 13, _position.y, _position.width - EditorGUIUtility.labelWidth + 13, _position.height), 
					valueProp, 
					new GUIContent()
				);
			}

			EditorGUI.EndProperty();
		}
	}
}