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

namespace GuiToolkit.Editor
{
	public static class ScreenshotOverlayTool
	{
		private struct CanvasSnapshot
		{
			public Canvas Canvas;
			public RenderMode RenderMode;
			public Camera WorldCamera;
			public int SortingOrder;
			public bool OverrideSorting;
		}

		// TODO: Extract TempScene handling into a reusable context (PrefabStageTempSceneContext).
		// Keep it simple for now – current one-off logic is fine until multiple tools need it.

		private static bool m_isPrefab;
		private static GameObject m_root;
		private static UiSpriteHolder m_spriteHolder;
		private static int m_width;
		private static int m_height;
		private static GameObject m_canvasGameObject;
		private static GameObject m_imageGameObject;
		private static Image m_image;
		private static Scene m_tempScene;
		private static string m_prefabPath;
		private static Camera m_tempCamera;
		private static Scene m_activeScene;
		private static GameObject m_instancedPrefab;
		private static readonly List<Scene> m_scenesLoaded = new();
		private static readonly List<CanvasSnapshot> s_canvasSnapshots = new();



		[MenuItem(StringConstants.CREATE_GUI_SCREENSHOT_OVERLAY)]
		public static void MakeScreenshotOverlay()
		{
			try
			{
				SafeMakeScreenshotOverlay();
			}
			catch (Exception e)
			{
				UiLog.LogError(e.Message);
				throw;
			}
			finally
			{
				EditorUtility.ClearProgressBar();
				ResetState();
			}
		}

		private static void SafeMakeScreenshotOverlay()
		{
			m_root = null;
			var scene = EditorCodeUtility.GetCurrentContextScene(out m_isPrefab);
			if (!scene.IsValid())
			{
				UiLog.LogError("No valid scene or prefab stage.");
				return;
			}

			// 1) size like GameView
			var screenSize = UiUtility.GetScreenSize();
			m_width = Mathf.Max(64, screenSize.x);
			m_height = Mathf.Max(64, screenSize.y);

			Log("Creating Overlay", 0);
			CreateOverlay();

			if (m_isPrefab)
			{
				Log("Creating Temp Scene", 15f);
				CreateTempScene();
			}

			Log("Creating Temp Camera", .3f);
			CreateTempCamera();

			Log("Switch Overlay canvases to ScreenSpaceCamera", .45f);
			SwitchOverlayCanvasesToCamera();

			Log("Render Texture", .6f);
			CaptureCameraToTexture();

			try
			{
				Log("Restore Canvases", .75f);
				RestoreCanvasSnapshots();
			}
			finally
			{
				if (m_tempCamera != null && m_tempCamera.gameObject.name == "__ScreenshotCamera__")
					UnityEngine.Object.DestroyImmediate(m_tempCamera.gameObject);
			}

			if (m_isPrefab)
			{
				Log("Destroy Temp Scene", .9f);
				DestroyTempScene();
			}

			// Done
			UiLog.Log($"Screenshot overlay created ({m_width}x{m_height}) at 50% opacity.");
		}

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
			m_canvasGameObject = new GameObject("__ScreenshotOverlayCanvas__");
			m_canvasGameObject.tag = "EditorOnly";
			SceneManager.MoveGameObjectToScene(m_canvasGameObject, SceneManager.GetActiveScene());
			if (m_isPrefab)
			{
				m_root = PrefabStageUtility.GetCurrentPrefabStage().prefabContentsRoot;
				m_canvasGameObject.transform.SetParent(m_root.transform);
				var rt2 = m_canvasGameObject.AddComponent<RectTransform>();
				rt2.anchorMin = Vector2.zero;
				rt2.anchorMax = Vector2.one;
				rt2.offsetMin = Vector2.zero;
				rt2.offsetMax = Vector2.zero;
			}

			var canvas = m_canvasGameObject.AddComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			canvas.sortingOrder = 32760; // very high
			canvas.overrideSorting = true;
			var raycaster = m_canvasGameObject.AddComponent<GraphicRaycaster>();
			raycaster.ignoreReversedGraphics = true;
			m_canvasGameObject.AddComponent<HideOnPlay>();

			// Image child
			m_imageGameObject = new GameObject("__ScreenshotOverlay__");
			m_imageGameObject.tag = "EditorOnly";
			m_imageGameObject.transform.SetParent(m_canvasGameObject.transform, false);
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

			//TODO: #44 DisablePicking(m_canvasGo, true) does not work for prefab editing in ScreenshotOverlayTool
			SceneVisibilityManager.instance.DisablePicking(m_canvasGameObject, true);
		}

		// --------------------------------------------------------------------
		// Helpers
		// --------------------------------------------------------------------

		private static void CreateTempCamera()
		{
			Log("Creating Temp Camera Game Object", .32f);
			var go = new GameObject("__ScreenshotCamera__");
			SceneManager.MoveGameObjectToScene(go, SceneManager.GetActiveScene());

			Log("Adding Camera Component", .34f);
			m_tempCamera = go.AddComponent<Camera>();

			Log("Setting camera properties", .36f);
			m_tempCamera.clearFlags = CameraClearFlags.SolidColor;
			m_tempCamera.backgroundColor = new Color(.5f, .5f, .5f, 0);
			m_tempCamera.cullingMask = 1 << LayerMask.NameToLayer("UI");
			m_tempCamera.orthographic = true;
			m_tempCamera.forceIntoRenderTexture = true;
			m_tempCamera.enabled = false;
		}

		private static void SwitchOverlayCanvasesToCamera()
		{
			s_canvasSnapshots.Clear();

			var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
			foreach (var c in canvases)
			{
				if (!c)
					continue;

				s_canvasSnapshots.Add(new CanvasSnapshot
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
					c.worldCamera = m_tempCamera;
					c.overrideSorting = true;
				}
			}

			Canvas.ForceUpdateCanvases();
		}

		private static void RestoreCanvasSnapshots()
		{
			foreach (var s in s_canvasSnapshots)
			{
				if (!s.Canvas) continue;
				s.Canvas.renderMode = s.RenderMode;
				s.Canvas.worldCamera = s.WorldCamera;
				s.Canvas.sortingOrder = s.SortingOrder;
				s.Canvas.overrideSorting = s.OverrideSorting;
			}
			Canvas.ForceUpdateCanvases();
			s_canvasSnapshots.Clear();
		}

		private static void CaptureCameraToTexture()
		{
			var rt = RenderTexture.GetTemporary(m_width, m_height, 24, RenderTextureFormat.ARGB32);
			var prevRT = m_tempCamera.targetTexture;

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
						canvas.worldCamera = m_tempCamera;
						canvas.overrideSorting = true;

						var scaler = wrapper.AddComponent<CanvasScaler>();
						scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
						scaler.referencePixelsPerUnit = 100f;

						m_instancedPrefab.transform.SetParent(wrapper.transform, false);
					}
				}

				m_tempCamera.targetTexture = rt;
				Log("Render", .66f);
				m_tempCamera.Render();

				Log("Applying rendered texture", .69f);
				var holder = Object.FindAnyObjectByType<UiSpriteHolder>(FindObjectsInactive.Include);
				var tmpTex = new Texture2D(m_width, m_height, TextureFormat.RGBA32, false, false);
				RenderTexture.active = rt;
				tmpTex.ReadPixels(new Rect(0, 0, m_width, m_height), 0, 0);
				tmpTex.Apply(false, false);
				var png = tmpTex.EncodeToPNG();
				Object.DestroyImmediate(tmpTex);

				// make sure you're modifying *serialized* data on the instance:
				holder.SetFromPngBytes(png, m_width, m_height);

				if (m_isPrefab)
				{
					Log("Apply Prefab", .72f);
					// Get prefab asset path of the instanced prefab
					var prefabAsset = PrefabUtility.GetCorrespondingObjectFromSource(m_instancedPrefab);
					var assetPath = AssetDatabase.GetAssetPath(prefabAsset);
					if (string.IsNullOrEmpty(assetPath))
					{
						UiLog.LogError("Could not resolve prefab asset path.");
					}
					else
					{
						// Open prefab contents off-stage, patch bytes, save, unload
						var root = PrefabUtility.LoadPrefabContents(assetPath);
						try
						{
							var holderInAsset = root.GetComponentInChildren<UiSpriteHolder>(true);
							holderInAsset.SetFromPngBytes(png, m_width, m_height);
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
				m_tempCamera.targetTexture = prevRT;
				RenderTexture.ReleaseTemporary(rt);
			}
		}

		private static void ResetState()
		{
			m_isPrefab = false;
			m_root = null;
			m_spriteHolder = null;
			m_width = 0;
			m_height = 0;
			m_canvasGameObject = null;
			m_imageGameObject = null;
			m_image = null;
			m_tempScene = default;
			m_prefabPath = null;
			m_tempCamera = null;
			m_activeScene = default;
			m_instancedPrefab = null;

			m_scenesLoaded.Clear();
			s_canvasSnapshots.Clear();
		}

		private static void Log( string _s, float _progress )
		{
			EditorUtility.DisplayProgressBar($"Creating GUI Screenshot overlay", _s, _progress);
			UiLog.Log(_s);
		}
	}
}
#endif
