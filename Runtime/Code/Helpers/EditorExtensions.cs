#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit
{
	public static class EditorExtensions
	{
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
