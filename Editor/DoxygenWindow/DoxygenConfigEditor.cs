using System.Threading;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	[CustomEditor(typeof(DoxygenConfig), true)]
	public class DoxygenConfigEditor : UnityEditor.Editor
	{
		public string CurentOutput = null;
		float DoxyoutputProgress = -1.0f;

		DoxygenThreadSafeOutput DoxygenOutput = null;
		List<string> DoxygenLog = null;
		bool ViewLog = false;
		Vector2 scroll;

		public override void OnInspectorGUI()
		{
			EditorUiUtility.WithHeadline("Doxygen Settings", () => DrawDefaultInspector());

			EditorUiUtility.WithHeadline("Doxygen Tools", () =>
			{
				if (!DoxygenRunner.DoxygenExecutableFound)
				{
					GUILayout.Space(20);

					EditorUiUtility.LabelCentered("Tools not available; it seems that Doxygen is not installed.", EditorStyles.boldLabel);

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

				GUILayout.Space(10);
				if (!DoxygenConfig.Instance.DocsGenerated)
					GUI.enabled = false;

				EditorUiUtility.Centered(() =>
				{
					if (GUILayout.Button("   Browse Documentation   ", GUILayout.Height(40)))
					{
						Application.OpenURL("File://" + DoxygenConfig.Instance.DocumentDirectory.FullPath + "/html/annotated.html");
					}
				});


				GUI.enabled = true;

				if (DoxygenOutput == null)
				{
					EditorUiUtility.Centered(() =>
					{
						if (GUILayout.Button("   Run Doxygen   ", GUILayout.Height(40)))
							RunDoxygen();
					});

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

					return;
				}

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
						DoxygenLog = DoxygenOutput.ReadFullLog();
						DoxyoutputProgress = -1.0f;
						DoxygenOutput = null;
					}
				}
			});
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
			Args[0] = Doxyfile.Write();

			DoxygenOutput = new DoxygenThreadSafeOutput();
			DoxygenOutput.SetStarted();

			Action<int> setcallback = (int returnCode) => OnDoxygenFinished(returnCode);

			DoxygenRunner Doxygen = new DoxygenRunner(Args, DoxygenOutput, setcallback);

			Thread DoxygenThread = new Thread(new ThreadStart(Doxygen.RunThreadedDoxy));
			DoxygenThread.Start();
		}
	}
}
