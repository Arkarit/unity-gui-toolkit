using System.Collections.Generic;
using System.Linq;
using GuiToolkit.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Test
{
	/// <summary>
	/// Isolation tests for <see cref="UiStandardElementTagger"/>. Fixtures are freshly created in the
	/// temp folder as a bare base prefab plus a variant with a single deliberate override, so neither
	/// styling components nor stale-schema re-serialization can perturb the result — the only change the
	/// tagger is allowed to make is the marker itself. This is what lets us assert that tagging does NOT
	/// touch a variant's pre-existing override.
	/// </summary>
	[EditorAware]
	public class TestUiStandardElementTagger
	{
		private const string BaseName = "TaggerTestBase";
		private const string VariantName = "TaggerTestVariant";
		private const float BaseAlpha = 1f;
		private const float VariantAlpha = 0.5f; // a deliberate override on the variant

		private string TempFolder => TestData.Instance.TempFolderPath.ToString();
		private string BasePath => $"{TempFolder}/{BaseName}.prefab";
		private string VariantPath => $"{TempFolder}/{VariantName}.prefab";

		[SetUp]
		public void SetUp()
		{
			TestData.Initialize();
			DeleteFixtures();
		}

		[TearDown]
		public void TearDown()
		{
			DeleteFixtures();
		}

		/// <summary>
		/// Deletes the fixtures explicitly, VARIANT FIRST — deleting the base while a variant still
		/// references it makes Unity reimport the variant with a missing parent and log an import error
		/// (which would fail the test). DeleteAsset on a non-existent path is a harmless no-op.
		/// </summary>
		private void DeleteFixtures()
		{
			AssetDatabase.DeleteAsset(VariantPath);
			AssetDatabase.DeleteAsset(BasePath);
			AssetDatabase.Refresh();
		}

		/// <summary>
		/// Creates a bare base prefab (a GameObject carrying only a style-free <see cref="CanvasGroup"/> at
		/// <see cref="BaseAlpha"/>) and a variant of it that overrides the alpha to <see cref="VariantAlpha"/>.
		/// The alpha override is what the corruption guard later asserts survives tagging. (The root's name
		/// can't be used as an override — SaveAsPrefabAsset names the root after the file.)
		/// </summary>
		private void CreateBaseAndVariant()
		{
			var baseGo = new GameObject(BaseName);
			baseGo.AddComponent<CanvasGroup>().alpha = BaseAlpha;
			var baseAsset = PrefabUtility.SaveAsPrefabAsset(baseGo, BasePath);
			Object.DestroyImmediate(baseGo);
			Assert.IsNotNull(baseAsset, "Failed to create base fixture prefab.");

			// Instantiate the base and save the instance under a new path → a variant (same pattern the
			// toolkit's own 'Create Default Prefabs Variants' tool uses).
			var instance = (GameObject) PrefabUtility.InstantiatePrefab(baseAsset);
			instance.GetComponent<CanvasGroup>().alpha = VariantAlpha; // deliberate override on the variant
			var variantAsset = PrefabUtility.SaveAsPrefabAsset(instance, VariantPath);
			Object.DestroyImmediate(instance);

			Assert.IsNotNull(variantAsset, "Failed to create variant fixture prefab.");
			Assert.AreEqual(PrefabAssetType.Variant, PrefabUtility.GetPrefabAssetType(variantAsset),
				"Fixture second prefab is not a variant.");
		}

		[Test]
		public void TagBaseAndVariant_TagsBothAndPreservesVariantOverride()
		{
			CreateBaseAndVariant();

			// Intentionally list the variant BEFORE the base to prove the tagger reorders base-first.
			var results = UiStandardElementTagger.Tag(new List<UiStandardElementTagger.TagRequest>
			{
				new() { PrefabPath = VariantPath, Key = VariantName },
				new() { PrefabPath = BasePath, Key = BaseName },
			});

			Assert.IsTrue(results.All(r => r.Ok), "All tag operations should report success.");

			// Base carries the marker as a plain component with its own custom identity.
			var baseMarker = AssetDatabase.LoadAssetAtPath<GameObject>(BasePath).GetComponent<UiStandardElement>();
			Assert.IsNotNull(baseMarker, "Base prefab should carry a marker.");
			Assert.AreEqual(EStandardElement.Custom, baseMarker.Element);
			Assert.AreEqual(BaseName, baseMarker.Key);
			Assert.IsFalse(baseMarker.IsInternal);

			// Variant inherits the marker and overrides the identity to its own custom id.
			var variantGo = AssetDatabase.LoadAssetAtPath<GameObject>(VariantPath);
			var variantMarker = variantGo.GetComponent<UiStandardElement>();
			Assert.IsNotNull(variantMarker, "Variant prefab should carry the (inherited) marker.");
			Assert.AreEqual(VariantName, variantMarker.Key, "Variant should claim its own identity.");

			// The crucial isolation assertion: tagging (which re-saved the base first, then the variant)
			// must NOT disturb the variant's pre-existing alpha override.
			var variantAlpha = variantGo.GetComponent<CanvasGroup>().alpha;
			Assert.AreEqual(VariantAlpha, variantAlpha, 0.0001f,
				"Variant's alpha override must survive tagging — no override corruption.");
		}

		[Test]
		public void Tag_IsIdempotentAndCanChangeIdentity()
		{
			CreateBaseAndVariant();

			UiStandardElementTagger.Tag(new List<UiStandardElementTagger.TagRequest>
			{
				new() { PrefabPath = BasePath, Key = BaseName },
			});

			// Re-tag the same prefab with a different identity + internal flag: should update in place,
			// not add a second marker (the component is [DisallowMultipleComponent]).
			var results = UiStandardElementTagger.Tag(new List<UiStandardElementTagger.TagRequest>
			{
				new() { PrefabPath = BasePath, Key = nameof(EStandardElement.StandardButton), Internal = true },
			});
			Assert.IsTrue(results.All(r => r.Ok));

			var go = AssetDatabase.LoadAssetAtPath<GameObject>(BasePath);
			var markers = go.GetComponents<UiStandardElement>();
			Assert.AreEqual(1, markers.Length, "Re-tagging must not add a second marker.");
			Assert.AreEqual(EStandardElement.StandardButton, markers[0].Element);
			Assert.AreEqual("", markers[0].CustomId, "Built-in identity clears the custom id.");
			Assert.IsTrue(markers[0].IsInternal);
		}

		[Test]
		public void Untag_RemovesMarker()
		{
			CreateBaseAndVariant();

			UiStandardElementTagger.Tag(new List<UiStandardElementTagger.TagRequest>
			{
				new() { PrefabPath = BasePath, Key = BaseName },
			});
			Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>(BasePath).GetComponent<UiStandardElement>());

			var results = UiStandardElementTagger.Untag(new List<string> { BasePath });
			Assert.IsTrue(results.All(r => r.Ok));

			Assert.IsNull(AssetDatabase.LoadAssetAtPath<GameObject>(BasePath).GetComponent<UiStandardElement>(),
				"Marker should be gone after untag.");
		}
	}
}
