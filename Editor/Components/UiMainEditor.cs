using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace GuiToolkit.Editor
{
	[CustomEditor(typeof(UiMain))]
	public class UiMainEditor : UnityEditor.Editor
	{
		private const string DefaultConfigPath = "Assets/Prefabs/ui-toolkit-variants";

		private readonly List<SerializedProperty> m_inlinePrefabProperties = new();
		private string[] m_inlinePrefabNames = System.Array.Empty<string>();
		private SerializedProperty m_prefabConfigProperty;
		private string m_configPath = DefaultConfigPath;

		private void OnEnable()
		{
			m_inlinePrefabProperties.Clear();
			m_prefabConfigProperty = serializedObject.FindProperty("m_prefabConfig");
			var names = new List<string>();
			EditorGeneralUtility.ForeachProperty(serializedObject, property =>
			{
				if (property.name.EndsWith("Prefab"))
				{
					m_inlinePrefabProperties.Add(property.Copy());
					names.Add(property.name);
				}
			});
			m_inlinePrefabNames = names.ToArray();
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			// Everything except the deprecated inline prefab fields (drawn conditionally below).
			DrawPropertiesExcluding(serializedObject, m_inlinePrefabNames);

			var assigned = m_inlinePrefabProperties.FindAll(p => p != null && p.objectReferenceValue != null);
			if (assigned.Count > 0)
			{
				// Only shown while inline fields still hold values; hidden entirely once migrated/empty.
				EditorGUILayout.Space();
				EditorGUILayout.LabelField("Prefabs (DEPRECATED — migrate to UiPrefabConfig)", EditorStyles.boldLabel);
				foreach (var property in assigned)
					EditorGUILayout.PropertyField(property, true);

				EditorGUILayout.Space();
				EditorGUILayout.HelpBox(
					"The inline prefab fields are deprecated. Migrate them into a UiPrefabConfig asset — " +
					"UiMain then sources its built-in prefabs (and their variants) from that config.",
					MessageType.Warning);

				m_configPath = EditorFileUtility.PathFieldReadFolder("Config Path", m_configPath);
				if (GUILayout.Button("Migrate Prefabs to UiPrefabConfig"))
					MigrateToConfig();
			}

			serializedObject.ApplyModifiedProperties();
		}

		private void MigrateToConfig()
		{
			// Use the already-assigned config, or create a fresh asset.
			var config = m_prefabConfigProperty.objectReferenceValue as UiPrefabConfig;
			if (config == null)
			{
				EditorFileUtility.EnsureUnityFolderExists(m_configPath);
				string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{m_configPath}/UiPrefabConfig.asset");
				config = ScriptableObject.CreateInstance<UiPrefabConfig>();
				AssetDatabase.CreateAsset(config, assetPath);
			}

			// Copy each inline value into the config, but only where the config entry is still empty,
			// so an already-curated config (e.g. with variants) is never clobbered.
			var configSo = new SerializedObject(config);
			foreach (var inline in m_inlinePrefabProperties)
			{
				if (inline == null || inline.objectReferenceValue == null)
					continue;
				var target = configSo.FindProperty(inline.name);
				if (target != null && target.objectReferenceValue == null)
					target.objectReferenceValue = inline.objectReferenceValue;
			}
			configSo.ApplyModifiedProperties();
			EditorUtility.SetDirty(config);

			// Point UiMain at the config and clear the (now migrated) inline fields.
			m_prefabConfigProperty.objectReferenceValue = config;
			foreach (var inline in m_inlinePrefabProperties)
				inline.objectReferenceValue = null;

			serializedObject.ApplyModifiedProperties();
			AssetDatabase.SaveAssets();

			EditorGUIUtility.PingObject(config);
			UiLog.LogInternal($"Migrated UiMain prefabs into '{AssetDatabase.GetAssetPath(config)}'. " +
				"Inline fields cleared; UiMain now sources prefabs from the config.");
		}
	}
}
