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


using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Text;

namespace GuiToolkit.Editor
{
	/// <summary>
	/// <para> An Editor Plugin for automatic doc generation through Doxygen</para>
	/// <para> Original Author: Jacob Pennock (http://Jacobpennock.com)</para>
	/// </summary>
	public class DoxygenWindow : EditorWindow
	{
		public static DoxygenWindow Instance;

		public enum WindowModes
		{
			Generate,
			Configuration,
			About
		}

		public string AssetsFolder;
		public static readonly string[] Themes = new string[3] { "Default", "Dark and Colorful", "Light and Clean" };
		WindowModes DisplayMode = WindowModes.Generate;
		StringReader reader;
		TextAsset basefile;
		float DoxyoutputProgress = -1.0f;
		public string DoxygenOutputString = null;
		public string CurentOutput = null;
		DoxygenThreadSafeOutput DoxygenOutput = null;
		List<string> DoxygenLog = null;
		bool ViewLog = false;
		Vector2 scroll;
		private string LocalConfig = string.Empty;

		[MenuItem("Window/Documentation with Doxygen")]
		public static void Init()
		{
			Instance = (DoxygenWindow)GetWindow(typeof(DoxygenWindow), false, "Documentation");
			Instance.minSize = new Vector2(420, 245);
			Instance.maxSize = new Vector2(420, 720);
		}

		void OnEnable()
		{
			AssetsFolder = Application.dataPath;
			DoxyoutputProgress = 0;
			DoxygenConfig.Initialize();
		}

		void OnDisable()
		{
			DoxyoutputProgress = 0;
			DoxygenLog = null;
		}

		void OnGUI()
		{
			GenerateGUI();
			return;
#if false
			DisplayHeadingToolbar();
			switch (DisplayMode)
			{
				case WindowModes.Generate:
					GenerateGUI();
					break;

				case WindowModes.Configuration:
					ConfigGUI();
					break;

				case WindowModes.About:
					AboutGUI();
					break;
			}
#endif
		}

#if false
		void DisplayHeadingToolbar()
		{
			GUIStyle normalButton = new GUIStyle(EditorStyles.toolbarButton);
			normalButton.fixedWidth = 140;
			GUILayout.Space(5);
			EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
			{
				if (GUILayout.Toggle(DisplayMode == WindowModes.Generate, "Generate Documentation", normalButton))
				{
					DisplayMode = WindowModes.Generate;
				}

				if (GUILayout.Toggle(DisplayMode == WindowModes.Configuration, "Settings/Configuration", normalButton))
				{
					DisplayMode = WindowModes.Configuration;
				}

				if (GUILayout.Toggle(DisplayMode == WindowModes.About, "About", normalButton))
				{
					DisplayMode = WindowModes.About;
				}
			}
			EditorGUILayout.EndHorizontal();
		}

		void ConfigGUI()
		{
			GUILayout.Space(10);
			if (DoxygenConfig.Instance.Project == "Enter your Project name (Required)" || DoxygenConfig.Instance.Project == "" ||
			    DoxygenConfig.Instance.PathtoDoxygen == "")
				GUI.enabled = false;

			if (GUILayout.Button("Save Configuration and Build new DoxyFile", GUILayout.Height(40)))
				Doxyfile.Instance.Write();

			GUI.enabled = true;

			GUILayout.Space(20);
			GUILayout.Label("Per User Settings:", EditorStyles.boldLabel);
			GUILayout.Space(5);
			GUILayout.Label("Paths", EditorStyles.boldLabel);
			GUILayout.Space(5);
			EditorGUILayout.BeginHorizontal();
			DoxygenConfig.Instance.PathtoDoxygen = EditorGUILayout.TextField("Doxygen.exe : ", DoxygenConfig.Instance.PathtoDoxygen);
			if (GUILayout.Button("...", EditorStyles.miniButtonRight, GUILayout.Width(22)))
				DoxygenConfig.Instance.PathtoDoxygen = EditorUtility.OpenFilePanel("Where is doxygen.exe installed?", "", "");
			EditorGUILayout.EndHorizontal();

			GUILayout.Space(5);
			EditorGUILayout.BeginHorizontal();
			DoxygenConfig.Instance.ScriptsDirectory = EditorGUILayout.TextField("Scripts folder: ", DoxygenConfig.Instance.ScriptsDirectory);
			if (GUILayout.Button("...", EditorStyles.miniButtonRight, GUILayout.Width(22)))
				DoxygenConfig.Instance.ScriptsDirectory =
					EditorUtility.OpenFolderPanel("Select your scripts folder", DoxygenConfig.Instance.ScriptsDirectory, "");
			EditorGUILayout.EndHorizontal();

			GUILayout.Space(15);
			GUILayout.Label("Theme", EditorStyles.boldLabel);
			GUILayout.Space(5);
			DoxygenConfig.Instance.SelectedTheme = EditorGUILayout.Popup(DoxygenConfig.Instance.SelectedTheme, Themes);


			GUILayout.Space(20);
			GUILayout.Label("Provide some details about the project", EditorStyles.boldLabel);
			GUILayout.Space(5);
			DoxygenConfig.Instance.Project = EditorGUILayout.TextField("Project Name: ", DoxygenConfig.Instance.Project);
			DoxygenConfig.Instance.Synopsis = EditorGUILayout.TextField("Project Brief: ", DoxygenConfig.Instance.Synopsis);
			DoxygenConfig.Instance.Version = EditorGUILayout.TextField("Project Version: ", DoxygenConfig.Instance.Version);


			GUILayout.Space(20);
			GUILayout.Label("Project settings", EditorStyles.boldLabel);
			GUILayout.Space(5);
			EditorGUILayout.BeginHorizontal();
			DoxygenConfig.Instance.DocDirectory = EditorGUILayout.TextField("Output folder: ", DoxygenConfig.Instance.DocDirectory);
			if (GUILayout.Button("...", EditorStyles.miniButtonRight, GUILayout.Width(22)))
				DoxygenConfig.Instance.DocDirectory =
					EditorUtility.OpenFolderPanel("Select your ouput Docs folder", DoxygenConfig.Instance.DocDirectory, "");
			EditorGUILayout.EndHorizontal();
			DoxygenConfig.Instance.Defines = EditorGUILayout.TextField("Defines (Separate with space): ", DoxygenConfig.Instance.Defines);

			GUILayout.Space(5);
			EditorGUILayout.BeginHorizontal();
			GUILayout.Space(5);
			GUILayout.Space(30);
			GUILayout.Label(
				"By default Doxygen will search through your whole Assets folder for C# script files to document. Then it will output the documentation it generates into a folder called \"Docs\" that is placed in your project folder next to the Assets folder. If you would like to set a specific script or output folder you can do so above. ",
				EditorStyles.wordWrappedMiniLabel);
			GUILayout.Space(30);
			EditorGUILayout.EndHorizontal();

			SerializedObject serObj = new SerializedObject(this);
			SerializedProperty parentProp = serObj.FindProperty("Config");

//			SerializedProperty prop = parentProp.FindPropertyRelative("ExcludePatterns");
//			EditorGUILayout.PropertyField(prop, true);
			serObj.ApplyModifiedProperties();
		}

		void AboutGUI()
		{
			GUIStyle CenterLable = new GUIStyle(EditorStyles.largeLabel);
			GUIStyle littletext = new GUIStyle(EditorStyles.miniLabel);
			CenterLable.alignment = TextAnchor.MiddleCenter;
			GUILayout.Space(20);
			GUILayout.Label("Automatic C# Documentation Generation through Doxygen", CenterLable);
			GUILayout.Label("Version: 1.0", CenterLable);
			GUILayout.Label("By: Jacob Pennock", CenterLable);

			GUILayout.Space(20);
			EditorGUILayout.BeginHorizontal();
			GUILayout.Space(20);
			GUILayout.Label("Follow me for more Unity tips and tricks", littletext);
			GUILayout.Space(15);
			if (GUILayout.Button("twitter"))
				Application.OpenURL("http://twitter.com/@JacobPennock");
			GUILayout.Space(20);
			EditorGUILayout.EndHorizontal();

			GUILayout.Space(10);
			EditorGUILayout.BeginHorizontal();
			GUILayout.Space(20);
			GUILayout.Label("Visit my site for more plugins and tutorials", littletext);
			if (GUILayout.Button("JacobPennock.com"))
				Application.OpenURL("http://www.jacobpennock.com/Blog/?cat=19");
			GUILayout.Space(20);
			EditorGUILayout.EndHorizontal();
		}
#endif
		void GenerateGUI()
		{
			if (Doxyfile.Instance.Exists)
			{
				//UnityEngine.Debug.Log(DoxyoutputProgress);
				GUILayout.Space(10);
				if (!DoxygenConfig.Instance.DocsGenerated)
					GUI.enabled = false;
				if (GUILayout.Button("Browse Documentation", GUILayout.Height(40)))
				{
					Application.OpenURL("File://" + Doxyfile.Instance.Directory + "/html/annotated.html");
				}

				GUI.enabled = true;

				if (DoxygenOutput == null)
				{
					if (GUILayout.Button("Run Doxygen", GUILayout.Height(40)))
					{
						DoxygenConfig.Instance.DocsGenerated = false;
						RunDoxygen();
					}

					if (DoxygenConfig.Instance.DocsGenerated && DoxygenLog != null)
					{
						if (GUILayout.Button("View Doxygen Log", EditorStyles.toolbarDropDown))
							ViewLog = !ViewLog;
						if (ViewLog)
						{
							scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.ExpandHeight(true));
							foreach (string logitem in DoxygenLog)
							{
								EditorGUILayout.SelectableLabel(logitem, EditorStyles.miniLabel,
									GUILayout.ExpandWidth(true));
							}

							EditorGUILayout.EndScrollView();
						}
					}
				}
				else
				{
					if (DoxygenOutput.isStarted() && !DoxygenOutput.isFinished())
					{
						string currentline = DoxygenOutput.ReadLine();
						DoxyoutputProgress = DoxyoutputProgress + 0.1f;
						if (DoxyoutputProgress >= 0.9f)
							DoxyoutputProgress = 0.75f;
						Rect r = EditorGUILayout.BeginVertical();
						EditorGUI.ProgressBar(r, DoxyoutputProgress, currentline);
						GUILayout.Space(40);
						EditorGUILayout.EndVertical();
					}

					if (DoxygenOutput.isFinished())
					{
						if (Event.current.type == EventType.Repaint)
						{
							/*
							If you look at what SetTheme is doing, I know, it seems a little scary to be
							calling file moving operations from inside a an OnGUI call like this. And
							maybe it would be a better choice to call SetTheme and update these other vars
							from inside of the OnDoxygenFinished callback. But since the callback is static
							that would require a little bit of messy singleton instance checking to make sure
							the call back was calling into the right functions. I did try to do it that way
							but for some reason the file operations failed every time. I'm not sure why.
							This is what I was getting from the debugger:

							Error in file: C:/BuildAgent/work/842f9551727e852/Runtime/Mono/MonoManager.cpp at line 2212
							UnityEditor.FileUtil:DeleteFileOrDirectory(String)
							UnityEditor.FileUtil:ReplaceFile(String, String) (at C:\BuildAgent\work\842f9557127e852\Editor\MonoGenerated\Editor\FileUtil.cs:42)

							Doing them here seems to work every time and the Repaint event check ensures that they will only be done once.
							*/
							SetTheme(DoxygenConfig.Instance.SelectedTheme);
							DoxygenLog = DoxygenOutput.ReadFullLog();
							DoxyoutputProgress = -1.0f;
							DoxygenOutput = null;
							DoxygenConfig.Instance.DocsGenerated = true;
							EditorPrefs.SetBool(DoxygenConfig.Instance.UnityProjectID + "DocsGenerated", DoxygenConfig.Instance.DocsGenerated);
						}
					}
				}
			}
			else
			{
				GUIStyle ErrorLabel = new GUIStyle(EditorStyles.largeLabel);
				ErrorLabel.alignment = TextAnchor.MiddleCenter;
				GUILayout.Space(20);
				GUI.contentColor = Color.red;
				GUILayout.Label(
					"You must set the path to your Doxygen install and \nbuild a new Doxyfile before you can generate documentation",
					ErrorLabel);
			}
		}

		public static void OnDoxygenFinished(int code)
		{
			if (code != 0)
			{
				UnityEngine.Debug.LogError("Doxygen finished with Error: return code " + code +
				                           "\nCheck the Doxygen Log for Errors.\nAlso try regenerating your Doxyfile,\nyou will new to close and reopen the\ndocumentation window before regenerating.");
			}
		}

		void SetTheme(int theme)
		{
			switch (theme)
			{
				case 1:
					FileUtil.ReplaceFile(AssetsFolder + "/Editor/Doxygen/Resources/DarkTheme/doxygen.css",
						DoxygenConfig.Instance.DocDirectory + "/html/doxygen.css");
					FileUtil.ReplaceFile(AssetsFolder + "/Editor/Doxygen/Resources/DarkTheme/tabs.css",
						DoxygenConfig.Instance.DocDirectory + "/html/tabs.css");
					FileUtil.ReplaceFile(AssetsFolder + "/Editor/Doxygen/Resources/DarkTheme/img_downArrow.png",
						DoxygenConfig.Instance.DocDirectory + "/html/img_downArrow.png");
					break;
				case 2:
					FileUtil.ReplaceFile(AssetsFolder + "/Editor/Doxygen/Resources/LightTheme/doxygen.css",
						DoxygenConfig.Instance.DocDirectory + "/html/doxygen.css");
					FileUtil.ReplaceFile(AssetsFolder + "/Editor/Doxygen/Resources/LightTheme/tabs.css",
						DoxygenConfig.Instance.DocDirectory + "/html/tabs.css");
					FileUtil.ReplaceFile(AssetsFolder + "/Editor/Doxygen/Resources/LightTheme/img_downArrow.png",
						DoxygenConfig.Instance.DocDirectory + "/html/img_downArrow.png");
					FileUtil.ReplaceFile(
						AssetsFolder + "/Editor/Doxygen/Resources/LightTheme/background_navigation.png",
						DoxygenConfig.Instance.DocDirectory + "/html/background_navigation.png");
					break;
			}
		}

		public void RunDoxygen()
		{
			string[] Args = new string[1];
			Args[0] = DoxygenConfig.Instance.DocDirectory + "/Doxyfile";

			DoxygenOutput = new DoxygenThreadSafeOutput();
			DoxygenOutput.SetStarted();

			Action<int> setcallback = (int returnCode) => OnDoxygenFinished(returnCode);

			DoxygenRunner Doxygen = new DoxygenRunner(Args, DoxygenOutput, setcallback);

			Thread DoxygenThread = new Thread(new ThreadStart(Doxygen.RunThreadedDoxy));
			DoxygenThread.Start();
		}

	}
}
