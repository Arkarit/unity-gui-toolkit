#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GuiToolkit.Editor
{
	public static class GuiToolkitSceneMenu
	{
		[MenuItem(StringConstants.SCENE_MENU_GENERATOR_HEADER + "SampleScene/Replace", false, 1501)]
		private static void OpenScene_0() => OpenScene("Assets/Scenes/SampleScene.unity", false);

		[MenuItem(StringConstants.SCENE_MENU_GENERATOR_HEADER + "SampleScene/Additive", false, 1502)]
		private static void OpenScene_0_Additive() => OpenScene("Assets/Scenes/SampleScene.unity", true);


		[MenuItem(StringConstants.SCENE_MENU_GENERATOR_HEADER + "DemoScene1/Replace", false, 1503)]
		private static void OpenScene_1() => OpenScene("Assets/Scenes/DemoScene1.unity", false);

		[MenuItem(StringConstants.SCENE_MENU_GENERATOR_HEADER + "DemoScene1/Additive", false, 1504)]
		private static void OpenScene_1_Additive() => OpenScene("Assets/Scenes/DemoScene1.unity", true);


		[MenuItem(StringConstants.SCENE_MENU_GENERATOR_HEADER + "Editor/Icon3DDemo/Replace", false, 2501)]
		private static void OpenScene_Editor_0() => OpenScene("Assets/Scenes/Icon3DDemo.unity", false);

		[MenuItem(StringConstants.SCENE_MENU_GENERATOR_HEADER + "Editor/Icon3DDemo/Additive", false, 2502)]
		private static void OpenScene_Editor_0_Additive() => OpenScene("Assets/Scenes/Icon3DDemo.unity", true);


		[MenuItem(StringConstants.SCENE_MENU_GENERATOR_HEADER + "Editor/Icon3DScrollStress/Replace", false, 2503)]
		private static void OpenScene_Editor_1() => OpenScene("Assets/Scenes/Icon3DScrollStress.unity", false);

		[MenuItem(StringConstants.SCENE_MENU_GENERATOR_HEADER + "Editor/Icon3DScrollStress/Additive", false, 2504)]
		private static void OpenScene_Editor_1_Additive() => OpenScene("Assets/Scenes/Icon3DScrollStress.unity", true);


		[MenuItem(StringConstants.SCENE_MENU_GENERATOR_HEADER + "Editor/RecyclingListDemo/Replace", false, 2505)]
		private static void OpenScene_Editor_2() => OpenScene("Assets/Scenes/RecyclingListDemo.unity", false);

		[MenuItem(StringConstants.SCENE_MENU_GENERATOR_HEADER + "Editor/RecyclingListDemo/Additive", false, 2506)]
		private static void OpenScene_Editor_2_Additive() => OpenScene("Assets/Scenes/RecyclingListDemo.unity", true);


		[MenuItem(StringConstants.SCENE_MENU_GENERATOR_HEADER + "Editor/SampleSceneTMPOnly/Replace", false, 2507)]
		private static void OpenScene_Editor_3() => OpenScene("Assets/Scenes/SampleSceneTMPOnly.unity", false);

		[MenuItem(StringConstants.SCENE_MENU_GENERATOR_HEADER + "Editor/SampleSceneTMPOnly/Additive", false, 2508)]
		private static void OpenScene_Editor_3_Additive() => OpenScene("Assets/Scenes/SampleSceneTMPOnly.unity", true);


		[MenuItem(StringConstants.SCENE_MENU_GENERATOR_HEADER + "Editor/Scene/Replace", false, 2509)]
		private static void OpenScene_Editor_4() => OpenScene("Assets/Scene.unity", false);

		[MenuItem(StringConstants.SCENE_MENU_GENERATOR_HEADER + "Editor/Scene/Additive", false, 2510)]
		private static void OpenScene_Editor_4_Additive() => OpenScene("Assets/Scene.unity", true);


		[MenuItem(StringConstants.SCENE_MENU_GENERATOR_HEADER + "Editor/UiScene1/Replace", false, 2511)]
		private static void OpenScene_Editor_5() => OpenScene("Assets/Scenes/UiScene1.unity", false);

		[MenuItem(StringConstants.SCENE_MENU_GENERATOR_HEADER + "Editor/UiScene1/Additive", false, 2512)]
		private static void OpenScene_Editor_5_Additive() => OpenScene("Assets/Scenes/UiScene1.unity", true);



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
