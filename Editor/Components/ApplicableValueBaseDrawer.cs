using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	[CustomPropertyDrawer(typeof(ApplicableValueBase), true)]
	public class ApplicableValueBaseDrawer : PropertyDrawer
	{
		public override void OnGUI( Rect _position, SerializedProperty _property, GUIContent _label )
		{
			EditorGUI.BeginProperty(_position, _label, _property);
			EditorGUI.LabelField(_position, _label);
			EditorGUI.EndProperty();
		}
	}
}