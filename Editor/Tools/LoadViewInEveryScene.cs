using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace GuiToolkit
{
	[InitializeOnLoad]
	public static class LoadViewInEveryScene
	{
		static LoadViewInEveryScene()
		{
			SceneManager.sceneLoaded += OnSceneLoaded;
		}

		private static void OnSceneLoaded(Scene _scene, LoadSceneMode _loadSceneMode)
		{
			if (!Application.isPlaying)
				return;

			UiToolkitConfiguration settings = UiToolkitConfiguration.Instance;
			if (!settings.LoadViewInEveryScene || !settings.UiMainPrefab || !settings.UiViewPrefab)
				return;
			
			var uiMain = Object.FindAnyObjectByType<UiMain>();
			if (uiMain && settings.ExceptUiMainExists)
				return;

			if (!uiMain)
			{
				uiMain = Object.Instantiate(settings.UiMainPrefab);
				if (!uiMain)
				{
					UiLog.LogError("Can not instantiate UIMain");
					return;
				}
			}
			
			var eventSystem = Object.FindAnyObjectByType<EventSystem>();
			if (!eventSystem)
			{
				var go = new GameObject("EventSystem");
				go.AddComponent<EventSystem>();
				go.AddComponent<StandaloneInputModule>();
			}
			
			// We need UiMain to settle before we can create a view
			CoRoutineRunner.Instance.StartCoroutine(DelayedCreateView(settings));
		}
		
		private static IEnumerator DelayedCreateView(UiToolkitConfiguration _settings)
		{
			yield return 0;
			var view = UiMain.Instance.CreateView(_settings.UiViewPrefab);
			view.Show();
		}
	}
}
