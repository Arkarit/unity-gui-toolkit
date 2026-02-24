using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using GuiToolkit;
using GuiToolkit.Editor;

namespace GuiToolkit.Test
{
	/// <summary>
	/// Editor tests for <see cref="CloneFolder"/>.
	///
	/// Test scenario:
	///   SourceFolder/
	///     AssetA.asset   (has an internal reference to AssetB, and an external reference to ExternalAsset)
	///     AssetB.asset
	///   ExternalAsset.asset  (sibling of SourceFolder - outside it)
	///
	/// After Clone(SourceFolder, DestFolder):
	///   - DestFolder/AssetA and DestFolder/AssetB must exist
	///   - DestFolder/AssetA.InternalRef must point to DestFolder/AssetB (rewired)
	///   - DestFolder/AssetA.ExternalRef must still point to ExternalAsset (kept)
	/// </summary>
	public class TestCloneFolder
	{
		// All temp assets live under this folder so TearDown can delete them in one call.
		private const string TempRoot = "Assets/Tests/TestObjects/Temp/CloneFolderTest";
		private const string SrcFolder = TempRoot + "/SourceFolder";
		private const string DstFolder = TempRoot + "/ClonedFolder";
		private const string ExternalAssetPath = TempRoot + "/ExternalAsset.asset";
		private const string AssetAPath        = SrcFolder + "/AssetA.asset";
		private const string AssetBPath        = SrcFolder + "/AssetB.asset";
		// Asset whose name matches the folder name – used in rename tests
		private const string SourceFolderName  = "SourceFolder";
		private const string AssetMatchedPath  = SrcFolder + "/SourceFolder_extra.asset";
		private const string AssetNoMatchPath  = SrcFolder + "/Unrelated.asset";

		// -------------------------------------------------------------------------
		// Setup / TearDown
		// -------------------------------------------------------------------------

		[SetUp]
		public void SetUp()
		{
			// Remove any leftover from a previous run
			TearDown();

			// Create the temp hierarchy
			EditorFileUtility.EnsureUnityFolderExists(SrcFolder);

			// AssetB - just a plain scriptable object (no references)
			var assetB = ScriptableObject.CreateInstance<CloneFolderTestAsset>();
			AssetDatabase.CreateAsset(assetB, AssetBPath);

			// ExternalAsset - lives OUTSIDE the source folder
			var external = ScriptableObject.CreateInstance<CloneFolderTestAsset>();
			AssetDatabase.CreateAsset(external, ExternalAssetPath);

			// AssetA - references both AssetB (internal) and ExternalAsset (external)
			var assetA = ScriptableObject.CreateInstance<CloneFolderTestAsset>();
			AssetDatabase.CreateAsset(assetA, AssetAPath);

			// Wire references via SerializedObject so they are persisted to disk
			var so = new SerializedObject(assetA);
			so.FindProperty(nameof(CloneFolderTestAsset.InternalRef)).objectReferenceValue = assetB;
			so.FindProperty(nameof(CloneFolderTestAsset.ExternalRef)).objectReferenceValue = external;
			so.ApplyModifiedPropertiesWithoutUndo();

			AssetDatabase.SaveAssets();
			// Force synchronous re-import so FindAssets picks up the new assets immediately.
			AssetDatabase.ImportAsset(AssetBPath, ImportAssetOptions.ForceSynchronousImport);
			AssetDatabase.ImportAsset(ExternalAssetPath, ImportAssetOptions.ForceSynchronousImport);
			AssetDatabase.ImportAsset(AssetAPath, ImportAssetOptions.ForceSynchronousImport);

			// Extra assets for rename tests: one whose name contains the folder name, one that doesn't
			var matched   = ScriptableObject.CreateInstance<CloneFolderTestAsset>();
			var unrelated = ScriptableObject.CreateInstance<CloneFolderTestAsset>();
			AssetDatabase.CreateAsset(matched,   AssetMatchedPath);
			AssetDatabase.CreateAsset(unrelated, AssetNoMatchPath);
			AssetDatabase.ImportAsset(AssetMatchedPath, ImportAssetOptions.ForceSynchronousImport);
			AssetDatabase.ImportAsset(AssetNoMatchPath, ImportAssetOptions.ForceSynchronousImport);
		}

		[TearDown]
		public void TearDown()
		{
			if (AssetDatabase.IsValidFolder(TempRoot))
				AssetDatabase.DeleteAsset(TempRoot);

			AssetDatabase.Refresh();
		}

		// -------------------------------------------------------------------------
		// Helpers
		// -------------------------------------------------------------------------

		/// <summary>
		/// Loads an asset from <paramref name="path"/> and asserts it is non-null.
		/// Uses base <see cref="Object"/> so type-resolution issues don't obscure existence failures.
		/// </summary>
		private static Object RequireAsset( string path, string label )
		{
			var obj = AssetDatabase.LoadAssetAtPath<Object>(path);
			Assert.IsNotNull(obj, $"{label} (path: '{path}')");
			return obj;
		}

		// -------------------------------------------------------------------------
		// Tests
		// -------------------------------------------------------------------------

		[Test]
		public void Clone_CreatesFolderAndAssets()
		{
			// Sanity: source must be set up correctly before we clone
			RequireAsset(AssetAPath, "Source AssetA must exist before clone");
			RequireAsset(AssetBPath, "Source AssetB must exist before clone");

			bool success = CloneFolder.Clone(SrcFolder, DstFolder);

			Assert.IsTrue(success, "Clone() should return true");
			Assert.IsTrue(AssetDatabase.IsValidFolder(DstFolder), "Destination folder should exist");
			RequireAsset(DstFolder + "/AssetA.asset", "Cloned AssetA should exist");
			RequireAsset(DstFolder + "/AssetB.asset", "Cloned AssetB should exist");
		}

		[Test]
		public void Clone_InternalReferenceIsRewired()
		{
			CloneFolder.Clone(SrcFolder, DstFolder);

			var cloneASO = new SerializedObject(RequireAsset(DstFolder + "/AssetA.asset", "Cloned AssetA must exist"));
			var cloneB = RequireAsset(DstFolder + "/AssetB.asset", "Cloned AssetB must exist");
			var origB = RequireAsset(AssetBPath, "Original AssetB must exist");

			var internalRef = cloneASO.FindProperty(nameof(CloneFolderTestAsset.InternalRef)).objectReferenceValue;
			Assert.AreEqual(cloneB, internalRef,
			"Internal reference should point to the cloned AssetB, not the original");
			Assert.AreNotEqual(origB, internalRef,
			"Internal reference must NOT point to the original AssetB");
		}

		[Test]
		public void Clone_ExternalReferenceIsPreserved()
		{
			CloneFolder.Clone(SrcFolder, DstFolder);

			var cloneASO = new SerializedObject(RequireAsset(DstFolder + "/AssetA.asset", "Cloned AssetA must exist"));
			var external = RequireAsset(ExternalAssetPath, "ExternalAsset must exist");

			var externalRef = cloneASO.FindProperty(nameof(CloneFolderTestAsset.ExternalRef)).objectReferenceValue;
			Assert.AreEqual(external, externalRef,
			"External reference should still point to the original ExternalAsset");
		}

		[Test]
		public void Clone_OriginalAssetsAreUnchanged()
		{
			CloneFolder.Clone(SrcFolder, DstFolder);

			var origASO = new SerializedObject(RequireAsset(AssetAPath, "Original AssetA must exist"));
			var origB = RequireAsset(AssetBPath, "Original AssetB must exist");
			var external = RequireAsset(ExternalAssetPath, "ExternalAsset must exist");

			Assert.AreEqual(origB, origASO.FindProperty(nameof(CloneFolderTestAsset.InternalRef)).objectReferenceValue,
			"Original AssetA.InternalRef should still point to original AssetB");
			Assert.AreEqual(external, origASO.FindProperty(nameof(CloneFolderTestAsset.ExternalRef)).objectReferenceValue,
			"Original AssetA.ExternalRef should still point to ExternalAsset");
		}

		[Test]
		public void Clone_DestinationAssignedNewGuids()
		{
			CloneFolder.Clone(SrcFolder, DstFolder);

			string guidOrigA = AssetDatabase.AssetPathToGUID(AssetAPath);
			string guidOrigB = AssetDatabase.AssetPathToGUID(AssetBPath);
			string guidCloneA = AssetDatabase.AssetPathToGUID(DstFolder + "/AssetA.asset");
			string guidCloneB = AssetDatabase.AssetPathToGUID(DstFolder + "/AssetB.asset");

			Assert.IsFalse(string.IsNullOrEmpty(guidCloneA), "Cloned AssetA must have a GUID");
			Assert.IsFalse(string.IsNullOrEmpty(guidCloneB), "Cloned AssetB must have a GUID");
			Assert.AreNotEqual(guidOrigA, guidCloneA, "Cloned AssetA should have a new GUID");
			Assert.AreNotEqual(guidOrigB, guidCloneB, "Cloned AssetB should have a new GUID");
		}

		[Test]
		public void RenameMatchingAssets_RenamesFilesContainingFolderName()
		{
			CloneFolder.Clone(SrcFolder, DstFolder);
			CloneFolder.RenameMatchingAssets(DstFolder, SourceFolderName, "ClonedFolder");

			// The matched asset should have been renamed
			RequireAsset(DstFolder + "/ClonedFolder_extra.asset", "Renamed asset should exist at new path");
			Assert.IsNull(AssetDatabase.LoadAssetAtPath<Object>(DstFolder + "/SourceFolder_extra.asset"),
				"Original-named asset should no longer exist after rename");
		}

		[Test]
		public void RenameMatchingAssets_DoesNotRenameNonMatchingFiles()
		{
			CloneFolder.Clone(SrcFolder, DstFolder);
			CloneFolder.RenameMatchingAssets(DstFolder, SourceFolderName, "ClonedFolder");

			// Assets whose names do not contain the folder name must be unchanged
			RequireAsset(DstFolder + "/Unrelated.asset", "Non-matching asset should still exist unchanged");
			RequireAsset(DstFolder + "/AssetA.asset",    "AssetA should still exist unchanged");
			RequireAsset(DstFolder + "/AssetB.asset",    "AssetB should still exist unchanged");
		}
	}
}
