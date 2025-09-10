using System.Collections;
using System.Threading;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using GuiToolkit.AssetHandling;
using Addressables = UnityEngine.AddressableAssets.Addressables;
using UnityEngine.ResourceManagement.AsyncOperations;

public class TestAddressablesProvider
{
	private const string kAddrKey = "GT_TestAddrPrefab"; // provide this Addressable in your project
	private AddressablesProvider _provider;

	[UnitySetUp]
	public IEnumerator Setup()
	{
		_provider = new AddressablesProvider();
		// Quick existence check via locations
		var locHandle = Addressables.LoadResourceLocationsAsync(kAddrKey);
		yield return locHandle;

		if (!locHandle.IsValid() || locHandle.Result == null || locHandle.Result.Count == 0)
		{
			if (locHandle.IsValid()) Addressables.Release(locHandle);
			Assert.Ignore($"Addressables key '{kAddrKey}' not found. Create an Addressable prefab with this key to run tests.");
		}

		Addressables.Release(locHandle);
	}

	[UnityTest]
	public IEnumerator LoadAssetAsync_WithStringKey_Succeeds_And_Release()
	{
		var done = false;
		IAssetHandle<GameObject> handle = null;

		var task = _provider.LoadAssetAsync<GameObject>(kAddrKey, CancellationToken.None)
			.ContinueWith(t =>
			{
				if (t.Exception != null) throw t.Exception;
				handle = t.Result;
				done = true;
			});

		while (!done) yield return null;

		Assert.NotNull(handle, "Handle is null");
		Assert.IsTrue(handle.IsLoaded, "Handle.IsLoaded false");
		Assert.NotNull(handle.Asset, "Asset is null");

		// Release should drop refcount without throwing
		_provider.Release(handle);
		// No assert for Result after release (not guaranteed); just ensure no exceptions
	}

	[UnityTest]
	public IEnumerator InstantiateAsync_WithStringKey_CreatesAndReleasesInstance()
	{
		var done = false;
		IInstanceHandle inst = null;

		var task = _provider.InstantiateAsync(kAddrKey, null, CancellationToken.None)
			.ContinueWith(t =>
			{
				if (t.Exception != null) throw t.Exception;
				inst = t.Result;
				done = true;
			});

		while (!done) yield return null;

		Assert.NotNull(inst);
		Assert.NotNull(inst.Instance);
		Assert.IsTrue(inst.Instance, "Instance should be alive");

		var go = inst.Instance;
		_provider.Release(inst);

		// Give a frame to allow destruction to settle
		yield return null;

		Assert.IsFalse(go, "Instance should be destroyed after Release()");
	}

	[UnityTest]
	public IEnumerator NormalizeKey_AssetReference_ProducesAddrKey_And_Loads()
	{
		// Load locations to get a runtime key, then create an AssetReference from the first location's PrimaryKey (string).
		var locsH = Addressables.LoadResourceLocationsAsync(kAddrKey);
		yield return locsH;

		if (!locsH.IsValid() || locsH.Result == null || locsH.Result.Count == 0)
		{
			if (locsH.IsValid()) Addressables.Release(locsH);
			Assert.Ignore($"Addressables key '{kAddrKey}' not found.");
		}

		var ar = new UnityEngine.AddressableAssets.AssetReference(kAddrKey);

		var canonical = _provider.NormalizeKey<GameObject>(ar);
		Assert.IsTrue(canonical.Id.StartsWith("addr:"), "Expected 'addr:' prefix");

		Addressables.Release(locsH);

		// Use the canonical key to load
		var done = false;
		IAssetHandle<GameObject> handle = null;

		var task = _provider.LoadAssetAsync<GameObject>(canonical, CancellationToken.None)
			.ContinueWith(t =>
			{
				if (t.Exception != null) throw t.Exception;
				handle = t.Result;
				done = true;
			});

		while (!done) yield return null;

		Assert.NotNull(handle);
		Assert.IsTrue(handle.IsLoaded);
		Assert.NotNull(handle.Asset);

		_provider.Release(handle);
	}
}
