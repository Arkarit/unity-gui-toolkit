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

			// Line height ratio of the source legacy font (font.lineHeight / font.fontSize).
			// Used to compute an accurate TMP m_lineSpacing value.
			public float LineHeightRatio;
		}

		private struct Stats
		{
			public int textConverted;
			public int textSkippedDueToCompanion;
			public int inputFieldsConverted;
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

			string inputFieldGuid    = YamlUtility.FindMonoScriptGuid(typeof(InputField));
			string tmpInputFieldGuid = YamlUtility.FindMonoScriptGuid(typeof(TMP_InputField));

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

			var crossFileIndex = BuildProjectWideCrossFileIndex();
			var stats = new Stats();

			// Suspend asset database refreshing for the duration of all file writes.
			// Without this, Unity calls ImportAsset hundreds of times consecutively which
			// causes a SIGSEGV crash in MonoBehaviour::Transfer / TypeTreeCache::GetTypeTree.
			AssetDatabase.StartAssetEditing();
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
						ProcessPrefabFile(prefabPath, newGuid, inputFieldGuid, tmpInputFieldGuid, crossFileIndex, ref stats);
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
						ProcessScenePlainObjects(currentAssetPath, scenePlainTexts, newGuid, inputFieldGuid, tmpInputFieldGuid, crossFileIndex, ref stats);
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
				AssetDatabase.StopAssetEditing();
			}

			// Single batch refresh — imports all modified prefabs and C# scripts at once.
			AssetDatabase.Refresh();

			ReloadCurrentAsset(isInPrefabStage, currentAssetPath);

			string summary =
				$"Converted:  {stats.textConverted} Legacy Text component(s)\n" +
				$"Skipped:    {stats.textSkippedDueToCompanion} (RequireComponent companions — see Console)\n" +
				$"InputFields:{stats.inputFieldsConverted} InputField → TMP_InputField\n" +
				$"Scripts:    {stats.scriptsUpdated} C# script(s) updated\n" +
				$"Files:      {stats.filesProcessed} modified";
			if (stats.errors > 0)
				summary += $"\nErrors:     {stats.errors} (see Console for details)";

			EditorUtility.DisplayDialog("Convert Legacy Text – Done", summary, "OK");
		}

		// -----------------------------------------------------------------------
		// Prefab processing
		// -----------------------------------------------------------------------

		private static void ProcessPrefabFile(string assetPath, string scriptGuid,
		                                       string inputFieldGuid, string tmpInputFieldGuid,
		                                       Dictionary<(string, long), Dictionary<string, HashSet<string>>> crossFileIndex, ref Stats stats)
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

			ApplyYamlConversions(assetPath, entries, scriptGuid, inputFieldGuid, tmpInputFieldGuid, crossFileIndex, ref stats);
		}

		// -----------------------------------------------------------------------
		// Scene plain-object processing
		// -----------------------------------------------------------------------

		private static void ProcessScenePlainObjects(string scenePath, List<Text> texts,
		                                              string scriptGuid,
		                                              string inputFieldGuid, string tmpInputFieldGuid,
		                                              Dictionary<(string, long), Dictionary<string, HashSet<string>>> crossFileIndex, ref Stats stats)
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

			ApplyYamlConversions(scenePath, entries, scriptGuid, inputFieldGuid, tmpInputFieldGuid, crossFileIndex, ref stats);
		}

		// -----------------------------------------------------------------------
		// YAML conversion
		// -----------------------------------------------------------------------

		private static void ApplyYamlConversions(string assetPath, IList<LegacyTextData> entries,
		                                          string scriptGuid,
		                                          string inputFieldGuid, string tmpInputFieldGuid,
		                                          Dictionary<(string, long), Dictionary<string, HashSet<string>>> crossFileIndex, ref Stats stats)
		{
			if (IsReadOnlyPackagePath(assetPath))
			{
				Debug.LogWarning($"[LegacyTextToLocalizedTmpConverter] Skipping read-only package asset: {assetPath}");
				return;
			}

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

			var perComponentMap = FindReferencingScriptFieldsPerComponent(yaml, convertedIds);
			MergeCrossFileRefs(assetPath, convertedIds, crossFileIndex, perComponentMap);
			var scriptFieldMap  = AggregateScriptFieldMap(perComponentMap);

			// Phase 1: Replace each Text block with its TMP equivalent.
			foreach (var data in entries)
			{
				bool autoLocalize = !CheckForTextPropertySetters(data.ComponentLocalId, perComponentMap)
			                    && !IsObviouslyRuntimeValue(data.Text);
				string newBlock = BuildTmpYamlBlock(data, scriptGuid, autoLocalize);
				string patched  = YamlUtility.ReplaceMonoBehaviourBlock(yaml, data.ComponentLocalId, newBlock);

				if (patched == null)
				{
					Debug.LogWarning($"[LegacyTextToLocalizedTmpConverter] Block replacement failed for " +
					                 $"localId={data.ComponentLocalId} in {assetPath}.");
					stats.errors++;
					continue;
				}

				yaml    = StripLegacyNameSuffix(patched, data.GameObjectLocalId);
				changed = true;
				stats.textConverted++;
			}

			if (!changed)
				return;

			// Phase 2: Auto-convert InputField → TMP_InputField for any InputFields whose
			// m_TextComponent or m_Placeholder pointed at one of the just-converted Text components.
			var inputFieldIds = new HashSet<long>();
			if (!string.IsNullOrEmpty(inputFieldGuid) && !string.IsNullOrEmpty(tmpInputFieldGuid))
			{
				inputFieldIds = FindInputFieldsReferencingIds(yaml, convertedIds, inputFieldGuid);
				foreach (long ifId in inputFieldIds)
				{
					string patched = SwapMonoBehaviourScriptGuid(yaml, ifId, inputFieldGuid, tmpInputFieldGuid);
					if (patched == null)
					{
						Debug.LogWarning($"[LegacyTextToLocalizedTmpConverter] Could not swap InputField GUID for " +
						                 $"localId={ifId} in {assetPath}.");
						stats.errors++;
						continue;
					}
					yaml = patched;
					stats.inputFieldsConverted++;
				}
			}

			// Single write — both Text and InputField changes committed together.
			File.WriteAllText(fullPath, yaml);
			stats.filesProcessed++;

			// Update dependent C# scripts: change field types from Text → TMP_Text.
			foreach (var kvp in scriptFieldMap)
			{
				if (TryUpdateCSharpScriptFields(kvp.Key, kvp.Value))
					stats.scriptsUpdated++;
			}

			// Update dependent C# scripts: change field types from InputField → TMP_InputField.
			if (inputFieldIds.Count > 0)
			{
				var ifPerComponentMap = FindReferencingScriptFieldsPerComponent(yaml, inputFieldIds);
				MergeCrossFileRefs(assetPath, inputFieldIds, crossFileIndex, ifPerComponentMap);
				var ifScriptFieldMap  = AggregateScriptFieldMap(ifPerComponentMap);

				foreach (var kvp in ifScriptFieldMap)
				{
					if (TryUpdateCSharpScriptFields(kvp.Key, kvp.Value,
					    srcTypeName: "InputField", dstTypeName: "TMP_InputField",
					    dstNamespace: "TMPro", checkKeepLegacy: false))
					{
						stats.scriptsUpdated++;
					}
				}
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

			string legacyFontName = text.font?.name;
			var (fontAsset, mat) = EditorCodeUtility.FindMatchingTMPFontAndMaterial(legacyFontName);

			if (fontAsset == null)
			{
				// No matching TMP font found by name — fall back to the TMP Settings default font so the
				// converted component always references a valid asset rather than relying on TMP's implicit
				// dynamic-atlas callback chain (which can leave text invisible in the Editor).
				var defaultFont = TMP_Settings.defaultFontAsset;
				if (defaultFont != null)
				{
					fontAsset = defaultFont;
					mat = defaultFont.material;
					Debug.LogWarning(
						$"[LegacyTextToLocalizedTmpConverter] No matching TMP font found for " +
						$"'{(string.IsNullOrEmpty(legacyFontName) ? "(none)" : legacyFontName)}' " +
						$"on '{text.gameObject.name}'. Using TMP default font '{defaultFont.name}' as fallback.",
						text.gameObject);
				}
			}

			if (fontAsset != null)
			{
				AssetDatabase.TryGetGUIDAndLocalFileIdentifier(fontAsset, out fontAssetGuid, out fontAssetLocal);
			}
			if (mat != null)
			{
				AssetDatabase.TryGetGUIDAndLocalFileIdentifier(mat, out matGuid, out matLocal);
			}

			// Compute the legacy font's line-height ratio (lineHeight / fontSize).
			// This is used for an accurate TMP m_lineSpacing conversion because the legacy lineSpacing
			// multiplier operates on the font's natural line height, which is font-specific.
			const float k_DefaultLineHeightRatio = 1.15f; // Good approximation for Arial and most Latin fonts.
			float lineHeightRatio = k_DefaultLineHeightRatio;
			if (text.font != null && text.font.fontSize > 0)
			{
				float ratio = (float)text.font.lineHeight / text.font.fontSize;
				if (ratio > 0.5f && ratio < 3.0f)
					lineHeightRatio = ratio;
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
				LineHeightRatio   = lineHeightRatio,
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
		internal static string BuildTmpYamlBlock(LegacyTextData data, string scriptGuid, bool autoLocalize = true)
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
			string lineSpacingStr = F(EditorCodeUtility.ConvertLineSpacingFromTextToTmp(data.LineSpacing, data.LineHeightRatio));
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
			sb.Append($"  m_overflowMode: {(data.VerticalOverflow == VerticalWrapMode.Overflow ? 0 : 3)}\n");
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
			sb.Append($"  m_autoLocalize: {(autoLocalize ? 1 : 0)}\n");
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
		/// Scans the YAML for MonoBehaviour blocks that hold field references to any of the
		/// <paramref name="convertedIds"/> and returns a two-level map:<br/>
		/// <c>componentId → (scriptGuid → set of field names that reference that component)</c><br/>
		/// This lets callers both determine per-component dependency info and derive the aggregate
		/// <c>scriptGuid → fieldNames</c> view needed for C# field-type updates.
		/// </summary>
		private static Dictionary<long, Dictionary<string, HashSet<string>>> FindReferencingScriptFieldsPerComponent(
			string yaml, HashSet<long> convertedIds)
		{
			var result = new Dictionary<long, Dictionary<string, HashSet<string>>>();

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

					if (!result.TryGetValue(fileId, out var scriptMap))
					{
						scriptMap = new Dictionary<string, HashSet<string>>();
						result[fileId] = scriptMap;
					}
					if (!scriptMap.TryGetValue(scriptGuid, out var set))
					{
						set = new HashSet<string>();
						scriptMap[scriptGuid] = set;
					}
					set.Add(fieldName);
				}
			}
			return result;
		}

		/// <summary>
		/// Derives the aggregate <c>scriptGuid → fieldNames</c> map from the per-component map,
		/// which is the form required by <see cref="TryUpdateCSharpScriptFields"/>.
		/// </summary>
		private static Dictionary<string, HashSet<string>> AggregateScriptFieldMap(
			Dictionary<long, Dictionary<string, HashSet<string>>> perComponentMap)
		{
			var aggregate = new Dictionary<string, HashSet<string>>();
			foreach (var scriptMap in perComponentMap.Values)
			{
				foreach (var kvp in scriptMap)
				{
					if (!aggregate.TryGetValue(kvp.Key, out var set))
					{
						set = new HashSet<string>();
						aggregate[kvp.Key] = set;
					}
					foreach (var fn in kvp.Value)
						set.Add(fn);
				}
			}
			return aggregate;
		}

		/// <summary>
		/// Checks whether any C# script that references <paramref name="componentId"/> sets
		/// <c>.text</c> directly on the referencing field.  When found, a warning is logged
		/// for each offending script+field combination.
		/// </summary>
		/// <returns><c>true</c> if at least one direct <c>.text</c> setter was found.</returns>
		private static bool CheckForTextPropertySetters(
			long componentId,
			Dictionary<long, Dictionary<string, HashSet<string>>> perComponentMap)
		{
			if (!perComponentMap.TryGetValue(componentId, out var scriptMap))
				return false;

			bool found = false;
			foreach (var kvp in scriptMap)
			{
				string scriptAssetPath = AssetDatabase.GUIDToAssetPath(kvp.Key);
				if (string.IsNullOrEmpty(scriptAssetPath))
					continue;

				string fullPath = YamlUtility.AssetPathToFullPath(scriptAssetPath);
				if (!File.Exists(fullPath))
					continue;

				string source = File.ReadAllText(fullPath);
				foreach (string fieldName in kvp.Value)
				{
					var rx = new Regex($@"\b{Regex.Escape(fieldName)}\.text\s*=");
					if (!rx.IsMatch(source))
						continue;

					Debug.LogWarning(
						$"[LegacyTextToLocalizedTmpConverter] '{scriptAssetPath}' sets '{fieldName}.text' directly. " +
						$"AutoLocalize has been disabled on the converted component. " +
						$"To enable localization, set LocaKey and re-enable AutoLocalize on the component.");
					found = true;
				}
			}
			return found;
		}

		/// <summary>
		/// Heuristic: returns <c>true</c> when <paramref name="text"/> is clearly a runtime-generated
		/// value (numeric placeholder, icon character, rich-text-wrapped number) rather than a
		/// static translatable string.  When true, <c>autoLocalize</c> is disabled on the converted
		/// component so the placeholder is not harvested as a loca key.
		/// </summary>
		internal static bool IsObviouslyRuntimeValue(string text)
		{
			if (string.IsNullOrEmpty(text))
				return false;

			// Pure numeric / format placeholder: "0", "100", "00/000", "0:00", "1/2"
			if (Regex.IsMatch(text, @"^[\d\s/:.%\-\n\r]+$"))
				return true;

			// Runtime format marker prefix convention (e.g. "#Level 2", "#Score: 100")
			if (text.StartsWith("#", StringComparison.Ordinal))
				return true;

			// Private-use-area Unicode characters (icon fonts, e.g. Font Awesome \uF03D)
			foreach (char c in text)
			{
				if (c >= '\uE000' && c <= '\uF8FF')
					return true;
			}

			// Rich-text tags wrapping only numeric/whitespace content (e.g. "<color=...>9</color>\n1")
			string stripped = Regex.Replace(text, @"<[^>]+>", string.Empty);
			if (!string.IsNullOrWhiteSpace(stripped) && Regex.IsMatch(stripped, @"^[\d\s/:.%\-\n\r]+$"))
				return true;

			return false;
		}


		/// <summary>
		/// Opens the C# source file identified by <paramref name="scriptGuid"/> and replaces all
		/// <c><paramref name="srcTypeName"/> fieldName</c> field-type occurrences (for the given
		/// <paramref name="fieldNames"/>) with <c><paramref name="dstTypeName"/> fieldName</c>.
		/// Also ensures <c>using <paramref name="dstNamespace"/>;</c> is present.
		/// <para>
		/// Default behaviour (no type name arguments): replaces <c>Text</c> → <c>TMP_Text</c> and
		/// adds <c>using TMPro;</c>, matching the legacy-Text conversion use case.
		/// </para>
		/// </summary>
		/// <returns><c>true</c> when the file was actually modified.</returns>
		private static bool TryUpdateCSharpScriptFields(
			string scriptGuid,
			IEnumerable<string> fieldNames,
			string srcTypeName    = "Text",
			string dstTypeName    = "TMP_Text",
			string dstNamespace   = "TMPro",
			bool   checkKeepLegacy = true)
		{
			string assetPath = AssetDatabase.GUIDToAssetPath(scriptGuid);
			if (string.IsNullOrEmpty(assetPath) || !assetPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
			{
				Debug.LogWarning($"[LegacyTextToLocalizedTmpConverter] Cannot resolve script for guid={scriptGuid}");
				return false;
			}

			if (IsReadOnlyPackagePath(assetPath))
			{
				Debug.LogWarning($"[LegacyTextToLocalizedTmpConverter] Skipping read-only package script: {assetPath}");
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
				// Skip fields decorated with [KeepLegacyText] (Text-conversion-only guard).
				if (checkKeepLegacy && HasKeepLegacyTextAttribute(source, fieldName))
				{
					Debug.Log($"[LegacyTextToLocalizedTmpConverter] Skipping field '{fieldName}' in {assetPath} — marked [KeepLegacyText].");
					continue;
				}

				// Match "SrcType fieldName" where SrcType is a standalone word and fieldName is
				// followed by a non-word char.
				updated = Regex.Replace(
					updated,
					$@"(?<!\w){Regex.Escape(srcTypeName)}(\s+{Regex.Escape(fieldName)}(?!\w))",
					$"{dstTypeName}$1");
			}

			if (updated == source)
				return false;

			// Ensure "using <dstNamespace>;" is present.
			string usingDirective = $"using {dstNamespace};";
			if (!Regex.IsMatch(updated, $@"^\s*using\s+{Regex.Escape(dstNamespace)}\s*;", RegexOptions.Multiline))
			{
				// Insert after the last "using" directive line.
				updated = Regex.Replace(
					updated,
					@"((?:^\s*using\s+[^\r\n]+[\r\n]+)+)",
					m => m.Value + usingDirective + "\n",
					RegexOptions.Multiline);
			}

			File.WriteAllText(fullPath, updated);
			Debug.Log($"[LegacyTextToLocalizedTmpConverter] Updated C# field types in {assetPath}");
			return true;
		}

		

		/// <summary>
		/// Returns true if <paramref name="assetPath"/> is inside a read-only Unity package location
		/// (PackageCache, built-in packages, or any path not under Assets/).
		/// These paths must never be written to.
		/// </summary>
		private static bool IsReadOnlyPackagePath(string assetPath)
		{
			if (string.IsNullOrEmpty(assetPath))
				return true;

			// Any path under Assets/ is writable project content.
			if (assetPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
				return false;

			// Everything else (Library/PackageCache, Packages/<built-in>, etc.) is read-only.
			return true;
		}

		/// <summary>
		/// Returns true if <paramref name="fieldName"/> is preceded by a <c>[KeepLegacyText]</c>
		/// attribute in <paramref name="source"/> — whether on the same line as the field or on
		/// preceding attribute lines (e.g. <c>[SerializeField][KeepLegacyText] private Text f;</c>
		/// or <c>[KeepLegacyText]\nprivate Text f;</c>).
		/// </summary>
		private static bool HasKeepLegacyTextAttribute(string source, string fieldName)
		{
			// Find the field declaration: "Text <fieldName>" as standalone tokens.
			var fieldDeclMatch = Regex.Match(
				source,
				$@"(?<!\w)Text\s+{Regex.Escape(fieldName)}(?!\w)",
				RegexOptions.Multiline);

			if (!fieldDeclMatch.Success)
				return false;

			// Extract up to 10 lines worth of text ending at the field declaration.
			// This covers both same-line attributes and attributes on preceding lines.
			int end = fieldDeclMatch.Index;
			int start = end;
			int newlines = 0;
			while (start > 0)
			{
				start--;
				if (source[start] == '\n' && ++newlines == 10)
					break;
			}

			string window = source.Substring(start, end - start);
			return Regex.IsMatch(window, @"\bKeepLegacyText\b");
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
		/// <summary>
		/// Scans <paramref name="yaml"/> for the <c>--- !u!1 &amp;{gameObjectLocalId}</c> anchor and
		/// strips a trailing <c> (Legacy)</c> suffix from its <c>m_Name</c> field, if present.
		/// </summary>
		private static string StripLegacyNameSuffix(string yaml, long gameObjectLocalId)
		{
			const string legacySuffix = " (Legacy)";
			string anchor = $"--- !u!1 &{gameObjectLocalId}";
			int anchorIdx = yaml.IndexOf(anchor, StringComparison.Ordinal);
			if (anchorIdx < 0)
				return yaml;

			// Bound search to the current YAML document block (up to the next document separator).
			int nextDoc = yaml.IndexOf("\n---", anchorIdx + anchor.Length, StringComparison.Ordinal);
			int searchEnd = nextDoc < 0 ? yaml.Length : nextDoc;

			int mNameIdx = yaml.IndexOf("  m_Name: ", anchorIdx, searchEnd - anchorIdx, StringComparison.Ordinal);
			if (mNameIdx < 0)
				return yaml;

			int lineEnd = yaml.IndexOf('\n', mNameIdx);
			if (lineEnd < 0)
				lineEnd = yaml.Length;

			string line = yaml.Substring(mNameIdx, lineEnd - mNameIdx);
			if (!line.EndsWith(legacySuffix, StringComparison.Ordinal))
				return yaml;

			string newLine = line.Substring(0, line.Length - legacySuffix.Length);
			return yaml.Remove(mNameIdx, lineEnd - mNameIdx).Insert(mNameIdx, newLine);
		}

		// -----------------------------------------------------------------------
		// Cross-file reference index
		// -----------------------------------------------------------------------

		/// <summary>
		/// Scans all prefabs and scenes under <c>Assets/</c> and builds an index of cross-file
		/// serialized field references pointing to components in OTHER assets.<br/>
		/// Key: (targetAssetGuid, targetLocalId) — the component being referenced.<br/>
		/// Value: scriptGuid → set of field names that hold the reference.
		/// </summary>
		private static Dictionary<(string, long), Dictionary<string, HashSet<string>>> BuildProjectWideCrossFileIndex()
		{
			var index = new Dictionary<(string, long), Dictionary<string, HashSet<string>>>();

			string[] guids = AssetDatabase.FindAssets("t:Prefab t:Scene");
			int total = guids.Length;

			var scriptGuidRx = new Regex(@"m_Script:\s*\{[^}]*\bguid:\s*([a-fA-F0-9]+)[^}]*\}",
				RegexOptions.Compiled);

			// Matches cross-file refs: fieldName: {fileID: 123, guid: abc, type: 3}
			var crossRefRx = new Regex(@"^\s+(\w+):\s*\{\s*fileID:\s*(-?\d+)\s*,\s*guid:\s*([a-fA-F0-9]+)",
				RegexOptions.Compiled | RegexOptions.Multiline);

			try
			{
				for (int i = 0; i < total; i++)
				{
					string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
					EditorUtility.DisplayProgressBar(
						"Convert Legacy Text",
						$"Building cross-file reference index… ({i + 1}/{total})",
						(float)i / Math.Max(total, 1));

					if (IsReadOnlyPackagePath(assetPath))
						continue;

					string fullPath = YamlUtility.AssetPathToFullPath(assetPath);
					if (!File.Exists(fullPath))
						continue;

					string yaml;
					try
					{
						yaml = File.ReadAllText(fullPath);
					}
					catch (Exception ex)
					{
						Debug.LogWarning($"[LegacyTextToLocalizedTmpConverter] Could not read '{assetPath}' for cross-file index: {ex.Message}");
						continue;
					}

					var blocks = Regex.Split(yaml, @"(?=^--- !u!114 &)", RegexOptions.Multiline);
					foreach (var block in blocks)
					{
						if (!block.StartsWith("--- !u!114 &", StringComparison.Ordinal))
							continue;

						var scriptMatch = scriptGuidRx.Match(block);
						if (!scriptMatch.Success)
							continue;

						string scriptGuid = scriptMatch.Groups[1].Value;

						foreach (Match m in crossRefRx.Matches(block))
						{
							string fieldName  = m.Groups[1].Value;
							string fileIdStr  = m.Groups[2].Value;
							string targetGuid = m.Groups[3].Value;

							if (!long.TryParse(fileIdStr, out long fileId))
								continue;

							var key = (targetGuid, fileId);
							if (!index.TryGetValue(key, out var scriptMap))
							{
								scriptMap  = new Dictionary<string, HashSet<string>>();
								index[key] = scriptMap;
							}
							if (!scriptMap.TryGetValue(scriptGuid, out var fieldSet))
							{
								fieldSet              = new HashSet<string>();
								scriptMap[scriptGuid] = fieldSet;
							}
							fieldSet.Add(fieldName);
						}
					}
				}
			}
			finally
			{
				EditorUtility.ClearProgressBar();
			}

			return index;
		}

		/// <summary>
		/// Scans all <c>!u!114</c> (MonoBehaviour) blocks in <paramref name="yaml"/> and returns
		/// the local IDs of any <see cref="InputField"/> components whose <c>m_TextComponent</c> or
		/// <c>m_Placeholder</c> field points to one of the supplied <paramref name="textIds"/>.
		/// These InputFields must also be converted to <c>TMP_InputField</c> because their text
		/// children are being converted to TMP components.
		/// </summary>
		private static HashSet<long> FindInputFieldsReferencingIds(
			string yaml, HashSet<long> textIds, string inputFieldGuid)
		{
			var result = new HashSet<long>();

			var blocks = Regex.Split(yaml, @"(?=^--- !u!114 &)", RegexOptions.Multiline);

			var anchorRx = new Regex(@"^--- !u!114 &(-?\d+)", RegexOptions.Multiline);
			var scriptRx = new Regex(
				@"m_Script:\s*\{[^}]*\bguid:\s*" + Regex.Escape(inputFieldGuid) + @"[^}]*\}",
				RegexOptions.Compiled);
			// Matches m_TextComponent or m_Placeholder referencing a same-file component (no guid).
			var fieldRefRx = new Regex(
				@"^\s+(?:m_TextComponent|m_Placeholder):\s*\{fileID:\s*(-?\d+)\}",
				RegexOptions.Compiled | RegexOptions.Multiline);

			foreach (string block in blocks)
			{
				if (!block.StartsWith("--- !u!114 &", StringComparison.Ordinal))
					continue;
				if (!scriptRx.IsMatch(block))
					continue;

				bool references = false;
				foreach (Match m in fieldRefRx.Matches(block))
				{
					if (long.TryParse(m.Groups[1].Value, out long refId) && textIds.Contains(refId))
					{
						references = true;
						break;
					}
				}
				if (!references)
					continue;

				var anchorMatch = anchorRx.Match(block);
				if (anchorMatch.Success && long.TryParse(anchorMatch.Groups[1].Value, out long localId))
					result.Add(localId);
			}

			return result;
		}

		/// <summary>
		/// Locates the <c>!u!114 &amp;{localId}</c> MonoBehaviour block in <paramref name="yaml"/>
		/// and replaces the <c>m_Script</c> GUID from <paramref name="oldGuid"/> to
		/// <paramref name="newGuid"/>. Returns the modified YAML string, or <c>null</c> if the
		/// block or GUID was not found.
		/// </summary>
		private static string SwapMonoBehaviourScriptGuid(
			string yaml, long localId, string oldGuid, string newGuid)
		{
			string anchor   = $"--- !u!114 &{localId}";
			int    blockStart = yaml.IndexOf(anchor, StringComparison.Ordinal);
			if (blockStart < 0)
				return null;

			// Scope the replacement to this block only (up to the next document separator).
			int nextDoc  = yaml.IndexOf("\n---", blockStart + anchor.Length, StringComparison.Ordinal);
			int blockEnd = nextDoc < 0 ? yaml.Length : nextDoc;

			string block = yaml.Substring(blockStart, blockEnd - blockStart);

			// Replace only the m_Script GUID to avoid touching other GUIDs in the block.
			string newBlock = Regex.Replace(
				block,
				@"(m_Script:\s*\{[^}]*\bguid:\s*)" + Regex.Escape(oldGuid),
				"$1" + newGuid);

			if (newBlock == block)
				return null; // GUID not found in this block

			return yaml.Substring(0, blockStart) + newBlock + yaml.Substring(blockEnd);
		}

		/// <summary>
		/// Merges cross-file references from <paramref name="crossFileIndex"/> that target any of
		/// the <paramref name="convertedIds"/> in <paramref name="assetPath"/> into
		/// <paramref name="perComponentMap"/>, so that scripts in other prefabs/scenes that hold
		/// a reference to the converted components are also discovered for field-type updates.
		/// </summary>
		private static void MergeCrossFileRefs(
			string assetPath,
			HashSet<long> convertedIds,
			Dictionary<(string, long), Dictionary<string, HashSet<string>>> crossFileIndex,
			Dictionary<long, Dictionary<string, HashSet<string>>> perComponentMap)
		{
			string assetGuid = AssetDatabase.AssetPathToGUID(assetPath);
			if (string.IsNullOrEmpty(assetGuid))
				return;

			foreach (long localId in convertedIds)
			{
				var key = (assetGuid, localId);
				if (!crossFileIndex.TryGetValue(key, out var scriptMap))
					continue;

				if (!perComponentMap.TryGetValue(localId, out var existing))
				{
					existing                 = new Dictionary<string, HashSet<string>>();
					perComponentMap[localId] = existing;
				}

				foreach (var kvp in scriptMap)
				{
					if (!existing.TryGetValue(kvp.Key, out var fieldSet))
					{
						fieldSet          = new HashSet<string>();
						existing[kvp.Key] = fieldSet;
					}
					foreach (string fieldName in kvp.Value)
					{
						fieldSet.Add(fieldName);
					}
				}
			}
		}

	}
}