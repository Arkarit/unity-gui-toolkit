using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace GuiToolkit.Test
{
	/// <summary>
	/// What the diagnostics report about the 3D icon renderer and the render texture manager: distinct icons, shared
	/// renders, memory, pending work, renders per tick, other managed textures.
	/// </summary>
	public class TestIcon3DDiagnostics
	{
		private const int TestLayer = 31;
		private const int Size = 32;

		private sealed class FixedProducer : IRenderTextureProducer
		{
			public RenderTextureSpec RenderTextureSpec { get; } = new()
			{
				FixedSize = new Vector2Int(64, 64), SizeGranularity = 1, ColorFormat = RenderTextureFormat.ARGB32, DepthBits = 0, MsaaSamples = 1,
			};

			public void OnRenderTextureChanged( RenderTexture _texture ) { }
		}

		private readonly List<Object> m_cleanup = new();
		private Canvas m_canvas;
		private UiIcon3DPreset m_preset;
		private Material m_opaque;

		[SetUp]
		public void SetUp()
		{
			UiIcon3DRenderer.Layer = TestLayer;
			UiIcon3DRenderer.MsaaSamples = 1;

			var canvasGo = Track(new GameObject("Canvas", typeof(Canvas)));
			m_canvas = canvasGo.GetComponent<Canvas>();
			m_canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			m_opaque = Track(Icon3DTestModels.CreateOpaque("Diag", Color.white));

			var presetGo = Track(new GameObject("Preset"));
			presetGo.SetActive(false);
			m_preset = presetGo.AddComponent<UiIcon3DPreset>();
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
			UiIcon3DRenderer.ResetPeakTickRenders();
		}

		[Test]
		public void Nothing_Alive_Reports_Nothing()
		{
			var icons = new List<Icon3DInfo>();
			var summary = Icon3DDiagnostics.Collect(icons);

			Assert.AreEqual(0, icons.Count);
			Assert.AreEqual(0, summary.Requests);
			Assert.AreEqual(0, summary.Pending);
			Assert.AreEqual(0, summary.IconBytes);
		}

		[Test]
		public void Identical_Icons_Are_One_Entry_With_Two_References()
		{
			var sphere = CreateSphere();
			CreateIcon(sphere);
			CreateIcon(sphere);
			CreateIcon(CreateSphere(1.5f));
			UiIcon3DRenderer.Process(0);   // the icons register their requests in the tick, nothing renders

			var icons = new List<Icon3DInfo>();
			var summary = Icon3DDiagnostics.Collect(icons);

			Assert.AreEqual(2, summary.Requests, "two distinct icons expected");
			Assert.AreEqual(new[] { 1, 2 }, icons.Select(_i => _i.References).OrderBy(_r => _r).ToArray());
		}

		[Test]
		public void Size_Memory_And_State_Follow_The_Render()
		{
			var icon = CreateIcon(CreateSphere());
			var icons = new List<Icon3DInfo>();
			UiIcon3DRenderer.Process(0);

			var before = Icon3DDiagnostics.Collect(icons);
			Assert.AreEqual(1, before.Pending, "a new icon is pending before its first render");
			Assert.IsFalse(icons[0].IsRendered);

			UiIcon3DRenderer.Flush();
			var after = Icon3DDiagnostics.Collect(icons);

			Assert.AreEqual(0, after.Pending);
			Assert.IsTrue(icons[0].IsRendered);
			Assert.GreaterOrEqual(icons[0].Size.x, Size);
			Assert.Greater(icons[0].Bytes, 0);
			Assert.AreEqual(icons[0].Bytes, after.IconBytes);
			Assert.GreaterOrEqual(after.TotalBytes, after.IconBytes);
			Assert.AreSame(icon.Prefab, icons[0].Prefab);
		}

		[Test]
		public void Renders_Per_Tick_And_Peak_Are_Reported()
		{
			for (int i = 0; i < 5; i++)
				CreateIcon(CreateSphere(1 + i * 0.1f));

			UiIcon3DRenderer.ResetPeakTickRenders();
			UiIcon3DRenderer.Process(3);
			var first = Icon3DDiagnostics.Collect(new List<Icon3DInfo>());
			Assert.AreEqual(3, first.LastTickRenders);
			Assert.AreEqual(3, first.PeakTickRenders);

			UiIcon3DRenderer.Process(3);
			var second = Icon3DDiagnostics.Collect(new List<Icon3DInfo>());
			Assert.AreEqual(2, second.LastTickRenders);
			Assert.AreEqual(3, second.PeakTickRenders, "the peak must stay at the highest tick");

			UiIcon3DRenderer.ResetPeakTickRenders();
			Assert.AreEqual(0, Icon3DDiagnostics.Collect(new List<Icon3DInfo>()).PeakTickRenders);
		}

		[Test]
		public void Animated_And_Two_Render_Icons_Are_Flagged()
		{
			var animated = CreateIcon(CreateSphere());
			animated.Mode = UiIcon3D.EMode.Animated;

			var glass = Track(GameObject.CreatePrimitive(PrimitiveType.Sphere));
			glass.SetActive(false);
			glass.GetComponent<Renderer>().sharedMaterial = Track(Icon3DTestModels.CreateTransparent("Glass", new Color(1, 1, 1, 0.5f)));
			CreateIcon(glass);
			UiIcon3DRenderer.Flush();

			var icons = new List<Icon3DInfo>();
			var summary = Icon3DDiagnostics.Collect(icons);

			Assert.AreEqual(1, summary.Animated);
			Assert.AreEqual(1, icons.Count(_i => _i.IsAnimated && _i.IsPlaying));
			Assert.AreEqual(1, icons.Count(_i => _i.ExactAlpha), "only the object with a transparent material needs two renders");
		}

		[Test]
		public void Other_Managed_Textures_Are_Listed_Apart_From_The_Icons()
		{
			CreateIcon(CreateSphere());
			UiIcon3DRenderer.Flush();

			var producer = new FixedProducer();
			string key = RenderTextureManager.CreateUniqueKey("diag");
			RenderTextureManager.RegisterProducer(key, producer);
			try
			{
				RenderTextureManager.FlushAll();

				var icons = new List<Icon3DInfo>();
				var others = new List<RenderTextureInfo>();
				var summary = Icon3DDiagnostics.Collect(icons, others);

				Assert.AreEqual(1, others.Count, "icon textures must not show up as 'other'");
				Assert.AreEqual(key, others[0].Key);
				Assert.AreEqual(64, others[0].Width);
				Assert.AreEqual(1, others[0].Producers);
				Assert.Greater(others[0].Bytes, 0);
				Assert.Greater(summary.TotalBytes, summary.IconBytes, "the total includes the other texture");
			}
			finally
			{
				RenderTextureManager.UnregisterProducer(key, producer);
				RenderTextureManager.FlushAll();
			}
		}

		[Test]
		public void Destroyed_Icons_Leave_The_Report()
		{
			var icon = CreateIcon(CreateSphere());
			UiIcon3DRenderer.Flush();
			Assert.AreEqual(1, Icon3DDiagnostics.Collect(new List<Icon3DInfo>()).Requests);

			Object.DestroyImmediate(icon.gameObject);
			Assert.AreEqual(0, Icon3DDiagnostics.Collect(new List<Icon3DInfo>()).Requests);
		}

		[TestCase(0L, "0 B")]
		[TestCase(1023L, "1023 B")]
		[TestCase(2048L, "2 KB")]
		[TestCase(5L * 1024 * 1024 + 512 * 1024, "5.5 MB")]
		public void Bytes_Are_Formatted_Readably( long _bytes, string _expected )
		{
			Assert.AreEqual(_expected, Icon3DDiagnostics.FormatBytes(_bytes));
		}

		private T Track<T>( T _obj ) where T : Object
		{
			m_cleanup.Add(_obj);
			return _obj;
		}

		private GameObject CreateSphere( float _scale = 1 )
		{
			var sphere = Track(GameObject.CreatePrimitive(PrimitiveType.Sphere));
			sphere.SetActive(false);
			sphere.transform.localScale = Vector3.one * _scale;
			sphere.GetComponent<Renderer>().sharedMaterial = m_opaque;
			return sphere;
		}

		private UiIcon3D CreateIcon( GameObject _prefab )
		{
			var go = Track(new GameObject("Icon", typeof(RectTransform)));
			go.transform.SetParent(m_canvas.transform, false);
			((RectTransform)go.transform).sizeDelta = new Vector2(Size, Size);
			var icon = go.AddComponent<UiIcon3D>();
			icon.Preset = m_preset;
			icon.Prefab = _prefab;
			return icon;
		}
	}
}
