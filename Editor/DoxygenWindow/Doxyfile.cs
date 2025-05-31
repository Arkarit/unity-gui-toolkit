using System.IO;
using System.Text;
using UnityEditor;
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
			string template = ReadTemplate();
			if (template == null)
			{
				Debug.LogError("DoxyfileTemplate not found");
				return null;
			}

			DoxygenConfig.EditorSave();

			string newfile = template.Replace("PROJECT_NAME           =", "PROJECT_NAME           = " + "\"" + DoxygenConfig.Instance.Project + "\"");
			newfile = newfile.Replace("PROJECT_NUMBER         =", "PROJECT_NUMBER         = " + DoxygenConfig.Instance.Version);
			newfile = newfile.Replace("PROJECT_BRIEF          =",
				"PROJECT_BRIEF          = " + "\"" + DoxygenConfig.Instance.Synopsis + "\"");
			newfile = newfile.Replace("OUTPUT_DIRECTORY       =",
				"OUTPUT_DIRECTORY       = " + "\"" + DoxygenConfig.Instance.DocumentDirectory.FullPath + "\"");
			newfile = newfile.Replace("IMAGE_PATH             =",
				"IMAGE_PATH             = " + "\"" + DoxygenConfig.Instance.DocumentDirectory.FullPath + "\"");
			newfile = newfile.Replace("INPUT                  =",
				"INPUT                  = " + "\"" + DoxygenConfig.Instance.ScriptsDirectory.FullPath + "\"");
			newfile = newfile.Replace("PREDEFINED             =", "PREDEFINED             = " + DoxygenConfig.Instance.Defines);

			newfile = newfile.Replace("DISTRIBUTE_GROUP_DOC   = NO", "DISTRIBUTE_GROUP_DOC   = YES");


			string excludePatterns = "";
			for (int i = 0; i < DoxygenConfig.Instance.ExcludePatterns.Count; i++)
			{
				if (i > 0)
					excludePatterns += " \\\n";
				excludePatterns += DoxygenConfig.Instance.ExcludePatterns[i];
			}

			if (!string.IsNullOrEmpty(excludePatterns))
			{
				newfile = newfile.Replace("EXCLUDE_PATTERNS       =", "EXCLUDE_PATTERNS       = " + excludePatterns);
			}

			StringBuilder sb = new StringBuilder();
			sb.Append(newfile);
			StreamWriter NewDoxyfile = new StreamWriter(Path);
			NewDoxyfile.Write(sb.ToString());
			NewDoxyfile.Close();

			return Path;
		}

		private static string ReadTemplate()
		{
			var result = Resources.Load<TextAsset>("DoxyfileTemplate");
			return result ? result.text : null;
		}
	}

}
