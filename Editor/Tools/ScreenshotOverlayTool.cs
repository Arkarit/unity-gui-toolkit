#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using static System.Net.Mime.MediaTypeNames;
using UnityEditor.SearchService;
using Image = UnityEngine.UI.Image;

namespace GuiToolkit.Editor
{
	public static class ScreenshotOverlayTool
	{
		private const string MenuPath = "Tools/GUI/Make 50% Screenshot Overlay";

		private static bool m_isPrefab;
		private static GameObject m_root;
		private static UiSpriteHolder m_spriteHolder;
		private static int m_width;
		private static int m_height;
		private static GameObject m_canvasGo;
		private static GameObject m_imageGameObject;
		private static Image m_image;
		

		[MenuItem(MenuPath, priority = -10000)]
		public static void MakeScreenshotOverlay()
		{
			m_root = null;
			var scene = EditorCodeUtility.GetCurrentContextScene(out m_isPrefab);
			if (!scene.IsValid())
			{
				Debug.LogError("No valid scene or prefab stage.");
				return;
			}

			if (m_isPrefab)
			{
				var roots = EditorCodeUtility.GetCurrentContextSceneRoots();
				if (roots == null || roots.Length == 0)
				{
					Debug.LogError("No roots found.");
					return;
				}
				
				m_root = roots[0];
			}

			// 1) size like GameView
			m_width = Mathf.Max(64, Mathf.RoundToInt(UiUtility.ScreenWidth()));
			m_height = Mathf.Max(64, Mathf.RoundToInt(UiUtility.ScreenHeight()));
			
			CreateOverlay();

			// 2) temp camera
			var cam = CreateTempCameraInStage();

			// 3) switch Overlay canvases to ScreenSpaceCamera (remember original state)
			var canvasSnaps = SwitchOverlayCanvasesToCamera(cam);

			// 4) render into RT and read back
			var tex = CaptureCameraToTexture(cam, m_width, m_height);

			// 5) restore canvases
			try { }
			finally
			{
				RestoreCanvasSnapshots(canvasSnaps);
//				if (cam != null && cam.gameObject.name == "__ScreenshotCamera__")
//					UnityEngine.Object.DestroyImmediate(cam.gameObject);
			}

			if (tex == null)
			{
				Debug.LogError("Failed to capture screenshot.");
				return;
			}

			// 7) non-pickable and dont-save
			MakeNonPickableAndDontSave();

			// Done
			Debug.Log($"Screenshot overlay created ({m_width}x{m_height}) at 50% opacity.");
		}

		private static void CreateOverlay()
		{
			// ensure a topmost canvas separate from project UI
			m_canvasGo = new GameObject("__ScreenshotOverlayCanvas__");
			PlaceInCurrentStage(m_canvasGo);
			if (m_isPrefab)
			{
				var roots = GetCurrentPrefabStage().scene.GetRootGameObjects();
				m_canvasGo.transform.SetParent(roots[0].transform);
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
		}

		// --------------------------------------------------------------------
		// Helpers
		// --------------------------------------------------------------------

		private static Camera CreateTempCameraInStage()
		{
			var go = new GameObject("__ScreenshotCamera__");
			if (m_root)
				go.transform.SetParent(m_root.transform);
			
			var cam = go.AddComponent<Camera>();
			cam.clearFlags = CameraClearFlags.SolidColor;
			cam.backgroundColor =new Color(.5f, .5f, .5f, 0);
			cam.cullingMask = 1 << LayerMask.NameToLayer("UI");
			cam.orthographic = false;
			cam.enabled = false;
//			cam.hideFlags = HideFlags.DontSave;
//			cam.forceIntoRenderTexture = true;
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
			
			foreach (var c in FindInCurrentStage<Canvas>())
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

		private static Texture2D CaptureCameraToTexture( Camera cam, int width, int height )
		{
			var rt = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
			var prevRT = cam.targetTexture;
			var prevActive = RenderTexture.active;

			try
			{
				cam.targetTexture = rt;
				cam.Render();
				RenderTexture.active = rt;

				var tex = m_spriteHolder.Texture;
				tex.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);
				tex.Apply(false, false);

				return tex;
			}
			finally
			{
				cam.targetTexture = prevRT;
				RenderTexture.active = prevActive;
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

		static T[] FindInCurrentStage<T>() where T : Component
		{
			// liefert nur Komponenten des aktiven Stages (PrefabStage oder MainStage)
			return CurrentStageHandle().FindComponentsOfType<T>();
		}

		private static void MakeNonPickableAndDontSave()
		{
			SceneVisibilityManager.instance.DisablePicking(m_canvasGo, true);
		}
	}
}
#endif
