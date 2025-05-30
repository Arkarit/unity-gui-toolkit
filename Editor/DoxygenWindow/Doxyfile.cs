using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;


namespace GuiToolkit.Editor
{
	public class Doxyfile
	{
		private static Doxyfile s_instance;
		private string m_template;

		public static Doxyfile Instance
		{
			get
			{
				if (s_instance == null)
					s_instance = new Doxyfile();

				return s_instance; 
			}
		}

		public string Template
		{
			get
			{
				if (m_template == null)
					m_template = ReadTemplate();
				return m_template;
			}
		}
		public string Path => DoxygenConfig.Instance.DocumentDirectory + "/Doxyfile";

		public bool Exists => File.Exists(Path);

		public void Write()
		{
			DoxygenConfig.EditorSave();

			EditorFileUtility.EnsureUnityFolderExists(DoxygenConfig.Instance.DocumentDirectory);

			string newfile = Template.Replace("PROJECT_NAME           =",
				"PROJECT_NAME           = " + "\"" + DoxygenConfig.Instance.Project + "\"");
			newfile = newfile.Replace("PROJECT_NUMBER         =", "PROJECT_NUMBER         = " + DoxygenConfig.Instance.Version);
			newfile = newfile.Replace("PROJECT_BRIEF          =",
				"PROJECT_BRIEF          = " + "\"" + DoxygenConfig.Instance.Synopsis + "\"");
			newfile = newfile.Replace("OUTPUT_DIRECTORY       =",
				"OUTPUT_DIRECTORY       = " + "\"" + DoxygenConfig.Instance.DocumentDirectory + "\"");
			newfile = newfile.Replace("IMAGE_PATH             =",
				"IMAGE_PATH             = " + "\"" + DoxygenConfig.Instance.DocumentDirectory + "\"");
			newfile = newfile.Replace("INPUT                  =",
				"INPUT                  = " + "\"" + DoxygenConfig.Instance.ScriptsDirectory + "\"");
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
			StreamWriter NewDoxyfile = new StreamWriter(DoxygenConfig.Instance.DocumentDirectory + "/Doxyfile");

			NewDoxyfile.Write(sb.ToString());
			NewDoxyfile.Close();
		}

		private string ReadTemplate()
		{
			var result = Resources.Load<TextAsset>("DoxyfileTemplate");
			return result ? result.text : null;
		}
	}

}
