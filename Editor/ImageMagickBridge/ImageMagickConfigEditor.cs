using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	[CustomEditor(typeof(ImageMagickConfig), true)]
	[EditorAware]
	public class ImageMagickConfigEditor : UnityEditor.Editor
	{
		private const string DownloadUrl = "https://imagemagick.org/script/download.php";

		private SerializedProperty m_executableOverrideProp;
		private SerializedProperty m_defaultOutputDirectoryProp;
		private SerializedProperty m_keepIntermediateFilesProp;

		private string m_lastTestOutput;
		private int? m_lastTestExitCode;

		private void OnEnable()
		{
			m_executableOverrideProp = serializedObject.FindProperty(nameof(ImageMagickConfig.ExecutableOverride));
			m_defaultOutputDirectoryProp = serializedObject.FindProperty(nameof(ImageMagickConfig.DefaultOutputDirectory));
			m_keepIntermediateFilesProp = serializedObject.FindProperty(nameof(ImageMagickConfig.KeepIntermediateFiles));
		}

		public override void OnInspectorGUI()
		{
			if (!AssetReadyGate.Ready)
				GUIUtility.ExitGUI();

			EditorUiUtility.WithHeadline("ImageMagick Settings", () =>
			{
				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField(m_executableOverrideProp);
				if (EditorGUI.EndChangeCheck())
				{
					serializedObject.ApplyModifiedProperties();
					ImageMagickRunner.RefreshExecutablePath();
					m_lastTestOutput = null;
					m_lastTestExitCode = null;
				}

				EditorGUILayout.PropertyField(m_defaultOutputDirectoryProp);
				EditorGUILayout.PropertyField(m_keepIntermediateFilesProp);
				serializedObject.ApplyModifiedProperties();
			});

			EditorUiUtility.WithHeadline("ImageMagick Tools", () =>
			{
				if (!ImageMagickRunner.MagickExecutableFound)
				{
					DrawNotInstalled();
					return;
				}

				DrawInstalled();
			});
		}

		private void DrawNotInstalled()
		{
			GUILayout.Space(20);

			EditorUiUtility.LabelCentered("ImageMagick not found.", EditorStyles.boldLabel);

			GUILayout.Space(10);
			EditorUiUtility.LabelCentered("The ImageMagick bridge needs an external");
			GUILayout.Space(-4);
			EditorUiUtility.LabelCentered("ImageMagick installation. Download it at the link below,");
			GUILayout.Space(-4);
			EditorUiUtility.LabelCentered("or set 'Executable Override' above if installed in a custom location.");

			GUILayout.Space(10);

			EditorUiUtility.Centered(() =>
			{
				if (GUILayout.Button("   Download ImageMagick   ", GUILayout.Height(40)))
					Application.OpenURL(DownloadUrl);
			});

			GUILayout.Space(10);

			EditorUiUtility.LabelCentered("Note: After installing ImageMagick, it might be necessary");
			GUILayout.Space(-4);
			EditorUiUtility.LabelCentered("to log out and back in (or restart Unity) for the");
			GUILayout.Space(-4);
			EditorUiUtility.LabelCentered("updated PATH to be picked up.");

			GUILayout.Space(10);

			EditorUiUtility.Centered(() =>
			{
				if (GUILayout.Button("   Re-detect Installation   ", GUILayout.Height(30)))
				{
					ImageMagickRunner.RefreshExecutablePath();
					m_lastTestOutput = null;
					m_lastTestExitCode = null;
				}
			});
		}

		private void DrawInstalled()
		{
			GUILayout.Space(6);
			EditorGUILayout.HelpBox($"ImageMagick found at:\n{ImageMagickRunner.MagickExecutablePath}", MessageType.Info);
			GUILayout.Space(6);

			EditorUiUtility.Centered(() =>
			{
				if (GUILayout.Button(
					new GUIContent("   Test Installation   ", "Run 'magick --version' and display the result."),
					GUILayout.Height(40)
				))
					RunVersionTest();
			});

			if (m_lastTestExitCode.HasValue)
			{
				GUILayout.Space(10);
				var type = m_lastTestExitCode.Value == 0 ? MessageType.Info : MessageType.Error;
				EditorGUILayout.HelpBox(m_lastTestOutput ?? string.Empty, type);
			}
		}

		private void RunVersionTest()
		{
			var sb = new System.Text.StringBuilder();
			Action<string> output = line =>
			{
				if (!string.IsNullOrEmpty(line))
					sb.AppendLine(line);
			};

			try
			{
				int exitCode = ImageMagickRunner.RunSync(new[] { "--version" }, output);
				m_lastTestExitCode = exitCode;
				m_lastTestOutput = sb.ToString().TrimEnd();
				if (exitCode != 0)
					m_lastTestOutput += $"\n\n(magick exited with code {exitCode})";
			}
			catch (FileNotFoundException)
			{
				m_lastTestExitCode = -1;
				m_lastTestOutput = "ImageMagick not found.";
			}
			catch (Exception e)
			{
				m_lastTestExitCode = -1;
				m_lastTestOutput = $"Exception while running magick:\n{e.Message}";
				UiLog.LogError(m_lastTestOutput);
			}
		}
	}
}
