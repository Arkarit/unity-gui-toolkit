#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace GuiToolkit.Editor
{
    public static class ScreenshotOverlayTool
    {
        private const string MenuPath = "Tools/GUI/Make 50% Screenshot Overlay";

        [MenuItem(MenuPath, priority = -10000)]
        public static void MakeScreenshotOverlay()
        {
            var scene = EditorCodeUtility.GetCurrentContextScene();
            if (!scene.IsValid())
            {
                Debug.LogError("No valid scene or prefab stage.");
                return;
            }

            // 1) size like GameView
            int width = Mathf.Max(64, Mathf.RoundToInt(UiUtility.ScreenWidth()));
            int height = Mathf.Max(64, Mathf.RoundToInt(UiUtility.ScreenHeight()));

            // 2) temp camera
            var cam = FindBestCameraInScene(scene) ?? CreateTempCamera(scene);

            // 3) switch Overlay canvases to ScreenSpaceCamera (remember original state)
            var canvasSnaps = SwitchOverlayCanvasesToCamera(scene, cam);

            // 4) render into RT and read back
            var tex = CaptureCameraToTexture(cam, width, height);

            // 5) restore canvases
            try { }
            finally
            {
                RestoreCanvasSnapshots(canvasSnaps);
                if (cam != null && cam.gameObject.name == "__ScreenshotCamera__")
                    UnityEngine.Object.DestroyImmediate(cam.gameObject);
            }

            if (tex == null)
            {
                Debug.LogError("Failed to capture screenshot.");
                return;
            }

            // 6) create topmost overlay canvas + image
            var overlayGo = CreateOverlayImage(scene, tex);
            if (!overlayGo) return;

            // 7) non-pickable and dont-save
            MakeNonPickableAndDontSave(overlayGo);

            // Done
            Debug.Log($"Screenshot overlay created ({width}x{height}) at 50% opacity.", overlayGo);
        }

        // --------------------------------------------------------------------
        // Helpers
        // --------------------------------------------------------------------

        private static Camera FindBestCameraInScene(Scene scene)
        {
            // prefer main camera
            foreach (var r in scene.GetRootGameObjects())
            {
                var cams = r.GetComponentsInChildren<Camera>(true);
                foreach (var c in cams)
                    if (c != null && c.enabled) return c;
            }
            return null;
        }

        private static Camera CreateTempCamera(Scene scene)
        {
            var go = new GameObject("__ScreenshotCamera__");
            SceneManager.MoveGameObjectToScene(go, scene);
            var cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.clear;
            cam.cullingMask = ~0; // everything
            cam.orthographic = false;
            cam.enabled = false; // manual render
            go.hideFlags = HideFlags.DontSave;
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

        private static List<CanvasSnapshot> SwitchOverlayCanvasesToCamera(Scene scene, Camera cam)
        {
            var snaps = new List<CanvasSnapshot>();
            foreach (var root in scene.GetRootGameObjects())
            {
                var canvases = root.GetComponentsInChildren<Canvas>(true);
                foreach (var c in canvases)
                {
                    if (!c) continue;
                    var snap = new CanvasSnapshot
                    {
                        Canvas = c,
                        RenderMode = c.renderMode,
                        WorldCamera = c.worldCamera,
                        SortingOrder = c.sortingOrder,
                        OverrideSorting = c.overrideSorting
                    };
                    snaps.Add(snap);

                    if (c.renderMode == RenderMode.ScreenSpaceOverlay)
                    {
                        c.renderMode = RenderMode.ScreenSpaceCamera;
                        c.worldCamera = cam;
                        c.overrideSorting = true; // keep explicit order
                        // do not change sortingOrder; we want same visual stacking
                    }
                }
            }
            Canvas.ForceUpdateCanvases();
            return snaps;
        }

        private static void RestoreCanvasSnapshots(List<CanvasSnapshot> snaps)
        {
            if (snaps == null) return;
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

        private static Texture2D CaptureCameraToTexture(Camera cam, int width, int height)
        {
            var rt = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var prevRT = cam.targetTexture;
            var prevActive = RenderTexture.active;

            try
            {
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;

                var tex = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);
                tex.Apply(false, false);
                tex.name = "SceneScreenshot";

                return tex;
            }
            finally
            {
                cam.targetTexture = prevRT;
                RenderTexture.active = prevActive;
                RenderTexture.ReleaseTemporary(rt);
            }
        }

        private static GameObject CreateOverlayImage(Scene scene, Texture2D tex)
        {
            // ensure a topmost canvas separate from project UI
            var canvasGo = new GameObject("__ScreenshotOverlayCanvas__");
            SceneManager.MoveGameObjectToScene(canvasGo, scene);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32760; // very high
            canvas.overrideSorting = true;
            var raycaster = canvasGo.AddComponent<GraphicRaycaster>();
            raycaster.ignoreReversedGraphics = true;
            canvasGo.hideFlags = HideFlags.DontSave;

            // Image child
            var imgGo = new GameObject("__ScreenshotOverlay__");
            imgGo.transform.SetParent(canvasGo.transform, false);
            var rt = imgGo.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var img = imgGo.AddComponent<Image>();
            img.raycastTarget = false;
            var sp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            sp.name = "SceneScreenshotSprite";
            img.sprite = sp;
            img.color = new Color(1f, 1f, 1f, 0.5f); // 50% opacity

            imgGo.hideFlags = HideFlags.DontSave;
            EditorSceneManager.MarkSceneDirty(scene);

            return canvasGo;
        }

        private static void MakeNonPickableAndDontSave(GameObject root)
        {
            // Non-pickable in Scene view
            try
            {
                var svmType = typeof(UnityEditor.SceneVisibilityManager);
                var instProp = svmType.GetProperty("instance",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                var inst = instProp?.GetValue(null, null);
                var disablePick = svmType.GetMethod("DisablePicking",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                disablePick?.Invoke(inst, new object[] { root, true });
            }
            catch { /* best effort */ }

            // Not saved and not accidentally edited
            SetHideFlagsRecursive(root, HideFlags.DontSave | HideFlags.NotEditable);
        }

        private static void SetHideFlagsRecursive(GameObject go, HideFlags flags)
        {
            go.hideFlags = flags;
            foreach (var c in go.GetComponents<Component>())
                if (c) c.hideFlags = flags;
            for (int i = 0; i < go.transform.childCount; i++)
                SetHideFlagsRecursive(go.transform.GetChild(i).gameObject, flags);
        }
    }
}
#endif
