using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	[CustomPropertyDrawer(typeof(TagField), true)]
	public class TagFieldDrawer : PropertyDrawer
	{
		public override void OnGUI( Rect _rect, SerializedProperty _property, GUIContent _label )
		{
			EditorGUI.BeginProperty(_rect, _label, _property);
			var tagProp = _property.FindPropertyRelative("Tag");
			var tag = tagProp.stringValue;
			tagProp.stringValue = EditorGUI.TagField(_rect,"Tag:", tag);
			EditorGUI.EndProperty();
		}
	}
}
