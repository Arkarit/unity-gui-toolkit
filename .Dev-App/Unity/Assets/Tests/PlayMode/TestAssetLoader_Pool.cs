using System;
using System.Collections;
using System.Threading;
using GuiToolkit.AssetHandling;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace GuiToolkit.Test
{
	public class TestAssetLoader_Pool
	{
		private sealed class TestPanel : UiPanel
		{
			public override bool Poolable => true;
		}

		private sealed class FakeProvider : IAssetProvider
		{
			public string Name => "Fake";
			public string ResName => "fake";
			public string Prefix => "fake:";
			public IAssetProviderEditorBridge EditorBridge => null;

			public bool IsInitialized => true;
			public void Init() { }
			public bool Supports( CanonicalAssetKey _key ) => true;
			public bool Supports( string _id ) => true;
			public bool Supports( object _obj ) => true;

			public CanonicalAssetKey NormalizeKey<T>( object _key ) where T : UnityEngine.Object => NormalizeKey(_key, typeof(T));
			public CanonicalAssetKey NormalizeKey( object _key, Type _type )
			{
				if (_key is CanonicalAssetKey ck)
					return ck;

				return new CanonicalAssetKey(this, "fake:test", _type);
			}

			public System.Threading.Tasks.Task<IAssetHandle<T>> LoadAssetAsync<T>( object _key, CancellationToken _ct )
				where T : UnityEngine.Object
			{
				throw new System.NotImplementedException();
			}

			public System.Threading.Tasks.Task<IInstanceHandle> InstantiateAsync(
				object _key, Transform _parent, CancellationToken _ct )
			{
				_ct.ThrowIfCancellationRequested();

				var go = new GameObject("PoolInstance");
				go.AddComponent<TestPanel>();
				if (_parent != null) go.transform.SetParent(_parent, false);

				return System.Threading.Tasks.Task.FromResult<IInstanceHandle>(new FakeInstanceHandle((CanonicalAssetKey)_key, go));
			}

			public void Release<T>( IAssetHandle<T> _handle ) where T : UnityEngine.Object => _handle?.Release();
			public void Release( IInstanceHandle _handle ) => _handle?.Release();
			public void ReleaseUnused() { }
		}

		private sealed class FakeInstanceHandle : IInstanceHandle
		{
			public CanonicalAssetKey Key { get; }
			public GameObject Instance { get; private set; }
			public bool IsReleased { get; private set; }

			public FakeInstanceHandle( CanonicalAssetKey key, GameObject go )
			{
				Key = key;
				Instance = go;
			}

			public void Release()
			{
#if UNITY_EDITOR
				if (Application.isPlaying) Object.Destroy(Instance);
				else Object.DestroyImmediate(Instance);
#else
            Object.Destroy(Instance);
#endif
				Instance = null;
				IsReleased = true;
			}
		}

		private UiPool m_pool;
		private FakeProvider m_provider;

		[UnitySetUp]
		public IEnumerator SetUp()
		{
			var poolGO = new GameObject("UiPool");
			m_pool = poolGO.AddComponent<UiPool>();
			m_pool.m_poolContainer = new GameObject("PoolContainer").transform;

#if UNITY_INCLUDE_TESTS
			AssetManager.OverrideProvidersForTests();
			AssetManager.OverrideProvidersForTests(new FakeProvider());
#endif
			m_provider = new FakeProvider();
#if UNITY_INCLUDE_TESTS
			AssetManager.OverrideProvidersForTests(m_provider);
#endif
			yield return null;
		}

		[TearDown]
		public void TearDown()
		{
			if (m_pool != null)
				Object.DestroyImmediate(m_pool.gameObject);
		}

		[UnityTest]
		public IEnumerator LoadAsync_UsesPool_Success_And_Reuse()
		{
			UiPanel first = null;

			var info = new UiPanelLoadInfo
			{
				PanelType = typeof(TestPanel),
				AssetProvider = m_provider,
				InstantiationType = UiPanelLoadInfo.EInstantiationType.Pool,
				OnSuccess = p => first = p
			};

			AssetLoader.Instance.LoadAsync(info);

			yield return null;

			Assert.IsNotNull(first);
			Assert.IsInstanceOf<TestPanel>(first);

			UiPool.Instance.Release(first);
			yield return null;

			UiPanel second = null;
			var info2 = new UiPanelLoadInfo
			{
				PanelType = typeof(TestPanel),
				AssetProvider = m_provider,
				InstantiationType = UiPanelLoadInfo.EInstantiationType.Pool,
				OnSuccess = p => second = p
			};

			AssetLoader.Instance.LoadAsync(info2);
			yield return null;

			Assert.IsNotNull(second);
			Assert.AreSame(first.gameObject, second.gameObject);
		}

		[UnityTest]
		public IEnumerator LoadAsync_Pool_Fails_When_Component_Missing_Calls_OnFail()
		{
			var badProvider = new FakeProviderWithoutPanel();
#if UNITY_INCLUDE_TESTS
			AssetManager.OverrideProvidersForTests(badProvider);
#endif

			UiPanelLoadInfo received = null;
			System.Exception receivedEx = null;

			var info = new UiPanelLoadInfo
			{
				PanelType = typeof(TestPanel),
				AssetProvider = badProvider,
				InstantiationType = UiPanelLoadInfo.EInstantiationType.Pool,
				OnFail = ( li, ex ) => { received = li; receivedEx = ex; }
			};

			AssetLoader.Instance.LoadAsync(info);
			yield return null;

			Assert.IsNotNull(received);
			Assert.IsNotNull(receivedEx);
		}

		private sealed class FakeProviderWithoutPanel : IAssetProvider
		{
			public string Name => "FakeNoPanel";
			public string ResName => "fake";
			public string Prefix => "fake:";
			public IAssetProviderEditorBridge EditorBridge => null;

			public bool IsInitialized => true;
			public void Init() { }
			public bool Supports( CanonicalAssetKey _key ) => true;
			public bool Supports( string _id ) => true;
			public bool Supports( object _obj ) => true;

			public CanonicalAssetKey NormalizeKey( object _key, Type _ )
				=> (CanonicalAssetKey)_key;
			public CanonicalAssetKey NormalizeKey<T>( object _key ) where T : Object
				=> (CanonicalAssetKey)_key;

			public System.Threading.Tasks.Task<IAssetHandle<T>> LoadAssetAsync<T>( object _key, CancellationToken _ct )
				where T : UnityEngine.Object
				=> throw new System.NotImplementedException();

			public System.Threading.Tasks.Task<IInstanceHandle> InstantiateAsync( object _key, Transform _parent, CancellationToken _ct )
			{
				_ct.ThrowIfCancellationRequested();
				var go = new GameObject("PoolInstance_NoPanel");
				if (_parent != null) go.transform.SetParent(_parent, false);
				return System.Threading.Tasks.Task.FromResult<IInstanceHandle>(
					new FakeInstanceHandle(NormalizeKey<GameObject>(_key), go));
			}

			public void Release<T>( IAssetHandle<T> _handle ) where T : UnityEngine.Object => _handle?.Release();
			public void Release( IInstanceHandle _handle ) => _handle?.Release();
			public void ReleaseUnused() { }
		}
	}
}