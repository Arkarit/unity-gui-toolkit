using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GuiToolkit.Editor.AiSupport
{
	/// <summary>
	/// Milestone 2b of the AI screen-authoring effort: renders a baked screen prefab to a PNG in Edit
	/// Mode so an external agent can <i>see</i> what it authored and iterate (bake → look → fix). No Play
	/// Mode needed, which is the whole point of the UGUI route (canvases screenshot fine in Edit Mode).
	///
	/// Isolation: the prefab is instantiated into a throw-away <see cref="EditorSceneManager.NewPreviewScene"/>
	/// and a dedicated camera is pinned to that scene (<c>Camera.scene</c>), so the user's open scenes are
	/// neither disturbed nor captured. The UiView's Canvas is switched to ScreenSpaceCamera for the shot
	/// and everything is torn down afterward.
	/// </summary>
	[EditorAware]
	public static class UiScreenPreview
	{
		public const int DefaultWidth = 1920;
		public const int DefaultHeight = 1080;

		/// <summary>Renders the prefab at <paramref name="_prefabPath"/> and returns raw PNG bytes.</summary>
		public static byte[] CapturePng( string _prefabPath, int _width = DefaultWidth, int _height = DefaultHeight )
		{
			if (string.IsNullOrEmpty(_prefabPath))
				throw new ArgumentException("Empty prefab path.");

			var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(_prefabPath);
			if (prefab == null)
				throw new ArgumentException($"No prefab found at '{_prefabPath}'.");

			_width = Mathf.Clamp(_width, 64, 4096);
			_height = Mathf.Clamp(_height, 64, 4096);

			Scene previewScene = default;
			GameObject instance = null;
			GameObject camGo = null;
			RenderTexture rt = null;
			Texture2D tex = null;
			var prevActiveRt = RenderTexture.active;

			try
			{
				previewScene = EditorSceneManager.NewPreviewScene();

				instance = UnityEngine.Object.Instantiate(prefab);
				instance.name = prefab.name;
				SceneManager.MoveGameObjectToScene(instance, previewScene);

				var canvas = instance.GetComponent<Canvas>() ?? instance.GetComponentInChildren<Canvas>(true);
				if (canvas == null)
					throw new Exception($"Prefab '{prefab.name}' has no Canvas to render (expected a UiView root).");

				camGo = new GameObject("__UiScreenPreviewCamera__");
				SceneManager.MoveGameObjectToScene(camGo, previewScene);
				var cam = camGo.AddComponent<Camera>();
				cam.scene = previewScene;              // render ONLY the preview scene
				cam.clearFlags = CameraClearFlags.SolidColor;
				cam.backgroundColor = new Color(0.16f, 0.16f, 0.18f, 1f);
				cam.orthographic = true;
				cam.cullingMask = ~0;
				cam.nearClipPlane = 0.01f;
				cam.farClipPlane = 1000f;
				cam.forceIntoRenderTexture = true;
				cam.enabled = false;
				cam.transform.position = new Vector3(0, 0, -100);
				cam.transform.rotation = Quaternion.identity;

				canvas.renderMode = RenderMode.ScreenSpaceCamera;
				canvas.worldCamera = cam;
				canvas.planeDistance = 100f;
				canvas.overrideSorting = true;

				// Rebuild layout so sizes/positions are final before the single render.
				Canvas.ForceUpdateCanvases();
				if (canvas.transform is RectTransform canvasRt)
					LayoutRebuilder.ForceRebuildLayoutImmediate(canvasRt);

				rt = RenderTexture.GetTemporary(_width, _height, 24, RenderTextureFormat.ARGB32);
				cam.targetTexture = rt;
				cam.Render();

				tex = new Texture2D(_width, _height, TextureFormat.RGBA32, false, false);
				RenderTexture.active = rt;
				tex.ReadPixels(new Rect(0, 0, _width, _height), 0, 0);
				tex.Apply(false, false);

				return tex.EncodeToPNG();
			}
			finally
			{
				RenderTexture.active = prevActiveRt;
				if (tex != null) UnityEngine.Object.DestroyImmediate(tex);
				if (rt != null) RenderTexture.ReleaseTemporary(rt);
				if (camGo != null) UnityEngine.Object.DestroyImmediate(camGo);
				if (instance != null) UnityEngine.Object.DestroyImmediate(instance);
				if (previewScene.IsValid()) EditorSceneManager.ClosePreviewScene(previewScene);
			}
		}

		/// <summary>Renders the prefab and returns the PNG as a base64 string.</summary>
		public static string CaptureBase64( string _prefabPath, int _width = DefaultWidth, int _height = DefaultHeight )
			=> Convert.ToBase64String(CapturePng(_prefabPath, _width, _height));

		#region Editor test

		[MenuItem(StringConstants.AI_SCREENSHOT_SELECTED_MENU_NAME, true)]
		private static bool ScreenshotSelectedValidate()
			=> Selection.activeObject is GameObject go && PrefabUtility.IsPartOfPrefabAsset(go);

		// Renders the selected prefab and writes a "<name>.preview.png" next to it, so the render path
		// can be exercised in-editor without the MCP round-trip.
		[MenuItem(StringConstants.AI_SCREENSHOT_SELECTED_MENU_NAME)]
		private static void ScreenshotSelected()
		{
			try
			{
				var go = Selection.activeObject as GameObject;
				string prefabPath = AssetDatabase.GetAssetPath(go);

				byte[] png = CapturePng(prefabPath);

				string dir = System.IO.Path.GetDirectoryName(prefabPath).Replace('\\', '/');
				string outPath = $"{dir}/{go.name}.preview.png";
				System.IO.File.WriteAllBytes(outPath, png);
				AssetDatabase.ImportAsset(outPath, ImportAssetOptions.ForceUpdate);

				// Keep the imported texture at its true resolution — the default importer scales
				// non-power-of-two images down to the nearest POT (1920x1080 -> 1024x512), which is
				// only a viewing artifact but reads as "wrong resolution" in the inspector.
				if (AssetImporter.GetAtPath(outPath) is TextureImporter importer)
				{
					importer.npotScale = TextureImporterNPOTScale.None;
					importer.maxTextureSize = 8192;
					importer.textureCompression = TextureImporterCompression.Uncompressed;
					importer.SaveAndReimport();
				}

				var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(outPath);
				EditorGUIUtility.PingObject(tex);
				Selection.activeObject = tex;
				UiLog.LogInternal($"Preview written to '{outPath}'.");
			}
			catch (Exception e)
			{
				UiLog.LogError($"Screenshot preview failed: {e.Message}\n{e.StackTrace}");
			}
		}

		#endregion
	}
}
