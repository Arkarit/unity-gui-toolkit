using System.Collections;
using GuiToolkit.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace GuiToolkit.Test
{
	public class TestPrefabInfo
	{
		[Test]
		public void Test()
		{
			Assert.DoesNotThrow(() => PrefabInfo.Create(null));
			PrefabInfo pi;
			GameObject go;

			pi = new PrefabInfo();
			AssertInvalidity(pi);
			pi = PrefabInfo.Create(null);
			AssertInvalidity(pi);

			go = TestData.Instance.RegularPrefabAsset.TryLoad<GameObject>();
			pi = PrefabInfo.Create(go);
			AssertRegularPrefabAsset(pi);

			go = TestData.Instance.VariantPrefabAsset.TryLoad<GameObject>();
			pi = PrefabInfo.Create(go);
			AssertVariantPrefabAsset(pi);

			go = TestData.Instance.ModelPrefabAsset.TryLoad<GameObject>();
			pi = PrefabInfo.Create(go);
			AssertModelPrefabAsset(pi);
		}

		private void AssertInvalidity( PrefabInfo _pi )
		{
			// Object itself must exist
			Assert.IsNotNull(_pi, "PrefabInfo instance should never be null.");
			if (_pi == null)
				return; // Safety-bail (no further null refs)

			// Core reference & validity
			Assert.IsNull(_pi.GameObject, "GameObject reference should be null for an invalid PrefabInfo.");
			Assert.IsFalse(_pi.IsValid, "IsValid must be false when GameObject is null.");

			// Basic prefab flags
			Assert.IsFalse(_pi.IsPrefab, "IsPrefab must be false for invalid PrefabInfo.");
			Assert.IsFalse(_pi.IsInstanceRoot, "IsInstanceRoot must be false for invalid PrefabInfo.");
			Assert.IsFalse(_pi.IsPartOfPrefab, "IsPartOfPrefab must be false for invalid PrefabInfo.");

			// Default enum states
			Assert.AreEqual(PrefabAssetType.NotAPrefab, _pi.AssetType, "AssetType should default to NotAPrefab.");
			Assert.AreEqual(PrefabInstanceStatus.NotAPrefab, _pi.InstanceStatus, "InstanceStatus should default to NotAPrefab.");

			// Lazy properties – should all be null / false
			Assert.IsNull(_pi.AssetPath, "AssetPath should be null for invalid PrefabInfo.");
			Assert.IsNull(_pi.AssetGuid, "AssetGuid should be null for invalid PrefabInfo.");
			Assert.IsFalse(_pi.HasOverrides, "HasOverrides must be false when no GameObject is present.");
		}

		private void AssertRegularPrefabAsset( PrefabInfo _pi )
		{
			Assert.IsNotNull(_pi);
			Assert.IsTrue(_pi.IsValid, "Regular prefab should be valid.");
			Assert.IsNotNull(_pi.GameObject, "GameObject must not be null.");

			Assert.IsTrue(_pi.IsPrefab, "Should be marked as prefab asset.");
			Assert.IsFalse(_pi.IsInstanceRoot, "Prefab assets are never instance roots.");
			Assert.IsTrue(_pi.IsPartOfPrefab, "Prefab asset should be part of a prefab.");

			Assert.AreEqual(PrefabAssetType.Regular, _pi.AssetType, "Expected Regular asset type.");
			Assert.AreEqual(PrefabInstanceStatus.NotAPrefab, _pi.InstanceStatus, "Asset should not be an instance.");

			Assert.IsFalse(_pi.HasOverrides, "Prefab assets should not have overrides.");
			Assert.IsFalse(_pi.IsVariantAsset);
			Assert.IsFalse(_pi.IsModelAsset);
		}

		private void AssertVariantPrefabAsset( PrefabInfo _pi )
		{
			Assert.IsNotNull(_pi);
			Assert.IsTrue(_pi.IsValid, "Variant prefab should be valid.");
			Assert.IsNotNull(_pi.GameObject, "GameObject must not be null.");

			Assert.IsTrue(_pi.IsPrefab, "Should be marked as prefab asset.");
			Assert.IsFalse(_pi.IsInstanceRoot, "Prefab assets are never instance roots.");
			Assert.IsTrue(_pi.IsPartOfPrefab, "Prefab asset should be part of a prefab.");

			Assert.AreEqual(PrefabAssetType.Variant, _pi.AssetType, "Expected Variant asset type.");
			Assert.AreEqual(PrefabInstanceStatus.NotAPrefab, _pi.InstanceStatus, "Asset should not be an instance.");

			Assert.IsFalse(_pi.HasOverrides, "Prefab assets should not have overrides.");
			Assert.IsTrue(_pi.IsVariantAsset);
			Assert.IsFalse(_pi.IsModelAsset);
		}

		private void AssertModelPrefabAsset( PrefabInfo _pi )
		{
			Assert.IsNotNull(_pi);
			Assert.IsTrue(_pi.IsValid, "Model prefab should be valid.");
			Assert.IsNotNull(_pi.GameObject, "GameObject must not be null.");

			Assert.IsTrue(_pi.IsPrefab, "Should be marked as prefab asset.");
			Assert.IsFalse(_pi.IsInstanceRoot, "Prefab assets are never instance roots.");
			Assert.IsTrue(_pi.IsPartOfPrefab, "Model asset should be part of a prefab.");

			Assert.AreEqual(PrefabAssetType.Model, _pi.AssetType, "Expected Model asset type.");
			Assert.AreEqual(PrefabInstanceStatus.NotAPrefab, _pi.InstanceStatus, "Asset should not be an instance.");

			Assert.IsFalse(_pi.HasOverrides, "Prefab assets should not have overrides.");
			Assert.IsFalse(_pi.IsVariantAsset);
			Assert.IsTrue(_pi.IsModelAsset);
		}
	}
}
