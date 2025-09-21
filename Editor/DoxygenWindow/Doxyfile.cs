using System;
using System.IO;
using GuiToolkit.Debugging;
using UnityEngine;


namespace GuiToolkit.Editor
{
	[EditorAware]
	public static class Doxyfile
	{
		private static string s_path;

		public static string Path => s_path ??= $"{System.IO.Path.GetTempPath()}/~~~tempDoxyfile.txt";
		public static bool Exists => File.Exists(Path);

		public static string Write()
		{
			AssetReadyGate.ThrowIfNotReady();
			
			var templateLines = ReadTemplate();
			if (templateLines == null)
			{
				Debug.LogError("DoxyfileTemplate not found");
				return null;
			}
			
			// This creates the config if it doesn't exist
			_ = DoxygenConfig.Instance;

			string excludePatterns = string.Empty;
			for (int i = 0; i < DoxygenConfig.Instance.ExcludePatterns.Count; i++)
				excludePatterns += $"{DoxygenConfig.Instance.ExcludePatterns[i]} ";

			string scriptsDirectories = string.Empty;
			for (int i = 0; i < DoxygenConfig.Instance.InputDirectories.Count; i++)
				scriptsDirectories += $"\"{DoxygenConfig.Instance.InputDirectories[i].FullPath}\" ";

			string defines = string.Empty;
			for (int i = 0; i < DoxygenConfig.Instance.Defines.Count; i++)
				defines += $"\"{DoxygenConfig.Instance.Defines[i]}\" ";

			string version = DoxygenConfig.Instance.Version;

			for (int i = 0; i < templateLines.Length; i++)
			{
				var line = templateLines[i].Trim();

				if (   ReplaceLineIfNecessary(ref line, "PROJECT_NAME", DoxygenConfig.Instance.Project)
				    || ReplaceLineIfNecessary(ref line, "PROJECT_NUMBER", version)
				    || ReplaceLineIfNecessary(ref line, "PROJECT_BRIEF", DoxygenConfig.Instance.Synopsis)
				    || ReplaceLineIfNecessary(ref line, "OUTPUT_DIRECTORY", DoxygenConfig.Instance.OutputDirectory.FullPath)
				    || ReplaceLineIfNecessary(ref line, "IMAGE_PATH", DoxygenConfig.Instance.OutputDirectory.FullPath)
				    || ReplaceLineIfNecessary(ref line, "USE_MDFILE_AS_MAINPAGE", DoxygenConfig.Instance.OptionalMainPage.FullPath)
				    || ReplaceLineIfNecessary(ref line, "INPUT", scriptsDirectories, null)
				    || ReplaceLineIfNecessary(ref line, "PREDEFINED", defines, null)
				    || ReplaceLineIfNecessary(ref line, "DISTRIBUTE_GROUP_DOC", "YES", null)
				    || ReplaceLineIfNecessary(ref line, "EXCLUDE_PATTERNS", excludePatterns, null)
				)
					templateLines[i] = line;
			}

			// Main page needs to be added to input to work
			if (!string.IsNullOrEmpty(DoxygenConfig.Instance.OptionalMainPage.FullPath))
				templateLines[templateLines.Length - 1] += $"\nINPUT += \"{DoxygenConfig.Instance.OptionalMainPage.FullPath}\"";

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
			var lineSplit = line.Split(' ');
			if (lineSplit.Length == 0)
				return false;

			if (lineSplit[0] != keyword)
				return false;

			if (quoteChar == null)
				quoteChar = string.Empty;

			line = $"{keyword} = {quoteChar}{replacement}{quoteChar}";
			return true;
		}


		private static string[] ReadTemplate()
		{
			var path = DebugUtility.GetCallingScriptDirectory() + "/DoxyfileTemplate";

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
