using System;
using System.IO;
using GuiToolkit.Style;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	public class UiToolkitConfigurationWindow : EditorWindow, IEditorAware
	{
		[SerializeField]
		private UiToolkitConfiguration m_settings;

		private SerializedObject m_serializedSettingsObject;
		private Vector2 scrollPos;

		private bool m_firstTimeInit = false;

		private void OnGUI()
		{
			if (!AssetReadyGate.Ready)
				GUIUtility.ExitGUI();
			
			if (!AssetReadyGate.ScriptableObjectExists<UiToolkitConfiguration>())
			{
				m_firstTimeInit = true;
				// Calling Instance for the first time automatically creates the asset
				_ = UiToolkitConfiguration.Instance;
			}

			if (m_firstTimeInit)
			{
				EditorGUILayout.HelpBox(UiToolkitConfiguration.HELP_FIRST_TIME, MessageType.Info);
				GUILayout.Space(EditorUiUtility.LARGE_SPACE_HEIGHT);
			}

			SerializedObject serializedObject = new SerializedObject(this);
			SerializedProperty settingsProp = serializedObject.FindProperty("m_settings");

			UiToolkitConfiguration thisSettings = settingsProp.objectReferenceValue as UiToolkitConfiguration;
			if (thisSettings == null)
			{
				thisSettings = UiToolkitConfiguration.Instance;
				settingsProp.objectReferenceValue = thisSettings;
			}

			serializedObject.ApplyModifiedProperties();
			m_serializedSettingsObject = new SerializedObject(thisSettings);

			GUILayout.BeginVertical();
			scrollPos = GUILayout.BeginScrollView(scrollPos);

			if (m_firstTimeInit)
			{
				EditorGUILayout.HelpBox(UiToolkitConfiguration.HELP_SCENES, MessageType.Info);
			}
			EditorGUILayout.PropertyField(m_serializedSettingsObject.FindProperty("m_sceneReferences"), true);

			if (m_firstTimeInit)
			{
				GUILayout.Space(EditorUiUtility.LARGE_SPACE_HEIGHT);
				EditorGUILayout.HelpBox(UiToolkitConfiguration.HELP_LOAD_VIEW_IN_EVERY_SCENE, MessageType.Info);
			}
			
			var loadViewInEveryScene = m_serializedSettingsObject.FindProperty("m_loadViewInEveryScene");
			EditorGUILayout.PropertyField(loadViewInEveryScene, true);
			if (loadViewInEveryScene.boolValue)
			{
				EditorGUI.indentLevel++;
				if (m_firstTimeInit)
					EditorGUILayout.HelpBox(UiToolkitConfiguration.HELP_LOAD_VIEW_IN_EVERY_SCENE_EXCEPT_UI_MAIN_EXISTS, MessageType.Info);
				EditorGUILayout.PropertyField(m_serializedSettingsObject.FindProperty("m_exceptUiMainExists"), true);
				if (m_firstTimeInit)
					EditorGUILayout.HelpBox(UiToolkitConfiguration.HELP_LOAD_VIEW_IN_EVERY_SCENE_UI_MAIN_PREFAB, MessageType.Info);
				EditorGUILayout.PropertyField(m_serializedSettingsObject.FindProperty("m_uiMainPrefab"), true);
				if (m_firstTimeInit)
					EditorGUILayout.HelpBox(UiToolkitConfiguration.HELP_LOAD_VIEW_IN_EVERY_SCENE_UI_VIEW_PREFAB, MessageType.Info);
				EditorGUILayout.PropertyField(m_serializedSettingsObject.FindProperty("m_uiViewPrefab"), true);
				EditorGUI.indentLevel--;
			}

			if (m_firstTimeInit)
			{
				GUILayout.Space(EditorUiUtility.LARGE_SPACE_HEIGHT);
				EditorGUILayout.HelpBox(UiToolkitConfiguration.HELP_ADDITIONAL_SCENES_PATH, MessageType.Info);
			}
			EditorFileUtility.PathFieldReadFolder(m_serializedSettingsObject.FindProperty("m_additionalScenesPath"));

			if (m_firstTimeInit)
			{
				GUILayout.Space(EditorUiUtility.LARGE_SPACE_HEIGHT);
				EditorGUILayout.HelpBox(UiToolkitConfiguration.HELP_PREFAB_VARIANTS_PATH, MessageType.Info);
			}
			EditorFileUtility.PathFieldReadFolder(m_serializedSettingsObject.FindProperty("m_prefabVariantsPath"));

			
			if (m_firstTimeInit)
			{
				GUILayout.Space(EditorUiUtility.LARGE_SPACE_HEIGHT);
				EditorGUILayout.HelpBox(UiToolkitConfiguration.HELP_STYLE_CONFIG, MessageType.Info);
			}

			HandleStyleConfig<UiStyleConfig>("m_uiMainStyleConfig", "UiMainStyleConfig");
			HandleStyleConfig<UiAspectRatioDependentStyleConfig>("m_uiAspectRatioDependentStyleConfig", "UiAspectRatioDependentStyleConfig");

			if (m_firstTimeInit)
			{
				GUILayout.Space(EditorUiUtility.LARGE_SPACE_HEIGHT);
				EditorGUILayout.HelpBox(UiToolkitConfiguration.HELP_POT_PATH, MessageType.Info);
			}
			EditorGUILayout.PropertyField(m_serializedSettingsObject.FindProperty("m_potPath"), true);
			EditorGUILayout.PropertyField(m_serializedSettingsObject.FindProperty("m_newKeyHighlightColor"), true);

			GUILayout.Space(EditorUiUtility.LARGE_SPACE_HEIGHT);
			if (m_firstTimeInit)
				EditorGUILayout.HelpBox(UiToolkitConfiguration.HELP_AUTO_MERGE_POT_TO_PO, MessageType.Info);
			EditorGUILayout.PropertyField(m_serializedSettingsObject.FindProperty("m_autoMergePotToPo"), true);
			if (m_firstTimeInit)
				EditorGUILayout.HelpBox(UiToolkitConfiguration.HELP_AUTO_SYNC_AFTER_MERGE, MessageType.Info);
			EditorGUILayout.PropertyField(m_serializedSettingsObject.FindProperty("m_autoSyncAfterMerge"), true);
			if (m_firstTimeInit)
				EditorGUILayout.HelpBox(UiToolkitConfiguration.HELP_AUTO_PULL_FROM_SHEETS_ON_BUILD, MessageType.Info);
			EditorGUILayout.PropertyField(m_serializedSettingsObject.FindProperty("m_autoPullFromSheetsOnBuild"), true);

			if (m_firstTimeInit)
			{
				GUILayout.Space(EditorUiUtility.LARGE_SPACE_HEIGHT);
				EditorGUILayout.HelpBox(UiToolkitConfiguration.HELP_GENERATED_ASSETS_DIR, MessageType.Info);
			}
			EditorGUILayout.PropertyField(m_serializedSettingsObject.FindProperty("m_generatedAssetsDir"), true);

			GUILayout.Space(EditorUiUtility.LARGE_SPACE_HEIGHT);
			EditorGUILayout.PropertyField(m_serializedSettingsObject.FindProperty("m_debugLoca"));
			EditorGUILayout.PropertyField(m_serializedSettingsObject.FindProperty("m_debugLocaLength"));
			EditorGUILayout.PropertyField(m_serializedSettingsObject.FindProperty("m_debugForceRtl"));
			EditorGUILayout.PropertyField(m_serializedSettingsObject.FindProperty("m_autoTranslateDisabled"));
			EditorGUILayout.PropertyField(m_serializedSettingsObject.FindProperty("m_verboseLogging"));

			GUILayout.Space(EditorUiUtility.LARGE_SPACE_HEIGHT);
			EditorGUILayout.PropertyField(m_serializedSettingsObject.FindProperty("m_languageWhitelistEnabled"), new GUIContent("Language Whitelist", UiToolkitConfiguration.HELP_LANGUAGE_WHITELIST_ENABLED));
			if (m_serializedSettingsObject.FindProperty("m_languageWhitelistEnabled").boolValue)
			{
				EditorGUILayout.HelpBox(UiToolkitConfiguration.HELP_LANGUAGE_WHITELIST, MessageType.Info);
				EditorGUILayout.PropertyField(m_serializedSettingsObject.FindProperty("m_languageWhitelist"), new GUIContent("Whitelisted Languages"), true);
			}

			GUILayout.Space(EditorUiUtility.LARGE_SPACE_HEIGHT);
			EditorGUILayout.PropertyField(m_serializedSettingsObject.FindProperty("m_globalCanvasScalerTemplate"));
			EditorGUILayout.PropertyField(m_serializedSettingsObject.FindProperty("m_transitionOverlay"));
			
			GUILayout.Space(EditorUiUtility.LARGE_SPACE_HEIGHT);
			if (m_firstTimeInit)
				EditorGUILayout.HelpBox(UiToolkitConfiguration.HELP_UI_SOUND_CONFIG, MessageType.Info);
			EditorGUILayout.PropertyField(m_serializedSettingsObject.FindProperty("m_uiSoundConfig"));

			GUILayout.Space(EditorUiUtility.LARGE_SPACE_HEIGHT);
			if (m_firstTimeInit)
				EditorGUILayout.HelpBox(UiToolkitConfiguration.HELP_STANDARD_ELEMENT_REGISTRY, MessageType.Info);
			using (new EditorGUI.DisabledScope(true))
				EditorGUILayout.PropertyField(m_serializedSettingsObject.FindProperty("m_standardElementRegistry"),
					new GUIContent("Standard Element Registry", UiToolkitConfiguration.HELP_STANDARD_ELEMENT_REGISTRY));

			GUILayout.Space(EditorUiUtility.LARGE_SPACE_HEIGHT);
			EditorGUILayout.PropertyField(m_serializedSettingsObject.FindProperty("m_assetProviderFactories"));
			EditorGUILayout.PropertyField(m_serializedSettingsObject.FindProperty("m_storageFactory"));
			

			m_serializedSettingsObject.ApplyModifiedProperties();

			if (FindObjectOfType<UiMain>() == null)
			{
				GUILayout.Space(EditorUiUtility.LARGE_SPACE_HEIGHT);
				if (m_firstTimeInit)
					EditorGUILayout.HelpBox(UiToolkitConfiguration.HELP_UI_MAIN, MessageType.Info);

				if (GUILayout.Button(new GUIContent("Create UiMain in active scene", UiToolkitConfiguration.HELP_UI_MAIN)))
				{
					string[] guids = AssetDatabase.FindAssets("UiMain t:prefab");
					foreach (string guid in guids)
					{
						GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
						if (go == null)
							continue;

						if (go.GetComponent<UiMain>() == null)
							continue;

						PrefabUtility.InstantiatePrefab(go);
						break;
					}
				}
			}

			GUILayout.EndScrollView ();
			GUILayout.EndVertical();
		}

		private void HandleStyleConfig<T>(string _memberName, string _name) where T : UiStyleConfig
		{
			var styleConfigProp = m_serializedSettingsObject.FindProperty(_memberName);
			var currentStyleConfig = styleConfigProp.objectReferenceValue as T;
			if (currentStyleConfig == null)
			{
				currentStyleConfig = FindStyleConfig<T>(_name);
				styleConfigProp.objectReferenceValue = currentStyleConfig;
			}

			bool isDefault = IsDefaultConfig(currentStyleConfig);
			if (isDefault)
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.PropertyField(styleConfigProp);
				if (GUILayout.Button("Clone", GUILayout.Width(100)))
					CloneStyleConfig(ref currentStyleConfig, _memberName, _name);
				EditorGUILayout.EndHorizontal();
				return;
			}

			EditorGUILayout.PropertyField(styleConfigProp);
			DrawStyleConfigParent(currentStyleConfig);
		}

		/// <summary>
		/// Which config the project's own style config builds on.
		///
		/// It belongs in this window because this is where a project's styling is set up, and where "Clone"
		/// used to be the only answer to "I need my own". A clone carries a full copy of everything and stops
		/// following the package the moment it is made; naming a parent stores only what actually differs.
		///
		/// Not offered for the config that ships inside the package: that one is the root of every chain, and
		/// writing to it is dropped on save anyway.
		/// </summary>
		private void DrawStyleConfigParent( UiStyleConfig _config )
		{
			if (_config == null)
				return;

			// Per repaint, like the SerializedObject for this window itself - the window is not a hot path,
			// and a cached one would have to be invalidated whenever the config field above changes.
			var serializedConfig = new SerializedObject(_config);
			var parentProp = serializedConfig.FindProperty("m_parent");
			if (parentProp == null)
				return;

			EditorGUI.indentLevel++;

			EditorGUI.BeginChangeCheck();
			EditorGUILayout.PropertyField
			(
				parentProp,
				new GUIContent
				(
					"Inherits from",
					"Another style config this one builds on. Only what is overridden here has to be stored;\n"
					+ "everything else is resolved through that config, matched by skin name and style name.\n"
					+ "Leave empty for a config that stands alone."
				)
			);

			if (EditorGUI.EndChangeCheck())
			{
				if (parentProp.objectReferenceValue == _config)
				{
					UiLog.LogError($"A style config cannot inherit from itself ('{_config.name}').");
					parentProp.objectReferenceValue = null;
				}

				serializedConfig.ApplyModifiedProperties();
				UiEventDefinitions.EvSkinChanged.InvokeAlways(0);
			}

			var parent = parentProp.objectReferenceValue as UiStyleConfig;
			EditorGUILayout.LabelField
			(
				parent == null
					? "Stands alone - every style it uses has to exist in it."
					: $"Only what differs from '{parent.name}' is stored here; the rest follows that config.",
				EditorStyles.miniLabel
			);

			EditorGUI.indentLevel--;
		}

		private void CloneStyleConfig<T>(ref T currentStyleConfig, string _memberName, string _name) where T : UiStyleConfig
		{
			string resourceDir = "Assets/Resources";
			EditorFileUtility.EnsureUnityFolderExists(resourceDir);
			var newConfigPath = $"{resourceDir}/{_name}.asset";
			if (File.Exists(EditorFileUtility.GetNativePath(newConfigPath)))
				if (!EditorUtility.DisplayDialog("Overwrite Configuration?", $"A config file at '{newConfigPath}' already exists. Should it be overwritten? (Not undoable)", "OK", "Cancel"))
					return;

			currentStyleConfig = Instantiate(currentStyleConfig);
			AssetDatabase.CreateAsset(currentStyleConfig, newConfigPath);

			// Instantiate() copies the skins' and styles' references back to the config they belong to, and
			// those still name the ORIGINAL — so without this the clone's styles believe they live in the
			// package asset, and the editor's cross-style synchronisation reacts to the wrong document.
			AiSupport.UiStyleWriter.RepairBackReferences(currentStyleConfig);

			var styleConfigProp = m_serializedSettingsObject.FindProperty(_memberName);
			styleConfigProp.objectReferenceValue = currentStyleConfig;
			m_serializedSettingsObject.ApplyModifiedProperties();
			AssetDatabase.SaveAssets();
		}

		private bool IsDefaultConfig<T>(T currentStyleConfig) where T : UiStyleConfig
		{
			if (currentStyleConfig == null)
				return false;

			var path = AssetDatabase.GetAssetPath(currentStyleConfig);
			return path.StartsWith(UiToolkitConfiguration.Instance.GetUiToolkitRootProjectDir(), StringComparison.Ordinal);
		}


		private T FindStyleConfig<T>(string _searchString) where T:UiStyleConfig
		{
			return EditorAssetUtility.FindScriptableObject<T>(
				new EditorAssetUtility.AssetSearchOptions()
				{
					Folders = new []{"Assets", "Packages"},
					SearchString = _searchString,
				});
		}

		[MenuItem(StringConstants.CONFIGURATION_MENU_NAME, priority = Constants.SETTINGS_MENU_PRIORITY)]
		public static UiToolkitConfigurationWindow GetWindow()
		{
			var window = GetWindow<UiToolkitConfigurationWindow>();
			window.titleContent = new GUIContent(StringConstants.CONFIGURATION_NAME);
			window.Focus();
			window.Repaint();
			return window;
		}
	}
}