using System;
using System.IO;
using UnityEngine;


namespace GuiToolkit.Editor
{
	public static class Doxyfile
	{
		private static string s_path;

		public static string Path => s_path ??= System.IO.Path.GetTempFileName();

		public static bool Exists => File.Exists(Path);

		public static string Write()
		{
			var templateLines = ReadTemplate();
			if (templateLines == null)
			{
				Debug.LogError("DoxyfileTemplate not found");
				return null;
			}

			DoxygenConfig.EditorSave();

			string excludePatterns = "";
			for (int i = 0; i < DoxygenConfig.Instance.ExcludePatterns.Count; i++)
			{
				if (i > 0)
					excludePatterns += " \\\n";
				excludePatterns += DoxygenConfig.Instance.ExcludePatterns[i];
			}

			for (int i = 0; i < templateLines.Length; i++)
			{
				var line = templateLines[i].Trim();

				if (   ReplaceLineIfNecessary(ref line, "PROJECT_NAME", DoxygenConfig.Instance.Project)
				    || ReplaceLineIfNecessary(ref line, "PROJECT_NUMBER", DoxygenConfig.Instance.Version)
				    || ReplaceLineIfNecessary(ref line, "PROJECT_BRIEF", DoxygenConfig.Instance.Synopsis)
				    || ReplaceLineIfNecessary(ref line, "OUTPUT_DIRECTORY", DoxygenConfig.Instance.DocumentDirectory.FullPath)
				    || ReplaceLineIfNecessary(ref line, "IMAGE_PATH", DoxygenConfig.Instance.DocumentDirectory.FullPath)
				    || ReplaceLineIfNecessary(ref line, "INPUT", DoxygenConfig.Instance.ScriptsDirectory.FullPath)
				    || ReplaceLineIfNecessary(ref line, "PREDEFINED", DoxygenConfig.Instance.Defines, null)
				    || ReplaceLineIfNecessary(ref line, "DISTRIBUTE_GROUP_DOC", "YES", null)
				    || ReplaceLineIfNecessary(ref line, "EXCLUDE_PATTERNS", excludePatterns, null)
				)
					templateLines[i] = line;
			}

			try
			{
				File.WriteAllLines(Path, templateLines);
			}
			catch (Exception e)
			{
				Debug.LogError($"Could not write doxyfile, exception:'{e.Message}'");
			}

			return Path;
		}

		private static bool ReplaceLineIfNecessary(ref string line, string keyword, string replacement, string quoteChar = "\"")
		{
			if (!line.StartsWith(keyword))
				return false;

			if (quoteChar == null)
				quoteChar = string.Empty;

			line = $"{keyword} = {quoteChar}{replacement}{quoteChar}";
			return true;
		}


		private static string[] ReadTemplate()
		{
			var path = EditorGeneralUtility.GetCallingScriptDirectory() + "/DoxyfileTemplate";

			try
			{
				return File.ReadAllLines(path);
			}
			catch (Exception e)
			{
				Debug.LogError($"Could not read doxyfile at path '{path}', exception:'{e.Message}'");
				return new String [0];
			}
		}
	}

}
