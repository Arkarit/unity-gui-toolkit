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
			bool hasPathFieldAttribute = _property.TryGetCustomAttribute(out PathFieldAttribute pathFieldAttribute, true);
			bool hasTooltipAttribute = _property.TryGetCustomAttribute(out TooltipAttribute tooltipAttribute, true);

			bool isFolder = !hasPathFieldAttribute || pathFieldAttribute.IsFolder;
			string relativePath = hasPathFieldAttribute ? pathFieldAttribute.RelativeToPath : null;
			string extensions = hasPathFieldAttribute ? pathFieldAttribute.Extensions : null;

			string tooltip = hasTooltipAttribute ? tooltipAttribute.tooltip : null;
			
			var path = isFolder ?
				EditorFileUtility.PathFieldReadFolder(_rect, ObjectNames.NicifyVariableName(_property.name), pathProp.stringValue, tooltip):
				EditorFileUtility.PathFieldReadFile(_rect, ObjectNames.NicifyVariableName(_property.name), pathProp.stringValue, extensions, tooltip);
			
			if (!string.IsNullOrEmpty(relativePath) && !string.IsNullOrEmpty(path))
				path = Path.GetRelativePath(relativePath, path);
			
			pathProp.stringValue = path;
		}
	}
}
