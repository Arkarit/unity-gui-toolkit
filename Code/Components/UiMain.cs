using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace GuiToolkit
{
	[RequireComponent(typeof(Camera))]
	[ExecuteAlways]
	public class UiMain : UiThing
	{
		private readonly static Dictionary<string, UiView> s_views = new Dictionary<string, UiView>();
private float count;

		[Header("Canvas Settings")]

		[SerializeField]
		private RenderMode m_renderMode = RenderMode.ScreenSpaceCamera;

		[SerializeField]
		private float m_layerDistance = 1;


		[System.Serializable]
		private class CEvLoad : UnityEvent<string,Action<UiView>, bool> {}
		private static CEvLoad EvLoad = new CEvLoad();
		
		private Camera m_camera;

		public float LayerDistance {get { return m_layerDistance; }}

		protected override void Awake()
		{
			base.Awake();
			m_camera = GetComponent<Camera>();

			if (Application.isPlaying)
				DontDestroyOnLoad(gameObject);

			SetViews();
		}

		protected override void AddEventListeners()
		{
			EvLoad.AddListener(OnEvLoad);
		}

		protected override void RemoveEventListeners()
		{
			EvLoad.RemoveListener(OnEvLoad);
		}

		private void OnEvLoad( string _sceneName, Action<UiView> _whenLoaded, bool _show )
		{
			if (_show)
				Show(_sceneName, _whenLoaded);
			else
				Load(_sceneName, _whenLoaded);
		}

		public static void InvokeShow(string _sceneName, Action<UiView> _whenLoaded = null)
		{
			EvLoad.Invoke(_sceneName, _whenLoaded, true);
		}

		public static void InvokeLoad(string _sceneName, Action<UiView> _whenLoaded = null)
		{
			EvLoad.Invoke(_sceneName, _whenLoaded, false);
		}

protected override void Update()
{
base.Update();
if (Application.isPlaying && count >= 0)
{
count += Time.deltaTime;
if (count > 3)
{
UiMain.InvokeLoad("UiScene1", (dummy)=> UiSplashMessage.InvokeShow("Hello World"));
count = -1;
}
}
}

#if UNITY_EDITOR
		public void OnValidate()
		{
			SetViews();
			foreach (var kv in s_views)
				kv.Value.SetRenderMode(m_renderMode, GetComponent<Camera>());
		}
#endif

		public void Show(string _sceneName, Action<UiView> _whenLoaded = null)
		{
			if (s_views.ContainsKey(_sceneName))
			{
				s_views[_sceneName].Show();
				_whenLoaded?.Invoke(s_views[_sceneName]);
				return;
			}
			StartCoroutine(LoadAsyncScene(_sceneName, true, _whenLoaded));
		}

		public void Load(string _sceneName, Action<UiView> _whenLoaded = null)
		{
			if (s_views.ContainsKey(_sceneName))
			{
				_whenLoaded?.Invoke(s_views[_sceneName]);
				return;
			}
			StartCoroutine(LoadAsyncScene(_sceneName, false, _whenLoaded));
		}

		public void Hide(string _name)
		{

		}

		IEnumerator LoadAsyncScene(string _name, bool _show, Action<UiView> _whenLoaded)
		{
			// The Application loads the Scene in the background as the current Scene runs.
			// This is particularly good for creating loading screens.
			// You could also load the Scene by using sceneBuildIndex. In this case Scene2 has
			// a sceneBuildIndex of 1 as shown in Build Settings.

			AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(UiSettings.Instance.GetScenePath(_name), LoadSceneMode.Additive);

			if (asyncLoad == null)
				yield break;

			// Wait until the asynchronous scene fully loads
			while (!asyncLoad.isDone)
			{
				yield return null;
			}

			Scene scene = SceneManager.GetSceneByName(_name);
			if (scene == null)
				yield break;

			var roots = scene.GetRootGameObjects();
			foreach (var root in roots)
			{
				UiView view = root.GetComponentInChildren<UiView>();
				if (view)
				{
					view.m_name = _name;
					view.gameObject.name = _name;
					view.transform.SetParent(transform);
					view.SetRenderMode(m_renderMode, m_camera);
					SetViews();
					view.Hide(true);
					if (_show)
						view.Show();

					_whenLoaded?.Invoke(view);

					break;
				}
			}

			roots = scene.GetRootGameObjects();
			foreach (var root in roots)
				Destroy(root);

			SceneManager.UnloadSceneAsync(scene);
		}

		private void SetViews()
		{
			s_views.Clear();

			foreach (Transform child in transform)
			{
				UiView uiView = child.GetComponent<UiView>();
				if (uiView == null)
					continue;
				if (string.IsNullOrEmpty(uiView.m_name))
					uiView.m_name = uiView.gameObject.name;

				bool keyFound = s_views.ContainsKey(uiView.m_name);

				Debug.Assert(!keyFound, $"Duplicate UiView name '{uiView.m_name}' found. (Check also game object name if UiView Name is not set)");
				if (keyFound)
					continue;

				s_views.Add(uiView.m_name, uiView);
			}

			SetSortOrder();
		}

		private void SetSortOrder()
		{
		}

	}
}