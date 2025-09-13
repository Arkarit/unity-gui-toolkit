using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using GuiToolkit.AssetHandling;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GuiToolkit.Test
{
	[EditorAware]
	public class TestUiPool_Canonical
	{
		private UiPool m_pool;
		private CanonicalAssetKey m_key;
		private FakeProvider m_provider;

		// Test component to verify IPoolable callbacks via GetAsync<T>
		private sealed class TestPoolable : MonoBehaviour, IPoolable
		{
			public int Created;
			public int Released;
			public void OnPoolCreated() { Created++; }
			public void OnPoolReleased() { Released++; }
		}

		#region Minimal fake provider with handles

		private sealed class FakeAssetHandle<T> : IAssetHandle<T> where T : UnityEngine.Object
		{
			public CanonicalAssetKey Key { get; }
			public T Asset { get; private set; }
			public bool IsLoaded => !IsReleased && Asset != null;
			public bool IsReleased { get; private set; }

			public FakeAssetHandle(CanonicalAssetKey _key, T _asset)
			{
				Key = _key;
				Asset = _asset;
			}

			public void Release()
			{
				IsReleased = true;
				Asset = null;
			}
		}

		private sealed class FakeInstanceHandle : IInstanceHandle
		{
			public CanonicalAssetKey Key { get; }
			public GameObject Instance { get; private set; }
			public bool IsReleased { get; private set; }

			public FakeInstanceHandle(CanonicalAssetKey _key, GameObject _go)
			{
				Key = _key;
				Instance = _go;
			}

			public void Release()
			{
#if UNITY_EDITOR
				if (Application.isPlaying) UnityEngine.Object.Destroy(Instance);
				else UnityEngine.Object.DestroyImmediate(Instance);
#else
				UnityEngine.Object.Destroy(Instance);
#endif
				Instance = null;
				IsReleased = true;
			}
		}

		private sealed class FakeProvider : IAssetProvider
		{
			private readonly Type m_alwaysAddComponentType;

			public FakeProvider(Type _alwaysAdd = null) { m_alwaysAddComponentType = _alwaysAdd; }

			public string Name => "Fake Provider";
			public string ResName => "fake";
			public IAssetProviderEditorBridge EditorBridge => null;

			public bool Supports(CanonicalAssetKey _key) => ReferenceEquals(_key.Provider, this);
			public bool Supports(string _id) => !string.IsNullOrEmpty(_id) && _id.StartsWith("fake:", StringComparison.Ordinal);
			public bool Supports(object _obj)
			{
				if (_obj is CanonicalAssetKey ck) return Supports(ck);
				if (_obj is string s) return Supports(s);
				return false;
			}

			public CanonicalAssetKey NormalizeKey<T>(object _key) where T : UnityEngine.Object =>
				NormalizeKey(_key, typeof(T));
			public CanonicalAssetKey NormalizeKey( object _key, Type _type)
			{
				if (_key is CanonicalAssetKey ck)
				{
					if (!ReferenceEquals(ck.Provider, this))
						throw new InvalidOperationException("Key belongs to different provider.");
					return ck;
				}

				if (_key is string s)
					return new CanonicalAssetKey(this, s.StartsWith("fake:", StringComparison.Ordinal) ? s : "fake:" + s, _type);

				throw new InvalidOperationException("Unsupported key type for FakeProvider.");
			}

			public Task<IAssetHandle<T>> LoadAssetAsync<T>(object _key, CancellationToken _ct) where T : UnityEngine.Object
			{
				_ct.ThrowIfCancellationRequested();
				CanonicalAssetKey ck = NormalizeKey<T>(_key);

				T asset;
				if (typeof(T) == typeof(GameObject))
				{
					GameObject go = new GameObject("FakeLoaded");
					if (m_alwaysAddComponentType != null) { go.AddComponent(m_alwaysAddComponentType); }
					asset = go as T;
				}
				else if (typeof(T) == typeof(Transform))
				{
					GameObject go = new GameObject("FakeLoaded");
					asset = (T)(UnityEngine.Object)go.transform;
				}
				else if (typeof(Component).IsAssignableFrom(typeof(T)))
				{
					GameObject go = new GameObject("FakeLoaded");
					go.AddComponent(m_alwaysAddComponentType ?? typeof(Transform));
					asset = go.GetComponent<T>();
				}
				else if (typeof(ScriptableObject).IsAssignableFrom(typeof(T)))
				{
					asset = ScriptableObject.CreateInstance(typeof(T)) as T;
				}
				else
				{
					throw new Exception("Unsupported T in FakeProvider.");
				}

				return Task.FromResult<IAssetHandle<T>>(new FakeAssetHandle<T>(ck, asset));
			}

			public Task<IInstanceHandle> InstantiateAsync(object _key, Transform _parent, CancellationToken _ct)
			{
				_ct.ThrowIfCancellationRequested();
				CanonicalAssetKey ck = NormalizeKey<GameObject>(_key);

				GameObject go = new GameObject("FakeInstance");
				if (m_alwaysAddComponentType != null) { go.AddComponent(m_alwaysAddComponentType); }
				if (_parent != null) { go.transform.SetParent(_parent, false); }

				return Task.FromResult<IInstanceHandle>(new FakeInstanceHandle(ck, go));
			}

			public void Release<T>(IAssetHandle<T> _handle) where T : UnityEngine.Object => _handle?.Release();
			public void Release(IInstanceHandle _handle) => _handle?.Release();
			public void ReleaseUnused() { }
		}

#if UNITY_INCLUDE_TESTS
		// Test hook from earlier step; keep here for safety if not already added.
		private static void OverrideProviders(params IAssetProvider[] _providers)
		{
			AssetManager.OverrideProvidersForTests(_providers);
		}
#endif

		#endregion

		[UnitySetUp]
		public IEnumerator SetUp()
		{
			var poolGO = new GameObject("UiPool");
			m_pool = poolGO.AddComponent<UiPool>();
			m_pool.m_poolContainer = new GameObject("PoolContainer").transform;

			// Provider inject (adds TestPoolable to instances so GetAsync<TestPoolable> succeeds)
			m_provider = new FakeProvider(typeof(TestPoolable));
#if UNITY_INCLUDE_TESTS
			OverrideProviders(m_provider);
#endif
			m_key = new CanonicalAssetKey(m_provider, "fake:ui-pool-canon", typeof(GameObject));

			yield return null;
		}

		[TearDown]
		public void TearDown()
		{
			if (m_pool != null) 
				UnityEngine.Object.DestroyImmediate(m_pool.gameObject);
		}

		[UnityTest]
		public IEnumerator GetAsync_GameObject_Reuses_Instance()
		{
			Task<GameObject> t1 = m_pool.GetAsync(m_key);
			while (!t1.IsCompleted) 
				yield return null;

			GameObject a = t1.Result;
			Assert.IsNotNull(a);
			m_pool.Release(a);

			Task<GameObject> t2 = m_pool.GetAsync(m_key);
			while (!t2.IsCompleted) 
				yield return null;

			GameObject b = t2.Result;
			Assert.AreSame(a, b);
		}

		[UnityTest]
		public IEnumerator GetAsync_Component_Triggers_IPoolable()
		{
			Task<TestPoolable> t = m_pool.GetAsync<TestPoolable>(m_key);
			while (!t.IsCompleted) 
				yield return null;

			TestPoolable comp = t.Result;
			Assert.IsNotNull(comp);
			Assert.AreEqual(1, comp.Created);

			m_pool.Release(comp);
			Assert.AreEqual(1, comp.Released);

			// Reuse increases Created again
			Task<TestPoolable> t2 = m_pool.GetAsync<TestPoolable>(m_key);
			while (!t2.IsCompleted) 
				yield return null;
			
			Assert.AreEqual(2, t2.Result.Created);
		}

		[UnityTest]
		public IEnumerator Clear_Destroys_Pooled_Canonical_Instances()
		{
			Task<GameObject> t1 = m_pool.GetAsync(m_key);
			while (!t1.IsCompleted) 
				yield return null;

			GameObject go = t1.Result;
			Assert.IsNotNull(go);

			m_pool.Release(go);
			m_pool.Clear();
			
			yield return null; // destruction finalizes

			Assert.IsTrue(go == null || go.Equals(null));
		}

		[UnityTest]
		public IEnumerator Cancellation_Propagates_As_Faulted_Task()
		{
			CancellationTokenSource cts = new CancellationTokenSource();
			cts.Cancel();

			Task<GameObject> t = m_pool.GetAsync(m_key, cts.Token);
			// Task may complete same frame
			while (!t.IsCompleted) yield return null;

			Assert.IsTrue(t.IsFaulted || t.IsCanceled);
		}
	}
}
