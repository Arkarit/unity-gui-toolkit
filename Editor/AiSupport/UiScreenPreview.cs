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
			using var session = BeginSession(_prefabPath, _width, _height);
			var texture = session.RenderFrame();
			try
			{
				return texture.EncodeToPNG();
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(texture);
			}
		}

		/// <summary>
		/// A prepared preview: the prefab instantiated in its own scene with a canvas and camera, ready to be
		/// rendered repeatedly. A single shot does not need this, but a motion filmstrip does — the instance has
		/// to survive between frames so an animation can be stepped on it, and rebuilding the scene per frame
		/// would reset whatever was being animated.
		/// </summary>
		internal static PreviewSession BeginSession( string _prefabPath, int _width, int _height )
			=> new PreviewSession(_prefabPath, _width, _height);

		internal sealed class PreviewSession : IDisposable
		{
			public GameObject Instance { get; private set; }

			private readonly int m_width;
			private readonly int m_height;
			private readonly RenderTexture m_previousActive;
			private Scene m_previewScene;
			private GameObject m_camGo;
			private GameObject m_hostGo;
			private Camera m_camera;
			private Canvas m_canvas;

			internal PreviewSession( string _prefabPath, int _width, int _height )
			{
				if (string.IsNullOrEmpty(_prefabPath))
					throw new ArgumentException("Empty prefab path.");

				var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(_prefabPath);
				if (prefab == null)
					throw new ArgumentException($"No prefab found at '{_prefabPath}'.");

				m_width = Mathf.Clamp(_width, 64, 4096);
				m_height = Mathf.Clamp(_height, 64, 4096);
				m_previousActive = RenderTexture.active;

				try
				{
					Build(prefab);
				}
				catch
				{
					Dispose();
					throw;
				}
			}

			private void Build( GameObject _prefab )
			{
				int _width = m_width;
				int _height = m_height;
				m_previewScene = EditorSceneManager.NewPreviewScene();
				var previewScene = m_previewScene;

				var instance = UnityEngine.Object.Instantiate(_prefab);
				Instance = instance;
				instance.name = _prefab.name;
				SceneManager.MoveGameObjectToScene(instance, previewScene);

				var canvas = instance.GetComponent<Canvas>() ?? instance.GetComponentInChildren<Canvas>(true);
				if (canvas == null)
				{
					// Panel prefabs carry no Canvas of their own — they are parented under a scene Canvas at
					// runtime. In a host project that is the majority of screens, so refusing to render them
					// would make the preview useless for exactly those. Do what the prefab stage does: put a
					// Canvas underneath, configured from the project's global CanvasScaler template so the
					// shot is to scale instead of depending on the requested pixel size.
					var hostGo = new GameObject("__UiScreenPreviewCanvas__",
						typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
					m_hostGo = hostGo;
					SceneManager.MoveGameObjectToScene(hostGo, previewScene);
					canvas = hostGo.GetComponent<Canvas>();

					var scaler = hostGo.GetComponent<CanvasScaler>();
					var config = UiToolkitConfiguration.Instance;
					var template = config != null ? config.GlobalCanvasScalerTemplate : null;
					if (template != null)
					{
						template.CopyTo(scaler);
					}
					else
					{
						scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
						scaler.referenceResolution = new Vector2(_width, _height);
						scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
						scaler.matchWidthOrHeight = 0.5f;
					}

					instance.transform.SetParent(hostGo.transform, false);
					if (instance.transform is RectTransform instanceRt)
					{
						instanceRt.anchoredPosition3D = Vector3.zero;
						instanceRt.localScale = Vector3.one;
						instanceRt.localRotation = Quaternion.identity;
					}
				}

				var camGo = new GameObject("__UiScreenPreviewCamera__");
				m_camGo = camGo;
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
				DetachFromSceneVolumes(camGo);

				canvas.renderMode = RenderMode.ScreenSpaceCamera;
				canvas.worldCamera = cam;
				canvas.planeDistance = 100f;
				canvas.overrideSorting = true;

				m_camera = cam;
				m_canvas = canvas;
			}

			/// <summary>
			/// Takes the preview camera out of the project's post-processing.
			///
			/// A pipeline's default volume profile applies to every camera that does not exclude it, and no
			/// Volume component has to exist anywhere for that — one client's default profile had depth of
			/// field, chromatic aberration, lens distortion and film grain all switched on. The preview then
			/// returns a blurred, colour-fringed image of a UI that is perfectly sharp in the editor, and the
			/// author spends the next hour looking for the mistake in their screen instead of in the camera.
			///
			/// A preview is a document of what was authored, so it renders the UI and nothing else.
			///
			/// Done by reflection because the toolkit does not depend on HDRP: on any other pipeline the type
			/// is absent and there is nothing to switch off.
			/// </summary>
			private static void DetachFromSceneVolumes( GameObject _cameraGo )
			{
				var type = Type.GetType("UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData, " +
					"Unity.RenderPipelines.HighDefinition.Runtime");
				if (type == null)
					return;

				var data = _cameraGo.GetComponent(type) ?? _cameraGo.AddComponent(type);
				if (data == null)
					return;

				// An empty mask means no volume anywhere reaches this camera. Deliberately NOT
				// customRenderingSettings: that switches the camera to its own frame settings, and a freshly
				// added component carries an empty set rather than a copy of the pipeline's defaults — which
				// renders a black image, as one earlier attempt at this proved.
				const int noLayers = 0;
				var field = type.GetField("volumeLayerMask");
				if (field != null)
					field.SetValue(data, (LayerMask)noLayers);
				else
					type.GetProperty("volumeLayerMask")?.SetValue(data, (LayerMask)noLayers);

				var anchor = type.GetField("volumeAnchorOverride");
				if (anchor != null)
					anchor.SetValue(data, null);
			}

			/// <summary>
			/// Renders one frame of the current state. The caller owns the returned texture and must destroy it.
			/// Layout is rebuilt each time, because between frames an animation may have moved or resized
			/// something a layout depends on.
			/// </summary>
			public Texture2D RenderFrame()
			{
				Canvas.ForceUpdateCanvases();
				if (m_canvas != null && m_canvas.transform is RectTransform canvasRt)
					LayoutRebuilder.ForceRebuildLayoutImmediate(canvasRt);

				RenderTexture rt = null;
				var previousActive = RenderTexture.active;
				try
				{
					rt = RenderTexture.GetTemporary(m_width, m_height, 24, RenderTextureFormat.ARGB32);
					m_camera.targetTexture = rt;
					m_camera.Render();

					var texture = new Texture2D(m_width, m_height, TextureFormat.RGBA32, false, false);
					RenderTexture.active = rt;
					texture.ReadPixels(new Rect(0, 0, m_width, m_height), 0, 0);
					texture.Apply(false, false);
					return texture;
				}
				finally
				{
					RenderTexture.active = previousActive;
					if (m_camera != null)
						m_camera.targetTexture = null;
					if (rt != null)
						RenderTexture.ReleaseTemporary(rt);
				}
			}

			public void Dispose()
			{
				RenderTexture.active = m_previousActive;
				if (m_camGo != null) UnityEngine.Object.DestroyImmediate(m_camGo);
				// Destroys the instance with it when we had to wrap it; the check below then no-ops.
				if (m_hostGo != null) UnityEngine.Object.DestroyImmediate(m_hostGo);
				if (Instance != null) UnityEngine.Object.DestroyImmediate(Instance);
				if (m_previewScene.IsValid()) EditorSceneManager.ClosePreviewScene(m_previewScene);

				m_camGo = null;
				m_hostGo = null;
				Instance = null;
				m_camera = null;
				m_canvas = null;
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
