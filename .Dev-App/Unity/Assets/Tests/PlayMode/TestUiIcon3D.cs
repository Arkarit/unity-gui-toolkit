using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace GuiToolkit.Test
{
	/// <summary>
	/// Tests for the <see cref="UiIcon3D"/> component: size from the rect, no flicker while re-rendering,
	/// sharing, release, bounds hint, periodic mode.
	///
	/// The component requests before the renderer renders and swaps to the finished image right after, so one
	/// <see cref="UiIcon3DRenderer.Flush"/> renders and shows. A tick with budget 0 requests without rendering.
	/// </summary>
	public class TestUiIcon3D
	{
		private const int TestLayer = 31;

		private readonly List<Object> m_cleanup = new();
		private Canvas m_canvas;
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
		public void Placeholder_Until_First_Image()
		{
			var icon = CreateIcon(new Vector2(64, 48));
			Assert.AreSame(RenderTextureManager.Placeholder, icon.RawImage.texture);
			Assert.IsFalse(icon.IsRendered);

			RenderAndShow();

			Assert.IsTrue(icon.IsRendered);
			Assert.IsInstanceOf<RenderTexture>(icon.RawImage.texture);
		}

		[Test]
		public void Texture_Size_Follows_The_Rect()
		{
			var icon = CreateIcon(new Vector2(64, 48));
			RenderAndShow();

			var texture = (RenderTexture)icon.RawImage.texture;
			Assert.AreEqual(64, texture.width);
			Assert.AreEqual(48, texture.height);

			icon.ResolutionScale = 2;
			RenderAndShow();
			texture = (RenderTexture)icon.RawImage.texture;
			Assert.AreEqual(128, texture.width);
			Assert.AreEqual(96, texture.height);
		}

		[Test]
		public void Old_Image_Stays_Until_The_New_One_Is_Rendered()
		{
			var icon = CreateIcon(new Vector2(64, 64));
			RenderAndShow();
			var before = icon.RawImage.texture;

			icon.RectTransform.sizeDelta = new Vector2(80, 80);
			UiIcon3DRenderer.Process(0);   // requests the new size, renders nothing: the old image must stay

			Assert.AreSame(before, icon.RawImage.texture, "new image shown before it was rendered / old one dropped");
			Assert.IsTrue(before != null, "old image was released before the new one exists");

			UiIcon3DRenderer.Flush();      // renders and swaps in the same tick
			Assert.AreNotSame(before, icon.RawImage.texture);
			Assert.IsTrue(icon.RawImage.texture != null, "RawImage shows a destroyed texture");
			Assert.AreEqual(80, icon.RawImage.texture.width);
			Assert.IsTrue(icon.IsRendered);
		}

		[Test]
		public void Identical_Icons_Share_One_Texture()
		{
			var sphere = CreateSphereTemplate();
			var a = CreateIcon(new Vector2(64, 64), sphere);
			var b = CreateIcon(new Vector2(64, 64), sphere);
			int rendersBefore = UiIcon3DRenderer.RenderCount;

			RenderAndShow();

			Assert.AreEqual(a.Key, b.Key);
			Assert.AreSame(a.RawImage.texture, b.RawImage.texture);
			Assert.AreEqual(1, UiIcon3DRenderer.RequestCount);
			Assert.AreEqual(1, UiIcon3DRenderer.RenderCount - rendersBefore);
		}

		[Test]
		public void Disabling_Releases_The_Texture()
		{
			var icon = CreateIcon(new Vector2(64, 64));
			RenderAndShow();
			Assert.AreEqual(1, UiIcon3DRenderer.RequestCount);

			icon.gameObject.SetActive(false);

			Assert.AreEqual(0, UiIcon3DRenderer.RequestCount);
			// Real null, not a destroyed texture: Assert.IsNull uses Unity's == and would accept a dead reference,
			// which then throws in every inspector and editor access
			Assert.IsTrue(ReferenceEquals(icon.RawImage.texture, null), "RawImage still references the released texture");

			icon.gameObject.SetActive(true);
			RenderAndShow();
			Assert.IsTrue(icon.IsRendered, "icon does not come back after re-enabling");
		}

		[Test]
		public void Collapsed_Rect_Holds_No_Texture()
		{
			var icon = CreateIcon(new Vector2(64, 64));
			RenderAndShow();

			icon.RectTransform.sizeDelta = Vector2.zero;
			RenderAndShow();

			Assert.AreEqual(0, UiIcon3DRenderer.RequestCount);
			Assert.AreSame(RenderTextureManager.Placeholder, icon.RawImage.texture);
		}

		[Test]
		public void Bounds_Hint_Overrides_Automatic_Framing()
		{
			var sphere = CreateSphereTemplate();
			var plain = CreateIcon(new Vector2(32, 32), sphere);

			// Same sphere, but framed as if it were four times as large: it must appear much smaller.
			// (A hint smaller than the object would clip it at the near plane, which is computed from the hint.)
			var hinted = CreateSphereTemplate();
			var hint = hinted.AddComponent<UiIcon3DBoundsHint>();
			hint.LocalBounds = new Bounds(Vector3.zero, Vector3.one * 4f);
			var hintedIcon = CreateIcon(new Vector2(32, 32), hinted);

			RenderAndShow();

			float plainCoverage = Coverage(plain.RawImage.texture);
			float hintedCoverage = Coverage(hintedIcon.RawImage.texture);
			Debug.Log($"ICON3D coverage plain {plainCoverage:F3} with bounds hint {hintedCoverage:F3}");
			Assert.Greater(hintedCoverage, 0f, "object vanished");
			Assert.Less(hintedCoverage, plainCoverage * 0.3f, "bounds hint ignored");
		}

		[Test]
		public void View_Rotation_Override_Changes_The_Key()
		{
			var sphere = CreateSphereTemplate();
			var a = CreateIcon(new Vector2(32, 32), sphere);
			var b = CreateIcon(new Vector2(32, 32), sphere);
			b.ViewRotation = Quaternion.Euler(90, 0, 0);
			b.OverrideViewRotation = true;

			RenderAndShow();

			Assert.AreNotEqual(a.Key, b.Key);
			Assert.AreEqual(2, UiIcon3DRenderer.RequestCount);
		}

		[UnityTest]
		public IEnumerator Periodic_Mode_Renders_Again()
		{
			var icon = CreateIcon(new Vector2(32, 32));
			icon.Mode = UiIcon3D.EMode.Periodic;
			icon.RefreshInterval = 0.05f;
			RenderAndShow();
			int renders = UiIcon3DRenderer.RenderCount;

			yield return new WaitForSecondsRealtime(0.15f);
			RenderAndShow();

			Assert.Greater(UiIcon3DRenderer.RenderCount, renders, "periodic icon not rendered again");
		}

		/// Regression: the image used to be shown one tick after it was rendered. In the editor nothing repaints
		/// in between, so a re-enabled icon stayed empty until the user clicked somewhere.
		[Test]
		public void Rendered_Image_Is_Shown_In_The_Same_Tick()
		{
			var icon = CreateIcon(new Vector2(32, 32));
			UiIcon3DRenderer.Flush();
			Assert.IsTrue(icon.IsRendered);
			Assert.IsInstanceOf<RenderTexture>(icon.RawImage.texture);

			icon.gameObject.SetActive(false);
			icon.gameObject.SetActive(true);
			UiIcon3DRenderer.Flush();
			Assert.IsTrue(icon.IsRendered, "re-enabled icon not shown after one tick");
		}

		[Test]
		public void Static_Mode_Does_Not_Render_Again()
		{
			var icon = CreateIcon(new Vector2(32, 32));
			RenderAndShow();
			int renders = UiIcon3DRenderer.RenderCount;

			RenderAndShow();
			RenderAndShow();

			Assert.AreEqual(renders, UiIcon3DRenderer.RenderCount);
			Assert.IsTrue(icon.IsRendered);
		}

		#region Helpers

		private static void RenderAndShow() => UiIcon3DRenderer.Flush();

		private T Track<T>( T _obj ) where T : Object
		{
			m_cleanup.Add(_obj);
			return _obj;
		}

		private UiIcon3D CreateIcon( Vector2 _size, GameObject _object = null )
		{
			var go = Track(new GameObject("Icon", typeof(RectTransform)));
			go.transform.SetParent(m_canvas.transform, false);
			((RectTransform)go.transform).sizeDelta = _size;
			var icon = go.AddComponent<UiIcon3D>();
			icon.Prefab = _object != null ? _object : CreateSphereTemplate();
			icon.Preset = CreatePreset();
			return icon;
		}

		private GameObject CreateSphereTemplate()
		{
			var sphere = Track(GameObject.CreatePrimitive(PrimitiveType.Sphere));
			sphere.SetActive(false);
			sphere.GetComponent<Renderer>().sharedMaterial = m_whiteLit;
			return sphere;
		}

		private UiIcon3DPreset m_preset;

		private UiIcon3DPreset CreatePreset()
		{
			if (m_preset != null)
				return m_preset;

			var go = Track(new GameObject("Preset"));
			go.SetActive(false);
			m_preset = go.AddComponent<UiIcon3DPreset>();
			m_preset.FitMode = UiIcon3DPreset.EFitMode.Sphere;

			var lightGo = new GameObject("Light");
			lightGo.transform.SetParent(go.transform, false);
			var light = lightGo.AddComponent<Light>();
			light.type = LightType.Directional;
			return m_preset;
		}

		private float Coverage( Texture _texture )
		{
			var rt = (RenderTexture)_texture;
			if (m_readback == null || m_readback.width != rt.width || m_readback.height != rt.height)
				m_readback = Track(new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false));

			var previous = RenderTexture.active;
			RenderTexture.active = rt;
			m_readback.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
			m_readback.Apply();
			RenderTexture.active = previous;

			int covered = 0;
			var pixels = m_readback.GetPixels();
			foreach (var p in pixels)
				if (p.a > 0.5f)
					covered++;

			return (float)covered / pixels.Length;
		}

		#endregion
	}
}
