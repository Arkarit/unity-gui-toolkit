using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GuiToolkit.AssetHandling;
using GuiToolkit.Exceptions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace GuiToolkit.Test
{
	/// <summary>
	/// Dynamic loading of 3D icon objects (phase 3): shared loads, races between loading and reassignment,
	/// failures, render order by visibility. A fake provider lets the test decide when each load completes.
	/// </summary>
	public class TestIcon3DLoading
	{
		private const int TestLayer = 31;
		private const string Prefix = "fake3d:";

		private sealed class FakeHandle : IAssetHandle<GameObject>
		{
			private readonly FakeProvider m_provider;
			public GameObject Asset { get; private set; }
			public CanonicalAssetKey Key { get; }
			public bool IsLoaded => Asset != null;

			public FakeHandle( FakeProvider _provider, CanonicalAssetKey _key, GameObject _asset )
			{
				m_provider = _provider;
				Key = _key;
				Asset = _asset;
			}

			public void Release()
			{
				m_provider.ReleaseCount.TryGetValue(Key.Id, out int count);
				m_provider.ReleaseCount[Key.Id] = count + 1;
				Asset = null;
			}
		}

		/// Loads complete only when the test calls Complete() or Fail().
		private sealed class FakeProvider : IAssetProvider
		{
			public readonly Dictionary<string, int> LoadCount = new();
			public readonly Dictionary<string, int> ReleaseCount = new();
			private readonly Dictionary<string, TaskCompletionSource<IAssetHandle<GameObject>>> m_pending = new();

			public string Name => "Fake 3D Icon Provider";
			public string ResName => "fake";
			public string Prefix => TestIcon3DLoading.Prefix;
			public IAssetProviderEditorBridge EditorBridge => null;
			public bool IsInitialized => true;
			public void Init() { }

			public void Complete( string _id, GameObject _asset )
			{
				var key = NormalizeKey(_id, typeof(GameObject));
				m_pending[_id].SetResult(new FakeHandle(this, key, _asset));
				m_pending.Remove(_id);
			}

			public void Fail( string _id )
			{
				m_pending[_id].SetException(new InvalidOperationException("fake load failure"));
				m_pending.Remove(_id);
			}

			public int Loads( string _id ) => LoadCount.TryGetValue(_id, out int count) ? count : 0;
			public int Releases( string _id ) => ReleaseCount.TryGetValue(_id, out int count) ? count : 0;

			public Task<IAssetHandle<T>> LoadAssetAsync<T>( object _key, CancellationToken _cancellationToken = default ) where T : Object
			{
				if (typeof(T) != typeof(GameObject))
					throw new NotSupportedException();

				string id = NormalizeKey(_key, typeof(T)).Id;
				LoadCount[id] = Loads(id) + 1;

				// Several loads of one id at once would mean the cache does not share them
				var tcs = new TaskCompletionSource<IAssetHandle<GameObject>>();
				m_pending[id] = tcs;
				return (Task<IAssetHandle<T>>)(object)tcs.Task;
			}

			public Task<IInstanceHandle> InstantiateAsync( object _key, Transform _parent = null, CancellationToken _cancellationToken = default ) =>
				throw new NotSupportedException();

			public void Release<T>( IAssetHandle<T> _handle ) where T : Object => _handle?.Release();
			public void Release( IInstanceHandle _handle ) => _handle?.Release();
			public void ReleaseUnused() { }

			public CanonicalAssetKey NormalizeKey<T>( object _key ) where T : Object => NormalizeKey(_key, typeof(T));

			public CanonicalAssetKey NormalizeKey( object _key, Type _type )
			{
				if (_key is CanonicalAssetKey canonical)
					return canonical;
				if (_key is string s)
					return new CanonicalAssetKey(this, s, _type);
				throw new InvalidOperationException("Unsupported key");
			}

			public bool Supports( CanonicalAssetKey _key ) => ReferenceEquals(_key.Provider, this);
			public bool Supports( string _id ) => !string.IsNullOrEmpty(_id) && _id.StartsWith(Prefix, StringComparison.Ordinal);
			public bool Supports( object _obj ) => _obj is CanonicalAssetKey k ? Supports(k) : _obj is string s && Supports(s);
		}

		private readonly List<Object> m_cleanup = new();
		private FakeProvider m_provider;
		private IAssetProvider[] m_savedProviders;
		private Canvas m_canvas;
		private UiIcon3DPreset m_preset;
		private Material m_whiteLit;

		[SetUp]
		public void SetUp()
		{
			try
			{
				m_savedProviders = AssetManager.AssetProviders;
			}
			catch (NotInitializedException)
			{
				m_savedProviders = null;
			}

			m_provider = new FakeProvider();
			AssetManager.OverrideProvidersForTests(m_provider);

			UiIcon3DRenderer.Layer = TestLayer;
			UiIcon3DRenderer.MsaaSamples = 1;

			var canvasGo = Track(new GameObject("Canvas", typeof(Canvas)));
			m_canvas = canvasGo.GetComponent<Canvas>();
			m_canvas.renderMode = RenderMode.ScreenSpaceOverlay;

			m_whiteLit = Track(new Material(Shader.Find("Standard")) { color = Color.white });

			var presetGo = Track(new GameObject("Preset"));
			presetGo.SetActive(false);
			m_preset = presetGo.AddComponent<UiIcon3DPreset>();
			var lightGo = new GameObject("Light");
			lightGo.transform.SetParent(presetGo.transform, false);
			lightGo.AddComponent<Light>().type = LightType.Directional;
		}

		[TearDown]
		public void TearDown()
		{
			foreach (var obj in m_cleanup)
				if (obj != null)
					Object.DestroyImmediate(obj);
			m_cleanup.Clear();

			Icon3DAssetCache.ReleaseAll();
			UiIcon3DRenderer.Shutdown();
			UiIcon3DRenderer.Layer = -1;
			UiIcon3DRenderer.MsaaSamples = 0;
			AssetManager.OverrideProvidersForTests(m_savedProviders);
		}

		[Test]
		public void Loaded_Object_Is_Rendered()
		{
			var icon = CreateIcon(Prefix + "a");
			UiIcon3DRenderer.Flush();

			Assert.IsTrue(icon.IsLoading);
			Assert.IsFalse(icon.IsRendered);
			Assert.AreSame(RenderTextureManager.Placeholder, icon.RawImage.texture);

			m_provider.Complete(Prefix + "a", CreateSphere());
			UiIcon3DRenderer.Flush();

			Assert.IsFalse(icon.IsLoading);
			Assert.IsTrue(icon.IsRendered);
			Assert.AreEqual(1, m_provider.Loads(Prefix + "a"));
		}

		[Test]
		public void Loading_Texture_Is_Shown_While_Loading()
		{
			var loading = Track(new Texture2D(2, 2));
			var icon = CreateIcon(Prefix + "a", _beforeEnable: i => i.LoadingTexture = loading);
			UiIcon3DRenderer.Flush();

			Assert.AreSame(loading, icon.RawImage.texture);

			m_provider.Complete(Prefix + "a", CreateSphere());
			UiIcon3DRenderer.Flush();
			Assert.IsInstanceOf<RenderTexture>(icon.RawImage.texture);
		}

		[Test]
		public void Icons_With_The_Same_Object_Share_One_Load()
		{
			var a = CreateIcon(Prefix + "a");
			var b = CreateIcon(Prefix + "a");
			UiIcon3DRenderer.Flush();
			m_provider.Complete(Prefix + "a", CreateSphere());
			UiIcon3DRenderer.Flush();

			Assert.AreEqual(1, m_provider.Loads(Prefix + "a"));
			Assert.AreEqual(1, Icon3DAssetCache.Count);
			Assert.AreEqual(a.Key, b.Key);

			a.gameObject.SetActive(false);
			Assert.AreEqual(0, m_provider.Releases(Prefix + "a"), "released while another icon still uses it");
			b.gameObject.SetActive(false);
			Assert.AreEqual(1, m_provider.Releases(Prefix + "a"));
			Assert.AreEqual(0, Icon3DAssetCache.Count);
		}

		/// The race the plan warns about: a pooled item is reused for another entry while the first load is pending.
		/// The late result must not end up in the icon, and its handle must still be released.
		[Test]
		public void Late_Load_Does_Not_End_Up_In_A_Reassigned_Icon()
		{
			var icon = CreateIcon(Prefix + "a");
			UiIcon3DRenderer.Flush();

			icon.PrefabId = Prefix + "b";
			UiIcon3DRenderer.Flush();

			var sphereB = CreateSphere();
			m_provider.Complete(Prefix + "b", sphereB);
			UiIcon3DRenderer.Flush();
			string keyB = icon.Key;
			Assert.IsTrue(icon.IsRendered);
			StringAssert.Contains($"/{sphereB.GetInstanceID()}/", keyB);

			// A arrives late
			m_provider.Complete(Prefix + "a", CreateSphere());
			UiIcon3DRenderer.Flush();

			Assert.AreEqual(keyB, icon.Key, "late load replaced the current object");
			Assert.AreEqual(1, m_provider.Releases(Prefix + "a"), "abandoned load was not released");
			Assert.AreEqual(1, UiIcon3DRenderer.RequestCount);
		}

		[Test]
		public void Changing_The_Object_Drops_The_Old_Image_At_Once()
		{
			var icon = CreateIcon(null);
			icon.Prefab = CreateSphere();
			UiIcon3DRenderer.Flush();
			Assert.IsTrue(icon.IsRendered);

			icon.Prefab = CreateSphere();
			UiIcon3DRenderer.Process(0);   // requests the new object, renders nothing

			Assert.AreSame(RenderTextureManager.Placeholder, icon.RawImage.texture, "image of the previous object still shown");
		}

		[Test]
		public void Failed_Load_Leaves_The_Icon_Empty_And_Logs_Once()
		{
			var icon = CreateIcon(Prefix + "broken");
			UiIcon3DRenderer.Flush();

			LogAssert.Expect(LogType.Error, new Regex("could not be loaded"));
			m_provider.Fail(Prefix + "broken");
			UiIcon3DRenderer.Flush();
			UiIcon3DRenderer.Flush();
			UiIcon3DRenderer.Flush();

			Assert.IsTrue(icon.HasLoadFailed);
			Assert.IsFalse(icon.IsRendered);
			Assert.AreSame(RenderTextureManager.Placeholder, icon.RawImage.texture);
			LogAssert.NoUnexpectedReceived();
		}

		[Test]
		public void Disabling_During_Load_Releases_When_The_Load_Completes()
		{
			var icon = CreateIcon(Prefix + "a");
			UiIcon3DRenderer.Flush();

			icon.gameObject.SetActive(false);
			Assert.AreEqual(0, Icon3DAssetCache.Count);

			m_provider.Complete(Prefix + "a", CreateSphere());
			UiIcon3DRenderer.Flush();

			Assert.AreEqual(1, m_provider.Releases(Prefix + "a"));
		}

		[Test]
		public void Visible_Icons_Are_Rendered_First()
		{
			// Created first, so plain age order would render it first
			var offScreen = CreateIcon(null);
			offScreen.Prefab = CreateSphere();
			offScreen.RectTransform.anchoredPosition = new Vector2(100000, 0);

			var onScreen = CreateIcon(null);
			onScreen.Prefab = CreateSphere();

			UiIcon3DRenderer.Process(1);

			Assert.IsTrue(onScreen.IsRendered, "visible icon not rendered first");
			Assert.IsFalse(offScreen.IsRendered);

			UiIcon3DRenderer.Process(1);
			Assert.IsTrue(offScreen.IsRendered, "off screen icon never rendered");
		}

		#region Helpers

		private T Track<T>( T _obj ) where T : Object
		{
			m_cleanup.Add(_obj);
			return _obj;
		}

		private UiIcon3D CreateIcon( string _prefabId, Action<UiIcon3D> _beforeEnable = null )
		{
			var go = Track(new GameObject("Icon", typeof(RectTransform)));
			go.SetActive(false);
			go.transform.SetParent(m_canvas.transform, false);
			((RectTransform)go.transform).sizeDelta = new Vector2(32, 32);
			var icon = go.AddComponent<UiIcon3D>();
			icon.Preset = m_preset;
			if (_prefabId != null)
				icon.PrefabId = _prefabId;
			_beforeEnable?.Invoke(icon);
			go.SetActive(true);
			return icon;
		}

		private GameObject CreateSphere()
		{
			var sphere = Track(GameObject.CreatePrimitive(PrimitiveType.Sphere));
			sphere.SetActive(false);
			sphere.GetComponent<Renderer>().sharedMaterial = m_whiteLit;
			return sphere;
		}

		#endregion
	}
}
