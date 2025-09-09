// Assets/Tests/Editor/TestDefaultAssetProvider.cs
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using GuiToolkit.AssetHandling;

namespace GuiToolkit.Test
{
	public class TestDefaultAssetProvider
	{
		private const string kTempRoot = "Assets/~Temp_GT_Tests";
		private const string kResFolder = kTempRoot + "/Resources";
		private const string kPrefabName = "GT_TestPrefab_DefaultProvider";
		private const string kPrefabPath = kResFolder + "/" + kPrefabName + ".prefab";

		private bool _createdTempRoot;
		private bool _createdResFolder;
		private bool _createdPrefab;
		private DefaultAssetProvider _provider;


		[OneTimeSetUp]
		public void OneTimeSetUp()
		{
			if (!AssetDatabase.IsValidFolder(kTempRoot))
			{
				AssetDatabase.CreateFolder("Assets", "~Temp_GT_Tests");
				_createdTempRoot = true;
			}
			if (!AssetDatabase.IsValidFolder(kResFolder))
			{
				AssetDatabase.CreateFolder(kTempRoot, "Resources");
				_createdResFolder = true;
			}
			if (!System.IO.File.Exists(kPrefabPath))
			{
				var go = new GameObject("PF_" + kPrefabName);
				try { PrefabUtility.SaveAsPrefabAsset(go, kPrefabPath); }
				finally { UnityEngine.Object.DestroyImmediate(go); }
				AssetDatabase.ImportAsset(kPrefabPath, ImportAssetOptions.ForceUpdate);
				_createdPrefab = true;
			}
			_provider = new DefaultAssetProvider();
		}

		[OneTimeTearDown]
		public void OneTimeTearDown()
		{
			// Delete only what THIS test created.
			if (_createdPrefab) AssetDatabase.DeleteAsset(kPrefabPath);
			if (_createdResFolder && AssetDatabase.IsValidFolder(kResFolder)) AssetDatabase.DeleteAsset(kResFolder);
			if (_createdTempRoot && AssetDatabase.IsValidFolder(kTempRoot)) AssetDatabase.DeleteAsset(kTempRoot);
			AssetDatabase.Refresh();
		}

		[Test]
		public async Task LoadAssetAsync_ResourcesPrefab_Succeeds()
		{
			// Arrange
			var key = kPrefabName; // Resources path without extension

			// Act
			var handle = await _provider.LoadAssetAsync<GameObject>(key, CancellationToken.None);

			// Assert
			Assert.NotNull(handle, "Handle is null");
			Assert.IsTrue(handle.IsLoaded, "Handle.IsLoaded false");
			Assert.NotNull(handle.Asset, "Loaded asset is null");
			Assert.AreEqual(kPrefabName, handle.Asset.name.Replace("PF_", ""), "Loaded the wrong prefab name");

			// Release is no-op for Default provider, but call to keep symmetry
			_provider.Release(handle);
		}

		[Test]
		public async Task InstantiateAsync_ResourcesPrefab_CreatesInstance_And_ReleaseDestroys()
		{
			// Arrange
			var key = kPrefabName;

			// Act
			var instHandle = await _provider.InstantiateAsync(key, null, CancellationToken.None);

			// Assert instance exists
			Assert.NotNull(instHandle);
			Assert.NotNull(instHandle.Instance);
			Assert.IsTrue(instHandle.Instance, "Instance has been destroyed unexpectedly");

			// Release should destroy the instance
			var go = instHandle.Instance;
			_provider.Release(instHandle);
			Assert.IsFalse(go, "Instance should be destroyed after Release()");
		}

		[Test]
		public void NormalizeKey_WrongProvider_Throws()
		{
			var otherProvider = new AddressablesProvider(); // only to construct a mismatched AssetKey object
			var foreignKey = new AssetKey(otherProvider, "addr:dummy", typeof(GameObject));

			Assert.Throws<System.InvalidOperationException>(() =>
			{
				_provider.NormalizeKey<GameObject>(foreignKey);
			});
		}

		[Test]
		public void NormalizeKey_Object_NotPersistent_Throws()
		{
			var go = new GameObject("TempSceneObject");
			try
			{
				Assert.Throws<System.InvalidOperationException>(() =>
				{
					_provider.NormalizeKey<GameObject>(go);
				});
			}
			finally
			{
				Object.DestroyImmediate(go);
			}
		}
	}
}