/*
Original copyright notice by Jacob Pennock:

/// <summary>
/// <para> A Editor Plugin for automatic doc generation through Doxygen</para>
/// <para> Author: Jacob Pennock (http://Jacobpennock.com)</para>
/// <para> Version: 1.0</para>	 
/// </summary>

Permission is hereby granted, free of charge, to any person  obtaining a copy of this software and associated documentation  files (the "Software"), to deal in the Software without  restriction, including without limitation the rights to use,  copy, modify, merge, publish, distribute, sublicense, and/or sell  copies of the Software, and to permit persons to whom the  Software is furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/


using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	/// <summary> 
	/// <para>A small data structure class hold values for making Doxygen config files </para>
	/// </summary>
	[Serializable]
	public class DoxygenConfig
	{
		public string Project;
		public string Synopsis = "";
		public string Version = "";
		public string ScriptsDirectory;
		public string DocDirectory;
		public string PathtoDoxygen = "";
		public string Defines;
		public List<string> ExcludePatterns;

		public string BaseFileString = null;
		public string UnityProjectID;

		public int SelectedTheme = 1;
		public bool DocsGenerated = false;

		public bool DoxyFileExists
		{
			get => m_doxyFileExists;
			set => m_doxyFileExists = value;
		}

		private static DoxygenConfig s_instance;

		private bool m_doxyFileExists = false;

		public static DoxygenConfig Instance
		{
			get
			{
				if (s_instance == null)
				{
					s_instance = new DoxygenConfig();
					s_instance.Load();
				}

				return s_instance;
			}

			private set => s_instance = value;
		}

		public DoxygenConfig()
		{
			UnityProjectID = UnityEditor.PlayerSettings.productName + ":";
		}

		public void Save()
		{
			// If all relevant paths are relative, we can use project-wide config rather than user-dependent
			bool isProjectConfig =
				PathtoDoxygen.StartsWith(".") &&
				ScriptsDirectory.StartsWith(".") &&
				DocDirectory.StartsWith(".");

			if (isProjectConfig)
			{
				string localConfig = Application.dataPath + "/doxygenWindow.json";

				try
				{
					string json = JsonUtility.ToJson(this, true);
					File.WriteAllText(localConfig, json);
				}
				catch
				{
				}
			}

			EditorPrefs.SetString(UnityProjectID + "DoxyProjectName", Project);
			EditorPrefs.SetString(UnityProjectID + "DoxyProjectNumber", Version);
			EditorPrefs.SetString(UnityProjectID + "DoxyProjectBrief", Synopsis);
			EditorPrefs.SetString(UnityProjectID + "DoxyProjectFolder", ScriptsDirectory);
			EditorPrefs.SetString(UnityProjectID + "DoxyProjectOutput", DocDirectory);
			EditorPrefs.SetString(UnityProjectID + "DoxyProjectDefines", Defines);
			EditorPrefs.SetString("DoxyEXE", PathtoDoxygen);
			EditorPrefs.SetInt(UnityProjectID + "DoxyTheme", SelectedTheme);

			EditorPrefs.SetInt(UnityProjectID + "DoxyExcludePatternsCount", ExcludePatterns.Count);
			for (int i = 0; i < ExcludePatterns.Count; i++)
			{
				string key = UnityProjectID + "DoxyExcludePattern" + i;
				EditorPrefs.SetString(key, ExcludePatterns[i]);
			}
		}


		public void Load()
		{
			if (BaseFileString == null)
				ReadBaseConfig();

			if (!LoadSavedConfig())
			{
				s_instance = null;
				Instance.Project = UnityEditor.PlayerSettings.productName;
				Instance.ScriptsDirectory = Application.dataPath;
				Instance.DocDirectory = Application.dataPath.Replace("Assets", "Docs");
			}

			if (EditorPrefs.HasKey(UnityProjectID + "DoxyFileExists"))
				DoxyFileExists = EditorPrefs.GetBool(UnityProjectID + "DoxyFileExists");
			if (EditorPrefs.HasKey(UnityProjectID + "DocsGenerated"))
				DocsGenerated = EditorPrefs.GetBool(UnityProjectID + "DocsGenerated");
			if (EditorPrefs.HasKey(UnityProjectID + "DoxyTheme"))
				SelectedTheme = EditorPrefs.GetInt(UnityProjectID + "DoxyTheme");
			if (EditorPrefs.HasKey("DoxyEXE"))
				Instance.PathtoDoxygen = EditorPrefs.GetString("DoxyEXE");

			ReadExcludePatterns();
		}

		public bool LoadSavedConfig()
		{
			string localConfig = Application.dataPath + "/doxygenWindow.json";

			try
			{
				string content = File.ReadAllText(localConfig);
				if (!string.IsNullOrEmpty(content))
				{
					Instance = JsonUtility.FromJson<DoxygenConfig>(content);
					return true;
				}
			}
			catch
			{
			}

			if (EditorPrefs.HasKey(UnityProjectID + "DoxyProjectName"))
			{
				Instance = new DoxygenConfig();
				Instance.Project = EditorPrefs.GetString(UnityProjectID + "DoxyProjectName");
				Instance.Version = EditorPrefs.GetString(UnityProjectID + "DoxyProjectNumber");
				Instance.Synopsis = EditorPrefs.GetString(UnityProjectID + "DoxyProjectBrief");
				Instance.DocDirectory = EditorPrefs.GetString(UnityProjectID + "DoxyProjectOutput");
				Instance.ScriptsDirectory = EditorPrefs.GetString(UnityProjectID + "DoxyProjectFolder");
				Instance.Defines = EditorPrefs.GetString(UnityProjectID + "DoxyProjectDefines");

				ReadExcludePatterns();
				return true;
			}

			return false;
		}

		private void ReadBaseConfig()
		{
			var basefile = (TextAsset)Resources.Load("BaseDoxyfile", typeof(TextAsset));
			var reader = new StringReader(basefile.text);
			if (reader == null)
				UnityEngine.Debug.LogError("BaseDoxyfile not found or not readable");
			else
				BaseFileString = reader.ReadToEnd();
		}

		private void ReadExcludePatterns()
		{
			DoxygenConfig.Instance.ExcludePatterns = new List<string>();

			int excludePatternsCount = 0;
			if (EditorPrefs.HasKey(UnityProjectID + "DoxyExcludePatternsCount"))
				excludePatternsCount = EditorPrefs.GetInt(UnityProjectID + "DoxyExcludePatternsCount");

			for (int i = 0; i < excludePatternsCount; i++)
			{
				string key = UnityProjectID + "DoxyExcludePattern" + i;
				if (!EditorPrefs.HasKey(key))
					break;
				DoxygenConfig.Instance.ExcludePatterns.Add(EditorPrefs.GetString(key));
			}
		}

	}
}
