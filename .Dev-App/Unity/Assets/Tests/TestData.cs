using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace GuiToolkit.Test
{
	[CreateAssetMenu(fileName = "TestData", menuName = StringConstants.CREATE_TEST_DATA)]
	public class TestData : AbstractSingletonScriptableObject<TestData>
	{
		[Header("Basic")]
		[PathField(_isFolder: true, _relativeToPath: ".")]
		[SerializeField] private PathField m_TempFolderPath;
		public PathField TempFolderPath => m_TempFolderPath;

		public void ClearTempFolder()
		{
			var guids = AssetDatabase.FindAssets(string.Empty, new[] {TempFolderPath.ToString()});
			foreach (var guid in guids)
			{
				var path = AssetDatabase.GUIDToAssetPath(guid);
				AssetDatabase.DeleteAsset(path);
			}
		}


		[Header("EditorAssetUtility")]
		[PathField(_isFolder: true, _relativeToPath: ".")]
		[SerializeField] private PathField[] m_sortByPrefabHierarchyPaths = Array.Empty<PathField>();
		public PathField[] SortByPrefabHierarchyPaths => m_sortByPrefabHierarchyPaths;


		[Header("PrefabInfo • Prefab Assets")]
		[PathField(_isFolder: false, _relativeToPath: ".", Extensions = "prefab")]
		[SerializeField] private PathField m_regularPrefabAsset;
		public PathField RegularPrefabAsset => m_regularPrefabAsset;

		[PathField(_isFolder: false, _relativeToPath: ".", Extensions = "prefab")]
		[SerializeField] private PathField m_variantPrefabAssetWithoutOverrides;
		public PathField VariantPrefabAssetWithoutOverrides => m_variantPrefabAssetWithoutOverrides;

		[PathField(_isFolder: false, _relativeToPath: ".", Extensions = "prefab")]
		[SerializeField] private PathField m_variantPrefabAssetWithOverrides;
		public PathField VariantPrefabAssetWithOverrides => m_variantPrefabAssetWithoutOverrides;

		[PathField(_isFolder: false, _relativeToPath: ".", Extensions = "fbx")]
		[SerializeField] private PathField m_modelPrefabAsset;
		public PathField ModelPrefabAsset => m_modelPrefabAsset;

		
		[Header("PrefabInfo • Nested Asset Instances")]
		
		[PathField(_isFolder: false, _relativeToPath: ".", Extensions = "prefab")]
		[SerializeField] private PathField m_nestedPrefabWithRegularInstance;
		public PathField NestedPrefabWithRegularInstance => m_nestedPrefabWithRegularInstance;
		
		[PathField(_isFolder: false, _relativeToPath: ".", Extensions = "prefab")]
		[SerializeField] private PathField m_nestedPrefabWithVariantInstance;
		public PathField NestedPrefabWithVariantInstance => m_nestedPrefabWithVariantInstance;


		[Header("PrefabInfo • Scene Instances")]

		[PathField(_isFolder: false, _relativeToPath: ".", Extensions = "unity")]
		[SerializeField] private PathField m_regularPrefabScene;
		public PathField RegularPrefabScene => m_regularPrefabScene;

		[PathField(_isFolder: false, _relativeToPath: ".", Extensions = "unity")]
		[SerializeField] private PathField m_variantPrefabScene;
		public PathField VariantPrefabScene => m_variantPrefabScene;

		[PathField(_isFolder: false, _relativeToPath: ".", Extensions = "unity")]
		[SerializeField] private PathField m_disconnectedPrefabScene;
		public PathField DisconnectedPrefabScene => m_disconnectedPrefabScene;

		[PathField(_isFolder: false, _relativeToPath: ".", Extensions = "unity")]
		[SerializeField] private PathField m_overriddenPrefabScene;
		public PathField OverriddenPrefabScene => m_overriddenPrefabScene;


		[Header("PrefabInfo • Non-Prefab Object")]

		[PathField(_isFolder: false, _relativeToPath: ".", Extensions = "unity")]
		[SerializeField] private PathField m_nonPrefabScene;
		public PathField NonPrefabScene => m_nonPrefabScene;
	}
}
