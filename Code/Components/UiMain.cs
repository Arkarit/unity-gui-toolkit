using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
using UnityEditor;
#endif

namespace GuiToolkit
{
	[RequireComponent(typeof(Camera))]
	[RequireComponent(typeof(UiPool))]
	[ExecuteAlways]
	public class UiMain : UiThing
	{
		[Header("Canvas Settings")]

		[SerializeField]
		private RenderMode m_renderMode = RenderMode.ScreenSpaceCamera;

		[SerializeField]
		private float m_layerDistance = 0.02f;

		private readonly Dictionary<string, UiView> m_scenes = new Dictionary<string, UiView>();

		public float LayerDistance => m_layerDistance;
		public RenderMode RenderMode => m_renderMode;
		public Camera Camera { get; private set; }
		public UiPool UiPool { get; private set; }

		private static UiMain s_instance;
		public static UiMain Instance
		{
			get
			{
				if (s_instance == null)
					s_instance = UnityEngine.Object.FindObjectOfType<UiMain>();
#if UNITY_EDITOR
				if (s_instance == null)
					Debug.LogError("Attempt to access UiMain.Instance, but game object containing the instance not found." +
						" Please set up a game object with an attached UiMain component!");
#endif
				return s_instance;
			}
			private set
			{
				s_instance = value;
			}
		}

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

			AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(UiToolkitConfiguration.Instance.GetScenePath(_name), LoadSceneMode.Additive);

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
						SetDefaultSceneVisibilities(root);
						m_scenes[_name] = view;

						if (_show)
							view.ShowTopmost(_instant);

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

		#region "Stack"

		private readonly Stack<UiView> m_stack = new Stack<UiView>();

		[SerializeField]
		private EStackAnimationType m_stackAnimationType = EStackAnimationType.None;

		[SerializeField]
		private AnimationCurve m_stackMovedInCurve;

		[SerializeField]
		private AnimationCurve m_stackPushedOutCurve;

		public void NavigationPush( UiView _uiView, bool _instant = false, Action _onFinishHide = null, Action _onFinishShow = null )
		{
			if (m_stack.Count > 0)
			{
				UiView currentShown = m_stack.Peek();
				currentShown.SetStackAnimationType(m_stackAnimationType, false, m_stackPushedOutCurve);
				currentShown.Hide(_instant, _onFinishHide);
			}
			_uiView.SetStackAnimationType(m_stackAnimationType, true, m_stackMovedInCurve);
			_uiView.ShowTopmost(_instant, _onFinishShow);
			m_stack.Push(_uiView);
		}

		public void NavigationPop( int _skip = 0, bool _instant = false, Action _onFinishHide = null, Action _onFinishShow = null )
		{
			bool stackValid = m_stack.Count >= 1 + _skip;
			if (!stackValid)
			{
				Debug.LogError($"Attempting to pop {1 + _skip} from UiView stack, but stack contains only {m_stack.Count}");
				return;
			}
			UiView currentShown = m_stack.Pop();
			currentShown.SetStackAnimationType(m_stackAnimationType, true, m_stackPushedOutCurve);
			currentShown.Hide(_instant, _onFinishHide);

			for (int i=0; i<_skip; i++)
				m_stack.Pop();

			if (m_stack.Count > 0)
			{
				UiView nextShown = m_stack.Peek();
				nextShown.SetStackAnimationType(m_stackAnimationType, false, m_stackMovedInCurve);
				nextShown.ShowTopmost(_instant, _onFinishShow);
			}
		}

		public UiView Peek()
		{
			if (m_stack.Count == 0)
				return null;
			return m_stack.Peek();
		}

		#endregion

		#region "Builtin Dialogs"

		[SerializeField]
		private Transform m_requesterContainer;

		[SerializeField]
		private UiRequester m_requesterPrefab;

		[SerializeField]
		private UiPlayerSettingsDialog m_settingsDialogPrefab;

		[SerializeField]
		[FormerlySerializedAs("m_splashMessagePrefab")]
		private UiToastMessageView m_toastMessageViewPrefab;

		[SerializeField]
		private UiKeyPressRequester m_keyPressRequester;

		public void ShowSettingsDialog()
		{
			UiPlayerSettingsDialog settingsDialog = CreateView(m_settingsDialogPrefab);
			settingsDialog.Show();
		}

		public void ShowToastMessageView(string _message, float _duration = 2)
		{
			UiToastMessageView.HideAll(true);
			UiToastMessageView message = m_toastMessageViewPrefab.PoolInstantiate();
			message.transform.SetParent(transform, false);
			message.Show(_message, _duration);
		}

		public void OkRequester( string _title, string _text, UnityAction _onOk = null, string _okText = null )
		{
			UiRequester requester = CreateView(m_requesterPrefab);
			Debug.Assert(requester);
			requester.OkRequester(_title, _text, _onOk, _okText);
		}

		public void YesNoRequester( string _title, string _text, bool _allowOutsideTap, UnityAction _onOk,
			UnityAction _onCancel = null, string _yesText = null, string _noText = null )
		{
			UiRequester requester = CreateView(m_requesterPrefab);
			Debug.Assert(requester);
			requester.YesNoRequester(_title, _text, _allowOutsideTap, _onOk, _onCancel, _yesText, _noText);
		}

		public void OkCancelInputRequester( string _title, string _text, bool _allowOutsideTap,UnityAction<string> _onOk, UnityAction _onCancel = null, 
			 string _placeholderText = null, string _inputText = null, string _yesText = null, string _noText = null )
		{
			UiRequester requester = CreateView(m_requesterPrefab);
			Debug.Assert(requester);
			requester.OkCancelInputRequester(_title, _text, _allowOutsideTap, _onOk, _onCancel, _placeholderText, _inputText, _yesText, _noText);
		}

		public void KeyPressRequester( UnityAction<KeyCode> _onEvent, string _title = null )
		{
			UiKeyPressRequester requester = CreateView(m_keyPressRequester);
			Debug.Assert(requester);
			requester.Requester(_onEvent, _title);
		}

		private T CreateView<T>(T _template) where T : UiView
		{
			T result = _template.PoolInstantiate();
			result.transform.SetParent(m_requesterContainer, false);
			return result;
		}
		#endregion

		#region "General"

		[SerializeField]
		protected Camera[] m_camerasToDisableWhenFullScreenView;

		private readonly Dictionary<UiView,bool> m_savedVisibilities = new Dictionary<UiView,bool>();
		private readonly Dictionary<Camera,bool> m_savedCameraActivenesses = new Dictionary<Camera,bool>();

		/// Caution! This currently can be called for only ONE dialog at a time!
		public void SetFullScreenView( UiView _uiView )
		{
			bool set = _uiView != null;
			if (set)
			{
				m_savedVisibilities.Clear();
				m_savedCameraActivenesses.Clear();
			}
			else if (m_savedVisibilities.Count == 0)
				return;

			for (int i=0; i<m_camerasToDisableWhenFullScreenView.Length; i++)
			{
				Camera camera = m_camerasToDisableWhenFullScreenView[i];

				if (set)
				{
					m_savedCameraActivenesses.Add(camera, camera.enabled);
					camera.enabled = false;
					continue;
				}

				if (m_savedCameraActivenesses.TryGetValue(camera, out bool isActive))
					camera.enabled = isActive;
			}

			for (int i = 0; i < transform.childCount; i++)
			{
				Transform t = transform.GetChild(i);
				UiView view = t.GetComponent<UiView>();

				if (view == null)
					continue;

				// Views which are in front of the full screen view are not hidden
				if (view == _uiView)
					break;

				if (set)
				{
					m_savedVisibilities.Add(view, view.gameObject.activeSelf);
					view.gameObject.SetActive(false);
					continue;
				}

				if (m_savedVisibilities.TryGetValue(view, out bool isActive))
					view.gameObject.SetActive(isActive);
			}
		}

		public void SortViews()
		{
			SetPlaneDistancesBySiblingIndex();
			SetSiblingIndicesByPlaneDistances();
		}

		public void SetAsLastSiblingOfLayer(UiView _view)
		{
			_view.transform.SetAsLastSibling();
			SortViews();
		}

		private void SetPlaneDistancesBySiblingIndex()
		{
			var layers = EnumHelper.GetValues<EUiLayerDefinition>();
			foreach(var layer in layers)
			{
				int layercount = 0;
				foreach (Transform childTransform in transform)
				{
					UiView view = childTransform.GetComponent<UiView>();
					if (!view || view.Layer != layer)
						continue;

					int planeIndex = (int)layer - layercount;
					float planeDistance = LayerDistance * (float) planeIndex;
					view.InitView(RenderMode, Camera, planeDistance, (int) EUiLayerDefinition.Back - planeIndex);
					layercount++;
				}
			}
		}

		private class PlaneDistanceComparer : IComparer<Transform>
		{
			// Call CaseInsensitiveComparer.Compare with the parameters reversed.
			public int Compare(Transform a, Transform b)
			{
				Canvas canvasA = a.GetComponent<Canvas>();
				Canvas canvasB = b.GetComponent<Canvas>();
				if (canvasA == null)
					return canvasB != null ? -1 : 0;
				if (canvasB == null)
					return 1;
				if (canvasA.planeDistance == canvasB.planeDistance)
					return 0;
				return canvasA.planeDistance < canvasB.planeDistance ? 1 : -1;
			}
		}

		private void SetSiblingIndicesByPlaneDistances()
		{
			Transform[] children = transform.GetChildrenArray();
			PlaneDistanceComparer comparer = new PlaneDistanceComparer();
			Array.Sort(children, comparer);
			for (int i=0; i<children.Length; i++)
				children[i].SetSiblingIndex(i);
		}

		public void Quit()
		{
#if UNITY_EDITOR
			EditorApplication.isPlaying = false;
#else
			Application.Quit();
#endif
		}

		public void SetFocus( TMP_InputField _inputField )
		{
			if (!_inputField.gameObject.activeInHierarchy)
				return;

			_inputField.ActivateInputField();
			_inputField.StartCoroutine(SetCaretPositionDelayed(_inputField));
		}
		private IEnumerator SetCaretPositionDelayed(TMP_InputField _inputField)
		{
			yield return new WaitForEndOfFrame();
			_inputField.MoveToEndOfLine( false, true );
		}

		#endregion

		#region "Internal"

		protected override void Awake()
		{
			base.Awake();

			InitGetters();

			if (Application.isPlaying)
				DontDestroyOnLoad(gameObject);
		}

		protected virtual void Start()
		{
#if UNITY_EDITOR
			CheckSceneSetup();
#endif
			SetDefaultSceneVisibilities(gameObject);
		}

		//FIXME: performance. Need some "dirty" stuff.
		protected virtual void Update()
		{
			Instance = this;
			InitGetters();
			SortViews();
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

		private void InitGetters()
		{
			Camera = this.GetOrCreateComponent<Camera>();

			UiPool = this.GetOrCreateComponent<UiPool>();
			if (UiPool.m_container == null)
			{
				GameObject poolContainer = new GameObject("Pool");
				poolContainer.transform.SetParent(transform);
				UiPool.m_container = poolContainer.transform;
			}
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
			InitGetters();
			SortViews();
		}

#endif
		#endregion

		#region "Exclude from frustum culling"
		private readonly List<IExcludeFromFrustumCulling> m_excludedFromFrustumCulling = new List<IExcludeFromFrustumCulling>();
		private readonly List<Bounds> m_excludedBounds = new List<Bounds>();
		private const float LARGE_NUMBER = 999999999.0f;
		private static readonly Bounds LARGE_BOUNDS = new Bounds(Vector3.zero, new Vector3(LARGE_NUMBER, LARGE_NUMBER, LARGE_NUMBER) );

		public void RegisterExcludeFrustumCulling(IExcludeFromFrustumCulling _toRegister)
		{
			m_excludedFromFrustumCulling.Add(_toRegister);
			m_excludedBounds.Add(new Bounds());
		}

		public void UnregisterExcludeFrustumCulling(IExcludeFromFrustumCulling _toRemove)
		{
			int idx = m_excludedFromFrustumCulling.IndexOf(_toRemove);
			if (idx == -1)
			{
				Debug.LogError($"Could not find {_toRemove} in list of frustum culling exclusion list");
				return;
			}
			m_excludedFromFrustumCulling.RemoveAt(idx);
			m_excludedBounds.RemoveAt(idx);
		}

		private void OnPreCull()
		{
			for (int i=0; i<m_excludedFromFrustumCulling.Count; i++)
			{
				m_excludedBounds[i] = m_excludedFromFrustumCulling[i].GetMesh().bounds;
				m_excludedFromFrustumCulling[i].GetMesh().bounds = LARGE_BOUNDS;
			}
		}

		private void OnPostRender()
		{
			for (int i=0; i<m_excludedFromFrustumCulling.Count; i++)
				m_excludedFromFrustumCulling[i].GetMesh().bounds = m_excludedBounds[i];
		}
		#endregion
	}

}