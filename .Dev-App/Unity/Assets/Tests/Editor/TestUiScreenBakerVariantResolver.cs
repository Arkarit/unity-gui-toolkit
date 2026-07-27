using System.Linq;
using GuiToolkit.Editor;
using GuiToolkit.Editor.AiSupport;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Test
{
	/// <summary>
	/// End-to-end proof of the palette variant-resolver: when a client project holds a prefab VARIANT of a
	/// tagged toolkit standard element, an authored screen must bake with the CLIENT variant, not the
	/// library default — so screens carry the project's look. Exercises the whole chain: marker inheritance
	/// → generator ranking (client out-ranks library) → baker template resolution through the registry.
	/// </summary>
	[EditorAware]
	public class TestUiScreenBakerVariantResolver
	{
		private const string Key = "OkButton";
		// The variant must live in the canonical client prefab-variants folder — that is what the generator
		// scans for client standard-element markers.
		private string VariantsFolder => UiToolkitConfiguration.Instance.PrefabVariantsPath.TrimEnd('/');
		private string VariantPath => $"{VariantsFolder}/OkButtonClientVariant.prefab";
		private string m_bakedPath;

		[SetUp]
		public void SetUp()
		{
			TestData.Initialize();
			Cleanup();
		}

		[TearDown]
		public void TearDown()
		{
			Cleanup();
			// Restore the registry to its clean library-only state (the client variant is gone now).
			UiScreenCatalogGenerator.Generate();
		}

		private void Cleanup()
		{
			// Delete the baked prefab (a VARIANT of the client variant) BEFORE the variant itself —
			// deleting the parent first would leave the child with a missing parent and log an import error.
			if (!string.IsNullOrEmpty(m_bakedPath))
			{
				AssetDatabase.DeleteAsset(m_bakedPath);
				m_bakedPath = null;
			}
			AssetDatabase.DeleteAsset(VariantPath);
		}

		[Test]
		public void ClientVariantOutranksLibraryWhenBaking()
		{
			// 1. The library OkButton must exist and be tagged.
			var lib = FindLibraryPrefab(Key);
			Assert.IsNotNull(lib, "Library OkButton prefab not found (expected under the toolkit mount).");
			Assert.IsTrue(EditorAssetUtility.IsPackagesOrInternalAsset(lib), "Library OkButton should count as library.");
			Assert.AreEqual(Key, lib.GetComponent<UiStandardElement>()?.Key,
				"Library OkButton must be tagged as a standard element.");

			// 2. Create a CLIENT variant of it in the canonical variants folder. It inherits the marker.
			EditorFileUtility.EnsureUnityFolderExists(VariantsFolder);
			var instance = (GameObject) PrefabUtility.InstantiatePrefab(lib);
			var variant = PrefabUtility.SaveAsPrefabAsset(instance, VariantPath);
			Object.DestroyImmediate(instance);
			Assert.AreEqual(PrefabAssetType.Variant, PrefabUtility.GetPrefabAssetType(variant));
			Assert.IsFalse(EditorAssetUtility.IsPackagesOrInternalAsset(variant), "Variant should count as client.");
			Assert.AreEqual(Key, variant.GetComponent<UiStandardElement>()?.Key, "Variant must inherit the marker.");

			// 3. Regenerate: the registry must now resolve the key to the CLIENT variant (ranking wins).
			UiScreenCatalogGenerator.Generate();
			var registry = UiToolkitConfiguration.Instance.StandardElementRegistry;
			Assert.IsNotNull(registry, "Registry should exist after generation.");
			Assert.AreEqual(VariantPath, AssetDatabase.GetAssetPath(registry.Resolve(Key)),
				"Registry should resolve the key to the client variant, not the library default.");

			// 4. Bake a screen using the OkButton template — the baker must pick the client variant.
			m_bakedPath = UiScreenBaker.Bake(
				"{\"name\":\"VariantResolverBakeTest\",\"root\":{\"template\":\"OkButton\",\"id\":\"root\",\"text\":\"@text:OK\"}}").path;
			Assert.IsFalse(string.IsNullOrEmpty(m_bakedPath), "Bake should return a path.");

			var bakedRoot = AssetDatabase.LoadAssetAtPath<GameObject>(m_bakedPath);
			var source = PrefabUtility.GetCorrespondingObjectFromSource(bakedRoot);
			Assert.AreEqual(VariantPath, AssetDatabase.GetAssetPath(source),
				"The baked OkButton must derive from the client variant, not the library default.");
		}

		private static GameObject FindLibraryPrefab( string _name )
		{
			return AssetDatabase.FindAssets($"{_name} t:Prefab")
				.Select(AssetDatabase.GUIDToAssetPath)
				.Select(AssetDatabase.LoadAssetAtPath<GameObject>)
				.FirstOrDefault(p => p != null && p.name == _name && EditorAssetUtility.IsPackagesOrInternalAsset(p));
		}
	}
}
