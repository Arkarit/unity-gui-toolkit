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
		// Temporary test code for verifying symlink resolver logic
		// TODO: Move to unit test class later
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

		/// <summary>
		/// Returns the visible children of a serialized property. Optionally hides the m_Script field.
		/// </summary>
		/// <param name="_serializedProperty">The parent serialized property.</param>
		/// <param name="_hideScript">If true, hides the m_Script property from the result.</param>
		/// <returns>An enumerable of all visible child properties.</returns>
		public static IEnumerable<SerializedProperty> GetVisibleChildren(this SerializedProperty _serializedProperty, bool _hideScript = true)
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

		/// <summary>
		/// Displays all visible properties in the serialized object using default Unity GUI.
		/// </summary>
		/// <param name="_this">The serialized object to display.</param>
		public static void DisplayProperties(this SerializedObject _this)
		{
			var props = _this.GetIterator().GetVisibleChildren();
			foreach (var prop in props)
				EditorGUILayout.PropertyField(prop, true);
		}

		/// <summary>
		/// Converts a physical file system path to a logical Unity project-relative path.
		/// In opposite to FileUtil.GetLogicalPath() it
		/// - supports symlinked files/directories (within a repo)
		/// - also works for Assets paths (FileUtil.GetLogicalPath() only works for packet paths, which renders it completely useless)
		/// </summary>
		/// <param name="_path">The physical path to convert.</param>
		/// <returns>The logical Unity path (e.g. Assets/...).</returns>
		public static string ToLogicalPath(this string _path)
		{
			if (string.IsNullOrEmpty(_path))
				return _path;

			// If path is inside repo but outside project, convert to symlink path if needed
			var result = SymlinkResolver.GetSource(_path);
			return FileUtil.GetProjectRelativePath(result);
		}

		/// <summary>
		/// Normalizes a directory path to use forward slashes and ensures a trailing slash.
		/// </summary>
		/// <param name="_directory">The directory path to normalize.</param>
		/// <returns>The normalized directory path ending with a single forward slash.</returns>
		public static string NormalizedDirectoryPath(this string _directory) =>
			NormalizedFilePath(_directory) + '/';

		/// <summary>
		/// Normalizes a file or directory path to use forward slashes and trims trailing slashes.
		/// </summary>
		/// <param name="_directory">The file or directory path to normalize.</param>
		/// <returns>The normalized path.</returns>
		public static string NormalizedFilePath(this string _directory) =>
			string.IsNullOrEmpty(_directory) ? string.Empty : Path.GetFullPath(_directory).Replace('\\', '/').TrimEnd('/');
	}
}
#endif
