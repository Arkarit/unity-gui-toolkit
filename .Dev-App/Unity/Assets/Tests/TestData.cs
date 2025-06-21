using System;
using UnityEngine;

namespace GuiToolkit.Test
{
	[CreateAssetMenu(fileName = "TestData", menuName = StringConstants.CREATE_TEST_DATA)]
	public class TestData : AbstractSingletonScriptableObject<TestData>
	{
		// ------------------------------------------------------------------
		// Existing data for SortByPrefabHierarchy
		// ------------------------------------------------------------------

		[Header("EditorAssetUtility")]
		[PathField(_isFolder: true, _relativeToPath: ".")]
		[SerializeField] private PathField[] m_sortByPrefabHierarchyPaths = Array.Empty<PathField>();
		public PathField[] SortByPrefabHierarchyPaths => m_sortByPrefabHierarchyPaths;

		// ------------------------------------------------------------------
		// PrefabInfo – Prefab assets
		// ------------------------------------------------------------------

		[Header("PrefabInfo • Prefab Assets")]

		/// <summary>Regular prefab asset (A1).</summary>
		[PathField(_isFolder: false, _relativeToPath: ".", Extensions = "prefab")]
		[SerializeField] private PathField m_regularPrefabAsset;
		public PathField RegularPrefabAsset => m_regularPrefabAsset;

		/// <summary>Variant prefab asset (A2).</summary>
		[PathField(_isFolder: false, _relativeToPath: ".", Extensions = "prefab")]
		[SerializeField] private PathField m_variantPrefabAsset;
		public PathField VariantPrefabAsset => m_variantPrefabAsset;

		/// <summary>Model prefab asset, e.g. FBX (A3).</summary>
		[PathField(_isFolder: false, _relativeToPath: ".", Extensions = "prefab")]
		[SerializeField] private PathField m_modelPrefabAsset;
		public PathField ModelPrefabAsset => m_modelPrefabAsset;

		// ------------------------------------------------------------------
		// PrefabInfo – Scene-based test cases
		// ------------------------------------------------------------------

		[Header("PrefabInfo • Scene Instances")]

		/// <summary>Scene containing a regular prefab instance (B1).</summary>
		[PathField(_isFolder: false, _relativeToPath: ".", Extensions = "unity")]
		[SerializeField] private PathField m_regularPrefabScene;
		public PathField RegularPrefabScene => m_regularPrefabScene;

		/// <summary>Scene containing a variant prefab instance (B2).</summary>
		[PathField(_isFolder: false, _relativeToPath: ".", Extensions = "unity")]
		[SerializeField] private PathField m_variantPrefabScene;
		public PathField VariantPrefabScene => m_variantPrefabScene;

		/// <summary>Scene with a disconnected prefab instance (B3).</summary>
		[PathField(_isFolder: false, _relativeToPath: ".", Extensions = "unity")]
		[SerializeField] private PathField m_disconnectedPrefabScene;
		public PathField DisconnectedPrefabScene => m_disconnectedPrefabScene;

		/// <summary>Scene with a prefab instance that has overrides (B4).</summary>
		[PathField(_isFolder: false, _relativeToPath: ".", Extensions = "unity")]
		[SerializeField] private PathField m_overriddenPrefabScene;
		public PathField OverriddenPrefabScene => m_overriddenPrefabScene;

		// ------------------------------------------------------------------
		// PrefabInfo – Non-prefab reference
		// ------------------------------------------------------------------

		[Header("PrefabInfo • Non-Prefab Object")]

		/// <summary>Scene containing a plain GameObject with no prefab linkage (C1).</summary>
		[PathField(_isFolder: false, _relativeToPath: ".", Extensions = "unity")]
		[SerializeField] private PathField m_nonPrefabScene;
		public PathField NonPrefabScene => m_nonPrefabScene;
	}
}
