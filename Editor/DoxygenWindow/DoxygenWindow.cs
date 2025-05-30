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

		[MenuItem("Window/Documentation with Doxygen")]
		public static void Init()
		{
			Instance = (DoxygenWindow)GetWindow(typeof(DoxygenWindow), false, "Doxygen");
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

		void GenerateGUI()
		{
			if (!DoxygenRunner.DoxygenExecutableFound)
			{
				GUILayout.Space(20);

				EditorUiUtility.LabelCentered("It seems that Doxygen is not installed.", EditorStyles.boldLabel);
				
				GUILayout.Space(10);
				EditorUiUtility.LabelCentered("DoxygenWindow needs an external Doxygen");
				GUILayout.Space(-4);
				EditorUiUtility.LabelCentered("installation to work. Download it at the link below");

				GUILayout.Space(10);
				EditorUiUtility.Centered(() =>
				{
					if (GUILayout.Button("   Download Doxygen   ", GUILayout.Height(40)))
					{
						Application.OpenURL("https://www.doxygen.nl/download.html");
					}
				});

				GUILayout.Space(10);
				EditorUiUtility.LabelCentered("Note: It might be necessary to log in and out");
				GUILayout.Space(-4);
				EditorUiUtility.LabelCentered("of your Operating system, after you installed Doxygen");
				GUILayout.Space(-4);
				EditorUiUtility.LabelCentered("to detect it.");

				return;
			}


			if (Doxyfile.Instance.Exists)
			{
				//UnityEngine.Debug.Log(DoxyoutputProgress);
				GUILayout.Space(10);
				if (!DoxygenConfig.Instance.DocsGenerated)
					GUI.enabled = false;
				if (GUILayout.Button("Browse Documentation", GUILayout.Height(40)))
				{
					Application.OpenURL("File://" + DoxygenConfig.Instance.DocumentDirectory + "/html/annotated.html");
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
							DoxygenConfig.Instance.SetTheme(DoxygenConfig.Instance.SelectedTheme);
							DoxygenLog = DoxygenOutput.ReadFullLog();
							DoxyoutputProgress = -1.0f;
							DoxygenOutput = null;
							DoxygenConfig.Instance.DocsGenerated = true;
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

		public void RunDoxygen()
		{
			string[] Args = new string[1];
			Args[0] = Path.GetFullPath(DoxygenConfig.Instance.DocumentDirectory + "/Doxyfile");

			DoxygenOutput = new DoxygenThreadSafeOutput();
			DoxygenOutput.SetStarted();

			Action<int> setcallback = (int returnCode) => OnDoxygenFinished(returnCode);

			DoxygenRunner Doxygen = new DoxygenRunner(Args, DoxygenOutput, setcallback);

			Thread DoxygenThread = new Thread(new ThreadStart(Doxygen.RunThreadedDoxy));
			DoxygenThread.Start();
		}

	}
}
