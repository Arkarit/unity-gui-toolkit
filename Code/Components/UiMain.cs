using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
using UnityEditor;
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

		[SerializeField]
		private UiRequester m_requesterPrefab;

		[SerializeField]
		private UiSplashMessage m_splashMessagePrefab;

		private Camera m_camera;
		private readonly Dictionary<string, UiView> m_scenes = new Dictionary<string, UiView>();


		public float LayerDistance {get { return m_layerDistance; }}

		public static UiMain Instance { get; private set; }

		#region "Scene Loading"
		public void LoadScene(string _sceneName, bool _show = true, bool _instant = false, Action<UiView> _whenLoaded = null)
		{
			if (CheckSceneValid(_sceneName))
			{
				if (_show)
					m_scenes[_sceneName].Show();

				if (_whenLoaded != null)
					_whenLoaded.Invoke(m_scenes[_sceneName]);
				return;
			}
			StartCoroutine(LoadAsyncScene(_sceneName, _show, _instant, _whenLoaded));
		}

		public void HideScene(string _sceneName, bool _instant = false)
		{
			if (CheckSceneValid(_sceneName))
				m_scenes[_sceneName].Hide( _instant );
		}

		public void ShowScene(string _sceneName, bool _instant = false)
		{
			LoadScene(_sceneName, true, _instant);
		}

		public void UnloadScene(string _sceneName, bool _instant = false)
		{
			if (CheckSceneValid(_sceneName))
			{
				m_scenes[_sceneName].Hide( _instant, () => DestroyScene(_sceneName) );
			}
		}

		private void DestroyScene( string _sceneName )
		{
			if (CheckSceneValid(_sceneName))
			{
				m_scenes[_sceneName].gameObject.Destroy();
				m_scenes.Remove(_sceneName);
			}
		}

		private IEnumerator LoadAsyncScene(string _name, bool _show, bool _instant, Action<UiView> _whenLoaded)
		{
			// The Application loads the Scene in the background as the current Scene runs.
			// This is particularly good for creating loading screens.
			// You could also load the Scene by using sceneBuildIndex. In this case Scene2 has
			// a sceneBuildIndex of 1 as shown in Build Settings.

			AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(UiSettings.Instance.GetScenePath(_name), LoadSceneMode.Additive);

			if (asyncLoad == null)
				yield break;

			asyncLoad.completed += (AsyncOperation operation) => 
			{
				Scene scene = SceneManager.GetSceneByName(_name);
				if (scene == null)
					return;

				var roots = scene.GetRootGameObjects();
				foreach (var root in roots)
				{
					UiView view = root.GetComponentInChildren<UiView>(true);
					if (view)
					{
						SceneManager.MoveGameObjectToScene(view.gameObject, SceneManager.GetSceneAt(0));

						view.gameObject.name = _name;
						view.transform.SetParent(transform, false);
						view.Init(m_renderMode, m_camera);
						SetDefaultSceneVisibilities(root);
						m_scenes[_name] = view;

						if (_show)
							view.Show(_instant);

						if (_whenLoaded != null)
							_whenLoaded.Invoke(view);

						break;
					}
				}

				roots = scene.GetRootGameObjects();
				foreach (var root in roots)
					Destroy(root);

				SceneManager.UnloadSceneAsync(scene);
			};

			yield return asyncLoad;

		}
		#endregion

		#region "Global Events"
		public void SetTag(string _tag, bool _instant = false)
		{
			UiView.EvSetTag.Invoke(_tag, _instant);
		}
		#endregion

		#region "Builtin Dialogs"
		public void SplashMessage(string _message, float _duration = 2)
		{
			UiView.InvokeHideInstant<UiSplashMessage>();
			UiSplashMessage message = m_splashMessagePrefab.PoolInstantiate();
			message.transform.SetParent(transform, false);
			message.Init(m_renderMode, m_camera);
			message.Show(_message, _duration);
		}

		public void OkRequester( string _title, string _text, UnityAction _onOk = null, string _okText = null )
		{
			UiRequester requester = CreateModalDialog(m_requesterPrefab);
			Debug.Assert(requester);
			requester.OkRequester(_title, _text, _onOk, _okText);
		}

		public void YesNoRequester( string _title, string _text, bool _allowOutsideTap, UnityAction _onOk,
			UnityAction _onCancel = null, string _yesText = null, string _noText = null )
		{
			UiRequester requester = CreateModalDialog(m_requesterPrefab);
			Debug.Assert(requester);
			requester.YesNoRequester(_title, _text, _allowOutsideTap, _onOk, _onCancel, _yesText, _noText);
		}

		private T CreateModalDialog<T>( T _template) where T : UiViewModal
		{
			bool foundOtherModalDialog = false;
			float lowestLayer = 100000f;

			foreach (var kv in m_scenes)
			{
				UiView view = kv.Value;
				if (!(view is UiViewModal) )
					continue;

				foundOtherModalDialog = true;
				if (view.Canvas.planeDistance < lowestLayer)
					lowestLayer = view.Canvas.planeDistance;
			}

			T result = _template.PoolInstantiate();
			result.transform.SetParent(m_requesterContainer, false);
			result.Init(m_renderMode, m_camera);

			// If another dialog was found, we place the new modal dialog above the highest dialog
			if (foundOtherModalDialog)
			{
				float newLayer = lowestLayer - LayerDistance;
				result.Canvas.planeDistance = newLayer;
			}

			return result;
		}
		#endregion

		#region "General"
		public void Quit()
		{
#if UNITY_EDITOR
			EditorApplication.isPlaying = false;
#else
			Application.Quit();
#endif
		}
		#endregion

		#region "Internal"
		protected override void Awake()
		{
			base.Awake();

			m_camera = GetComponent<Camera>();

			if (Application.isPlaying)
				DontDestroyOnLoad(gameObject);

			Instance = this;
		}

		protected virtual void Start()
		{
#if UNITY_EDITOR
			CheckSceneSetup();
#endif
			SetOrder(); 
			SetDefaultSceneVisibilities(gameObject);
		}

		private static void SetDefaultSceneVisibilities(GameObject _gameObject)
		{
			MonoBehaviour[] monoBehaviours = _gameObject.GetComponentsInChildren<MonoBehaviour>(true);
			foreach( MonoBehaviour monoBehaviour in monoBehaviours )
				if (monoBehaviour is ISetDefaultSceneVisibility)
					((ISetDefaultSceneVisibility)monoBehaviour).SetDefaultSceneVisibility();
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			Instance = null;
		}

		protected void OnTransformChildrenChanged()
		{
			SetOrder();
		}


		private void SetOrder()
		{
			UiView[] views = GetComponentsInChildren<UiView>(true);
			foreach (var view in views)
				view.Init(m_renderMode, GetComponent<Camera>());
		}

		private bool CheckSceneValid(string _sceneName)
		{
			if (!m_scenes.ContainsKey(_sceneName))
				return false;

			if (m_scenes[_sceneName] == null)
			{
				m_scenes.Remove(_sceneName);
				return false;
			}

			return true;
		}
		#endregion

		#region "Editor"
#if UNITY_EDITOR
		// Catch the most common errors in scene setup
		private void CheckSceneSetup()
		{
			if (m_renderMode == RenderMode.ScreenSpaceCamera || m_renderMode == RenderMode.WorldSpace)
			{
				Camera[] cameras = Camera.allCameras;
				Camera uiCamera = GetComponent<Camera>();
				if (uiCamera == null)
					throw new Exception($"UiMain GameObject '{gameObject.name}' needs an attached camera!");

				int uiLayer = 1 << LayerMask.NameToLayer("UI");
				if ((uiCamera.cullingMask & uiLayer) == 0)
				{
					Debug.LogError($"UiMain camera on GameObject '{gameObject.name}' needs the culling mask 'UI' flag set!");
				}

				float uiCameraDepth = uiCamera.depth;

				foreach (var camera in cameras)
				{
					if (camera == uiCamera || camera.targetTexture != null)
						continue;

					if (camera.depth >= uiCameraDepth)
					{
						Debug.LogError($"UI Camera needs highest depth. Camera depth {camera.depth} detected on camera '{camera.gameObject.name}', which is >= Ui camera depth ({uiCameraDepth})" );
					}
				}
			}
		}

		private void OnValidate()
		{
			Instance = this;
			SetOrder();
		}

#endif
		#endregion

	}

}