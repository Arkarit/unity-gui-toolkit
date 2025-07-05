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
		//TODO: move this to tests
		static EditorExtensions()
		{
			Debug.Log(SymlinkResolver.DumpSymlinks());

			string s1a = "Assets/External/unity-gui-toolkit-editor/Graphic/UiRoundedImageEditor.cs";
			string t1a = SymlinkResolver.GetTarget(s1a);
			string s2a = SymlinkResolver.GetSource(t1a);
			Debug.Log($"{s1a} -> {t1a} target to source:({s2a})");

			string s1b = "Assets/External/unity-gui-toolkit-editor/Graphic/";
			string t1b = SymlinkResolver.GetTarget(s1b);
			string s2b = SymlinkResolver.GetSource(t1b);
			Debug.Log($"{s1b} -> {t1b} target to source:({s2b})");

			string s1c = "Assets/External/unity-gui-toolkit-editor/Graphic";
			string t1c = SymlinkResolver.GetTarget(s1c);
			string s2c = SymlinkResolver.GetSource(t1c);
			Debug.Log($"{s1c} -> {t1c} target to source:({s2c})");

			string s1d = "Assets/External/unity-gui-toolkit-editor/";
			string t1d = SymlinkResolver.GetTarget(s1d);
			string s2d = SymlinkResolver.GetSource(t1d);
			Debug.Log($"{s1d} -> {t1d} target to source:({s2d})");

			string s1e = "Assets/External/unity-gui-toolkit-editor";
			string t1e = SymlinkResolver.GetTarget(s1e);
			string s2e = SymlinkResolver.GetSource(t1e);
			Debug.Log($"{s1e} -> {t1e} target to source:({s2e})");

		}

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

		public static string ToLogicalPath( this string _path )
		{
			if (string.IsNullOrEmpty(_path))
				return _path;

			// In case we have a path which points to a file outside of the project, but within the repo,
			// we need the symlink path, not the path outside
			var result = SymlinkResolver.GetSource(_path);
			return FileUtil.GetProjectRelativePath(result);
		}

		public static string NormalizedDirectoryPath(this string _directory) => 
			NormalizedFilePath(_directory) + '/';
		
		public static string NormalizedFilePath(this string _directory) => 
			string.IsNullOrEmpty(_directory) ? string.Empty : Path.GetFullPath(_directory).Replace('\\', '/').TrimEnd('/');
		
	}
}
#endif