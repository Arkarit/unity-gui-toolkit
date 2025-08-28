#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Image = UnityEngine.UI.Image;
using Object = UnityEngine.Object;
using System.IO;

namespace GuiToolkit.Editor
{
	public static class ScreenshotOverlayTool
	{
		private static bool m_isPrefab;
		private static GameObject m_root;
		private static UiSpriteHolder m_spriteHolder;
		private static int m_width;
		private static int m_height;
		private static GameObject m_canvasGo;
		private static GameObject m_imageGameObject;
		private static Image m_image;
		private static Scene m_tempScene;
		private static string m_prefabPath;


		[MenuItem(StringConstants.CREATE_GUI_SCREENSHOT_OVERLAY)]
		public static void MakeScreenshotOverlay()
		{
			try
			{
				SafeMakeScreenshotOverlay();
			}
			catch (Exception e)
			{
				Debug.LogError(e.Message);
				throw;
			}
			finally
			{
				EditorUtility.ClearProgressBar();
			}
		}

		private static void SafeMakeScreenshotOverlay()
		{
			m_root = null;
			var scene = EditorCodeUtility.GetCurrentContextScene(out m_isPrefab);
			if (!scene.IsValid())
			{
				Debug.LogError("No valid scene or prefab stage.");
				return;
			}

			// 1) size like GameView
			m_width = Mathf.Max(64, Mathf.RoundToInt(UiUtility.ScreenWidth()));
			m_height = Mathf.Max(64, Mathf.RoundToInt(UiUtility.ScreenHeight()));

			Log("Creating Overlay", 0);
			CreateOverlay();

			if (m_isPrefab)
			{
				Log("Creating Temp Scene", 15f);
				CreateTempScene();
			}

			Log("Creating Temp Camera", .3f);
			var cam = CreateTempCamera();

			Log("Switch Overlay canvases to ScreenSpaceCamera", .45f);
			var canvasSnaps = SwitchOverlayCanvasesToCamera(cam);

			Log("Render Texture", .6f);
			CaptureCameraToTexture(cam, m_width, m_height);

			try
			{
				Log("Restore Canvases", .75f);
				RestoreCanvasSnapshots(canvasSnaps);
			}
			finally
			{
				if (cam != null && cam.gameObject.name == "__ScreenshotCamera__")
					UnityEngine.Object.DestroyImmediate(cam.gameObject);
			}

			if (m_isPrefab)
			{
				Log("Destroy Temp Scene", .9f);
				DestroyTempScene();
			}

			// Done
			Debug.Log($"Screenshot overlay created ({m_width}x{m_height}) at 50% opacity.");
		}

		private static Scene m_activeScene;
		private static GameObject m_instancedPrefab;
		private static readonly List<Scene> m_scenesLoaded = new();

		private static void CreateTempScene()
		{
			var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
			m_prefabPath = prefabStage.assetPath;
			var prefabObj = AssetDatabase.LoadAssetAtPath<GameObject>(prefabStage.assetPath);
			for (int i = 0; i < SceneManager.sceneCount; i++)
			{
				var sc = SceneManager.GetSceneAt(i);
				if (!sc.isLoaded)
					continue;

				m_scenesLoaded.Add(sc);
			}

			m_tempScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
			m_activeScene = SceneManager.GetActiveScene();
			SceneManager.SetActiveScene(m_tempScene);
			foreach (var scene in m_scenesLoaded)
				EditorSceneManager.CloseScene(scene, false);

			m_instancedPrefab = (GameObject)PrefabUtility.InstantiatePrefab(prefabObj, m_tempScene);
		}

		private static void DestroyTempScene()
		{
			foreach (var scene in m_scenesLoaded)
				EditorSceneManager.OpenScene(scene.path, OpenSceneMode.Additive);

			SceneManager.SetActiveScene(m_activeScene);
			EditorSceneManager.CloseScene(m_tempScene, true);
			PrefabStageUtility.OpenPrefab(m_prefabPath);
			m_scenesLoaded.Clear();
		}

		private static void CreateOverlay()
		{
			// ensure a topmost canvas separate from project UI
			m_canvasGo = new GameObject("__ScreenshotOverlayCanvas__");
			SceneManager.MoveGameObjectToScene(m_canvasGo, SceneManager.GetActiveScene());
			if (m_isPrefab)
			{
				m_root = GetCurrentPrefabStage().prefabContentsRoot;
				m_canvasGo.transform.SetParent(m_root.transform);
				var rt2 = m_canvasGo.AddComponent<RectTransform>();
				rt2.anchorMin = Vector2.zero;
				rt2.anchorMax = Vector2.one;
				rt2.offsetMin = Vector2.zero;
				rt2.offsetMax = Vector2.zero;
			}

			var canvas = m_canvasGo.AddComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			canvas.sortingOrder = 32760; // very high
			canvas.overrideSorting = true;
			var raycaster = m_canvasGo.AddComponent<GraphicRaycaster>();
			raycaster.ignoreReversedGraphics = true;

			// Image child
			m_imageGameObject = new GameObject("__ScreenshotOverlay__");
			m_imageGameObject.transform.SetParent(m_canvasGo.transform, false);
			var rt = m_imageGameObject.AddComponent<RectTransform>();
			rt.anchorMin = Vector2.zero;
			rt.anchorMax = Vector2.one;
			rt.offsetMin = Vector2.zero;
			rt.offsetMax = Vector2.zero;
			m_spriteHolder = m_imageGameObject.AddComponent<UiSpriteHolder>();
			m_spriteHolder.Create("__SceneScreenshotSprite__", m_width, m_height);
			m_image = m_imageGameObject.AddComponent<Image>();
			m_image.raycastTarget = false;
			m_image.sprite = m_spriteHolder.Sprite;
			m_image.color = new Color(1f, 1f, 1f, 0.5f); // 50% opacity

			if (m_isPrefab)
			{
				var stage = PrefabStageUtility.GetCurrentPrefabStage();
				PrefabUtility.SaveAsPrefabAsset(stage.prefabContentsRoot, stage.assetPath);
			}
			else
			{
				var scene = SceneManager.GetActiveScene();
				EditorSceneManager.MarkSceneDirty(scene);
			}

			SceneVisibilityManager.instance.DisablePicking(m_canvasGo, true);
		}

		// --------------------------------------------------------------------
		// Helpers
		// --------------------------------------------------------------------

		private static Camera CreateTempCamera()
		{
			Log("Creating Temp Camera Game Object", .32f);
			var go = new GameObject("__ScreenshotCamera__");
			SceneManager.MoveGameObjectToScene(go, SceneManager.GetActiveScene());

			Log("Adding Camera Component", .34f);
			var cam = go.AddComponent<Camera>();

			Log("Setting camera properties", .36f);
			cam.clearFlags = CameraClearFlags.SolidColor;
			cam.backgroundColor = new Color(.5f, .5f, .5f, 0);
			cam.cullingMask = 1 << LayerMask.NameToLayer("UI");
			cam.orthographic = true;
			cam.forceIntoRenderTexture = true;
			cam.enabled = false;
			return cam;
		}

		private struct CanvasSnapshot
		{
			public Canvas Canvas;
			public RenderMode RenderMode;
			public Camera WorldCamera;
			public int SortingOrder;
			public bool OverrideSorting;
		}

		private static List<CanvasSnapshot> SwitchOverlayCanvasesToCamera( Camera cam )
		{
			var snaps = new List<CanvasSnapshot>();

			var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
			foreach (var c in canvases)
			{
				if (!c)
					continue;

				snaps.Add(new CanvasSnapshot
				{
					Canvas = c,
					RenderMode = c.renderMode,
					WorldCamera = c.worldCamera,
					SortingOrder = c.sortingOrder,
					OverrideSorting = c.overrideSorting
				});

				if (c.renderMode == RenderMode.ScreenSpaceOverlay)
				{
					c.renderMode = RenderMode.ScreenSpaceCamera;
					c.worldCamera = cam;
					c.overrideSorting = true;
				}
			}

			Canvas.ForceUpdateCanvases();
			return snaps;
		}

		private static void RestoreCanvasSnapshots( List<CanvasSnapshot> snaps )
		{
			if (snaps == null)
				return;

			foreach (var s in snaps)
			{
				if (!s.Canvas) continue;
				s.Canvas.renderMode = s.RenderMode;
				s.Canvas.worldCamera = s.WorldCamera;
				s.Canvas.sortingOrder = s.SortingOrder;
				s.Canvas.overrideSorting = s.OverrideSorting;
			}
			Canvas.ForceUpdateCanvases();
		}

		private static void CaptureCameraToTexture( Camera cam, int width, int height )
		{
			var rt = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
			var prevRT = cam.targetTexture;

			try
			{
				if (m_isPrefab)
				{
					var canvas = m_instancedPrefab.GetComponent<Canvas>();

					// We need a canvas to render; if none is set, create a temporary one
					if (!canvas)
					{
						Log("Adding Canvas wrapper", .63f);
						var wrapper = new GameObject("__TempCanvasRoot__");
						SceneManager.MoveGameObjectToScene(wrapper, m_tempScene);
						wrapper.layer = LayerMask.NameToLayer("UI");

						canvas = wrapper.AddComponent<Canvas>();
						canvas.renderMode = RenderMode.ScreenSpaceCamera;
						canvas.worldCamera = cam;
						canvas.overrideSorting = true;

						var scaler = wrapper.AddComponent<CanvasScaler>();
						scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
						scaler.referencePixelsPerUnit = 100f;

						m_instancedPrefab.transform.SetParent(wrapper.transform, false);
					}
				}

				cam.targetTexture = rt;
				Log("Render", .66f);
				cam.Render();

				Log("Applying rendered texture", .69f);
				var holder = Object.FindAnyObjectByType<UiSpriteHolder>(FindObjectsInactive.Include);
				var tmpTex = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
				RenderTexture.active = rt;
				tmpTex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
				tmpTex.Apply(false, false);
				var png = tmpTex.EncodeToPNG();
				Object.DestroyImmediate(tmpTex);

				// make sure you're modifying *serialized* data on the instance:
				holder.SetFromPngBytes(png, width, height);

				if (m_isPrefab)
				{
					Log("Apply Prefab", .72f);
					// Get prefab asset path of the instanced prefab
					var prefabAsset = PrefabUtility.GetCorrespondingObjectFromSource(m_instancedPrefab);
					var assetPath = AssetDatabase.GetAssetPath(prefabAsset);
					if (string.IsNullOrEmpty(assetPath))
					{
						Debug.LogError("Could not resolve prefab asset path.");
					}
					else
					{
						// Open prefab contents off-stage, patch bytes, save, unload
						var root = PrefabUtility.LoadPrefabContents(assetPath);
						try
						{
							var holderInAsset = root.GetComponentInChildren<UiSpriteHolder>(true);
							holderInAsset.SetFromPngBytes(png, width, height);
							PrefabUtility.SaveAsPrefabAsset(root, assetPath);
						}
						finally
						{
							PrefabUtility.UnloadPrefabContents(root);
						}
					}
				}

				Log("Save Assets", .73f);
				AssetDatabase.SaveAssets();
			}
			finally
			{
				cam.targetTexture = prevRT;
				RenderTexture.ReleaseTemporary(rt);
			}
		}

		private static PrefabStage GetCurrentPrefabStage() => PrefabStageUtility.GetCurrentPrefabStage();

		private static StageHandle CurrentStageHandle()
		{
			var ps = GetCurrentPrefabStage();
			return ps != null ? ps.stageHandle : StageUtility.GetMainStage().stageHandle;
		}

		private static void PlaceInCurrentStage( GameObject go )
		{
			var ps = GetCurrentPrefabStage();
			if (ps != null)
				StageUtility.PlaceGameObjectInCurrentStage(go);           // << wichtig für Prefab Mode
			else
				SceneManager.MoveGameObjectToScene(go, SceneManager.GetActiveScene());
		}

		private static void Log( string _s, float _progress )
		{
			EditorUtility.DisplayProgressBar($"Creating GUI Screenshot overlay", _s, _progress);
			Debug.Log(_s);
		}
	}
}
#endif
