using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace GuiToolkit.Editor
{
	[CustomEditor(typeof(UiPrefabConfig))]
	public class UiPrefabConfigEditor : UnityEditor.Editor
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
					m_prefabProperties.Add(property.Copy());
			});
		}

		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();

			if (IsAnyPrefabInternal())
			{
				EditorGUILayout.Space();
				EditorGUILayout.LabelField("Prefab Variants", EditorStyles.boldLabel);
				EditorGUILayout.HelpBox(
					"Some entries still point at the toolkit's built-in prefabs. Create project-local " +
					"variants to override their look without touching the package.",
					MessageType.Info);
				m_clonePath = EditorFileUtility.PathFieldReadFolder("Prefab Path", m_clonePath);
				if (GUILayout.Button("Create Default Prefabs Variants"))
					CloneDefaultPrefabs();
			}

			serializedObject.ApplyModifiedProperties();
		}

		private bool IsAnyPrefabInternal()
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
			foreach (var property in m_prefabProperties)
			{
				var prop = property;
				if (EditorAssetUtility.IsPackagesOrInternalAsset(prop.objectReferenceValue))
					Clone(ref prop);
			}
			serializedObject.ApplyModifiedProperties();
			AssetDatabase.SaveAssets();
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
