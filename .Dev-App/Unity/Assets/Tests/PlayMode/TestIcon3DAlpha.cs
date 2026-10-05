using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace GuiToolkit.Test
{
	/// <summary>
	/// The alpha of transparent object materials (phase 6): standard blending leaves a 50% layer at alpha 0.25 in the
	/// render target; the exact mode renders over black and over white and derives the alpha from both.
	/// </summary>
	public class TestIcon3DAlpha
	{
		private const int TestLayer = 31;
		private const int Size = 32;
		private const float LayerAlpha = 0.5f;

		private readonly List<Object> m_cleanup = new();
		private Canvas m_canvas;
		private UiIcon3DPreset m_preset;
		private Material m_opaque;
		private Material m_glass;
		private Texture2D m_readback;

		[SetUp]
		public void SetUp()
		{
			UiIcon3DRenderer.Layer = TestLayer;
			UiIcon3DRenderer.MsaaSamples = 1;

			var canvasGo = Track(new GameObject("Canvas", typeof(Canvas)));
			m_canvas = canvasGo.GetComponent<Canvas>();
			m_canvas.renderMode = RenderMode.ScreenSpaceOverlay;

			m_opaque = Track(Icon3DTestModels.CreateOpaque("Opaque", Color.white, 0, 0));
			m_glass = Track(Icon3DTestModels.CreateTransparent("Glass", new Color(1, 1, 1, LayerAlpha)));
			m_readback = Track(new Texture2D(Size, Size, TextureFormat.RGBA32, false));

			var presetGo = Track(new GameObject("Preset"));
			presetGo.SetActive(false);
			m_preset = presetGo.AddComponent<UiIcon3DPreset>();
			m_preset.FitMode = UiIcon3DPreset.EFitMode.Sphere;
			m_preset.Padding = 0.2f;
			var lightGo = new GameObject("Light");
			lightGo.transform.SetParent(presetGo.transform, false);
			var light = lightGo.AddComponent<Light>();
			light.type = LightType.Directional;
			light.intensity = 1f;
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
		public void Fast_Mode_Leaves_The_Known_Wrong_Alpha()
		{
			m_preset.AlphaMode = UiIcon3DPreset.EAlphaMode.Fast;
			var center = Center(Render(CreateSphere(m_glass)));
			Debug.Log($"ICON3D fast, 50% glass: {center}");

			// SrcAlpha / OneMinusSrcAlpha also applies to alpha: 0.5 * 0.5
			Assert.AreEqual(LayerAlpha * LayerAlpha, center.a, 0.08f);
		}

		[Test]
		public void Exact_Mode_Gives_The_Right_Alpha_And_Premultiplied_Colour()
		{
			m_preset.AlphaMode = UiIcon3DPreset.EAlphaMode.Exact;
			var pixels = Read(Render(CreateSphere(m_glass)));
			var center = pixels[Size / 2 * Size + Size / 2];
			Debug.Log($"ICON3D exact, 50% glass: {center}");

			Assert.AreEqual(LayerAlpha, center.a, 0.06f);
			Assert.LessOrEqual(Mathf.Max(center.r, center.g, center.b), center.a + 0.02f, "colour exceeds alpha - not premultiplied");
			Assert.Greater(center.r, 0.05f, "the glass has no colour at all");
			Assert.AreEqual(0f, pixels[0].a, 0.01f, "background is not transparent");
		}

		[Test]
		public void Auto_Mode_Detects_A_Transparent_Material()
		{
			m_preset.AlphaMode = UiIcon3DPreset.EAlphaMode.Auto;
			var center = Center(Render(CreateSphere(m_glass)));
			Assert.AreEqual(LayerAlpha, center.a, 0.06f, "Auto did not take the exact path for an object with a transparent material");
		}

		[Test]
		public void Auto_Mode_Leaves_Opaque_Objects_Alone()
		{
			m_preset.AlphaMode = UiIcon3DPreset.EAlphaMode.Auto;
			var auto = Hash(Read(Render(CreateSphere(m_opaque))));

			UiIcon3DRenderer.Shutdown();
			m_preset.AlphaMode = UiIcon3DPreset.EAlphaMode.Fast;
			var fast = Hash(Read(Render(CreateSphere(m_opaque))));

			Assert.AreEqual(fast, auto, "an opaque object renders differently in Auto than in Fast");
		}

		[Test]
		public void Exact_Mode_Does_Not_Change_An_Opaque_Object()
		{
			m_preset.AlphaMode = UiIcon3DPreset.EAlphaMode.Fast;
			var fast = Read(Render(CreateSphere(m_opaque)));

			UiIcon3DRenderer.Shutdown();
			m_preset.AlphaMode = UiIcon3DPreset.EAlphaMode.Exact;
			var exact = Read(Render(CreateSphere(m_opaque)));

			float worst = 0;
			for (int i = 0; i < fast.Length; i++)
			{
				worst = Mathf.Max(worst, Mathf.Abs(fast[i].a - exact[i].a), Mathf.Abs(fast[i].r - exact[i].r),
					Mathf.Abs(fast[i].g - exact[i].g), Mathf.Abs(fast[i].b - exact[i].b));
			}

			Debug.Log($"ICON3D exact vs fast on an opaque sphere: largest channel difference {worst:F3}");
			Assert.Less(worst, 0.03f);
		}

		[Test]
		public void Glass_In_Front_Of_An_Opaque_Object_Is_Fully_Opaque()
		{
			m_preset.AlphaMode = UiIcon3DPreset.EAlphaMode.Exact;
			var root = Track(new GameObject("Both"));
			root.SetActive(false);
			var back = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			back.GetComponent<Renderer>().sharedMaterial = m_opaque;
			back.transform.SetParent(root.transform, false);
			var front = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			front.GetComponent<Renderer>().sharedMaterial = m_glass;
			front.transform.SetParent(root.transform, false);
			front.transform.localScale = Vector3.one * 1.3f;
			var hint = root.AddComponent<UiIcon3DBoundsHint>();
			hint.LocalBounds = new Bounds(Vector3.zero, Vector3.one * 2.6f);

			var center = Center(Render(root));
			Assert.AreEqual(1f, center.a, 0.03f);
		}

		[Test]
		public void Opaque_Background_Skips_The_Second_Render()
		{
			m_preset.AlphaMode = UiIcon3DPreset.EAlphaMode.Exact;
			m_preset.BackgroundColor = new Color(0.2f, 0.4f, 0.6f, 1f);
			var pixels = Read(Render(CreateSphere(m_glass)));

			// Over an opaque background there is nothing to derive: the background stays what the preset says
			Assert.AreEqual(1f, pixels[0].a, 0.01f);
			Assert.AreEqual(0.4f, pixels[0].g, 0.03f);
		}

		#region Helpers

		private T Track<T>( T _obj ) where T : Object
		{
			m_cleanup.Add(_obj);
			return _obj;
		}

		private GameObject CreateSphere( Material _material )
		{
			var sphere = Track(GameObject.CreatePrimitive(PrimitiveType.Sphere));
			sphere.SetActive(false);
			sphere.GetComponent<Renderer>().sharedMaterial = _material;
			return sphere;
		}

		private UiIcon3D Render( GameObject _prefab )
		{
			var go = Track(new GameObject("Icon", typeof(RectTransform)));
			go.transform.SetParent(m_canvas.transform, false);
			((RectTransform)go.transform).sizeDelta = new Vector2(Size, Size);
			var icon = go.AddComponent<UiIcon3D>();
			icon.Preset = m_preset;
			icon.Prefab = _prefab;
			UiIcon3DRenderer.Flush();
			Assert.IsTrue(icon.IsRendered);
			return icon;
		}

		private Color[] Read( UiIcon3D _icon )
		{
			var previous = RenderTexture.active;
			RenderTexture.active = (RenderTexture)_icon.RawImage.texture;
			m_readback.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
			m_readback.Apply();
			RenderTexture.active = previous;
			return m_readback.GetPixels();
		}

		private Color Center( UiIcon3D _icon ) => Read(_icon)[Size / 2 * Size + Size / 2];

		private static string Hash( Color[] _pixels )
		{
			var sb = new System.Text.StringBuilder();
			foreach (var pixel in _pixels)
				sb.Append((int)(pixel.a * 20)).Append((int)(pixel.r * 20)).Append(',');
			return sb.ToString();
		}

		#endregion
	}
}
