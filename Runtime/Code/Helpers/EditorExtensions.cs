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
		private static readonly List<(string symlink, string target)> s_symlinks = new();
		static EditorExtensions()
		{
			s_symlinks = SymlinkResolver.ResolveAll();
string s = "Symlinks found:\n";
foreach (var valueTuple in s_symlinks)
{
s += $"{valueTuple.symlink} -> {valueTuple.target}\n";
}
Debug.Log(s);
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

		public static string ToLogicalPath( this string _s )
		{
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

		public static string ToPhysicalPath( this string _s )
		{
			return string.IsNullOrEmpty(_s) ? _s : FileUtil.GetPhysicalPath(_s);
		}
	}
}
#endif