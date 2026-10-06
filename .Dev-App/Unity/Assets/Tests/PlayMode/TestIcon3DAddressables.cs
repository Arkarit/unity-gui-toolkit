using System.Collections;
using System.Text.RegularExpressions;
using GuiToolkit.AssetHandling;
using GuiToolkit.Exceptions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Addressables = UnityEngine.AddressableAssets.Addressables;
using Object = UnityEngine.Object;

namespace GuiToolkit.Test
{
	/// <summary>
	/// 3D icon objects loaded through the real Addressables provider (TestIcon3DLoading uses a fake one):
	/// the lease/cache lifecycle against genuine async handles, release, and an icon end to end.
	/// The objects GT_TestAddrIcon3D_0..2 are created by Icon3DAddressablesTestAsset; the tests are ignored without them.
	/// </summary>
	public class TestIcon3DAddressables
	{
		private const int TestLayer = 31;
		private const int Count = 3;
		private static string Id( int _i ) => $"addr:GT_TestAddrIcon3D_{_i}";

		private IAssetProvider[] m_savedProviders;
		private GameObject m_root;

		[UnitySetUp]
		public IEnumerator SetUp()
		{
			var locations = Addressables.LoadResourceLocationsAsync("GT_TestAddrIcon3D_0");
			yield return locations;
			bool found = locations.IsValid() && locations.Result != null && locations.Result.Count > 0;
			if (locations.IsValid())
				Addressables.Release(locations);

			if (!found)
				Assert.Ignore("Addressable 'GT_TestAddrIcon3D_0' not found. Open the project in the editor once so Icon3DAddressablesTestAsset creates it.");

			try
			{
				m_savedProviders = AssetManager.AssetProviders;
			}
			catch (NotInitializedException)
			{
				m_savedProviders = null;
			}

			var provider = new AddressablesAssetProvider();
			provider.Init();
			while (!provider.IsInitialized)
				yield return null;

			AssetManager.OverrideProvidersForTests(provider);

			UiIcon3DRenderer.Layer = TestLayer;
			UiIcon3DRenderer.MsaaSamples = 1;

			m_root = new GameObject("Root", typeof(Canvas));
			m_root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
		}

		[TearDown]
		public void TearDown()
		{
			if (m_root != null)
				Object.DestroyImmediate(m_root);

			Icon3DAssetCache.ReleaseAll();
			UiIcon3DRenderer.Shutdown();
			UiIcon3DRenderer.Layer = -1;
			UiIcon3DRenderer.MsaaSamples = 0;
			if (m_savedProviders != null)
				AssetManager.OverrideProvidersForTests(m_savedProviders);
		}

		private static IEnumerator WaitLoaded( Icon3DAssetLease _lease, float _timeout = 10f )
		{
			float end = Time.realtimeSinceStartup + _timeout;
			while (!_lease.IsLoaded && !_lease.HasFailed && Time.realtimeSinceStartup < end)
				yield return null;
		}

		private static IEnumerator Settle( int _frames = 30 )
		{
			for (int i = 0; i < _frames; i++)
			{
				Icon3DAssetCache.Update();
				yield return null;
			}
		}

		[UnityTest]
		public IEnumerator Cache_Loads_Prefab_Through_Addressables()
		{
			var lease = Icon3DAssetCache.Acquire(Id(0));
			yield return WaitLoaded(lease);

			Assert.IsTrue(lease.IsLoaded, "load did not complete");
			Assert.IsFalse(lease.HasFailed);
			Assert.IsNotNull(lease.Asset);
			Assert.IsNotNull(lease.Asset.GetComponentInChildren<Renderer>());

			lease.Release();
			Assert.AreEqual(0, Icon3DAssetCache.Count);
		}

		[UnityTest]
		public IEnumerator Two_Leases_Share_One_Load()
		{
			var a = Icon3DAssetCache.Acquire(Id(0));
			var b = Icon3DAssetCache.Acquire(Id(0));
			Assert.AreEqual(1, Icon3DAssetCache.Count);

			yield return WaitLoaded(a);
			Assert.AreSame(a.Asset, b.Asset);

			a.Release();
			Assert.IsNotNull(b.Asset, "released while another lease still uses it");
			b.Release();
			Assert.AreEqual(0, Icon3DAssetCache.Count);
		}

		[UnityTest]
		public IEnumerator Release_While_Loading_Is_Cleaned_Up_And_Id_Loads_Again()
		{
			var lease = Icon3DAssetCache.Acquire(Id(1));
			lease.Release();   // before the async load could complete

			yield return Settle();
			Assert.AreEqual(0, Icon3DAssetCache.Count);
			LogAssert.NoUnexpectedReceived();

			var again = Icon3DAssetCache.Acquire(Id(1));
			yield return WaitLoaded(again);
			Assert.IsTrue(again.IsLoaded);
			again.Release();
		}

		[UnityTest]
		public IEnumerator Rapid_Reassignment_Keeps_Only_The_Last_Object()
		{
			var leases = new Icon3DAssetLease[Count];
			for (int i = 0; i < Count; i++)
				leases[i] = Icon3DAssetCache.Acquire(Id(i));

			// rapid scroll: give up all but the last straight away
			for (int i = 0; i < Count - 1; i++)
				leases[i].Release();

			yield return WaitLoaded(leases[Count - 1]);
			Assert.IsTrue(leases[Count - 1].IsLoaded);

			yield return Settle();
			Assert.AreEqual(1, Icon3DAssetCache.Count);
			leases[Count - 1].Release();
			Assert.AreEqual(0, Icon3DAssetCache.Count);
		}

		[UnityTest]
		public IEnumerator Missing_Key_Fails_Without_Throwing()
		{
			// Addressables logs the invalid key itself, then the cache reports the failed load once
			LogAssert.Expect(LogType.Error, new Regex("No Location found for Key=GT_DoesNotExist"));
			LogAssert.Expect(LogType.Error, new Regex("could not be loaded"));
			var lease = Icon3DAssetCache.Acquire("addr:GT_DoesNotExist");
			yield return WaitLoaded(lease);

			Assert.IsTrue(lease.HasFailed);
			Assert.IsNull(lease.Asset);
			lease.Release();
		}

		[UnityTest]
		public IEnumerator Icon_Renders_Object_Loaded_By_Addressables()
		{
			var presetGo = new GameObject("Preset");
			presetGo.transform.SetParent(m_root.transform, false);
			presetGo.SetActive(false);
			var preset = presetGo.AddComponent<UiIcon3DPreset>();
			var lightGo = new GameObject("Light");
			lightGo.transform.SetParent(presetGo.transform, false);
			lightGo.AddComponent<Light>().type = LightType.Directional;

			var go = new GameObject("Icon", typeof(RectTransform));
			go.SetActive(false);
			go.transform.SetParent(m_root.transform, false);
			((RectTransform) go.transform).sizeDelta = new Vector2(32, 32);
			var icon = go.AddComponent<UiIcon3D>();
			icon.Preset = preset;
			icon.PrefabId = Id(0);
			go.SetActive(true);

			float end = Time.realtimeSinceStartup + 10f;
			while (!icon.IsRendered && !icon.HasLoadFailed && Time.realtimeSinceStartup < end)
			{
				UiIcon3DRenderer.Flush();
				yield return null;
			}

			Assert.IsFalse(icon.HasLoadFailed, "load failed");
			Assert.IsTrue(icon.IsRendered, "never rendered");
			Assert.IsInstanceOf<RenderTexture>(icon.RawImage.texture);

			go.SetActive(false);
			Assert.AreEqual(0, Icon3DAssetCache.Count);
		}
	}
}
