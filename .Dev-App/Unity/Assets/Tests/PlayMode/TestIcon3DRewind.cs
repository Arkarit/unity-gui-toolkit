using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GuiToolkit.Test
{
	/// <summary>
	/// Rewinding an animated icon (hover out): the animation runs back to the first frame of its loop and stands still
	/// there; hovering in again meanwhile carries on forward from the current time. Real time passes in these tests - the
	/// Animator runs with the time of the frame - and the test clip (the tentacle's) is one second long and loops.
	/// </summary>
	public class TestIcon3DRewind
	{
		private const int TestLayer = 31;
		private const int Size = 32;

		private readonly List<Object> m_cleanup = new();
		private readonly List<Icon3DHandle> m_handles = new();
		private UiIcon3DPreset m_preset;
		private GameObject m_tentacle;
		private Texture2D m_readback;
		private Canvas m_canvas;

		[OneTimeSetUp]
		public void GenerateModels()
		{
#if UNITY_EDITOR
			Icon3DTestModels.EnsureAssets();
#endif
		}

		private static bool s_warmedUp;

		/// The first render of a session can take a third of a second (shaders, pipeline start): it would eat into a test that
		/// measures how long a rewind takes. Done once, before the first test.
		[UnitySetUp]
		public IEnumerator WarmUp()
		{
			if (s_warmedUp)
				yield break;

			s_warmedUp = true;
			UiIcon3DRenderer.Layer = TestLayer;
			UiIcon3DRenderer.MsaaSamples = 1;
#if UNITY_EDITOR
			var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Icon3DTestModels.TentaclePath);
			var handle = UiIcon3DRenderer.RenderAnimated(prefab, null, new Vector2Int(Size, Size));
			handle.IsVisible = true;
			yield return Wait(1f);
			handle.Release();
#endif
			UiIcon3DRenderer.Shutdown();
		}

		[SetUp]
		public void SetUp()
		{
			UiIcon3DRenderer.Layer = TestLayer;
			UiIcon3DRenderer.MsaaSamples = 1;

			var canvasGo = Track(new GameObject("Canvas", typeof(Canvas)));
			m_canvas = canvasGo.GetComponent<Canvas>();
			m_canvas.renderMode = RenderMode.ScreenSpaceOverlay;

			var presetGo = Track(new GameObject("Preset"));
			presetGo.SetActive(false);
			m_preset = presetGo.AddComponent<UiIcon3DPreset>();
			m_preset.FitMode = UiIcon3DPreset.EFitMode.Sphere;
			var lightGo = new GameObject("Light");
			lightGo.transform.SetParent(presetGo.transform, false);
			var light = lightGo.AddComponent<Light>();
			light.type = LightType.Directional;

			m_readback = Track(new Texture2D(Size, Size, TextureFormat.RGBA32, false));
#if UNITY_EDITOR
			m_tentacle = AssetDatabase.LoadAssetAtPath<GameObject>(Icon3DTestModels.TentaclePath);
#endif
			Assert.IsNotNull(m_tentacle, "test model missing (Gui Toolkit > 3D Icons > Dev: Rebuild Test Models, in edit mode)");
		}

		[TearDown]
		public void TearDown()
		{
			foreach (var handle in m_handles)
				handle.Release();
			m_handles.Clear();

			foreach (var obj in m_cleanup)
				if (obj != null)
					Object.DestroyImmediate(obj);
			m_cleanup.Clear();

			UiIcon3DRenderer.Shutdown();
			UiIcon3DRenderer.Layer = -1;
			UiIcon3DRenderer.MsaaSamples = 0;
		}

		private Icon3DHandle Animated( GameObject _object = null )
		{
			var handle = UiIcon3DRenderer.RenderAnimated(_object != null ? _object : m_tentacle, m_preset, new Vector2Int(Size, Size));
			handle.IsVisible = true;
			m_handles.Add(handle);
			return handle;
		}

		/// Lets real time pass, in frames, until the condition holds or the time is up.
		private static IEnumerator Until( System.Func<bool> _condition, float _seconds )
		{
			float end = Time.realtimeSinceStartup + _seconds;
			while (!_condition() && Time.realtimeSinceStartup < end)
				yield return null;
		}

		private static IEnumerator Wait( float _seconds )
		{
			float end = Time.realtimeSinceStartup + _seconds;
			while (Time.realtimeSinceStartup < end)
				yield return null;
		}

		private static float NormalizedTime( Icon3DHandle _handle ) =>
			_handle.Request.Instance.GetComponentInChildren<Animator>().GetCurrentAnimatorStateInfo(0).normalizedTime;

		[UnityTest]
		public IEnumerator Rewind_Ends_On_The_First_Frame_And_Stands_Still()
		{
			var handle = Animated();

			// The first frame of this icon: what the rewind has to come back to. It is read a frame or two after the first
			// render, so it is not exactly time 0 - a pixel or so of the contour differs. Hence no exact comparison below.
			yield return Until(() => handle.IsRendered, 2f);
			Assert.IsTrue(handle.IsRendered);
			var first = Read(handle.Texture);

			yield return Wait(0.6f);
			Assert.IsTrue(handle.IsPlaying);
			var running = Read(handle.Texture);

			handle.Rewind(4f);
			Assert.IsTrue(handle.IsRewinding);
			yield return Until(() => !handle.IsPlaying, 3f);

			Assert.IsFalse(handle.IsPlaying, "the rewind never ended");
			Assert.IsFalse(handle.IsRewinding);

			var rewound = Read(handle.Texture);

			// Back at the start means: much closer to the first frame than to the frame it was at when the rewind began
			int toRunning = CountDifferent(first, running);
			int toRewound = CountDifferent(first, rewound);
			Debug.Log($"ICON3D pixels differing from the first frame: running {toRunning}, rewound {toRewound}");
			Assert.Greater(toRunning, 30, "the animation had not moved: nothing to compare");
			Assert.Less(toRewound * 4, toRunning, "the rewound image is not the first frame");
		}

		private static int CountDifferent( Color[] _a, Color[] _b )
		{
			int count = 0;
			for (int i = 0; i < _a.Length; i++)
				if (Mathf.Abs(_a[i].a - _b[i].a) > 0.3f || Mathf.Abs(_a[i].r - _b[i].r) > 0.3f)
					count++;
			return count;
		}

		[UnityTest]
		public IEnumerator Rewinding_Is_Faster_Than_Playing()
		{
			var handle = Animated();
			yield return Wait(0.8f);

			float start = Time.realtimeSinceStartup;
			handle.Rewind(4f);
			yield return Until(() => !handle.IsPlaying, 3f);
			float took = Time.realtimeSinceStartup - start;

			Debug.Log($"ICON3D rewind of about 0.8 s of animation at 4x took {took:F2} s");
			Assert.IsFalse(handle.IsPlaying);
			Assert.Less(took, 0.5f, "rewinding at 4x should take about a quarter of the time played");
			Assert.Greater(took, 0.05f, "it jumped instead of running back");
		}

		[UnityTest]
		public IEnumerator Only_The_Current_Loop_Is_Rewound()
		{
			var handle = Animated();
			yield return Wait(1.3f);   // 1.3 loops

			float start = Time.realtimeSinceStartup;
			handle.Rewind(1f);
			yield return Until(() => !handle.IsPlaying, 3f);
			float took = Time.realtimeSinceStartup - start;

			Debug.Log($"ICON3D rewind of 0.3 s into the second loop at 1x took {took:F2} s");
			Assert.IsFalse(handle.IsPlaying);
			Assert.Less(took, 0.7f, "it ran back over the whole animation instead of to the start of the loop");
		}

		[UnityTest]
		public IEnumerator Playing_Again_During_The_Rewind_Carries_On_From_Where_It_Is()
		{
			var handle = Animated();
			yield return Wait(0.7f);

			handle.Rewind(1f);
			yield return Wait(0.15f);
			Assert.IsTrue(handle.IsRewinding);
			float turning = NormalizedTime(handle);

			handle.IsPlaying = true;
			Assert.IsFalse(handle.IsRewinding);
			yield return Wait(0.15f);
			float after = NormalizedTime(handle);

			Debug.Log($"ICON3D turned around at {turning:F2}, {after:F2} a moment later");
			Assert.Greater(turning, 0.15f, "it had already run back to the start");
			Assert.Greater(after, turning, "it did not carry on forward");
			Assert.Greater(after, 0.2f, "it started over instead of carrying on");
			Assert.IsTrue(handle.IsPlaying);
		}

		[UnityTest]
		public IEnumerator Rewind_Without_An_Animator_Freezes_Where_It_Is()
		{
			var sphere = Track(GameObject.CreatePrimitive(PrimitiveType.Sphere));
			sphere.SetActive(false);
			sphere.GetComponent<Renderer>().sharedMaterial = Track(Icon3DTestModels.CreateOpaque("Plain", Color.white));

			var handle = Animated(sphere);
			yield return Wait(0.1f);
			handle.Rewind(2f);

			Assert.IsFalse(handle.IsPlaying, "nothing to run back: the icon has to stop");
			Assert.IsFalse(handle.IsRewinding);
		}

		[UnityTest]
		public IEnumerator Rewind_Of_A_Frozen_Icon_Does_Nothing()
		{
			var handle = Animated();
			yield return Wait(0.4f);
			handle.IsPlaying = false;
			var before = Read(handle.Texture);

			handle.Rewind(4f);
			yield return Wait(0.2f);

			Assert.IsFalse(handle.IsPlaying);
			Assert.IsFalse(handle.IsRewinding);
			var after = Read(handle.Texture);
			for (int i = 0; i < before.Length; i++)
				Assert.AreEqual(before[i].a, after[i].a, 0.001f);
		}

		[UnityTest]
		public IEnumerator The_Component_Rewinds_In_Rewinding_Mode()
		{
			var go = Track(new GameObject("Icon", typeof(RectTransform)));
			go.transform.SetParent(m_canvas.transform, false);
			((RectTransform)go.transform).sizeDelta = new Vector2(Size, Size);
			var icon = go.AddComponent<UiIcon3D>();
			icon.Preset = m_preset;
			icon.Prefab = m_tentacle;
			icon.RewindSpeed = 4f;
			icon.Mode = UiIcon3D.EMode.Animated;

			yield return Wait(0.5f);
			Assert.IsTrue(icon.IsRendered);

			icon.Mode = UiIcon3D.EMode.Rewinding;
			yield return Wait(0.6f);

			// Stood still on the first frame: the image no longer changes
			var first = Read((RenderTexture)icon.RawImage.texture);
			yield return Wait(0.2f);
			var second = Read((RenderTexture)icon.RawImage.texture);
			for (int i = 0; i < first.Length; i++)
				Assert.AreEqual(first[i].a, second[i].a, 0.001f, "the icon keeps changing after the rewind");

			// And it plays again from there
			icon.Mode = UiIcon3D.EMode.Animated;
			var images = new HashSet<string>();
			for (int i = 0; i < 6; i++)
			{
				yield return Wait(0.12f);
				images.Add(Hash(Read((RenderTexture)icon.RawImage.texture)));
			}

			Assert.Greater(images.Count, 2, "the animation did not start again");
		}

		private Color[] Read( Texture _texture )
		{
			var previous = RenderTexture.active;
			RenderTexture.active = (RenderTexture)_texture;
			m_readback.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
			m_readback.Apply();
			RenderTexture.active = previous;
			return m_readback.GetPixels();
		}

		private static string Hash( Color[] _pixels )
		{
			var sb = new System.Text.StringBuilder();
			for (int i = 0; i < _pixels.Length; i += 7)
				sb.Append((int)(_pixels[i].a * 10));
			return sb.ToString();
		}

		private T Track<T>( T _obj ) where T : Object
		{
			m_cleanup.Add(_obj);
			return _obj;
		}
	}
}
