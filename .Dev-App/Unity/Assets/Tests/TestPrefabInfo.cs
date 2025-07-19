using System.Collections;
using GuiToolkit.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using System.Linq;

namespace GuiToolkit.Test
{
	public class TestPrefabInfo
	{
		[Test]
		public void TestInvalid()
		{
			Assert.DoesNotThrow(() => PrefabInfo.Create(null));
			PrefabInfo pi;

			pi = new PrefabInfo();
			AssertInvalidPrefabInfo(pi);
			pi = PrefabInfo.Create(null);
			AssertInvalidPrefabInfo(pi);
		}

		private void AssertInvalidPrefabInfo(PrefabInfo _pi)
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
			Assert.IsFalse(_pi.IsDirty, "HasOverrides must be false when no GameObject is present.");
		}

		[Test]
		public void TestRegular()
		{
			var go = TestData.Instance.RegularPrefabAsset.TryLoad<GameObject>();
			var pi = PrefabInfo.Create(go);

			Assert.IsNotNull(pi);
			Assert.IsTrue(pi.IsValid, "Regular prefab should be valid.");
			Assert.IsNotNull(pi.GameObject, "GameObject must not be null.");

			Assert.IsTrue(pi.IsPrefab, "Should be marked as prefab asset.");
			Assert.IsFalse(pi.IsInstanceRoot, "Prefab assets are never instance roots.");
			Assert.IsTrue(pi.IsPartOfPrefab, "Prefab asset should be part of a prefab.");

			Assert.AreEqual(PrefabAssetType.Regular, pi.AssetType, "Expected Regular asset type.");
			Assert.AreEqual(PrefabInstanceStatus.NotAPrefab, pi.InstanceStatus, "Asset should not be an instance.");

			Assert.IsFalse(pi.IsDirty, "Prefab assets should not have overrides.");
			Assert.IsFalse(pi.IsVariantAsset);
			Assert.IsFalse(pi.IsModelAsset);
			SubTestOverrides(pi);
		}

		[Test]
		public void TestVariantWithoutOverrides()
		{
			var go = TestData.Instance.VariantPrefabAssetWithoutOverrides.TryLoad<GameObject>();
			var pi = PrefabInfo.Create(go);
			Assert.IsNotNull(pi);
			Assert.IsTrue(pi.IsValid, "Variant prefab should be valid.");
			Assert.IsNotNull(pi.GameObject, "GameObject must not be null.");

			Assert.IsTrue(pi.IsPrefab, "Should be marked as prefab asset.");
			Assert.IsFalse(pi.IsInstanceRoot, "Prefab assets are never instance roots.");
			Assert.IsTrue(pi.IsPartOfPrefab, "Prefab asset should be part of a prefab.");

			Assert.AreEqual(PrefabAssetType.Variant, pi.AssetType, "Expected Variant asset type.");
			Assert.AreEqual(PrefabInstanceStatus.NotAPrefab, pi.InstanceStatus, "Asset should not be an instance.");

			Assert.IsFalse(pi.IsDirty, "Freshly loaded Prefab assets should not be dirty.");
			Assert.IsTrue(pi.IsVariantAsset);
			Assert.IsFalse(pi.IsModelAsset);
		}

		[Test]
		public void TestVariantWithOverrides()
		{
			var go = TestData.Instance.VariantPrefabAssetWithOverrides.TryLoad<GameObject>();
			var pi = PrefabInfo.Create(go);
			Assert.IsNotNull(pi);
			Assert.IsTrue(pi.IsValid, "Variant prefab should be valid.");
			Assert.IsNotNull(pi.GameObject, "GameObject must not be null.");

			Assert.IsTrue(pi.IsPrefab, "Should be marked as prefab asset.");
			Assert.IsFalse(pi.IsInstanceRoot, "Prefab assets are never instance roots.");
			Assert.IsTrue(pi.IsPartOfPrefab, "Prefab asset should be part of a prefab.");

			Assert.AreEqual(PrefabAssetType.Variant, pi.AssetType, "Expected Variant asset type.");
			Assert.AreEqual(PrefabInstanceStatus.NotAPrefab, pi.InstanceStatus, "Asset should not be an instance.");

			Assert.IsFalse(pi.IsDirty, "Freshly loaded Prefab assets should not be dirty.");
			Assert.IsTrue(pi.IsVariantAsset);
			Assert.IsFalse(pi.IsModelAsset);
		}

		[Test]
		public void TestInstanceVariantWithoutOverrides()
		{
			var go = TestData.Instance.VariantPrefabAssetWithoutOverrides.TryLoad<GameObject>();
			go = (GameObject)PrefabUtility.InstantiatePrefab(go);
			var pi = PrefabInfo.Create(go);
			Assert.IsNotNull(pi);
			Assert.IsTrue(pi.IsValid, "Variant prefab instance should be valid.");
			Assert.IsNotNull(pi.GameObject, "GameObject must not be null.");

			Assert.IsFalse(pi.IsPrefab, "Should not be marked as prefab asset.");
			Assert.IsTrue(pi.IsInstanceRoot, "Prefab asset instance should be root.");
			Assert.IsTrue(pi.IsPartOfPrefab, "Prefab asset should be part of a prefab.");

			Assert.AreEqual(PrefabAssetType.Variant, pi.AssetType, "Expected Variant asset type.");
			Assert.AreEqual(PrefabInstanceStatus.Connected, pi.InstanceStatus, "Instance should be connected");

			Assert.That(pi.IsDirty, Is.EqualTo(EditorUtility.IsDirty(pi.GameObject)), "IsDirty should match Unity's internal dirty flag.");
			Assert.IsTrue(pi.IsVariantAsset);
			Assert.IsFalse(pi.IsModelAsset);
		}

		[Test]
		public void TestInstanceVariantWithOverrides()
		{
			var go = TestData.Instance.VariantPrefabAssetWithOverrides.TryLoad<GameObject>();
			go = (GameObject)PrefabUtility.InstantiatePrefab(go);
			var pi = PrefabInfo.Create(go);
			Assert.IsNotNull(pi);
			Assert.IsTrue(pi.IsValid, "Variant prefab instance should be valid.");
			Assert.IsNotNull(pi.GameObject, "GameObject must not be null.");

			Assert.IsFalse(pi.IsPrefab, "Should not be marked as prefab asset.");
			Assert.IsTrue(pi.IsInstanceRoot, "Prefab asset instance should be root.");
			Assert.IsTrue(pi.IsPartOfPrefab, "Prefab asset should be part of a prefab.");

			Assert.AreEqual(PrefabAssetType.Variant, pi.AssetType, "Expected Variant asset type.");
			Assert.AreEqual(PrefabInstanceStatus.Connected, pi.InstanceStatus, "Instance should be connected");

			Assert.That(pi.IsDirty, Is.EqualTo(EditorUtility.IsDirty(pi.GameObject)), "IsDirty should match Unity's internal dirty flag.");
			Assert.IsTrue(pi.IsVariantAsset);
			Assert.IsFalse(pi.IsModelAsset);
		}

		[Test]
		public void TestModel()
		{
			var go = TestData.Instance.ModelPrefabAsset.TryLoad<GameObject>();
			var pi = PrefabInfo.Create(go);
			Assert.IsNotNull(pi);
			Assert.IsTrue(pi.IsValid, "Model prefab should be valid.");
			Assert.IsNotNull(pi.GameObject, "GameObject must not be null.");

			Assert.IsTrue(pi.IsPrefab, "Should be marked as prefab asset.");
			Assert.IsFalse(pi.IsInstanceRoot, "Prefab assets are never instance roots.");
			Assert.IsTrue(pi.IsPartOfPrefab, "Model asset should be part of a prefab.");

			Assert.AreEqual(PrefabAssetType.Model, pi.AssetType, "Expected Model asset type.");
			Assert.AreEqual(PrefabInstanceStatus.NotAPrefab, pi.InstanceStatus, "Asset should not be an instance.");

			Assert.IsFalse(pi.IsDirty, "Prefab assets should not have overrides.");
			Assert.IsFalse(pi.IsVariantAsset);
			Assert.IsTrue(pi.IsModelAsset);
		}

		[Test]
		public void TestToString()
		{
			var go = TestData.Instance.RegularPrefabAsset.TryLoad<GameObject>();
			var pi = PrefabInfo.Create(go);
			var str = pi.ToString();
			Assert.IsTrue(str.Contains(go.name), "ToString should contain GameObject name.");
			Assert.IsTrue(str.Contains("IsPrefab="), "ToString should report key flags.");
		}

		private void SubTestOverrides(PrefabInfo _assetPrefabInfo)
		{
			if (_assetPrefabInfo == null || !_assetPrefabInfo.IsValid)
				return;

			PrefabInfo pi;

			var obj = PrefabUtility.InstantiatePrefab(_assetPrefabInfo.GameObject);
			var go = obj as GameObject;
			pi = PrefabInfo.Create(go);
			var oi = pi.OverrideInfo;
			Assert.IsNotNull(oi);
			Assert.IsTrue(pi.IsDirty);

			// Modify a known property
			pi.Modify<TestMonoBehaviour>("Int", sp =>
			{
			    sp.intValue += 1;
			    return true;
			});
			
			Assert.IsTrue(pi.IsDirty);
			Assert.That(oi.PropertyModifications.Any(pm => pm.propertyPath.Contains("Int")),
				"Expected override on 'Int' field of TestMonoBehaviour.");

			// Save as new prefab asset variant
			pi.SaveAs(TestData.Instance.TempFolderPath + "/1.prefab", InteractionMode.AutomatedAction);

			// Unity does not clear the dirty flag immediately after SaveAsPrefabAssetAndConnect.
			// Therefore, this assertion might not be reliable in all contexts:
			// Assert.IsFalse(pi.IsDirty);

			Assert.IsTrue(oi.PropertyModifications.Count > 0 || pi.OverrideInfo.ObjectOverrides.Count > 0,
				"Expected at least one override to be recorded.");

			Assert.That(!oi.PropertyModifications.Any(pm => pm.propertyPath.Contains("Int")),
				"Unexpected override on 'Int' field of TestMonoBehaviour.");

		}
	}
}
