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
	public class AssetManagerTests
	{
		private sealed class FakeAssetHandle<T> : IAssetHandle<T> where T : UnityEngine.Object
		{
			public CanonicalAssetKey Key { get; }
			public T Asset { get; private set; }
			public bool IsReleased { get; private set; }
			public bool IsLoaded => !IsReleased && Asset != null;

			public FakeAssetHandle( CanonicalAssetKey _key, T _asset )
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

			public FakeInstanceHandle( CanonicalAssetKey _key, GameObject _go )
			{
				Key = _key;
				Instance = _go;
			}

			public void Release()
			{
				if (Instance != null)
					UnityEngine.Object.Destroy(Instance);

				Instance = null;
				IsReleased = true;
			}
		}

		private sealed class FakeProvider : IAssetProvider
		{
			public int ReleaseUnusedCalls;

			public string Name => "Fake Provider";
			public string ResName => "fake";
			public IAssetProviderEditorBridge EditorBridge => null;

			public bool Supports( CanonicalAssetKey _key )
			{
				return ReferenceEquals(_key.Provider, this);
			}

			public bool Supports( string _id )
			{
				return !string.IsNullOrEmpty(_id) && _id.StartsWith("fake:", StringComparison.Ordinal);
			}

			public bool Supports( object _obj )
			{
				if (_obj is CanonicalAssetKey ck)
					return Supports(ck);

				if (_obj is string s)
					return Supports(s);

#if UNITY_EDITOR
				if (_obj is UnityEngine.Object)
					return false; // Not supported by this fake
#endif
				return false;
			}

			public CanonicalAssetKey NormalizeKey<T>( object _key ) where T : UnityEngine.Object
			{
				if (_key is CanonicalAssetKey ck)
				{
					if (!ReferenceEquals(ck.Provider, this))
						throw new InvalidOperationException("Key belongs to a different provider.");
					return ck;
				}

				if (_key is string s)
				{
					string id = s.StartsWith("fake:", StringComparison.Ordinal) ? s : "fake:" + s;
					return new CanonicalAssetKey(this, id, typeof(T));
				}

				// Keep it strict in tests
				throw new InvalidOperationException("Unsupported key type for FakeProvider.");
			}

			public Task<IAssetHandle<T>> LoadAssetAsync<T>( object _key, CancellationToken _ct ) where T : UnityEngine.Object
			{
				_ct.ThrowIfCancellationRequested();

				CanonicalAssetKey ck = NormalizeKey<T>(_key);

				T asset;

				if (typeof(T) == typeof(GameObject))
				{
					asset = new GameObject("FakeLoaded").gameObject as T;
				}
				else if (typeof(T) == typeof(Transform))
				{
					var go = new GameObject("FakeLoaded");
					asset = (T)(UnityEngine.Object)go.transform;
				}
				else if (typeof(Component).IsAssignableFrom(typeof(T)))
				{
					var go = new GameObject("FakeLoaded");
					asset = go.AddComponent(typeof(T)) as T;
				}
				else if (typeof(ScriptableObject).IsAssignableFrom(typeof(T)))
				{
					asset = ScriptableObject.CreateInstance(typeof(T)) as T;
				}
				else
				{
					throw new Exception("Unsupported T in FakeProvider.");
				}

				IAssetHandle<T> handle = new FakeAssetHandle<T>(ck, asset);
				return Task.FromResult(handle);
			}

			public Task<IInstanceHandle> InstantiateAsync( object _key, Transform _parent, CancellationToken _ct )
			{
				_ct.ThrowIfCancellationRequested();

				CanonicalAssetKey ck = NormalizeKey<GameObject>(_key);

				GameObject go = new GameObject("FakeInstance");
				if (_parent != null)
					go.transform.SetParent(_parent, false);

				IInstanceHandle handle = new FakeInstanceHandle(ck, go);
				return Task.FromResult(handle);
			}

			public void Release<T>( IAssetHandle<T> _handle ) where T : UnityEngine.Object
			{
				_handle?.Release();
			}

			public void Release( IInstanceHandle _handle )
			{
				_handle?.Release();
			}

			public void ReleaseUnused()
			{
				ReleaseUnusedCalls++;
			}
		}


		private FakeProvider m_provider;
		private CanonicalAssetKey m_keyGO;

		[UnitySetUp]
		public IEnumerator SetUp()
		{
			m_provider = new FakeProvider();

			// Inject provider into AssetManager (skips real Init).
			AssetManager.OverrideProvidersForTests(m_provider);

			// Build a canonical key that points at our provider.
			m_keyGO = new CanonicalAssetKey(m_provider, "fake-id", typeof(GameObject));

			yield return null;
		}

		[UnityTest]
		public IEnumerator LoadAssetAsync_Callbacks_Fires_OnSuccess()
		{
			TaskCompletionSource<GameObject> tcs = new TaskCompletionSource<GameObject>();

			AssetManager.LoadAssetAsync<GameObject>(
				m_keyGO,
				_onSuccess: go => tcs.TrySetResult(go),
				_onFail: _ => tcs.TrySetResult(null)
			);

			while (!tcs.Task.IsCompleted)
				yield return null;

			GameObject loaded = tcs.Task.Result;
			Assert.IsNotNull(loaded);
			Assert.AreEqual("FakeLoaded", loaded.name);

			// Release should call handle.Release (tracked internally).
			Assert.DoesNotThrow(() => AssetManager.Release(m_keyGO));

			// Destroy created object as a courtesy (fake handle already detached).
			if (loaded != null)
			{
				UnityEngine.Object.Destroy(loaded);
				yield return null;
			}
		}

		[UnityTest]
		public IEnumerator InstantiateAsync_Generics_Component_Returns_Component()
		{
			TaskCompletionSource<Transform> tcs = new TaskCompletionSource<Transform>();

			AssetManager.InstantiateAsync<Transform>(
				m_keyGO,
				_onSuccess: tr => tcs.TrySetResult(tr),
				_onFail: _ => tcs.TrySetResult(null)
			);

			while (!tcs.Task.IsCompleted)
				yield return null;

			Transform tr = tcs.Task.Result;
			Assert.IsNotNull(tr);
			Assert.IsNotNull(tr.gameObject);
			Assert.AreEqual("FakeInstance", tr.gameObject.name);

			// Release destroys the instance.
			AssetManager.Release(m_keyGO);
			yield return null;
			Assert.IsTrue(tr == null || tr.gameObject == null);
		}

		[UnityTest]
		public IEnumerator InstantiateAsync_Cancellation_Stops_Work()
		{
			var cts = new System.Threading.CancellationTokenSource();
			cts.Cancel();

			bool success = false, fail = false;
			AssetManager.InstantiateAsync<GameObject>(
				m_keyGO,
				_onSuccess: _ => success = true,
				_onFail: _ => fail = true,
				_cancellationToken: cts.Token);

			yield return null;
			Assert.IsFalse(success);
			Assert.IsTrue(fail);
		}

		[Test]
		public void ReleaseUnused_Forwards_To_Provider()
		{
			int before = m_provider.ReleaseUnusedCalls;
			AssetManager.ReleaseUnused();
			int after = m_provider.ReleaseUnusedCalls;
			Assert.Greater(after, before);
		}

		[Test]
		public void Release_On_Unknown_Key_Is_NoThrow()
		{
			CanonicalAssetKey another = new CanonicalAssetKey(m_provider, "other", typeof(GameObject));
			Assert.DoesNotThrow(() => AssetManager.Release(another));
		}

		[Test]
		public void Release_Twice_NoThrow()
		{
			Assert.DoesNotThrow(() => AssetManager.Release(m_keyGO));
			Assert.DoesNotThrow(() => AssetManager.Release(m_keyGO));
		}

	}
}