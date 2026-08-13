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
				go.AddComponent(ResolveInputModuleType());
			}
			
			// We need UiMain to settle before we can create a view
			CoRoutineRunner.Instance.StartCoroutine(DelayedCreateView(settings));
		}
		
		/// <summary>
		/// The input module matching this project's Active Input Handling.
		/// </summary>
		/// <remarks>
		/// StandaloneInputModule reads the legacy Input class, so under "Input System Package (New)" it
		/// throws instead of working — the same fault as the input proxy had, just in UGUI's event
		/// plumbing. Resolved by name rather than by a direct reference so this editor assembly needs no
		/// dependency on an optional package; if the type is not there, the legacy module was the right
		/// answer anyway.
		/// </remarks>
		private static System.Type ResolveInputModuleType()
		{
#if ENABLE_INPUT_SYSTEM
			var type = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
			if (type != null)
				return type;
#endif
			return typeof(StandaloneInputModule);
		}

		private static IEnumerator DelayedCreateView(UiToolkitConfiguration _settings)
		{
			yield return 0;
			var view = UiMain.Instance.CreateView(_settings.UiViewPrefab);
			view.Show();
		}
	}
}
