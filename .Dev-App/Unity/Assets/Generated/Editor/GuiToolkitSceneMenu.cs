#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GuiToolkit.Editor
{
	public static class GuiToolkitSceneMenu
	{
		[MenuItem(StringConstants.SCENE_MENU_GENERATOR_HEADER + "SampleScene", false)]
		private static void OpenScene_0() => OpenScene("Assets/Scenes/SampleScene.unity", false);

		[MenuItem(StringConstants.SCENE_MENU_GENERATOR_HEADER + "SampleScene (Additive)", false)]
		private static void OpenScene_0_Additive() => OpenScene("Assets/Scenes/SampleScene.unity", true);


		[MenuItem(StringConstants.SCENE_MENU_GENERATOR_HEADER + "DemoScene1", false)]
		private static void OpenScene_1() => OpenScene("Assets/Scenes/DemoScene1.unity", false);

		[MenuItem(StringConstants.SCENE_MENU_GENERATOR_HEADER + "DemoScene1 (Additive)", false)]
		private static void OpenScene_1_Additive() => OpenScene("Assets/Scenes/DemoScene1.unity", true);



		private static void OpenScene( string _path, bool _additive )
		{
			if (!_additive)
			{
				if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
					return;

				EditorSceneManager.OpenScene(_path, OpenSceneMode.Single);
				return;
			}

			// Additive: keep current scene(s)
			EditorSceneManager.OpenScene(_path, OpenSceneMode.Additive);
			Scene opened = SceneManager.GetSceneByPath(_path);
			if (opened.IsValid())
			{
				SceneManager.SetActiveScene(opened);
			}
		}
	}
}
#endif
