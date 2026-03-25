using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace GuiToolkit.Editor
{
	/// <summary>
	/// Converts <see cref="UnityEngine.UI.Text"/> components (and any companion components that
	/// declare <c>[RequireComponent(typeof(Text))]</c>) to <see cref="UiLocalizedTextMeshProUGUI"/>
	/// via direct YAML block replacement. No domain reload required.
	/// <para>
	/// Because the replacement writes the new MonoBehaviour block at the same YAML anchor
	/// (<c>&amp;localFileId</c>), all other serialized references that point to the original Text
	/// component remain valid.
	/// </para>
	/// </summary>
	[EditorAware]
	internal static class LegacyTextToLocalizedTmpConverter
	{
		// -----------------------------------------------------------------------
		// Menu items
		// -----------------------------------------------------------------------

		[MenuItem(StringConstants.LOCA_MISC_CONVERT_LEGACY_TEXT_SCENE_MENU_NAME,
		          priority = Constants.LOCA_MISC_CONVERT_LEGACY_TEXT_SCENE_MENU_PRIORITY)]
		private static void ConvertInCurrentSceneOrPrefab() => RunConversion(entireProject: false);

		[MenuItem(StringConstants.LOCA_MISC_CONVERT_LEGACY_TEXT_PROJECT_MENU_NAME,
		          priority = Constants.LOCA_MISC_CONVERT_LEGACY_TEXT_PROJECT_MENU_PRIORITY)]
		private static void ConvertInProject() => RunConversion(entireProject: true);

		// -----------------------------------------------------------------------
		// Data types
		// -----------------------------------------------------------------------

		/// <summary>
		/// All data captured from a single legacy <see cref="Text"/> component that is needed to
		/// generate the equivalent <see cref="UiLocalizedTextMeshProUGUI"/> YAML block.
		/// </summary>
		internal struct LegacyTextData
		{
			// YAML anchors
			public long ComponentLocalId;
			public long GameObjectLocalId;

			// Text content & style
			public string Text;
			public Color Color;
			public float FontSize;
			public bool RichText;
			public bool AutoSize;
			public float AutoSizeMin;
			public float AutoSizeMax;
			public TextAnchor Alignment;
			public bool RaycastTarget;
			public bool Maskable;
			public bool Enabled;
			public float LineSpacing;
			public FontStyle FontStyle;
			public HorizontalWrapMode HorizontalOverflow;
			public VerticalWrapMode VerticalOverflow;

			// Font YAML references (empty string / 0 when no font found)
			public string FontAssetGuid;
			public long FontAssetLocalId;
			public string MaterialGuid;
			public long MaterialLocalId;
		}

		private struct Stats
		{
			public int textConverted;
			public int textSkippedDueToCompanion;
			public int scriptsUpdated;
			public int filesProcessed;
			public int errors;
		}

		// -----------------------------------------------------------------------
		// Core logic
		// -----------------------------------------------------------------------

		private static void RunConversion(bool entireProject)
		{
			string newGuid = YamlUtility.FindMonoScriptGuid(typeof(UiLocalizedTextMeshProUGUI));
			if (string.IsNullOrEmpty(newGuid))
			{
				EditorUtility.DisplayDialog("Convert Legacy Text",
					"Could not locate the UiLocalizedTextMeshProUGUI MonoScript GUID.\n" +
					"Make sure the UI Toolkit package is imported correctly.", "OK");
				return;
			}

			bool isInPrefabStage = PrefabStageUtility.GetCurrentPrefabStage() != null;
			string currentAssetPath = isInPrefabStage
				? PrefabStageUtility.GetCurrentPrefabStage().assetPath
				: EditorSceneManager.GetActiveScene().path;

			// Collect scope.
			var prefabPaths         = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			var scenePlainTexts     = new List<Text>();

			if (entireProject)
			{
				foreach (string guid in AssetDatabase.FindAssets("t:Prefab"))
					prefabPaths.Add(AssetDatabase.GUIDToAssetPath(guid));

				if (!isInPrefabStage && !string.IsNullOrEmpty(currentAssetPath))
					GatherPlainSceneObjects(scenePlainTexts);
			}
			else
			{
				if (isInPrefabStage)
				{
					prefabPaths.Add(currentAssetPath);
				}
				else
				{
					GatherFromScene(prefabPaths, scenePlainTexts);
				}
			}

			bool hasSceneWork = scenePlainTexts.Count > 0;
			string scope = entireProject
				? "the entire project (all prefabs + plain objects in the current scene)"
				: (isInPrefabStage ? "the currently open prefab" : "the current scene");

			string msg = $"Convert all Legacy Text → UiLocalizedTextMeshProUGUI in {scope}.\n\n" +
			             $"Prefabs to process: {prefabPaths.Count}\n" +
			             $"Plain scene objects: {scenePlainTexts.Count}\n\n" +
			             "Text components that have [RequireComponent(typeof(Text))] companions will be skipped " +
			             "with an error message — fix those manually first.\n" +
			             "Dependent C# scripts with Text field references will be updated automatically.\n" +
			             "This operation cannot be undone.\n\nContinue?";

			if (!EditorUtility.DisplayDialog("Convert Legacy Text", msg, "Convert", "Cancel"))
				return;

			if (!string.IsNullOrEmpty(currentAssetPath))
				YamlUtility.SaveCurrentSceneOrPrefab();

			var stats = new Stats();

			try
			{
				int total = prefabPaths.Count + (hasSceneWork ? 1 : 0);
				int done  = 0;

				foreach (string prefabPath in prefabPaths)
				{
					EditorUtility.DisplayProgressBar(
						"Convert Legacy Text",
						$"Processing {Path.GetFileName(prefabPath)}…",
						(float)done / Math.Max(total, 1));
					done++;

					try
					{
						ProcessPrefabFile(prefabPath, newGuid, ref stats);
					}
					catch (Exception ex)
					{
						Debug.LogError($"[LegacyTextToLocalizedTmpConverter] Error processing {prefabPath}: {ex}");
						stats.errors++;
					}
				}

				if (hasSceneWork)
				{
					EditorUtility.DisplayProgressBar("Convert Legacy Text", "Processing plain scene objects…", 1f);
					try
					{
						ProcessScenePlainObjects(currentAssetPath, scenePlainTexts, newGuid, ref stats);
					}
					catch (Exception ex)
					{
						Debug.LogError($"[LegacyTextToLocalizedTmpConverter] Error processing scene objects: {ex}");
						stats.errors++;
					}
				}
			}
			finally
			{
				EditorUtility.ClearProgressBar();
			}

			ReloadCurrentAsset(isInPrefabStage, currentAssetPath);

			string summary =
				$"Converted:  {stats.textConverted} Legacy Text component(s)\n" +
				$"Skipped:    {stats.textSkippedDueToCompanion} (RequireComponent companions — see Console)\n" +
				$"Scripts:    {stats.scriptsUpdated} C# script(s) updated\n" +
				$"Files:      {stats.filesProcessed} modified";
			if (stats.errors > 0)
				summary += $"\nErrors:     {stats.errors} (see Console for details)";

			EditorUtility.DisplayDialog("Convert Legacy Text – Done", summary, "OK");
		}

		// -----------------------------------------------------------------------
		// Prefab processing
		// -----------------------------------------------------------------------

		private static void ProcessPrefabFile(string assetPath, string scriptGuid, ref Stats stats)
		{
			var prefabContents = PrefabUtility.LoadPrefabContents(assetPath);
			var entries = new List<LegacyTextData>();

			try
			{
				foreach (var text in prefabContents.GetComponentsInChildren<Text>(true))
				{
					if (PrefabUtility.IsPartOfPrefabInstance(text.gameObject))
						continue;

					if (!TryCaptureTextData(text, out var data, out bool companionSkip))
					{
						if (companionSkip)
							stats.textSkippedDueToCompanion++;
						else
							Debug.LogWarning($"[LegacyTextToLocalizedTmpConverter] Could not capture data from " +
							                 $"'{text.gameObject.name}' in {assetPath} — skipped.");
						continue;
					}

					entries.Add(data);
				}
			}
			finally
			{
				// Unload WITHOUT saving — the YAML file stays as-is with the legacy Text blocks.
				PrefabUtility.UnloadPrefabContents(prefabContents);
			}

			if (entries.Count == 0)
				return;

			ApplyYamlConversions(assetPath, entries, scriptGuid, ref stats);
		}

		// -----------------------------------------------------------------------
		// Scene plain-object processing
		// -----------------------------------------------------------------------

		private static void ProcessScenePlainObjects(string scenePath, List<Text> texts,
		                                              string scriptGuid, ref Stats stats)
		{
			var entries = new List<LegacyTextData>();

			foreach (var text in texts)
			{
				if (!TryCaptureTextData(text, out var data, out bool companionSkip))
				{
					if (companionSkip)
						stats.textSkippedDueToCompanion++;
					else
						Debug.LogWarning($"[LegacyTextToLocalizedTmpConverter] Could not capture data from " +
						                 $"'{text.gameObject.name}' in scene — skipped.");
					continue;
				}
				entries.Add(data);
			}

			if (entries.Count == 0)
				return;

			// Save to ensure the scene YAML reflects in-memory state.
			EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

			ApplyYamlConversions(scenePath, entries, scriptGuid, ref stats);
		}

		// -----------------------------------------------------------------------
		// YAML conversion
		// -----------------------------------------------------------------------

		private static void ApplyYamlConversions(string assetPath, IList<LegacyTextData> entries,
		                                          string scriptGuid, ref Stats stats)
		{
			string fullPath = YamlUtility.AssetPathToFullPath(assetPath);
			if (!File.Exists(fullPath))
			{
				Debug.LogWarning($"[LegacyTextToLocalizedTmpConverter] File not found: {fullPath}");
				return;
			}

			string yaml    = File.ReadAllText(fullPath);
			bool   changed = false;

			// Before modifying, scan the YAML for MonoBehaviour blocks that reference any of the
			// Text component IDs we are about to convert. Those scripts need their field types updated.
			var convertedIds = new HashSet<long>();
			foreach (var e in entries)
				convertedIds.Add(e.ComponentLocalId);

			var scriptFieldMap = FindReferencingScriptFields(yaml, convertedIds);

			foreach (var data in entries)
			{
				string newBlock = BuildTmpYamlBlock(data, scriptGuid);
				string patched  = YamlUtility.ReplaceMonoBehaviourBlock(yaml, data.ComponentLocalId, newBlock);

				if (patched == null)
				{
					Debug.LogWarning($"[LegacyTextToLocalizedTmpConverter] Block replacement failed for " +
					                 $"localId={data.ComponentLocalId} in {assetPath}.");
					stats.errors++;
					continue;
				}

				yaml    = patched;
				changed = true;
				stats.textConverted++;
			}

			if (!changed)
				return;

			File.WriteAllText(fullPath, yaml);
			AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
			stats.filesProcessed++;

			// Update dependent C# scripts: change field types from Text → TMP_Text.
			foreach (var kvp in scriptFieldMap)
			{
				if (TryUpdateCSharpScriptFields(kvp.Key, kvp.Value))
					stats.scriptsUpdated++;
			}
		}

		// -----------------------------------------------------------------------
		// Data capture
		// -----------------------------------------------------------------------

		private static bool TryCaptureTextData(Text text, out LegacyTextData data, out bool skippedDueToCompanion)
		{
			data = default;
			skippedDueToCompanion = false;

			if (!YamlUtility.TryGetLocalFileId(text, out long componentId))
				return false;
			if (!YamlUtility.TryGetLocalFileId(text.gameObject, out long gameObjectId))
				return false;

			// Font lookup.
			string fontAssetGuid  = "";
			long   fontAssetLocal = 0;
			string matGuid        = "";
			long   matLocal       = 0;

			var (fontAsset, mat) = EditorCodeUtility.FindMatchingTMPFontAndMaterial(text.font?.name);
			if (fontAsset != null)
			{
				AssetDatabase.TryGetGUIDAndLocalFileIdentifier(fontAsset, out fontAssetGuid, out fontAssetLocal);
			}
			if (mat != null)
			{
				AssetDatabase.TryGetGUIDAndLocalFileIdentifier(mat, out matGuid, out matLocal);
			}

			// Companion components (anything with [RequireComponent(typeof(Text))]).
			foreach (var comp in text.GetComponents<Component>())
			{
				if (comp == null || comp == text)
					continue;
				var attrs = comp.GetType().GetCustomAttributes(typeof(RequireComponent), inherit: true);
				foreach (RequireComponent rc in attrs)
				{
					if (rc.m_Type0 == typeof(Text) || rc.m_Type1 == typeof(Text) || rc.m_Type2 == typeof(Text))
					{
						Debug.LogError($"[LegacyTextToLocalizedTmpConverter] Skipping Text on '{text.gameObject.name}' " +
						               $"because companion '{comp.GetType().Name}' requires Text. Remove or fix the " +
						               $"companion first, then convert again.", text);
						data = default;
						skippedDueToCompanion = true;
						return false;
					}
				}
			}

			data = new LegacyTextData
			{
				ComponentLocalId  = componentId,
				GameObjectLocalId = gameObjectId,
				Text              = text.text ?? "",
				Color             = text.color,
				FontSize          = text.fontSize,
				RichText          = text.supportRichText,
				AutoSize          = text.resizeTextForBestFit,
				AutoSizeMin       = text.resizeTextMinSize,
				AutoSizeMax       = text.resizeTextMaxSize,
				Alignment         = text.alignment,
				RaycastTarget     = text.raycastTarget,
				Maskable          = text.maskable,
				Enabled           = text.enabled,
				LineSpacing       = text.lineSpacing,
				FontStyle         = text.fontStyle,
				HorizontalOverflow = text.horizontalOverflow,
				VerticalOverflow   = text.verticalOverflow,
				FontAssetGuid     = fontAssetGuid,
				FontAssetLocalId  = fontAssetLocal,
				MaterialGuid      = matGuid,
				MaterialLocalId   = matLocal,
			};
			return true;
		}

		// -----------------------------------------------------------------------
		// YAML block generation
		// -----------------------------------------------------------------------

		/// <summary>
		/// Builds the YAML content for a <see cref="UiLocalizedTextMeshProUGUI"/> MonoBehaviour
		/// block from captured legacy <see cref="Text"/> data. The returned string starts with
		/// <c>MonoBehaviour:\n</c> and ends without a trailing newline, ready to be passed to
		/// <see cref="YamlUtility.ReplaceMonoBehaviourBlock"/>.
		/// </summary>
		internal static string BuildTmpYamlBlock(LegacyTextData data, string scriptGuid)
		{
			// Alignment mapping: TextAnchor → TMP horizontal + vertical alignment flags.
			int hAlign, vAlign;
			switch (data.Alignment)
			{
				case TextAnchor.UpperLeft:    hAlign = 1;   vAlign = 256;  break;
				case TextAnchor.UpperCenter:  hAlign = 2;   vAlign = 256;  break;
				case TextAnchor.UpperRight:   hAlign = 4;   vAlign = 256;  break;
				case TextAnchor.MiddleLeft:   hAlign = 1;   vAlign = 512;  break;
				case TextAnchor.MiddleCenter: hAlign = 2;   vAlign = 512;  break;
				case TextAnchor.MiddleRight:  hAlign = 4;   vAlign = 512;  break;
				case TextAnchor.LowerLeft:    hAlign = 1;   vAlign = 1024; break;
				case TextAnchor.LowerCenter:  hAlign = 2;   vAlign = 1024; break;
				case TextAnchor.LowerRight:   hAlign = 4;   vAlign = 1024; break;
				default:                      hAlign = 2;   vAlign = 512;  break;
			}

			// Font color as uint32 (RGBA little-endian byte order Unity uses for TMP).
			int   cr      = Mathf.Clamp(Mathf.RoundToInt(data.Color.r * 255f), 0, 255);
			int   cg      = Mathf.Clamp(Mathf.RoundToInt(data.Color.g * 255f), 0, 255);
			int   cb      = Mathf.Clamp(Mathf.RoundToInt(data.Color.b * 255f), 0, 255);
			int   ca      = Mathf.Clamp(Mathf.RoundToInt(data.Color.a * 255f), 0, 255);
			uint  rgba32  = (uint)(cr | (cg << 8) | (cb << 16) | (ca << 24));

			// Float helpers.
			string fontSizeStr    = F(data.FontSize);
			string autoSizeMinStr = F(data.AutoSizeMin);
			string autoSizeMaxStr = F(data.AutoSizeMax);
			string lineSpacingStr = F(EditorCodeUtility.ConvertLineSpacingFromTextToTmp(data.LineSpacing));
			int    fontStyleInt   = (int)EditorCodeUtility.MapFontStyle(data.FontStyle);

			// Font YAML refs.
			string fontAssetRef;
			string sharedMatRef;
			if (string.IsNullOrEmpty(data.FontAssetGuid))
			{
				fontAssetRef = "{fileID: 0}";
				sharedMatRef = "{fileID: 0}";
			}
			else
			{
				fontAssetRef = $"{{fileID: {data.FontAssetLocalId}, guid: {data.FontAssetGuid}, type: 2}}";
				string matGuidEffective = string.IsNullOrEmpty(data.MaterialGuid) ? data.FontAssetGuid : data.MaterialGuid;
				long   matIdEffective   = data.MaterialLocalId == 0 ? data.FontAssetLocalId : data.MaterialLocalId;
				sharedMatRef = $"{{fileID: {matIdEffective}, guid: {matGuidEffective}, type: 2}}";
			}

			var sb = new StringBuilder(2048);
			sb.Append("MonoBehaviour:\n");
			sb.Append("  m_ObjectHideFlags: 0\n");
			sb.Append("  m_CorrespondingSourceObject: {fileID: 0}\n");
			sb.Append("  m_PrefabInstance: {fileID: 0}\n");
			sb.Append("  m_PrefabAsset: {fileID: 0}\n");
			sb.Append($"  m_GameObject: {{fileID: {data.GameObjectLocalId}}}\n");
			sb.Append($"  m_Enabled: {(data.Enabled ? 1 : 0)}\n");
			sb.Append("  m_EditorHideFlags: 0\n");
			sb.Append($"  m_Script: {{fileID: 11500000, guid: {scriptGuid}, type: 3}}\n");
			sb.Append("  m_Name: \n");
			sb.Append("  m_EditorClassIdentifier: \n");
			sb.Append("  m_Material: {fileID: 0}\n");
			sb.Append("  m_Color: {r: 1, g: 1, b: 1, a: 1}\n");
			sb.Append($"  m_RaycastTarget: {(data.RaycastTarget ? 1 : 0)}\n");
			sb.Append("  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}\n");
			sb.Append($"  m_Maskable: {(data.Maskable ? 1 : 0)}\n");
			sb.Append("  m_OnCullStateChanged:\n");
			sb.Append("    m_PersistentCalls:\n");
			sb.Append("      m_Calls: []\n");
			sb.Append($"  m_text: \"{EscapeYamlString(data.Text)}\"\n");
			sb.Append("  m_isRightToLeft: 0\n");
			sb.Append($"  m_fontAsset: {fontAssetRef}\n");
			sb.Append($"  m_sharedMaterial: {sharedMatRef}\n");
			sb.Append("  m_fontSharedMaterials: []\n");
			sb.Append("  m_fontMaterial: {fileID: 0}\n");
			sb.Append("  m_fontMaterials: []\n");
			sb.Append("  m_fontColor32:\n");
			sb.Append("    serializedVersion: 2\n");
			sb.Append($"    rgba: {rgba32}\n");
			sb.Append($"  m_fontColor: {{r: {FC(data.Color.r)}, g: {FC(data.Color.g)}, b: {FC(data.Color.b)}, a: {FC(data.Color.a)}}}\n");
			sb.Append("  m_enableVertexGradient: 0\n");
			sb.Append("  m_colorMode: 3\n");
			sb.Append("  m_fontColorGradient:\n");
			sb.Append("    topLeft: {r: 1, g: 1, b: 1, a: 1}\n");
			sb.Append("    topRight: {r: 1, g: 1, b: 1, a: 1}\n");
			sb.Append("    bottomLeft: {r: 1, g: 1, b: 1, a: 1}\n");
			sb.Append("    bottomRight: {r: 1, g: 1, b: 1, a: 1}\n");
			sb.Append("  m_fontColorGradientPreset: {fileID: 0}\n");
			sb.Append("  m_spriteAsset: {fileID: 0}\n");
			sb.Append("  m_tintAllSprites: 0\n");
			sb.Append("  m_StyleSheet: {fileID: 0}\n");
			sb.Append("  m_TextStyleHashCode: -1183493901\n");
			sb.Append("  m_overrideHtmlColors: 0\n");
			sb.Append("  m_faceColor:\n");
			sb.Append("    serializedVersion: 2\n");
			sb.Append("    rgba: 4294967295\n");
			sb.Append($"  m_fontSize: {fontSizeStr}\n");
			sb.Append($"  m_fontSizeBase: {fontSizeStr}\n");
			sb.Append("  m_fontWeight: 400\n");
			sb.Append($"  m_enableAutoSizing: {(data.AutoSize ? 1 : 0)}\n");
			sb.Append($"  m_fontSizeMin: {autoSizeMinStr}\n");
			sb.Append($"  m_fontSizeMax: {autoSizeMaxStr}\n");
			sb.Append($"  m_fontStyle: {fontStyleInt}\n");
			sb.Append($"  m_HorizontalAlignment: {hAlign}\n");
			sb.Append($"  m_VerticalAlignment: {vAlign}\n");
			sb.Append("  m_textAlignment: 65535\n");
			sb.Append("  m_characterSpacing: 0\n");
			sb.Append("  m_wordSpacing: 0\n");
			sb.Append($"  m_lineSpacing: {lineSpacingStr}\n");
			sb.Append("  m_lineSpacingMax: 0\n");
			sb.Append("  m_paragraphSpacing: 0\n");
			sb.Append("  m_charWidthMaxAdj: 0\n");
			sb.Append($"  m_enableWordWrapping: {(data.HorizontalOverflow == HorizontalWrapMode.Wrap ? 1 : 0)}\n");
			sb.Append("  m_wordWrappingRatios: 0.4\n");
			sb.Append($"  m_overflowMode: {(data.VerticalOverflow == VerticalWrapMode.Overflow ? 1 : 0)}\n");
			sb.Append("  m_linkedTextComponent: {fileID: 0}\n");
			sb.Append("  parentLinkedComponent: {fileID: 0}\n");
			sb.Append("  m_enableKerning: 1\n");
			sb.Append("  m_enableExtraPadding: 0\n");
			sb.Append("  checkPaddingRequired: 0\n");
			sb.Append($"  m_isRichText: {(data.RichText ? 1 : 0)}\n");
			sb.Append("  m_EmojiFallbackSupport: 1\n");
			sb.Append("  m_parseCtrlCharacters: 1\n");
			sb.Append("  m_isOrthographic: 1\n");
			sb.Append("  m_isCullingEnabled: 0\n");
			sb.Append("  m_VertexBufferAutoSizeReduction: 0\n");
			sb.Append("  m_useMaxVisibleDescender: 1\n");
			sb.Append("  m_pageToDisplay: 1\n");
			sb.Append("  m_margin: {x: 0, y: 0, z: 0, w: 0}\n");
			sb.Append("  m_isUsingLegacyAnimationComponent: 0\n");
			sb.Append("  m_isVolumetricText: 0\n");
			sb.Append("  m_hasFontAssetChanged: 0\n");
			sb.Append("  m_baseMaterial: {fileID: 0}\n");
			sb.Append("  m_maskOffset: {x: 0, y: 0, z: 0, w: 0}\n");
			sb.Append("  m_autoLocalize: 1\n");
			sb.Append("  m_group: \n");
			sb.Append("  m_locaKey: ");

			return sb.ToString();
		}

		// -----------------------------------------------------------------------
		// Gathering helpers
		// -----------------------------------------------------------------------

		private static void GatherFromScene(HashSet<string> prefabPaths, List<Text> plainTexts)
		{
			foreach (var text in Object.FindObjectsOfType<Text>(true))
			{
				if (PrefabUtility.IsPartOfPrefabInstance(text.gameObject))
				{
					string path = GetDefiningPrefabPath(text);
					if (!string.IsNullOrEmpty(path))
						prefabPaths.Add(path);
				}
				else
				{
					plainTexts.Add(text);
				}
			}
		}

		private static void GatherPlainSceneObjects(List<Text> plainTexts)
		{
			foreach (var text in Object.FindObjectsOfType<Text>(true))
			{
				if (!PrefabUtility.IsPartOfPrefabInstance(text.gameObject))
					plainTexts.Add(text);
			}
		}

		private static string GetDefiningPrefabPath(Component c)
		{
			var original = PrefabUtility.GetCorrespondingObjectFromOriginalSource(c);
			if (original == null) return null;
			string path = AssetDatabase.GetAssetPath(original);
			if (string.IsNullOrEmpty(path))
			{
				Debug.LogWarning($"[LegacyTextToLocalizedTmpConverter] Could not resolve prefab path for " +
				                 $"'{c.gameObject.name}' (component: {c.GetType().Name}).");
			}
			return string.IsNullOrEmpty(path) ? null : path;
		}

		private static void ReloadCurrentAsset(bool isInPrefabStage, string assetPath)
		{
			if (string.IsNullOrEmpty(assetPath)) return;

			if (isInPrefabStage)
			{
				StageUtility.GoBackToPreviousStage();
				EditorApplication.delayCall +=
					() => AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<GameObject>(assetPath));
			}
			else
			{
				EditorSceneManager.OpenScene(assetPath, OpenSceneMode.Single);
			}
		}

		// -----------------------------------------------------------------------
		// C# script field updating
		// -----------------------------------------------------------------------

		/// <summary>
		/// Scans a prefab/scene YAML string and returns, for each MonoBehaviour script that has
		/// field references pointing to any of the given <paramref name="convertedIds"/>, the
		/// script GUID mapped to the set of field names that need their C# type changed from
		/// <c>Text</c> to <c>TMP_Text</c>.
		/// </summary>
		private static Dictionary<string, HashSet<string>> FindReferencingScriptFields(
			string yaml, HashSet<long> convertedIds)
		{
			var result = new Dictionary<string, HashSet<string>>();

			// Split on MonoBehaviour block headers.
			var blockSplit = Regex.Split(yaml, @"(?=^--- !u!114 &)", RegexOptions.Multiline);

			// Matches:  m_Script: {fileID: 11500000, guid: XXXX, type: 3}
			var scriptGuidRx = new Regex(@"m_Script:\s*\{[^}]*\bguid:\s*([a-fA-F0-9]+)[^}]*\}",
				RegexOptions.Compiled);

			// Matches:  someFieldName: {fileID: 1234567890123456789}
			var fieldRefRx = new Regex(@"^\s+(\w+):\s*\{fileID:\s*(-?\d+)\}",
				RegexOptions.Compiled | RegexOptions.Multiline);

			foreach (var block in blockSplit)
			{
				if (!block.StartsWith("--- !u!114 &", StringComparison.Ordinal))
					continue;

				var scriptMatch = scriptGuidRx.Match(block);
				if (!scriptMatch.Success)
					continue;

				string scriptGuid = scriptMatch.Groups[1].Value;

				foreach (Match fm in fieldRefRx.Matches(block))
				{
					if (!long.TryParse(fm.Groups[2].Value, out long fileId))
						continue;
					if (!convertedIds.Contains(fileId))
						continue;

					string fieldName = fm.Groups[1].Value;

				if (!result.TryGetValue(scriptGuid, out var set))
					{
						set = new HashSet<string>();
						result[scriptGuid] = set;
					}
					set.Add(fieldName);
				}
			}
			return result;
		}

		/// <summary>
		/// Opens the C# source file identified by <paramref name="scriptGuid"/> and replaces all
		/// <c>Text fieldName</c> field-type occurrences (for the given <paramref name="fieldNames"/>)
		/// with <c>TMP_Text fieldName</c>. Also ensures <c>using TMPro;</c> is present.
		/// </summary>
		/// <returns><c>true</c> when the file was actually modified.</returns>
		private static bool TryUpdateCSharpScriptFields(string scriptGuid, IEnumerable<string> fieldNames)
		{
			string assetPath = AssetDatabase.GUIDToAssetPath(scriptGuid);
			if (string.IsNullOrEmpty(assetPath) || !assetPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
			{
				Debug.LogWarning($"[LegacyTextToLocalizedTmpConverter] Cannot resolve script for guid={scriptGuid}");
				return false;
			}

			string fullPath = YamlUtility.AssetPathToFullPath(assetPath);
			if (!File.Exists(fullPath))
			{
				Debug.LogWarning($"[LegacyTextToLocalizedTmpConverter] Script not found: {fullPath}");
				return false;
			}

			string source  = File.ReadAllText(fullPath);
			string updated = source;

			foreach (string fieldName in fieldNames)
			{
				// Match "Text fieldName" where Text is a standalone word (not UnityEngine.UI.Text etc.)
				// and fieldName is followed by a non-word char.
				updated = Regex.Replace(
					updated,
					$@"(?<!\w)Text(\s+{Regex.Escape(fieldName)}(?!\w))",
					"TMP_Text$1");
			}

			if (updated == source)
				return false;

			// Ensure "using TMPro;" is present.
			if (!Regex.IsMatch(updated, @"^\s*using\s+TMPro\s*;", RegexOptions.Multiline))
			{
				// Insert after the last "using" directive line.
				updated = Regex.Replace(
					updated,
					@"((?:^\s*using\s+[^\r\n]+[\r\n]+)+)",
					m => m.Value + "using TMPro;\n",
					RegexOptions.Multiline);
			}

			File.WriteAllText(fullPath, updated);
			AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
			Debug.Log($"[LegacyTextToLocalizedTmpConverter] Updated C# field types in {assetPath}");
			return true;
		}

		

		/// <summary>Float → YAML string with up to 6 significant digits, invariant culture.</summary>
		private static string F(float v) => v.ToString("G6", CultureInfo.InvariantCulture);

		/// <summary>Float → short color component string (4 sig digits).</summary>
		private static string FC(float v) => v.ToString("G4", CultureInfo.InvariantCulture);

		/// <summary>
		/// Escapes a string for use inside a YAML double-quoted scalar.
		/// </summary>
		internal static string EscapeYamlString(string s)
		{
			if (string.IsNullOrEmpty(s)) return "";
			var sb = new StringBuilder(s.Length + 16);
			foreach (char c in s)
			{
				switch (c)
				{
					case '\\': sb.Append("\\\\"); break;
					case '"':  sb.Append("\\\""); break;
					case '\n': sb.Append("\\n");  break;
					case '\r': sb.Append("\\r");  break;
					case '\t': sb.Append("\\t");  break;
					default:   sb.Append(c);      break;
				}
			}
			return sb.ToString();
		}
	}
}
