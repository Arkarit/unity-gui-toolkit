using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace GuiToolkit
{
	[RequireComponent(typeof(Camera))]
	[ExecuteAlways]
	public class UiMain : UiThing
	{
		[Header("Canvas Settings")]

		[SerializeField]
		private RenderMode m_renderMode = RenderMode.ScreenSpaceCamera;

		[SerializeField]
		private float m_layerDistance = 0.02f;

		[SerializeField]
		private Transform m_requesterContainer;

		private Camera m_camera;
		private readonly Dictionary<string, UiView> m_views = new Dictionary<string, UiView>();
		private UiRequester m_requester;

		public float LayerDistance {get { return m_layerDistance; }}

		public static UiMain Instance { get; private set; }

		public void LoadScene(string _sceneName, bool _show = true, Action<UiView> _whenLoaded = null)
		{
			if (m_views.ContainsKey(_sceneName))
			{
				if (_show)
					m_views[_sceneName].Show();

				if (_whenLoaded != null)
					_whenLoaded.Invoke(m_views[_sceneName]);
				return;
			}
			StartCoroutine(LoadAsyncScene(_sceneName, true, _whenLoaded));
		}

		public void HideScene(string _sceneName)
		{
			if (m_views.ContainsKey(_sceneName))
				m_views[_sceneName].Hide();
		}

		public void OkRequester(string _title, string _text, Action _onClosed = null, UiRequester.Options _options = null)
		{
			if (m_requester == null)
			{
				Debug.LogError("Attempt to create requester, but template in UiMain m_requester is null");
				return;
			}

			UiRequester requester = (UiRequester)CreateModalDialog(m_requester);
			Debug.Assert(requester);
			requester.OkRequester(_title, _text, _onClosed, _options);
		}

		public void Quit()
		{
#if UNITY_EDITOR
			if (Application.isPlaying)
				Debug.Log("Application can not quit in Editor play mode. In the real application it will quit here.");
			else
				Debug.LogError("Application is not running. Nothing to quit.");
#else
			Application.Quit();
#endif
		}

		public void YesNoRequester(string _title, string _text, Action _onOk, Action _onCancel = null, UiRequester.Options _options = null)
		{
			if (m_requester == null)
			{
				Debug.LogError("Attempt to create requester, but template in UiMain m_requester is null");
				return;
			}

			UiRequester requester = (UiRequester)CreateModalDialog(m_requester);
			Debug.Assert(requester);
			requester.YesNoRequester(_title, _text, _onOk, _onCancel, _options);
		}

		protected override void Awake()
		{
			base.Awake();

			m_camera = GetComponent<Camera>();

			if (Application.isPlaying)
				DontDestroyOnLoad(gameObject);

			ReInit();
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			Instance = null;
		}

		protected void OnTransformChildrenChanged()
		{
			ReInit();
		}

#if UNITY_EDITOR
		private void OnValidate()
		{
			ReInit();
			foreach (var kv in m_views)
				kv.Value.SetRenderMode(m_renderMode, GetComponent<Camera>());
		}
#endif

		private UiViewModal CreateModalDialog(UiViewModal _template)
		{
			float lowestLayer = 100000f;
			foreach (var kv in m_views)
			{
				UiView view = kv.Value;
				if (!(view is UiViewModal) )
					continue;

				if (view.Canvas.planeDistance < lowestLayer)
					lowestLayer = view.Canvas.planeDistance;
			}
			float newLayer = lowestLayer - LayerDistance;

			UiViewModal result = Instantiate(_template);
			result.transform.SetParent(m_requesterContainer);
			result.SetRenderMode(m_renderMode, m_camera);
			result.Canvas.planeDistance = newLayer;

			return result;
		}

		private IEnumerator LoadAsyncScene(string _name, bool _show, Action<UiView> _whenLoaded)
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
					ReInit();

					//Don't call Hide() here, since the view may auto destroy on hide...
					view.gameObject.SetActive(false);
					if (_show)
						view.Show();

					if (_whenLoaded != null)
						_whenLoaded.Invoke(view);

					break;
				}
			}

			roots = scene.GetRootGameObjects();
			foreach (var root in roots)
				Destroy(root);

			SceneManager.UnloadSceneAsync(scene);
		}

		private void ReInit()
		{
			Instance = this;

			m_views.Clear();
			m_requester = null;

			foreach (Transform child in transform)
			{
				UiView uiView = child.GetComponent<UiView>();
				if (uiView == null)
					continue;
				if (string.IsNullOrEmpty(uiView.m_name))
					uiView.m_name = uiView.gameObject.name;

				uiView.InitEvents();

				if (uiView is UiRequester)
				{
					ReInitRequester(uiView);
				}

				bool keyFound = m_views.ContainsKey(uiView.m_name);

				Debug.Assert(!keyFound, $"Duplicate UiView name '{uiView.m_name}' found. (Check also game object name if UiView Name is not set)");
				if (keyFound)
					continue;

				m_views.Add(uiView.m_name, uiView);
			}

			SetSortOrder();
		}

		private void ReInitRequester( UiView uiView )
		{
			if (m_requester != null)
			{
				Debug.LogError("Multiple UiRequester detected. There can be only one currently.");
				return;
			}

			m_requester = (UiRequester)uiView;
			if (Application.isPlaying)
				m_requester.gameObject.SetActive(false);
		}

		private void SetSortOrder()
		{
		}

	}
}