using System;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	[CustomPropertyDrawer(typeof(PathField), true)]
	public class PathFieldDrawer : PropertyDrawer
	{
		public override void OnGUI( Rect _rect, SerializedProperty _property, GUIContent _label )
		{
			var pathProp = _property.FindPropertyRelative("Path");
			var thisPathField = (PathField) pathProp.boxedValue;
			
			PathFieldAttribute attribute = (PathFieldAttribute)Attribute.GetCustomAttribute(typeof(PathField), typeof(PathFieldAttribute));

			bool isFolder = attribute?.IsFolder ?? true;
			bool isRelativeIfPossible = attribute?.IsRelativeIfPossible ?? true;
			
			var path = isFolder ?
				EditorFileUtility.PathFieldReadFolder(_rect, ObjectNames.NicifyVariableName(_property.name), pathProp.stringValue):
				EditorFileUtility.PathFieldReadFile(_rect, ObjectNames.NicifyVariableName(_property.name), pathProp.stringValue);
			
			if (isRelativeIfPossible)
			{
				
			}
			
			pathProp.stringValue = path;
		}
	}
}
