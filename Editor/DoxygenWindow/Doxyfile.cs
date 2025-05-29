using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;


namespace GuiToolkit.Editor
{
	public class Doxyfile
	{
		private static Doxyfile s_instance;

		public static Doxyfile Instance
		{
			get
			{
				if (s_instance == null)
					s_instance = new Doxyfile();

				return s_instance; 
			}
		}

		public string DoxyfileLocation
		{
			get
			{
				var result = DoxygenConfig.Instance.DocDirectory;
				if (DoxygenConfig.Instance.DocDirectory.StartsWith("."))
					result = Application.dataPath + "/../" + DoxygenConfig.Instance.PathtoDoxygen.Replace("doxygen.exe", "") + result;
				return result;
			}
		}

		public void Write()
		{
			DoxygenConfig.Instance.Save();

			System.IO.Directory.CreateDirectory(DoxyfileLocation);

			string newfile = DoxygenConfig.Instance.BaseFileString.Replace("PROJECT_NAME           =",
				"PROJECT_NAME           = " + "\"" + DoxygenConfig.Instance.Project + "\"");
			newfile = newfile.Replace("PROJECT_NUMBER         =", "PROJECT_NUMBER         = " + DoxygenConfig.Instance.Version);
			newfile = newfile.Replace("PROJECT_BRIEF          =",
				"PROJECT_BRIEF          = " + "\"" + DoxygenConfig.Instance.Synopsis + "\"");
			newfile = newfile.Replace("OUTPUT_DIRECTORY       =",
				"OUTPUT_DIRECTORY       = " + "\"" + DoxygenConfig.Instance.DocDirectory + "\"");
			newfile = newfile.Replace("IMAGE_PATH             =",
				"IMAGE_PATH             = " + "\"" + DoxygenConfig.Instance.DocDirectory + "\"");
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

			switch (DoxygenConfig.Instance.SelectedTheme)
			{
				case 0:
					newfile = newfile.Replace("GENERATE_TREEVIEW      = NO", "GENERATE_TREEVIEW      = YES");
					break;
				case 1:
					newfile = newfile.Replace("SEARCHENGINE           = YES", "SEARCHENGINE           = NO");
					newfile = newfile.Replace("CLASS_DIAGRAMS         = YES", "CLASS_DIAGRAMS         = NO");
					break;
			}

			StringBuilder sb = new StringBuilder();
			sb.Append(newfile);
			StreamWriter NewDoxyfile = new StreamWriter(DoxyfileLocation + @"\Doxyfile");

			NewDoxyfile.Write(sb.ToString());
			NewDoxyfile.Close();
			DoxygenConfig.Instance.DoxyFileExists = true;
			EditorPrefs.SetBool(DoxygenConfig.Instance.UnityProjectID + "DoxyFileExists", DoxygenConfig.Instance.DoxyFileExists);
		}
	}

}
