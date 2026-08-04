using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace GuiToolkit.Editor
{
	[EditorAware]
	[CustomEditor(typeof(UiMain))]
	public class UiMainEditor : UnityEditor.Editor
	{
		private const string DefaultClonePath = "Assets/Prefabs/ui-toolkit-variants";

		/// <summary>Name suffix by which the fallback prefab fields on UiMain are discovered.</summary>
		private const string FallbackSuffix = "PrefabFallback";

		private const string FoldoutPrefKey = "GuiToolkit.UiMainEditor.PrefabFallbacksFoldout";

		private const string FallbackHelp =
			"Not needed in a properly set-up project. Standard elements are resolved through the generated "
			+ "UiStandardElementRegistry, which prefers this project's prefab variants — these fields are "
			+ "read ONLY when the registry has no entry for that element.\n\n"
			+ "Assign something here to satisfy a legacy look the registry cannot express, as in a project "
			+ "whose live prefabs predate it. Note that the registry still wins where it has an entry, so a "
			+ "value set here can look like it is being ignored — because it is.";

		private readonly List<SerializedProperty> m_prefabProperties = new();
		private string[] m_prefabPropertyNames = System.Array.Empty<string>();
		private string m_clonePath = DefaultClonePath;
		private bool m_clonePathInitialized;

		private void OnEnable()
		{
			m_prefabProperties.Clear();
			EditorGeneralUtility.ForeachProperty(serializedObject, property =>
			{
				if (property.name.EndsWith(FallbackSuffix))
					m_prefabProperties.Add(property);
			});

			m_prefabPropertyNames = new string[m_prefabProperties.Count];
			for (int i = 0; i < m_prefabProperties.Count; i++)
			{
				m_prefabPropertyNames[i] = m_prefabProperties[i].name;
			}
		}

		private void FindProperty(ref SerializedProperty _property, string _name)
		{
			_property = serializedObject.FindProperty(_name);
			m_prefabProperties.Add(_property);
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			// Everything EXCEPT the fallback fields, which get their own foldout below. Drawing the
			// default inspector here would show them twice.
			DrawPropertiesExcluding(serializedObject, m_prefabPropertyNames);

			EditorGUILayout.Space();

			bool foldout = EditorPrefs.GetBool(FoldoutPrefKey, false);
			bool newFoldout = EditorGUILayout.Foldout(foldout, "Prefab Fallbacks", true);
			if (newFoldout != foldout)
				EditorPrefs.SetBool(FoldoutPrefKey, newFoldout);

			if (newFoldout)
			{
				EditorGUILayout.HelpBox(FallbackHelp, MessageType.Info);

				foreach (var property in m_prefabProperties)
				{
					EditorGUILayout.PropertyField(property);
				}

				DrawCloneTools();
			}

			serializedObject.ApplyModifiedProperties();
		}

		private void DrawCloneTools()
		{
			if (IsAnyPrefabCloned())
			{
				// Default the clone target to the project's canonical prefab-variants folder (what the
				// catalog generator scans), so freshly created variants are discovered automatically.
				// Initialized once; the user can still override it via the field below.
				if (!m_clonePathInitialized)
				{
					string configPath = UiToolkitConfiguration.Instance.PrefabVariantsPath;
					if (!string.IsNullOrEmpty(configPath))
						m_clonePath = configPath;
					m_clonePathInitialized = true;
				}

				m_clonePath = EditorFileUtility.PathFieldReadFolder("Prefab Path", m_clonePath);
				if (GUILayout.Button("Create Default Prefabs Variants"))
					CloneDefaultPrefabs();
			}
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
