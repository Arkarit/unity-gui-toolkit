using System.IO;
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

			bool hasAttribute = _property.TryGetCustomAttribute(out PathFieldAttribute attribute);

			bool isFolder = !hasAttribute || attribute.IsFolder;
			string relativePath = hasAttribute ? attribute.RelativeToPath : null;
			
			var path = isFolder ?
				EditorFileUtility.PathFieldReadFolder(_rect, ObjectNames.NicifyVariableName(_property.name), pathProp.stringValue):
				EditorFileUtility.PathFieldReadFile(_rect, ObjectNames.NicifyVariableName(_property.name), pathProp.stringValue);
			
			if (!string.IsNullOrEmpty(relativePath) && !string.IsNullOrEmpty(path))
				path = Path.GetRelativePath(relativePath, path);
			
			pathProp.stringValue = path;
		}
	}
}
