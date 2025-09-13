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
	public class TestAssetLoader
	{
		private class TestPanel : UiPanel { }

		private sealed class FakeProvider : IAssetProvider
		{
			public string Name => "Fake";
			public string ResName => "fake";
			public IAssetProviderEditorBridge EditorBridge => null;

			public bool Supports( CanonicalAssetKey _key ) => true;
			public bool Supports( string _id ) => true;
			public bool Supports( object _obj ) => true;

			public CanonicalAssetKey NormalizeKey(object _key, Type _type) => (CanonicalAssetKey) _key;
			public CanonicalAssetKey NormalizeKey<T>( object _key ) where T : UnityEngine.Object => (CanonicalAssetKey) _key;

			public Task<IAssetHandle<T>> LoadAssetAsync<T>( object _key, CancellationToken _ct ) where T : UnityEngine.Object
			{
				throw new NotImplementedException();
			}

			public Task<IInstanceHandle> InstantiateAsync( object _key, Transform _parent, CancellationToken _ct )
			{
				_ct.ThrowIfCancellationRequested();
				var go = new GameObject("FakeInstance");
				go.AddComponent<TestPanel>();
				return Task.FromResult<IInstanceHandle>(new FakeInstanceHandle((CanonicalAssetKey)_key, go));
			}

			public void Release<T>( IAssetHandle<T> _handle ) where T : UnityEngine.Object => _handle.Release();
			public void Release( IInstanceHandle _handle ) => _handle.Release();
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
				UnityEngine.Object.DestroyImmediate(Instance);
				Instance = null;
				IsReleased = true;
			}
		}

		private CanonicalAssetKey m_key;
		private FakeProvider m_provider;

		[SetUp]
		public void Setup()
		{
			m_provider = new FakeProvider();
			AssetManager.OverrideProvidersForTests(m_provider);
			m_key = new CanonicalAssetKey(m_provider, typeof(TestPanel));
		}

		[UnityTest]
		public IEnumerator LoadAsync_DirectInstantiate_Success()
		{
			UiPanel result = null;
			var loadInfo = new UiPanelLoadInfo
			{
				PanelType = typeof(TestPanel),
				AssetProvider = m_provider,
				InstantiationType = UiPanelLoadInfo.EInstantiationType.Instantiate,
				OnSuccess = p => result = p
			};

			AssetLoader.Instance.LoadAsync(loadInfo);

			// Warten bis async fertig
			yield return null;
			Assert.IsNotNull(result);
			Assert.IsInstanceOf<TestPanel>(result);
		}

		[UnityTest]
		public IEnumerator LoadAsync_Cancelled_OnFail()
		{
			UiPanelLoadInfo info = new UiPanelLoadInfo
			{
				PanelType = typeof(TestPanel),
				AssetProvider = m_provider,
				InstantiationType = UiPanelLoadInfo.EInstantiationType.Instantiate,
				OnFail = ( li, ex ) => Assert.IsInstanceOf<OperationCanceledException>(ex)
			};

			var cts = new CancellationTokenSource();
			cts.Cancel();

			AssetLoader.Instance.LoadAsync(info);
			yield return null;
		}
	}
}