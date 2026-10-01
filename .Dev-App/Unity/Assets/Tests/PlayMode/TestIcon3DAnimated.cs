using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GuiToolkit.Test
{
	/// <summary>
	/// Animated 3D icons (phase 4): the object's own animation plays, the instance is invisible outside its own render,
	/// its lights do not leak, it can be frozen and resumed, and nothing stops animating for lack of a real camera.
	/// </summary>
	public class TestIcon3DAnimated
	{
		private const int TestLayer = 31;
		private const int Size = 32;

		/// Moves its first child sideways every frame - "the animation the object brings along".
		/// Driven by the frame count, not the time: test runner frames are only milliseconds apart, a time based
		/// motion moved less than a pixel per frame and the test could not see it.
		private class Wobble : MonoBehaviour
		{
			private void Update()
			{
				if (transform.childCount > 0)
					transform.GetChild(0).localPosition = new Vector3(Mathf.Sin(Time.frameCount * 1.3f) * 0.6f, 0, 0);
			}
		}

		private readonly List<Object> m_cleanup = new();
		private Canvas m_canvas;
		private UiIcon3DPreset m_preset;
		private Material m_whiteLit;
		private Texture2D m_readback;

		[SetUp]
		public void SetUp()
		{
			UiIcon3DRenderer.Layer = TestLayer;
			UiIcon3DRenderer.MsaaSamples = 1;

			var canvasGo = Track(new GameObject("Canvas", typeof(Canvas)));
			m_canvas = canvasGo.GetComponent<Canvas>();
			m_canvas.renderMode = RenderMode.ScreenSpaceOverlay;

			m_whiteLit = Track(new Material(Shader.Find("Standard")) { color = Color.white });
			m_readback = Track(new Texture2D(Size, Size, TextureFormat.RGBA32, false));

			var presetGo = Track(new GameObject("Preset"));
			presetGo.SetActive(false);
			m_preset = presetGo.AddComponent<UiIcon3DPreset>();
			m_preset.FitMode = UiIcon3DPreset.EFitMode.Sphere;
			var lightGo = new GameObject("Light");
			lightGo.transform.SetParent(presetGo.transform, false);
			var light = lightGo.AddComponent<Light>();
			light.type = LightType.Directional;
			light.intensity = 0.3f;
		}

		[TearDown]
		public void TearDown()
		{
			foreach (var obj in m_cleanup)
				if (obj != null)
					Object.DestroyImmediate(obj);
			m_cleanup.Clear();

			UiIcon3DRenderer.Shutdown();
			UiIcon3DRenderer.Layer = -1;
			UiIcon3DRenderer.MsaaSamples = 0;
		}

		[Test]
		public void Animated_Icon_Renders_Every_Frame()
		{
			var icon = CreateIcon(CreateSphere());
			icon.Mode = UiIcon3D.EMode.Animated;
			UiIcon3DRenderer.Flush();
			Assert.IsTrue(icon.IsRendered);

			int renders = UiIcon3DRenderer.RenderCount;
			UiIcon3DRenderer.Flush();
			UiIcon3DRenderer.Flush();
			UiIcon3DRenderer.Flush();
			Assert.AreEqual(renders + 3, UiIcon3DRenderer.RenderCount);
		}

		[Test]
		public void Frame_Divider_Skips_Frames()
		{
			var icon = CreateIcon(CreateSphere());
			icon.Mode = UiIcon3D.EMode.Animated;
			icon.FrameDivider = 3;
			UiIcon3DRenderer.Flush();

			int renders = UiIcon3DRenderer.RenderCount;
			for (int i = 0; i < 6; i++)
				UiIcon3DRenderer.Flush();

			Assert.AreEqual(renders + 2, UiIcon3DRenderer.RenderCount);
		}

		[UnityTest]
		public IEnumerator The_Objects_Own_Animation_Plays()
		{
			var icon = CreateIcon(CreateWobbler());
			icon.Mode = UiIcon3D.EMode.Animated;

			var images = new HashSet<string>();
			for (int i = 0; i < 8; i++)
			{
				yield return null;   // the instance's Update runs, the renderer ticks in the player loop
				UiIcon3DRenderer.Flush();
				images.Add(Hash(Read(icon.RawImage.texture)));
			}

			Debug.Log($"ICON3D distinct animated frames: {images.Count} of 8");
			Assert.Greater(images.Count, 2, "image does not change - the object's animation does not reach the icon");
		}

		[Test]
		public void Animated_Instance_Is_Invisible_To_Scene_Cameras()
		{
			var handle = UiIcon3DRenderer.RenderAnimated(CreateSphere(10), m_preset, new Vector2Int(Size, Size));
			try
			{
				UiIcon3DRenderer.Flush();
				Assert.IsTrue(handle.IsRendered);

				// A scene camera right at the stage, seeing every layer
				var cameraGo = Track(new GameObject("Scene Camera"));
				cameraGo.transform.position = UiIcon3DRenderer.StagePosition + new Vector3(0, 0, -30);
				var camera = cameraGo.AddComponent<Camera>();
				camera.enabled = false;
				camera.cullingMask = ~0;
				camera.clearFlags = CameraClearFlags.SolidColor;
				camera.backgroundColor = Color.clear;
				var target = Track(new RenderTexture(Size, Size, 24));
				camera.targetTexture = target;
				camera.Render();

				var center = Read(target)[Size / 2 * Size + Size / 2];
				Assert.AreEqual(0f, center.a, 0.01f, "scene camera sees the animated icon instance");
			}
			finally
			{
				handle.Release();
			}
		}

		[Test]
		public void Animated_Objects_Light_Does_Not_Reach_Other_Icons()
		{
			var glowing = CreateSphere();
			var lightGo = new GameObject("Blue Point");
			lightGo.transform.SetParent(glowing.transform, false);
			lightGo.transform.localPosition = new Vector3(0, 0, -1);
			var light = lightGo.AddComponent<Light>();
			light.type = LightType.Point;
			light.color = Color.blue;
			light.range = 5;
			light.intensity = 3;

			var animated = CreateIcon(glowing, Quaternion.identity);
			animated.Mode = UiIcon3D.EMode.Animated;
			UiIcon3DRenderer.Flush();

			var plain = CreateIcon(CreateSphere(), Quaternion.identity);
			UiIcon3DRenderer.Flush();

			var animatedCenter = Center(animated);
			var plainCenter = Center(plain);
			Debug.Log($"ICON3D animated with blue light {animatedCenter}, static after it {plainCenter}");
			Assert.Greater(animatedCenter.b - animatedCenter.r, 0.2f, "animated object's own light missing");
			Assert.Less(Mathf.Abs(plainCenter.b - plainCenter.r), 0.05f, "animated object's light leaked into a static icon");
		}

		[Test]
		public void Switching_To_Static_Freezes_The_Current_Frame()
		{
			var icon = CreateIcon(CreateSphere());
			icon.Mode = UiIcon3D.EMode.Animated;
			UiIcon3DRenderer.Flush();
			UiIcon3DRenderer.Flush();
			var texture = icon.RawImage.texture;

			icon.Mode = UiIcon3D.EMode.Static;
			UiIcon3DRenderer.Flush();
			int renders = UiIcon3DRenderer.RenderCount;
			UiIcon3DRenderer.Flush();
			UiIcon3DRenderer.Flush();

			Assert.AreEqual(renders, UiIcon3DRenderer.RenderCount, "frozen icon still renders");
			Assert.AreSame(texture, icon.RawImage.texture, "frozen frame replaced (a new render would show the entry pose)");
			Assert.IsTrue(icon.IsRendered);

			icon.Mode = UiIcon3D.EMode.Animated;
			UiIcon3DRenderer.Flush();
			UiIcon3DRenderer.Flush();
			Assert.Greater(UiIcon3DRenderer.RenderCount, renders, "animation does not resume");
		}

		[Test]
		public void Static_To_Animated_Keeps_The_Static_Image_Until_The_First_Frame()
		{
			var icon = CreateIcon(CreateSphere());
			UiIcon3DRenderer.Flush();
			var staticTexture = icon.RawImage.texture;

			icon.Mode = UiIcon3D.EMode.Animated;
			UiIcon3DRenderer.Process(0);   // the animated request exists - and renders anyway: animated icons are not budgeted

			Assert.IsTrue(icon.RawImage.texture != null);
			Assert.IsTrue(icon.IsRendered, "animated icon not shown after its first tick");
			Assert.AreNotSame(staticTexture, icon.RawImage.texture);
		}

		[Test]
		public void Offscreen_Animated_Icon_Does_Not_Render_Every_Frame()
		{
			var icon = CreateIcon(CreateSphere());
			icon.Mode = UiIcon3D.EMode.Animated;
			UiIcon3DRenderer.Flush();
			Assert.IsTrue(icon.IsRendered);

			icon.RectTransform.anchoredPosition = new Vector2(100000, 0);
			UiIcon3DRenderer.Flush();   // learns it is invisible
			int renders = UiIcon3DRenderer.RenderCount;
			UiIcon3DRenderer.Flush();
			UiIcon3DRenderer.Flush();

			Assert.AreEqual(renders, UiIcon3DRenderer.RenderCount);
		}

		[Test]
		public void Animated_Instance_Does_Not_Stop_Or_Walk_Away()
		{
			var template = CreateSphere();
			var animator = template.AddComponent<Animator>();
			animator.cullingMode = AnimatorCullingMode.CullCompletely;
			animator.applyRootMotion = true;

			var handle = UiIcon3DRenderer.RenderAnimated(template, m_preset, new Vector2Int(Size, Size));
			try
			{
				UiIcon3DRenderer.Flush();
				var instanceAnimator = handle.Request.Instance.GetComponent<Animator>();
				Assert.AreEqual(AnimatorCullingMode.AlwaysAnimate, instanceAnimator.cullingMode);
				Assert.IsFalse(instanceAnimator.applyRootMotion);
			}
			finally
			{
				handle.Release();
			}
		}

		[Test]
		public void Animated_Instance_Has_No_Active_Physics()
		{
			var template = CreateSphere();   // primitives come with a collider
			template.AddComponent<Rigidbody>();

			var handle = UiIcon3DRenderer.RenderAnimated(template, m_preset, new Vector2Int(Size, Size));
			try
			{
				UiIcon3DRenderer.Flush();
				var instance = handle.Request.Instance;
				Assert.IsFalse(instance.GetComponent<Collider>().enabled, "collider of the permanently active instance is enabled");
				var body = instance.GetComponent<Rigidbody>();
				Assert.IsTrue(body.isKinematic);
				Assert.IsFalse(body.detectCollisions);
			}
			finally
			{
				handle.Release();
			}
		}

		[Test]
		public void Releasing_Destroys_The_Instance()
		{
			var handle = UiIcon3DRenderer.RenderAnimated(CreateSphere(), m_preset, new Vector2Int(Size, Size));
			UiIcon3DRenderer.Flush();
			var instance = handle.Request.Instance;
			Assert.IsNotNull(instance);

			handle.Release();
			Assert.IsFalse(instance.activeSelf, "released instance still active");
			Assert.AreEqual(0, UiIcon3DRenderer.RequestCount);
		}

		#region Helpers

		private T Track<T>( T _obj ) where T : Object
		{
			m_cleanup.Add(_obj);
			return _obj;
		}

		private UiIcon3D CreateIcon( GameObject _prefab, Quaternion? _rotation = null )
		{
			var go = Track(new GameObject("Icon", typeof(RectTransform)));
			go.transform.SetParent(m_canvas.transform, false);
			((RectTransform)go.transform).sizeDelta = new Vector2(Size, Size);
			var icon = go.AddComponent<UiIcon3D>();
			icon.Preset = m_preset;
			icon.Prefab = _prefab;
			if (_rotation.HasValue)
			{
				icon.ViewRotation = _rotation.Value;
				icon.OverrideViewRotation = true;
			}
			return icon;
		}

		private GameObject CreateSphere( float _scale = 1 )
		{
			var sphere = Track(GameObject.CreatePrimitive(PrimitiveType.Sphere));
			sphere.SetActive(false);
			sphere.transform.localScale = Vector3.one * _scale;
			sphere.GetComponent<Renderer>().sharedMaterial = m_whiteLit;
			return sphere;
		}

		/// A root with a script that moves its child sphere; the bounds hint keeps the frame fixed.
		private GameObject CreateWobbler()
		{
			var root = Track(new GameObject("Wobbler"));
			root.SetActive(false);
			root.AddComponent<Wobble>();
			var hint = root.AddComponent<UiIcon3DBoundsHint>();
			hint.LocalBounds = new Bounds(Vector3.zero, new Vector3(2.4f, 2.4f, 2.4f));

			var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			sphere.GetComponent<Renderer>().sharedMaterial = m_whiteLit;
			sphere.transform.SetParent(root.transform, false);
			sphere.transform.localScale = Vector3.one * 0.6f;
			return root;
		}

		private Color[] Read( Texture _texture )
		{
			var rt = (RenderTexture)_texture;
			var previous = RenderTexture.active;
			RenderTexture.active = rt;
			m_readback.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
			m_readback.Apply();
			RenderTexture.active = previous;
			return m_readback.GetPixels();
		}

		private Color Center( UiIcon3D _icon ) => Read(_icon.RawImage.texture)[Size / 2 * Size + Size / 2];

		private static string Hash( Color[] _pixels )
		{
			var sb = new System.Text.StringBuilder();
			for (int i = 0; i < _pixels.Length; i += 7)
				sb.Append((int)(_pixels[i].a * 10));
			return sb.ToString();
		}

		#endregion
	}
}
