using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	[EditorAware]
	public static class SceneMenuGenerator
	{
		private const int BASE_PRIO = 1500;
		private const string TEMPLATE_NAME = "SceneMenuGeneratorTemplate";
		private const string TEMPLATE_FILE_NAME = TEMPLATE_NAME + ".cs";
		private const string CLASS_NAME = "GuiToolkitSceneMenu";
		private const string CLASS_FILE_NAME = CLASS_NAME + ".cs";
		
		private const string TEMPLATE_MARKER = "/* TEMPLATE */";
		private const string MENUENTRY_TEMPLATE =
			"\t\t[MenuItem(StringConstants.SCENE_MENU_GENERATOR_HEADER + \"{0}/Replace\", false, {3})]\n" +
			"\t\tprivate static void OpenScene_{1}() => OpenScene(\"{2}\", false);\n\n" +
			"\t\t[MenuItem(StringConstants.SCENE_MENU_GENERATOR_HEADER + \"{0}/Additive\", false, {4})]\n" +
			"\t\tprivate static void OpenScene_{1}_Additive() => OpenScene(\"{2}\", true);\n\n\n";

		[MenuItem(StringConstants.SCENE_MENU_GENERATOR, false, BASE_PRIO)]
		private static void Menu() => Generate();

		private static void Generate()
		{
			string template = LoadTemplate();
			if (template == null)
				return;

			string menuEntries = GenerateMenuEntries();
			template = template.Replace(TEMPLATE_NAME, CLASS_NAME).Replace(TEMPLATE_MARKER, menuEntries);
			WriteGeneratedClass(template);
		}

		private static void WriteGeneratedClass(string _content)
		{
			try
			{
				var generatedDir = UiToolkitConfiguration.Instance.GeneratedAssetsDir + "/Editor";
				EditorFileUtility.EnsureFolderExists(generatedDir);
				string path = generatedDir + "/" + CLASS_FILE_NAME;
				File.WriteAllText(path, _content);
				AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}
		}

		private static string GenerateMenuEntries()
		{
			string result = string.Empty;
			EditorBuildSettingsScene[] buildScenes = EditorBuildSettings.scenes;
			for (int i = 0; i < buildScenes.Length; i++)
			{
				EditorBuildSettingsScene s = buildScenes[i];
				if (!s.enabled)
					continue;

				string path = s.path;
				string name = Path.GetFileNameWithoutExtension(path);
				result += GenerateMenuEntry(i, name, path);
			}

			return result;
		}

		private static string GenerateMenuEntry( int _sceneIdx, string _sceneName, string _scenePath )
		{
			string sceneIdx = _sceneIdx.ToString();
			string prio0 = (BASE_PRIO + _sceneIdx * 2 + 1).ToString();
			string prio1 = (BASE_PRIO + _sceneIdx * 2 + 2).ToString();
			
			return string.Format(MENUENTRY_TEMPLATE, _sceneName, sceneIdx, _scenePath, prio0, prio1);
		}

		private static string LoadTemplate()
		{
			var thisPath = EditorCodeUtility.GetThisFilePath();
			var dir = Path.GetDirectoryName(thisPath);
			var path = dir + "/" + TEMPLATE_FILE_NAME;

			try
			{
				return File.ReadAllText(path);
			}
			catch (Exception e)
			{
				Debug.LogException(e);
				return null;
			}
		}
	}
}
