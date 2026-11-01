using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	[EditorAware]
	public static class SceneMenuGenerator
	{
		private const int BASE_PRIO = 1500;
		// Far enough above the build scenes that Unity draws a separator between the two groups.
		private const int EDITOR_SCENES_PRIO = BASE_PRIO + 1000;

		/// <summary>
		/// Asset label that puts a scene which is not in the build into the menu as well, under "Editor/".
		/// Set it in the Inspector's label field at the bottom of the scene asset; it is stored in the scene's
		/// .meta, so it travels with the project. Regenerate the list afterwards.
		/// </summary>
		public const string EDITOR_SCENE_LABEL = "SceneMenu";
		private const string EDITOR_SCENES_SUBMENU = "Editor/";

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
			var listed = new HashSet<string>();
			EditorBuildSettingsScene[] buildScenes = EditorBuildSettings.scenes;
			for (int i = 0; i < buildScenes.Length; i++)
			{
				EditorBuildSettingsScene s = buildScenes[i];
				if (!s.enabled)
					continue;

				string path = s.path;
				string name = Path.GetFileNameWithoutExtension(path);
				result += GenerateMenuEntry(i.ToString(), name, path, BASE_PRIO + i * 2);
				listed.Add(path);
			}

			return result + GenerateEditorSceneEntries(listed);
		}

		// Scenes labelled EDITOR_SCENE_LABEL that the build list above doesn't already have.
		private static string GenerateEditorSceneEntries( HashSet<string> _listed )
		{
			var paths = AssetDatabase.FindAssets($"l:{EDITOR_SCENE_LABEL} t:Scene")
				.Select(AssetDatabase.GUIDToAssetPath)
				.Where(path => path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase) && !_listed.Contains(path))
				.OrderBy(path => Path.GetFileNameWithoutExtension(path), StringComparer.OrdinalIgnoreCase)
				.ThenBy(path => path, StringComparer.OrdinalIgnoreCase)
				.ToList();

			// Two scenes of the same name would collide in the menu; their folder tells them apart.
			var duplicateNames = new HashSet<string>(paths
				.GroupBy(path => Path.GetFileNameWithoutExtension(path), StringComparer.OrdinalIgnoreCase)
				.Where(group => group.Count() > 1)
				.Select(group => group.Key), StringComparer.OrdinalIgnoreCase);

			string result = string.Empty;
			for (int i = 0; i < paths.Count; i++)
			{
				string path = paths[i];
				string name = Path.GetFileNameWithoutExtension(path);
				if (duplicateNames.Contains(name))
					name += $" ({Path.GetFileName(Path.GetDirectoryName(path))})";

				result += GenerateMenuEntry($"Editor_{i}", EDITOR_SCENES_SUBMENU + name, path, EDITOR_SCENES_PRIO + i * 2);
			}

			return result;
		}

		private static string GenerateMenuEntry( string _id, string _menuName, string _scenePath, int _prio )
		{
			string prio0 = (_prio + 1).ToString();
			string prio1 = (_prio + 2).ToString();

			return string.Format(MENUENTRY_TEMPLATE, _menuName, _id, _scenePath, prio0, prio1);
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
