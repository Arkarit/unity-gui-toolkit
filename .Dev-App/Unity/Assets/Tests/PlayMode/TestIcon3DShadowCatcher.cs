using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace GuiToolkit.Test
{
	/// <summary>
	/// The shadow catcher (phase 6): an invisible ground under the object that only shows the shadow of the preset's
	/// directional light, as alpha. Compared against the same icon without it, because "no catcher" is the oracle.
	/// </summary>
	public class TestIcon3DShadowCatcher
	{
		private const int TestLayer = 31;
		private const int Size = 64;

		private readonly List<Object> m_cleanup = new();
		private Canvas m_canvas;
		private UiIcon3DPreset m_preset;
		private Material m_white;
		private Texture2D m_readback;
		private ShadowQuality m_qualityShadows;
		private float m_qualityDistance;
		private int m_qualityCascades;

		[SetUp]
		public void SetUp()
		{
			m_qualityShadows = QualitySettings.shadows;
			m_qualityDistance = QualitySettings.shadowDistance;
			m_qualityCascades = QualitySettings.shadowCascades;

			UiIcon3DRenderer.Layer = TestLayer;
			UiIcon3DRenderer.MsaaSamples = 1;

			var canvasGo = Track(new GameObject("Canvas", typeof(Canvas)));
			m_canvas = canvasGo.GetComponent<Canvas>();
			m_canvas.renderMode = RenderMode.ScreenSpaceOverlay;

			m_white = Track(Icon3DTestModels.CreateOpaque("White", Color.white, 0, 0));
			m_readback = Track(new Texture2D(Size, Size, TextureFormat.RGBA32, false));

			var presetGo = Track(new GameObject("Preset"));
			presetGo.SetActive(false);
			m_preset = presetGo.AddComponent<UiIcon3DPreset>();
			m_preset.FitMode = UiIcon3DPreset.EFitMode.Sphere;
			m_preset.Padding = 0.25f;
			m_preset.ViewRotation = Quaternion.Euler(40, 0, 0);   // looking down: the ground is visible

			// Key light from the left and above: the shadow falls to the right of the object, where the camera sees the ground
			var lightGo = new GameObject("Light");
			lightGo.transform.SetParent(presetGo.transform, false);
			lightGo.transform.localRotation = Quaternion.Euler(10, 70, 0);
			var light = lightGo.AddComponent<Light>();
			light.type = LightType.Directional;
			light.intensity = 1f;

			// Last, so that TearDown has everything it restores
			if (!UiIcon3DRenderer.Backend.SupportsShadowCatcher)
				Assert.Ignore($"{UiIcon3DRenderer.Backend.GetType().Name} has no shadow catcher");
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

			QualitySettings.shadows = m_qualityShadows;
			QualitySettings.shadowDistance = m_qualityDistance;
			QualitySettings.shadowCascades = m_qualityCascades;
		}

		[Test]
		public void The_Catcher_Adds_A_Soft_Black_Shadow_Next_To_The_Object()
		{
			var without = RenderSphere(false, 0.6f);
			var with = RenderSphere(true, 0.6f);

			int added = 0;
			float maxAlpha = 0, worstColour = 0;
			for (int i = 0; i < with.Length; i++)
			{
				if (without[i].a < 0.01f && with[i].a > 0.05f)
				{
					added++;
					maxAlpha = Mathf.Max(maxAlpha, with[i].a);
					worstColour = Mathf.Max(worstColour, with[i].r, with[i].g, with[i].b);
				}
			}

			Debug.Log($"ICON3D shadow catcher: {added} pixels added, darkest alpha {maxAlpha:F2}, brightest colour in them {worstColour:F2}");
			Assert.Greater(added, 30, "no shadow appeared");
			Assert.LessOrEqual(maxAlpha, 0.6f + 0.03f, "shadow darker than its strength");
			Assert.Less(worstColour, 0.02f, "the shadow is not black (premultiplied black = no colour)");
		}

		[Test]
		public void The_Catcher_Leaves_The_Object_Itself_Alone()
		{
			var without = RenderSphere(false, 0.6f);
			var with = RenderSphere(true, 0.6f);

			int centre = Size / 2 * Size + Size / 2;
			Assert.AreEqual(1f, with[centre].a, 0.02f);
			Assert.AreEqual(without[centre].r, with[centre].r, 0.03f);
			Assert.AreEqual(0f, with[0].a, 0.01f, "the corner is not transparent: the ground is visible");
		}

		[Test]
		public void Strength_Scales_The_Shadow()
		{
			float weak = ShadowPeak(RenderSphere(true, 0.3f));
			float strong = ShadowPeak(RenderSphere(true, 0.6f));

			Debug.Log($"ICON3D shadow strength: peak alpha {weak:F2} at 0.3, {strong:F2} at 0.6");
			Assert.Greater(strong, 0.3f, "no shadow to compare");
			Assert.AreEqual(0.5f, weak / strong, 0.15f);
		}

		[Test]
		public void The_Scenes_Quality_Level_Does_Not_Decide_Whether_There_Is_A_Shadow()
		{
			QualitySettings.shadows = ShadowQuality.Disable;
			QualitySettings.shadowDistance = 0f;
			QualitySettings.shadowCascades = 4;

			var with = RenderSphere(true, 0.6f);
			Assert.Greater(ShadowPeak(with), 0.3f, "no shadow with shadows disabled in the quality settings");

			Assert.AreEqual(ShadowQuality.Disable, QualitySettings.shadows, "quality settings not restored");
			Assert.AreEqual(0f, QualitySettings.shadowDistance, "shadow distance not restored");
			Assert.AreEqual(4, QualitySettings.shadowCascades, "cascades not restored");
		}

		[Test]
		public void Without_The_Catcher_Nothing_Changes()
		{
			float peak = ShadowPeak(RenderSphere(false, 0.6f));
			Assert.AreEqual(0f, peak, 0.01f);
		}

		#region Helpers

		private Color[] RenderSphere( bool _catcher, float _strength )
		{
			UiIcon3DRenderer.Shutdown();
			m_preset.ShadowCatcher.Enabled = _catcher;
			m_preset.ShadowCatcher.Strength = _strength;

			var sphere = Track(GameObject.CreatePrimitive(PrimitiveType.Sphere));
			sphere.SetActive(false);
			sphere.GetComponent<Renderer>().sharedMaterial = m_white;

			var go = Track(new GameObject("Icon", typeof(RectTransform)));
			go.transform.SetParent(m_canvas.transform, false);
			((RectTransform)go.transform).sizeDelta = new Vector2(Size, Size);
			var icon = go.AddComponent<UiIcon3D>();
			icon.Preset = m_preset;
			icon.Prefab = sphere;
			UiIcon3DRenderer.Flush();
			Assert.IsTrue(icon.IsRendered);

			var previous = RenderTexture.active;
			RenderTexture.active = (RenderTexture)icon.RawImage.texture;
			m_readback.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
			m_readback.Apply();
			RenderTexture.active = previous;
			return m_readback.GetPixels();
		}

		/// Darkest alpha among the translucent pixels: the shadow, not the (alpha 1) object.
		private static float ShadowPeak( Color[] _pixels )
		{
			float max = 0;
			foreach (var pixel in _pixels)
				if (pixel.a < 0.97f)
					max = Mathf.Max(max, pixel.a);
			return max;
		}

		private T Track<T>( T _obj ) where T : Object
		{
			m_cleanup.Add(_obj);
			return _obj;
		}

		#endregion
	}
}
