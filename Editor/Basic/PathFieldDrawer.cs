using System.IO;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	[CustomPropertyDrawer(typeof(PathField), true)]
	public class PathFieldDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect _rect, SerializedProperty _property, GUIContent _label)
		{
			var pathProp = _property.FindPropertyRelative("m_path");

			bool hasPathFieldAttribute = _property.TryGetCustomAttribute(out PathFieldAttribute pathFieldAttr, true);
			bool hasTooltipAttribute   = _property.TryGetCustomAttribute(out TooltipAttribute tooltipAttr, true);

			bool   isFolder      = !hasPathFieldAttribute || pathFieldAttr.IsFolder;
			string relativeTo    = hasPathFieldAttribute ? pathFieldAttr.RelativeToPath : null;
			string extensionsCsv = hasPathFieldAttribute ? pathFieldAttr.Extensions     : null;
			string tooltip       = hasTooltipAttribute   ? tooltipAttr.tooltip          : null;

			// Draw the regular path UI -----------------------------------------------------
			string path = isFolder
				? EditorFileUtility.PathFieldReadFolder(_rect, ObjectNames.NicifyVariableName(_property.name),
				                                        pathProp.stringValue, tooltip)
				: EditorFileUtility.PathFieldReadFile(_rect, ObjectNames.NicifyVariableName(_property.name),
				                                      pathProp.stringValue, extensionsCsv, tooltip);

			// Post-process for relative paths
			if (!string.IsNullOrEmpty(relativeTo) && !string.IsNullOrEmpty(path))
				path = Path.GetRelativePath(relativeTo, path);

			// Apply (button / text field) modification
			pathProp.stringValue = path.Replace('\\', '/');

			// ---------------------------------------------------------------------------
			// Drag-and-Drop support
			// ---------------------------------------------------------------------------
			HandleDragAndDrop(_rect, pathProp, isFolder, extensionsCsv, relativeTo);
		}

		/// <summary>
		/// Processes drag events when the mouse is over the property rectangle.
		/// Accepts folders or files (filtered by extension list).
		/// </summary>
		private static void HandleDragAndDrop(Rect _rect,
		                                      SerializedProperty _pathProp,
		                                      bool _isFolder,
		                                      string _extCsv,
		                                      string _relativeBase)
		{
			Event evt = Event.current;
			if (!_rect.Contains(evt.mousePosition))
				return;

			// Only react to drag operations
			if (evt.type != EventType.DragUpdated && evt.type != EventType.DragPerform)
				return;

			// Pick first asset/object reference
			if (DragAndDrop.objectReferences.Length == 0)
				return;

			string candidatePath = AssetDatabase.GetAssetPath(DragAndDrop.objectReferences[0]);
			bool   isDirectory   = Directory.Exists(candidatePath);

			// Validate: folder vs file
			if (_isFolder != isDirectory)
				return;

			// Validate: extension filter (if any)
			if (!_isFolder && !string.IsNullOrEmpty(_extCsv))
			{
				string ext = Path.GetExtension(candidatePath).TrimStart('.').ToLowerInvariant();
				bool   match = false;
				foreach (string wanted in _extCsv.Split(','))
				{
					if (ext == wanted.Trim().ToLowerInvariant())
					{
						match = true;
						break;
					}
				}
				if (!match)
					return;
			}

			// All good – show copy cursor
			DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

			if (evt.type == EventType.DragPerform)
			{
				DragAndDrop.AcceptDrag();
				// Convert to relative, if requested
				if (!string.IsNullOrEmpty(_relativeBase))
					candidatePath = Path.GetRelativePath(_relativeBase, candidatePath);

				_pathProp.stringValue = candidatePath.Replace('\\', '/');
				_pathProp.serializedObject.ApplyModifiedProperties();   // persist instantly
			}

			evt.Use();   // mark event as handled
		}
	}
}
