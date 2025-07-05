#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit
{
	[InitializeOnLoad]
	public static class EditorExtensions
	{
		static EditorExtensions() => Debug.Log(SymlinkResolver.DumpSymlinks());

		public static IEnumerable<SerializedProperty> GetVisibleChildren( this SerializedProperty _serializedProperty, bool _hideScript = true )
		{
			SerializedProperty currentProperty = _serializedProperty.Copy();

			if (currentProperty.NextVisible(true))
			{
				do
				{
					if (_hideScript && currentProperty.name == "m_Script")
						continue;

					yield return currentProperty;
				}
				while (currentProperty.NextVisible(false));
			}
		}

		public static void DisplayProperties( this SerializedObject _this )
		{
			var props = _this.GetIterator().GetVisibleChildren();
			foreach (var prop in props)
				EditorGUILayout.PropertyField(prop, true);
		}

		// Caution, WIP!
		public static string ToLogicalPath( this string _s )
		{
return _s;
			if (string.IsNullOrEmpty(_s))
				return _s;

			var directory = Path.GetDirectoryName(_s);
			if (string.IsNullOrEmpty(directory))
				return _s;

			var rootPath = Application.dataPath;
			var logicalPath = FileUtil.GetLogicalPath(_s);
			Debug.Log($"--:: In:'{_s}' Out:'{logicalPath}'");

			if (!string.IsNullOrEmpty(logicalPath))
				return logicalPath;

			return "";
		}
	}
}
#endif