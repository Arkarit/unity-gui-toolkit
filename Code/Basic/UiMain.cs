using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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

		[Header("File Settings")]

		[SerializeField]
		private string m_scenesPath = "Scenes/";

		[SerializeField]
		private bool m_unloadAdditionalScenesOnPlay = false;

		[Header("Canvas Settings")]

		[SerializeField]
		private RenderMode m_renderMode = RenderMode.ScreenSpaceCamera;

		[SerializeField]
		private float m_layerDistance = 1;
		
		private Camera m_camera;

		public float LayerDistance {get { return m_layerDistance; }}

		protected override void Awake()
		{
			base.Awake();
			m_camera = GetComponent<Camera>();

			if (Application.isPlaying)
				DontDestroyOnLoad(gameObject);

			SetViews();

#if UNITY_EDITOR
			OnlyActiveScene.s_scenesPath = m_scenesPath;
			OnlyActiveScene.s_unloadAdditionalScenesOnPlay = m_unloadAdditionalScenesOnPlay;
#endif
		}

protected override void Update()
{
base.Update();
if (Application.isPlaying && count >= 0)
{
count += Time.deltaTime;
if (count > 3)
{
Show("UiScene1");
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
			OnlyActiveScene.s_scenesPath = m_scenesPath;
			OnlyActiveScene.s_unloadAdditionalScenesOnPlay = m_unloadAdditionalScenesOnPlay;
		}
#endif

		public void Show(string _path)
		{
			if (s_views.ContainsKey(_path))
			{
				s_views[_path].Show();
				return;
			}
			StartCoroutine(LoadAsyncScene(_path));
		}

		public void Hide(string _name)
		{

		}

		IEnumerator LoadAsyncScene(string _name)
		{
			// The Application loads the Scene in the background as the current Scene runs.
			// This is particularly good for creating loading screens.
			// You could also load the Scene by using sceneBuildIndex. In this case Scene2 has
			// a sceneBuildIndex of 1 as shown in Build Settings.

			AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(m_scenesPath + _name, LoadSceneMode.Additive);

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
					view.Show();
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