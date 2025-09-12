// Assets/Tests/Editor/TestDefaultAssetProvider.cs
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using GuiToolkit.AssetHandling;
using Object = UnityEngine.Object;

namespace GuiToolkit.Test
{
	public class TestDefaultAssetProvider
	{
		private const string kTempRoot = "Assets/~Temp_GT_Tests";
		private const string kResFolder = kTempRoot + "/Resources";
		private const string kPrefabName = "GT_TestPrefab_DefaultProvider";
		private const string kPrefabPath = kResFolder + "/" + kPrefabName + ".prefab";

		private bool m_createdTempRoot;
		private bool m_createdResFolder;
		private bool m_createdPrefab;

		private DefaultAssetProvider m_provider;

		[OneTimeSetUp]
		public void OneTimeSetUp()
		{
			if (!AssetDatabase.IsValidFolder(kTempRoot))
			{
				AssetDatabase.CreateFolder("Assets", "~Temp_GT_Tests");
				m_createdTempRoot = true;
			}

			if (!AssetDatabase.IsValidFolder(kResFolder))
			{
				AssetDatabase.CreateFolder(kTempRoot, "Resources");
				m_createdResFolder = true;
			}

			if (!File.Exists(kPrefabPath))
			{
				var go = new GameObject("PF_" + kPrefabName);
				try
				{
					PrefabUtility.SaveAsPrefabAsset(go, kPrefabPath);
				}
				finally
				{
					Object.DestroyImmediate(go);
				}

				AssetDatabase.ImportAsset(kPrefabPath, ImportAssetOptions.ForceUpdate);
				m_createdPrefab = true;
			}

			m_provider = new DefaultAssetProvider();
		}

		[OneTimeTearDown]
		public void OneTimeTearDown()
		{
			// Delete only what THIS test created.
			if (m_createdPrefab)
				AssetDatabase.DeleteAsset(kPrefabPath);

			if (m_createdResFolder && AssetDatabase.IsValidFolder(kResFolder))
				AssetDatabase.DeleteAsset(kResFolder);

			if (m_createdTempRoot && AssetDatabase.IsValidFolder(kTempRoot))
				AssetDatabase.DeleteAsset(kTempRoot);

			AssetDatabase.Refresh();
		}

		[Test]
		public async Task LoadAssetAsync_ResourcesPrefab_Succeeds()
		{
			// Arrange
			var key = kPrefabName; // Resources path without extension

			// Act
			var handle = await m_provider.LoadAssetAsync<GameObject>(key, CancellationToken.None);

			// Assert
			Assert.NotNull(handle, "Handle is null");
			Assert.IsTrue(handle.IsLoaded, "Handle.IsLoaded false");
			Assert.NotNull(handle.Asset, "Loaded asset is null");
			Assert.AreEqual(kPrefabName, handle.Asset.name.Replace("PF_", ""), "Loaded the wrong prefab name");

			// Release is no-op for Default provider, but call to keep symmetry
			m_provider.Release(handle);
		}

		[Test]
		public async Task InstantiateAsync_ResourcesPrefab_CreatesInstance_And_ReleaseDestroys()
		{
			// Arrange
			var key = kPrefabName;

			// Act
			var instHandle = await m_provider.InstantiateAsync(key, null, CancellationToken.None);

			// Assert instance exists
			Assert.NotNull(instHandle);
			Assert.NotNull(instHandle.Instance);
			Assert.IsTrue(instHandle.Instance, "Instance has been destroyed unexpectedly");

			// Release should destroy the instance
			var go = instHandle.Instance;
			m_provider.Release(instHandle);
			Assert.IsFalse(go, "Instance should be destroyed after Release()");
		}

		[Test]
		public void NormalizeKey_WrongProvider_Throws()
		{
			// Only used to create a foreign AssetKey for mismatch testing
			var otherProvider = new AddressablesProvider();
			var foreignKey = new CanonicalAssetKey(otherProvider, "addr:dummy", typeof(GameObject));

			Assert.Throws<System.InvalidOperationException>(() =>
			{
				m_provider.NormalizeKey<GameObject>(foreignKey);
			});
		}
	}
}
