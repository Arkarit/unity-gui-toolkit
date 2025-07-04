using GuiToolkit.Style;
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
			if (!settings.m_loadViewInEveryScene || !settings.m_uiMainPrefab || !settings.m_uiViewPrefab)
				return;
			
			var uiMain = Object.FindAnyObjectByType<UiMain>();
			if (uiMain && settings.m_exceptUiMainExists)
				return;

			if (!uiMain)
			{
				uiMain = Object.Instantiate(settings.m_uiMainPrefab);
				if (!uiMain)
				{
					Debug.LogError("Can not instantiate UIMain");
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
			var view = UiMain.Instance.CreateView(_settings.m_uiViewPrefab);
			view.Show();
		}
	}
}
