using System.Collections.Generic;
using System.IO;
using Codice.CM.SEIDInfo;
using UnityEngine;
using UnityEditor;

namespace GuiToolkit.Editor
{
	[CustomEditor(typeof(UiMain))]
	public class UiMainEditor : UnityEditor.Editor
	{
		private const string DefaultClonePath = "Assets/Prefabs/ui-toolkit-variants";
		
		private readonly List<SerializedProperty> m_prefabProperties = new();
		private string m_clonePath = DefaultClonePath;
		
		private void OnEnable()
		{
			m_prefabProperties.Clear();
			EditorGeneralUtility.ForeachProperty(serializedObject, property =>
			{
				if (property.name.EndsWith("Prefab"))
					m_prefabProperties.Add(property);
			});
		}
		
		private void FindProperty(ref SerializedProperty _property, string _name)
		{
			_property = serializedObject.FindProperty(_name);
			m_prefabProperties.Add(_property);
		}

		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();
			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Prefabs", EditorStyles.boldLabel);
			if (IsAnyPrefabCloned())
			{
				m_clonePath = EditorFileUtility.PathFieldReadFolder("Prefab Path", m_clonePath);
				if (GUILayout.Button("Create Default Prefabs Variants"))
					CloneDefaultPrefabs();
//				if (GUILayout.Button("Create Default Prefabs Variants"))
//					EditorPrefabUtility.CreatePrefabVariants(EditorPrefabUtility.BuiltinPrefabDir, UiToolkitConfiguration.Instance.m_prefabVariantsPath);
//				if (GUILayout.Button("Test"))
//					EditorAssetUtility.CreatePrefabVariants
//					(
//						"Assets/External/unity-gui-toolkit/Prefabs/_Test/VariantTest", 
//						"Assets/External/unity-gui-toolkit/Prefabs/_Test/VariantTestCloned", 
//						new EditorAssetUtility.PrefabVariantsOptions()
//						{
//							Postfix = " ClonedVariant",
//						}
//					);
//
			}

			serializedObject.ApplyModifiedProperties();
		}

		private bool IsAnyPrefabCloned()
		{
			foreach (var property in m_prefabProperties)
			{
				if (EditorAssetUtility.IsPackagesOrInternalAsset(property.objectReferenceValue))
					return true;
			}
			
			return false;
		}
		
		private void CloneDefaultPrefabs()
		{
			for (int i=0; i<m_prefabProperties.Count; i++)
			{
				var property = m_prefabProperties[i];
				if (EditorAssetUtility.IsPackagesOrInternalAsset(property.objectReferenceValue))
					Clone(ref property);
			}
		}

		private void Clone(ref SerializedProperty _property)
		{
			var obj = _property.objectReferenceValue;
			if (!obj)
				return;
			
			var component = obj as Component;
			if (!component)
				return;
			
			if (!EditorAssetUtility.IsPackagesOrInternalAsset(component))
				return;
			
			var assetPath = AssetDatabase.GetAssetPath(component);
			if (string.IsNullOrEmpty(assetPath))
				return;
			
			var asset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
			if (!asset)
				return;
			
			var filename = Path.GetFileNameWithoutExtension(assetPath);
			var extension = Path.GetExtension(assetPath);
			var newAssetPath = $"{m_clonePath}/{filename}{extension}";
			var variantName = $"{filename}Variant{extension}";
			var variantPath = $"{m_clonePath}/{variantName}";
			
			if (File.Exists(variantPath))
			{
				var existing = AssetDatabase.LoadAssetAtPath<GameObject>(variantPath);
				_property.objectReferenceValue = existing;
				return;
			}
			
			var prefab = PrefabUtility.InstantiatePrefab(asset) as GameObject;
			if (!prefab)
				return;
			
			EditorFileUtility.EnsureUnityFolderExists(m_clonePath);
			var variant = PrefabUtility.SaveAsPrefabAsset(prefab, newAssetPath);
			
			var componentInClone = EditorGeneralUtility.FindMatchingChildInPrefab(variant, component.gameObject);
			_property.objectReferenceValue = componentInClone;
			
			AssetDatabase.RenameAsset(newAssetPath, variantName);
			
			prefab.SafeDestroy();
		}
	}
}
