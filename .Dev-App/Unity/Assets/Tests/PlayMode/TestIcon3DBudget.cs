using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace GuiToolkit.Test
{
	/// <summary>
	/// The per frame budget of static 3D icon renders: a count (RendersPerFrame) and CPU milliseconds
	/// (RenderMilliseconds); whichever is reached first ends the batch, but every tick renders at least one icon.
	/// The time limits are tested with extreme values - a real limit would make the test depend on the machine.
	/// </summary>
	public class TestIcon3DBudget
	{
		private const int TestLayer = 31;
		private const int Size = 32;
		private const int IconCount = 6;

		private readonly List<Object> m_cleanup = new();
		private readonly List<UiIcon3D> m_icons = new();
		private UiIcon3DPreset m_preset;

		[SetUp]
		public void SetUp()
		{
			UiIcon3DRenderer.Layer = TestLayer;
			UiIcon3DRenderer.MsaaSamples = 1;

			var canvasGo = Track(new GameObject("Canvas", typeof(Canvas)));
			canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

			var presetGo = Track(new GameObject("Preset"));
			presetGo.SetActive(false);
			m_preset = presetGo.AddComponent<UiIcon3DPreset>();

			// Distinct objects: identical icons would share one render
			var material = Track(Icon3DTestModels.CreateOpaque("Budget", Color.white));
			for (int i = 0; i < IconCount; i++)
			{
				var sphere = Track(GameObject.CreatePrimitive(PrimitiveType.Sphere));
				sphere.SetActive(false);
				sphere.GetComponent<Renderer>().sharedMaterial = material;
				sphere.transform.localScale = Vector3.one * (1 + i * 0.1f);

				var go = Track(new GameObject("Icon " + i, typeof(RectTransform)));
				go.transform.SetParent(canvasGo.transform, false);
				((RectTransform)go.transform).sizeDelta = new Vector2(Size, Size);
				var icon = go.AddComponent<UiIcon3D>();
				icon.Preset = m_preset;
				icon.Prefab = sphere;
				m_icons.Add(icon);
			}
		}

		[TearDown]
		public void TearDown()
		{
			foreach (var obj in m_cleanup)
				if (obj != null)
					Object.DestroyImmediate(obj);
			m_cleanup.Clear();
			m_icons.Clear();

			UiIcon3DRenderer.Shutdown();
			UiIcon3DRenderer.Layer = -1;
			UiIcon3DRenderer.MsaaSamples = 0;
			UiIcon3DRenderer.RenderMilliseconds = -1;
		}

		[Test]
		public void An_Exhausted_Time_Budget_Still_Renders_One_Icon()
		{
			int before = UiIcon3DRenderer.RenderCount;
			UiIcon3DRenderer.Process(100, 0.00001f);

			Assert.AreEqual(1, UiIcon3DRenderer.RenderCount - before, "progress guarantee: exactly one render when the time is up from the start");
			Assert.AreEqual(IconCount - 1, UiIcon3DRenderer.DeferredByTimeBudget);
		}

		[Test]
		public void The_Queue_Drains_Even_With_A_Tiny_Time_Budget()
		{
			int ticks = 0;
			while (m_icons.Exists(_i => !_i.IsRendered) && ticks < IconCount * 2)
			{
				UiIcon3DRenderer.Process(100, 0.00001f);
				ticks++;
			}

			Assert.IsTrue(m_icons.TrueForAll(_i => _i.IsRendered), "some icons were never rendered");
			Assert.AreEqual(IconCount, ticks, "one icon per tick");
		}

		[Test]
		public void Zero_Milliseconds_Means_No_Time_Limit()
		{
			int before = UiIcon3DRenderer.RenderCount;
			UiIcon3DRenderer.Process(100, 0);

			Assert.AreEqual(IconCount, UiIcon3DRenderer.RenderCount - before);
			Assert.AreEqual(0, UiIcon3DRenderer.DeferredByTimeBudget);
		}

		[Test]
		public void The_Count_Budget_Still_Applies_With_A_Generous_Time_Budget()
		{
			int before = UiIcon3DRenderer.RenderCount;
			UiIcon3DRenderer.Process(2, 100000f);

			Assert.AreEqual(2, UiIcon3DRenderer.RenderCount - before);
			Assert.AreEqual(0, UiIcon3DRenderer.DeferredByTimeBudget, "deferred by the COUNT is not deferred by time");
		}

		[Test]
		public void The_Time_Spent_Is_Reported()
		{
			UiIcon3DRenderer.Process(100, 0);
			Assert.Greater(UiIcon3DRenderer.LastStaticRenderMilliseconds, 0f);

			UiIcon3DRenderer.Process(100, 0);   // nothing pending any more
			Assert.AreEqual(0f, UiIcon3DRenderer.LastStaticRenderMilliseconds);
		}

		[Test]
		public void The_Setting_Comes_From_The_Configuration_Unless_Overridden()
		{
			UiIcon3DRenderer.RenderMilliseconds = -1;
			float configured = UiIcon3DRenderer.RenderMilliseconds;
			Assert.GreaterOrEqual(configured, 0f);

			UiIcon3DRenderer.RenderMilliseconds = 12.5f;
			Assert.AreEqual(12.5f, UiIcon3DRenderer.RenderMilliseconds);

			UiIcon3DRenderer.RenderMilliseconds = -1;
			Assert.AreEqual(configured, UiIcon3DRenderer.RenderMilliseconds);
		}

		private T Track<T>( T _obj ) where T : Object
		{
			m_cleanup.Add(_obj);
			return _obj;
		}
	}
}
