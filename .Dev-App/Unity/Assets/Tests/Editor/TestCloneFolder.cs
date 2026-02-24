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
	///   ExternalAsset.asset  (sibling of SourceFolder – outside it)
	///
	/// After Clone(SourceFolder, DestFolder):
	///   - DestFolder/AssetA and DestFolder/AssetB must exist
	///   - DestFolder/AssetA.InternalRef must point to DestFolder/AssetB (rewired)
	///   - DestFolder/AssetA.ExternalRef must still point to ExternalAsset (kept)
	/// </summary>
	public class TestCloneFolder
	{
		// All temp assets live under this folder so TearDown can delete them in one call.
		private const string TempRoot  = "Assets/Tests/TestObjects/Temp/CloneFolderTest";
		private const string SrcFolder = TempRoot + "/SourceFolder";
		private const string DstFolder = TempRoot + "/ClonedFolder";
		private const string ExternalAssetPath = TempRoot + "/ExternalAsset.asset";
		private const string AssetAPath = SrcFolder + "/AssetA.asset";
		private const string AssetBPath = SrcFolder + "/AssetB.asset";

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

			// AssetB – just a plain scriptable object (no references)
			var assetB = ScriptableObject.CreateInstance<CloneFolderTestAsset>();
			AssetDatabase.CreateAsset(assetB, AssetBPath);

			// ExternalAsset – lives OUTSIDE the source folder
			EditorFileUtility.EnsureUnityFolderExists(TempRoot);
			var external = ScriptableObject.CreateInstance<CloneFolderTestAsset>();
			AssetDatabase.CreateAsset(external, ExternalAssetPath);

			// AssetA – references both AssetB (internal) and ExternalAsset (external)
			var assetA = ScriptableObject.CreateInstance<CloneFolderTestAsset>();
			AssetDatabase.CreateAsset(assetA, AssetAPath);

			// Wire references via SerializedObject so they are persisted to disk
			var so = new SerializedObject(assetA);
			so.FindProperty(nameof(CloneFolderTestAsset.InternalRef)).objectReferenceValue  = assetB;
			so.FindProperty(nameof(CloneFolderTestAsset.ExternalRef)).objectReferenceValue  = external;
			so.ApplyModifiedPropertiesWithoutUndo();

			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}

		[TearDown]
		public void TearDown()
		{
			if (AssetDatabase.IsValidFolder(TempRoot))
				AssetDatabase.DeleteAsset(TempRoot);

			AssetDatabase.Refresh();
		}

		// -------------------------------------------------------------------------
		// Tests
		// -------------------------------------------------------------------------

		[Test]
		public void Clone_CreatesFolderAndAssets()
		{
			bool success = CloneFolder.Clone(SrcFolder, DstFolder);

			Assert.IsTrue(success, "Clone() should return true");
			Assert.IsTrue(AssetDatabase.IsValidFolder(DstFolder), "Destination folder should exist");

			string cloneA = DstFolder + "/AssetA.asset";
			string cloneB = DstFolder + "/AssetB.asset";
			Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<CloneFolderTestAsset>(cloneA), "Cloned AssetA should exist");
			Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<CloneFolderTestAsset>(cloneB), "Cloned AssetB should exist");
		}

		[Test]
		public void Clone_InternalReferenceIsRewired()
		{
			CloneFolder.Clone(SrcFolder, DstFolder);

			var cloneA = AssetDatabase.LoadAssetAtPath<CloneFolderTestAsset>(DstFolder + "/AssetA.asset");
			var cloneB = AssetDatabase.LoadAssetAtPath<CloneFolderTestAsset>(DstFolder + "/AssetB.asset");
			var origB  = AssetDatabase.LoadAssetAtPath<CloneFolderTestAsset>(AssetBPath);

			Assert.IsNotNull(cloneA, "Cloned AssetA must exist");
			Assert.IsNotNull(cloneB, "Cloned AssetB must exist");
			Assert.AreEqual(cloneB, cloneA.InternalRef,
				"Internal reference should point to the cloned AssetB, not the original");
			Assert.AreNotEqual(origB, cloneA.InternalRef,
				"Internal reference must NOT point to the original AssetB");
		}

		[Test]
		public void Clone_ExternalReferenceIsPreserved()
		{
			CloneFolder.Clone(SrcFolder, DstFolder);

			var cloneA   = AssetDatabase.LoadAssetAtPath<CloneFolderTestAsset>(DstFolder + "/AssetA.asset");
			var external = AssetDatabase.LoadAssetAtPath<CloneFolderTestAsset>(ExternalAssetPath);

			Assert.IsNotNull(cloneA, "Cloned AssetA must exist");
			Assert.AreEqual(external, cloneA.ExternalRef,
				"External reference should still point to the original ExternalAsset");
		}

		[Test]
		public void Clone_OriginalAssetsAreUnchanged()
		{
			CloneFolder.Clone(SrcFolder, DstFolder);

			var origA    = AssetDatabase.LoadAssetAtPath<CloneFolderTestAsset>(AssetAPath);
			var origB    = AssetDatabase.LoadAssetAtPath<CloneFolderTestAsset>(AssetBPath);
			var external = AssetDatabase.LoadAssetAtPath<CloneFolderTestAsset>(ExternalAssetPath);

			Assert.AreEqual(origB,    origA.InternalRef, "Original AssetA.InternalRef should still point to original AssetB");
			Assert.AreEqual(external, origA.ExternalRef, "Original AssetA.ExternalRef should still point to ExternalAsset");
		}

		[Test]
		public void Clone_DestinationAssignedNewGuids()
		{
			CloneFolder.Clone(SrcFolder, DstFolder);

			string guidOrigA  = AssetDatabase.AssetPathToGUID(AssetAPath);
			string guidOrigB  = AssetDatabase.AssetPathToGUID(AssetBPath);
			string guidCloneA = AssetDatabase.AssetPathToGUID(DstFolder + "/AssetA.asset");
			string guidCloneB = AssetDatabase.AssetPathToGUID(DstFolder + "/AssetB.asset");

			Assert.AreNotEqual(guidOrigA, guidCloneA, "Cloned AssetA should have a new GUID");
			Assert.AreNotEqual(guidOrigB, guidCloneB, "Cloned AssetB should have a new GUID");
		}
	}

	/// <summary>
	/// Minimal ScriptableObject used as a test asset for <see cref="TestCloneFolder"/>.
	/// </summary>
	public class CloneFolderTestAsset : ScriptableObject
	{
		public CloneFolderTestAsset InternalRef;
		public Object               ExternalRef;
	}
}
