using NUnit.Framework.Internal;
using System;
using System.Reflection;
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
			var thisPathField = (PathField) _property.boxedValue;

			TryGetAttribute(_property, out PathFieldAttribute attribute);

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

		public static bool TryGetAttribute<TA>(SerializedProperty _property, out TA _value) 
			where TA : Attribute
		{
			_value = default;
			var obj = _property.serializedObject.targetObject;
			if (obj == null)
				return false;

			FieldInfo field = obj.GetType().GetField(_property.name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
			if (field == null) 
				return false;

			_value = field.GetCustomAttribute<TA>();
			return _value != null;
		}
	}
}
