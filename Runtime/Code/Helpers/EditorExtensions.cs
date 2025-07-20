#if UNITY_EDITOR
using System;
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

		/// <summary>
		/// Displays all visible properties in the serialized object using default Unity GUI.
		/// </summary>
		/// <param name="_this">The serialized object to display.</param>
		public static void DisplayProperties( this SerializedObject _this )
		{
			var props = _this.GetIterator().GetVisibleChildren();
			foreach (var prop in props)
				EditorGUILayout.PropertyField(prop, true);
		}

		/// <summary>
		/// Converts a physical file system path to a logical Unity project-relative path.
		/// Unlike <see cref="FileUtil.GetLogicalPath"/>, this method:
		/// - supports symlinked files and directories (e.g. inside a Git repo),
		/// - works reliably for both Assets and Packages,
		/// - and avoids Unity's built-in limitations.
		/// </summary>
		/// <param name="_path">The absolute physical path to convert.</param>
		/// <returns>
		/// The logical Unity path (e.g. "Assets/..." or "Packages/..."),
		/// or null/empty if the input is invalid.
		/// </returns>
		public static string ToLogicalPath( this string _path )
		{
			if (string.IsNullOrEmpty(_path))
				return _path;

			// Resolve symlink if the path is a redirected source (e.g. via junction or symlink)
			var result = SymlinkResolver.GetSource(_path);

			// Convert to project-relative logical path (e.g. "Assets/Foo/Bar.prefab")
			return FileUtil.GetProjectRelativePath(result);
		}

		/// <summary>
		/// Resolves symlinks or junctions and returns the physical source path.
		/// This is the inverse of what Unity typically works with (logical → physical).
		/// </summary>
		/// <param name="_path">The path to resolve (can be logical or physical).</param>
		/// <returns>
		/// The true physical path pointing to the source location,
		/// or null/empty if the input is invalid.
		/// </returns>
		public static string ToPhysicalPath( this string _path )
		{
			if (string.IsNullOrEmpty(_path))
				return _path;

			return SymlinkResolver.GetSource(_path);
		}

		/// <summary>
		/// Normalizes a directory path to use forward slashes and ensures a trailing slash.
		/// </summary>
		/// <param name="_directory">The directory path to normalize.</param>
		/// <returns>The normalized directory path ending with a single forward slash.</returns>
		public static string NormalizedDirectoryPath( this string _directory ) =>
			NormalizedFilePath(_directory) + '/';

		/// <summary>
		/// Normalizes a file or directory path to use forward slashes and trims trailing slashes.
		/// </summary>
		/// <param name="_directory">The file or directory path to normalize.</param>
		/// <returns>The normalized path.</returns>
		public static string NormalizedFilePath( this string _directory ) =>
			string.IsNullOrEmpty(_directory) ? string.Empty : Path.GetFullPath(_directory).Replace('\\', '/').TrimEnd('/');

		/// <summary>
		/// Checks if a given game object is root.
		/// In Prefab Edit Mode, the visible GameObject is placed inside a wrapper root.
		/// This detects if the object is the real root of the edited prefab.
		/// </summary>
		/// <param name="_gameObject"></param>
		/// <returns>true if root, false if not root or game object is null</returns>
		public static bool IsRoot( this GameObject _gameObject )
		{
			if (_gameObject == null )
				return false;

			if (EditorGameObjectUtility.IsEditingPrefab(_gameObject))
				return _gameObject.transform.parent != null && _gameObject.transform.parent.parent == null;

			return _gameObject.transform.parent == null;
		}

		/// <summary>
		/// Checks if the component's game object is root.
		/// </summary>
		public static bool IsRoot(this Component _component) => _component != null && _component.gameObject.IsRoot();
		
		/// <summary>
		/// Checks if the transform's game object is root.
		/// </summary>
		public static bool IsRoot(this Transform _transform) => _transform != null && _transform.gameObject.IsRoot();

		/// <summary>
		/// Normalizes a file or directory path to use forward slashes and remove redundant slashes.
		/// If the path points to a directory, a trailing slash is added. If it is a file, the trailing slash is removed.
		/// </summary>
		/// <remarks>
		/// Note: This method queries the filesystem via File.GetAttributes(), which may impact performance
		/// if used frequently or inside tight loops (e.g. GUI code or asset import).
		/// Consider caching results or restricting usage to initialization code.
		/// </remarks>
		/// <param name="_path">The file or directory path to normalize.</param>
		/// <returns>
		/// The normalized absolute path using forward slashes. 
		/// If the path is invalid or inaccessible, the original input is returned.
		/// </returns>
		public static string NormalizedPath( this string _path )
		{
			if (string.IsNullOrEmpty(_path))
				return _path;

			FileAttributes attr;
			try
			{
				attr = File.GetAttributes(_path);
			}
			catch (Exception e)
			{
				// Could be FileNotFoundException, UnauthorizedAccessException etc.
				// Log and fall back to returning original input
				Debug.LogWarning($"Exception while accessing path '{_path}': {e.Message}");
				return _path;
			}

			bool isDirectory = (attr & FileAttributes.Directory) == FileAttributes.Directory;
			return isDirectory ? NormalizedDirectoryPath(_path) : NormalizedFilePath(_path);
		}

	}
}
#endif
