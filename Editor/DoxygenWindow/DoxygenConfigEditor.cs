using System.Threading;
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	[CustomEditor(typeof(DoxygenConfig), true)]
	public class DoxygenConfigEditor : UnityEditor.Editor
	{
		private float m_progress;
		private DoxygenThreadSafeOutput m_doxygenOutput = null;

		private static string s_doxygenLogPath;

		public bool IsDoxygenExeWorking => IsDoxygenExeActive && m_doxygenOutput.isStarted() && !m_doxygenOutput.isFinished();

		private static string DoxygenLogPath => s_doxygenLogPath ??= ($"{System.IO.Path.GetTempPath()}unityDoxygen.log").Replace('\\', '/');
		private bool IsDoxygenExeActive => m_doxygenOutput != null;
		private bool IsDoxygenExeFinished => IsDoxygenExeActive && m_doxygenOutput.isFinished();
		private bool m_showAbout;

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

				using (new EditorGUI.DisabledScope(!DoxygenConfig.Instance.DocsGenerated))
				{
					EditorUiUtility.Centered(() =>
					{
						if (GUILayout.Button(
							new GUIContent("   Browse Documentation   ", "Press this to view your Documentation when it has been generated."),
							GUILayout.Height(40)
						))
							Application.OpenURL("File://" + DoxygenConfig.Instance.OutputDirectory.FullPath + "/html/annotated.html");
					});
				}

				using (new EditorGUI.DisabledScope(IsDoxygenExeActive))
				{
					EditorUiUtility.Centered(() =>
					{
						if (GUILayout.Button(
							new GUIContent("   Run Doxygen   ", "Press this to (re)-generate your documentation."), 
							GUILayout.Height(40)
						))
							RunDoxygen();
					});
				}

				using (new EditorGUI.DisabledScope(!File.Exists(DoxygenLogPath)))
				{
					EditorUiUtility.Centered(() =>
					{
						if (GUILayout.Button(
							new GUIContent("   View last Doxygen Log   ", $"Open the last Doxygen Log. You can also find it at the path '{DoxygenLogPath}'"),
							GUILayout.Height(40)
						))
							Application.OpenURL("File://" + DoxygenLogPath);
					});
				}

				using (new EditorGUI.DisabledScope(!Doxyfile.Exists))
				{
					EditorUiUtility.Centered(() =>
					{
						if (GUILayout.Button(
							new GUIContent("   View last Doxyfile   ", $"Open the last Doxyfile. You can also find it at the path '{Doxyfile.Path}'"),
							GUILayout.Height(40)
						))
							Application.OpenURL("File://" + Doxyfile.Path);
					});
				}

			});

			if (IsDoxygenExeWorking)
			{
				string currentLine = m_doxygenOutput.ReadLine();
				if (m_progress < 0.75f)
					m_progress += 0.01f;

				Rect r = EditorGUILayout.BeginVertical();
				EditorGUI.ProgressBar(r, m_progress, currentLine);
				GUILayout.Space(40);
				EditorGUILayout.EndVertical();
				EditorUtility.SetDirty(target);
				return;
			}
			
			if (IsDoxygenExeFinished)
			{
				if (Event.current.type == EventType.Repaint)
				{
					var doxygenLog = m_doxygenOutput.ReadFullLog();

					try
					{
						File.WriteAllText(DoxygenLogPath, doxygenLog);
					}
					catch (Exception e)
					{
						Debug.LogError($"Could not write doxygen log, exception:{e.Message}");
					}

					m_progress = 0;
					m_doxygenOutput = null;
				}
			}

			m_showAbout = EditorGUILayout.Foldout(m_showAbout, "About...");
			if (m_showAbout)
				AboutGUI();

		}

		private void AboutGUI()
		{
			GUILayout.Label("Original Message by Jacob Pennock:");
			GUILayout.Space(20);


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

		public static void OnDoxygenFinished(int _code)
		{
			if (_code != 0)
			{
				UnityEngine.Debug.LogError("Doxygen finished with Error: return code " + _code +
				                           "\nPlease check the Doxygen Log for Errors.");
			}
		}

		public void RunDoxygen()
		{
			string[] args = new string[1];
			args[0] = Doxyfile.Write();
			m_progress = 0;

			m_doxygenOutput = new DoxygenThreadSafeOutput();
			m_doxygenOutput.SetStarted();

			Action<int> callback = (int returnCode) => OnDoxygenFinished(returnCode);

			DoxygenRunner doxygenRunner = new DoxygenRunner(args, m_doxygenOutput, callback);
			Thread doxygenThread = new Thread(doxygenRunner.RunThreadedDoxygen);
			doxygenThread.Start();
		}
	}
}
