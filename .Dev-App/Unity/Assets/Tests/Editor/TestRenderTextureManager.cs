using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace GuiToolkit.Test
{
	[EditorAware]
	public class TestRenderTextureManager
	{
		private class FakeProducer : IRenderTextureProducer
		{
			public RenderTextureSpec Spec = RenderTextureSpec.Default;
			public RenderTexture Texture;
			public int NotifyCount;

			public RenderTextureSpec RenderTextureSpec => Spec;

			public void OnRenderTextureChanged( RenderTexture _texture )
			{
				Texture = _texture;
				NotifyCount++;
			}
		}

		private class FakeConsumer : IRenderTextureConsumer
		{
			public Vector2Int Size;
			public RenderTexture Texture;
			public int NotifyCount;

			public FakeConsumer( int _w, int _h ) => Size = new Vector2Int(_w, _h);

			public Vector2Int RequestedSize => Size;

			public void OnRenderTextureChanged( RenderTexture _texture )
			{
				Texture = _texture;
				NotifyCount++;
			}
		}

		private readonly List<(string key, IRenderTextureUser user)> m_registered = new();

		[TearDown]
		public void TearDown()
		{
			foreach (var (key, user) in m_registered)
			{
				if (user is IRenderTextureProducer p)
					RenderTextureManager.UnregisterProducer(key, p);
				else
					RenderTextureManager.UnregisterConsumer(key, (IRenderTextureConsumer)user);
			}

			m_registered.Clear();
		}

		private FakeProducer AddProducer( string _key, RenderTextureSpec? _spec = null )
		{
			var result = new FakeProducer();
			if (_spec.HasValue)
				result.Spec = _spec.Value;
			RenderTextureManager.RegisterProducer(_key, result);
			m_registered.Add((_key, result));
			return result;
		}

		private FakeConsumer AddConsumer( string _key, int _w, int _h )
		{
			var result = new FakeConsumer(_w, _h);
			RenderTextureManager.RegisterConsumer(_key, result);
			m_registered.Add((_key, result));
			return result;
		}

		[Test]
		public void Producer_Without_Consumer_Gets_No_Texture()
		{
			var key = RenderTextureManager.CreateUniqueKey("test");
			var producer = AddProducer(key);

			Assert.IsNull(producer.Texture);
			Assert.IsNull(RenderTextureManager.GetTexture(key));
		}

		[Test]
		public void Consumer_Without_Producer_Gets_No_Texture()
		{
			var key = RenderTextureManager.CreateUniqueKey("test");
			var consumer = AddConsumer(key, 100, 100);

			Assert.IsNull(consumer.Texture);
			Assert.AreEqual(1, consumer.NotifyCount, "consumer must learn that there is no texture yet");
		}

		[Test]
		public void Texture_Is_Created_Regardless_Of_Registration_Order()
		{
			var keyA = RenderTextureManager.CreateUniqueKey("test");
			var producerA = AddProducer(keyA);
			var consumerA = AddConsumer(keyA, 100, 50);

			var keyB = RenderTextureManager.CreateUniqueKey("test");
			var consumerB = AddConsumer(keyB, 100, 50);
			var producerB = AddProducer(keyB);

			Assert.IsNotNull(producerA.Texture);
			Assert.AreSame(producerA.Texture, consumerA.Texture);
			Assert.IsNotNull(producerB.Texture);
			Assert.AreSame(producerB.Texture, consumerB.Texture);
			Assert.AreNotSame(producerA.Texture, producerB.Texture);
		}

		[Test]
		public void Size_Is_Largest_Consumer_Rounded_To_Granularity()
		{
			var key = RenderTextureManager.CreateUniqueKey("test");
			AddProducer(key);
			AddConsumer(key, 100, 20);
			AddConsumer(key, 40, 70);

			var texture = RenderTextureManager.GetTexture(key);
			Assert.AreEqual(112, texture.width);
			Assert.AreEqual(80, texture.height);
		}

		[Test]
		public void Fixed_Size_Overrides_Consumers()
		{
			var spec = RenderTextureSpec.Default;
			spec.FixedSize = new Vector2Int(64, 32);
			var key = RenderTextureManager.CreateUniqueKey("test");
			AddProducer(key, spec);
			AddConsumer(key, 500, 500);

			var texture = RenderTextureManager.GetTexture(key);
			Assert.AreEqual(64, texture.width);
			Assert.AreEqual(32, texture.height);
		}

		[Test]
		public void Resize_Keeps_Texture_Object_And_Notifies()
		{
			var key = RenderTextureManager.CreateUniqueKey("test");
			var producer = AddProducer(key);
			var consumer = AddConsumer(key, 100, 100);
			var before = consumer.Texture;
			int notifiesBefore = consumer.NotifyCount;

			consumer.Size = new Vector2Int(300, 200);
			RenderTextureManager.FlushAll();

			Assert.AreSame(before, consumer.Texture);
			Assert.AreSame(before, producer.Texture);
			Assert.AreEqual(304, consumer.Texture.width);
			Assert.AreEqual(208, consumer.Texture.height);
			Assert.Greater(consumer.NotifyCount, notifiesBefore);
		}

		[Test]
		public void Unchanged_Flush_Does_Not_Notify()
		{
			var key = RenderTextureManager.CreateUniqueKey("test");
			AddProducer(key);
			var consumer = AddConsumer(key, 100, 100);
			int notifiesBefore = consumer.NotifyCount;

			RenderTextureManager.FlushAll();
			RenderTextureManager.FlushAll();

			Assert.AreEqual(notifiesBefore, consumer.NotifyCount);
		}

		[Test]
		public void Removing_Producer_Releases_Texture()
		{
			var key = RenderTextureManager.CreateUniqueKey("test");
			var producer = AddProducer(key);
			var consumer = AddConsumer(key, 100, 100);
			var texture = consumer.Texture;

			RenderTextureManager.UnregisterProducer(key, producer);
			m_registered.RemoveAll(r => r.user == producer);

			Assert.IsNull(consumer.Texture);
			Assert.IsTrue(texture == null, "texture must be destroyed");
		}

		[Test]
		public void Removing_Everyone_Forgets_Keyword()
		{
			int countBefore = RenderTextureManager.Count;
			var key = RenderTextureManager.CreateUniqueKey("test");
			var producer = AddProducer(key);
			var consumer = AddConsumer(key, 100, 100);

			RenderTextureManager.UnregisterProducer(key, producer);
			RenderTextureManager.UnregisterConsumer(key, consumer);
			m_registered.Clear();

			Assert.AreEqual(countBefore, RenderTextureManager.Count);
		}

		[Test]
		public void Spec_Change_Is_Applied_On_Flush()
		{
			var key = RenderTextureManager.CreateUniqueKey("test");
			var producer = AddProducer(key);
			AddConsumer(key, 100, 100);

			producer.Spec.MsaaSamples = 4;
			producer.Spec.DepthBits = 0;
			RenderTextureManager.SetDirty(key);
			RenderTextureManager.FlushAll();

			var texture = RenderTextureManager.GetTexture(key);
			Assert.AreEqual(4, texture.antiAliasing);
			Assert.AreEqual(0, texture.depth);
		}

		[Test]
		public void Components_Link_Camera_And_RawImage()
		{
			var key = RenderTextureManager.CreateUniqueKey("test");
			var canvasGo = new GameObject("Canvas", typeof(Canvas));
			var cameraGo = new GameObject("Camera", typeof(Camera));
			try
			{
				canvasGo.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
				var imageGo = new GameObject("RawImage", typeof(RectTransform), typeof(UnityEngine.UI.RawImage));
				imageGo.transform.SetParent(canvasGo.transform, false);
				((RectTransform)imageGo.transform).sizeDelta = new Vector2(128, 64);

				var consumer = imageGo.AddComponent<UiRawImageRenderTextureConsumer>();
				consumer.Key = key;
				var rawImage = imageGo.GetComponent<UnityEngine.UI.RawImage>();
				Assert.AreSame(RenderTextureManager.Placeholder, rawImage.texture, "placeholder while no camera exists");

				var producer = cameraGo.AddComponent<UiRenderTextureProducer>();
				producer.Key = key;
				var camera = cameraGo.GetComponent<Camera>();

				Assert.IsNotNull(producer.Texture);
				Assert.AreSame(producer.Texture, rawImage.texture);
				Assert.AreSame(producer.Texture, camera.targetTexture);
				Assert.IsTrue(camera.enabled);
				Assert.AreEqual(128, producer.Texture.width);
				Assert.AreEqual(64, producer.Texture.height);

				// Resize while the camera targets the texture; must not log "Releasing render texture that is set as Camera.targetTexture"
				((RectTransform)imageGo.transform).sizeDelta = new Vector2(256, 64);
				RenderTextureManager.FlushAll();
				Assert.AreEqual(256, producer.Texture.width);
				Assert.AreSame(producer.Texture, camera.targetTexture);

				Object.DestroyImmediate(producer);
				Assert.AreSame(RenderTextureManager.Placeholder, rawImage.texture, "placeholder after camera is gone");
				Assert.IsNull(camera.targetTexture);
			}
			finally
			{
				Object.DestroyImmediate(cameraGo);
				Object.DestroyImmediate(canvasGo);
			}
		}

		[Test]
		public void Unique_Keys_Differ()
		{
			Assert.AreNotEqual(RenderTextureManager.CreateUniqueKey(), RenderTextureManager.CreateUniqueKey());
		}
	}

	public class TestPerCameraSetting
	{
		private static int s_global;
		private static string s_globalText;

		[SetUp]
		public void SetUp()
		{
			s_global = 1;
			s_globalText = "original";
		}

		[Test]
		public void Apply_And_Restore_Round_Trip()
		{
			var setting = new PerCameraSetting<int>(42, true).Bind(() => s_global, v => s_global = v);

			setting.Apply();
			Assert.AreEqual(42, s_global);
			Assert.IsTrue(setting.IsApplied);

			setting.Restore();
			Assert.AreEqual(1, s_global);
			Assert.IsFalse(setting.IsApplied);
		}

		[Test]
		public void Disabled_Setting_Leaves_Global_Untouched()
		{
			var setting = new PerCameraSetting<int>(42).Bind(() => s_global, v => s_global = v);

			setting.Apply();
			Assert.AreEqual(1, s_global);
			setting.Restore();
			Assert.AreEqual(1, s_global);
		}

		[Test]
		public void Double_Apply_Keeps_First_Original()
		{
			var setting = new PerCameraSetting<string>("override", true).Bind(() => s_globalText, v => s_globalText = v);

			setting.Apply();
			setting.Apply();
			setting.Restore();

			Assert.AreEqual("original", s_globalText);
		}

		[Test]
		public void Nested_Settings_Restore_In_Reverse_Order()
		{
			var outer = new PerCameraSetting<int>(10, true).Bind(() => s_global, v => s_global = v);
			var inner = new PerCameraSetting<int>(20, true).Bind(() => s_global, v => s_global = v);

			outer.Apply();
			inner.Apply();
			Assert.AreEqual(20, s_global);

			inner.Restore();
			Assert.AreEqual(10, s_global);
			outer.Restore();
			Assert.AreEqual(1, s_global);
		}

		[Test]
		public void Camera_Render_Applies_And_Restores_RenderSettings()
		{
			var originalFogColor = RenderSettings.fogColor;
			var overrideColor = new Color(0.1f, 0.2f, 0.3f, 1);
			Color? seenDuringRender = null;
			Camera.CameraCallback probe = _ => seenDuringRender = RenderSettings.fogColor;

			var go = new GameObject("PerCameraSettingsTestCamera");
			var texture = new RenderTexture(16, 16, 24);
			try
			{
				var camera = go.AddComponent<Camera>();
				camera.enabled = false;
				camera.targetTexture = texture;
				var settings = go.AddComponent<UiCameraRenderSettings>();
				settings.FogColor.Value = overrideColor;
				settings.FogColor.Enabled = true;

				// onPreRender fires after onPreCull, where the settings are applied
				Camera.onPreRender += probe;
				camera.Render();

				Assert.IsTrue(seenDuringRender.HasValue, "camera did not render");
				Assert.AreEqual(overrideColor, seenDuringRender.Value);
				Assert.AreEqual(originalFogColor, RenderSettings.fogColor);
				Assert.IsFalse(settings.IsApplied);
			}
			finally
			{
				Camera.onPreRender -= probe;
				Object.DestroyImmediate(go);
				Object.DestroyImmediate(texture);
			}
		}

		[Test]
		public void Capture_Copies_Global()
		{
			s_global = 7;
			var setting = new PerCameraSetting<int>(0).Bind(() => s_global, v => s_global = v);

			setting.Capture();

			Assert.AreEqual(7, setting.Value);
		}
	}
}
