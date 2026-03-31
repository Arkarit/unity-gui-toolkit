using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	/// <summary>
	/// Scans the entire project for <see cref="UiLocalizedTextMeshProUGUI"/> components that
	/// have <c>m_autoLocalize=1</c> but whose text content is either an obvious runtime
	/// placeholder (numeric, icon character, etc.) or is referenced by a C# script that
	/// directly assigns <c>.text</c>.  Sets <c>m_autoLocalize=0</c> on any detected component
	/// so it is no longer harvested as a false loca key.
	/// <para>
	/// Detection uses two complementary strategies:<br/>
	/// 1. <b>Heuristic</b>: the initial text looks like a runtime value (pure number, '#' prefix,
	///    icon Unicode, rich-text-wrapped number).<br/>
	/// 2. <b>Reference scan</b>: any C# <c>MonoBehaviour</c> anywhere in the project holds a
	///    serialized reference to the component <em>and</em> assigns <c>fieldName.text =</c>.
	///    Both same-file and cross-file references are detected.
	/// </para>
	/// <para>
	/// A single pass through all project YAML files builds the complete cross-reference index,
	/// keeping total runtime O(project size) rather than O(components × files).
	/// </para>
	/// </summary>
	public static class AutoLocalizeAuditor
	{
		private const string TOOL_TAG = "[AutoLocalizeAuditor]";

		[MenuItem(StringConstants.LOCA_MISC_FIX_AUTO_LOCALIZE_MENU_NAME,
		          priority = Constants.LOCA_MISC_FIX_AUTO_LOCALIZE_MENU_PRIORITY)]
		public static void RunAuditAndFix()
		{
			string scriptGuid = YamlUtility.FindMonoScriptGuid(typeof(UiLocalizedTextMeshProUGUI));
			if (string.IsNullOrEmpty(scriptGuid))
			{
				Debug.LogError($"{TOOL_TAG} Cannot find MonoScript GUID for UiLocalizedTextMeshProUGUI.");
				return;
			}

			var allAssetPaths = CollectProjectYamlAssetPaths();
			if (allAssetPaths.Count == 0)
			{
				Debug.Log($"{TOOL_TAG} No scenes or prefabs found under Assets/.");
				return;
			}

			Debug.Log($"{TOOL_TAG} Scanning {allAssetPaths.Count} assets...");
			EditorUtility.DisplayProgressBar("AutoLocalize Auditor", "Scanning project…", 0f);

			try
			{
				// ── Phase 1 & 2 (single pass) ──────────────────────────────────────────
				// Collect UiLocalizedTextMeshProUGUI components with autoLocalize=1
				// AND build the reverse-reference index across all project YAML files.
				var components = new List<ComponentEntry>();
				// Key: (assetGuid of the referenced component's file, component localId)
				// Value: all MonoBehaviours that hold a serialized field pointing to that component
				var reverseRefs = new Dictionary<(string, long), List<RefEntry>>();

				for (int i = 0; i < allAssetPaths.Count; i++)
				{
					string ap = allAssetPaths[i];
					float progress = (float)i / allAssetPaths.Count;
					EditorUtility.DisplayProgressBar("AutoLocalize Auditor",
						$"Scanning {Path.GetFileName(ap)}…", progress * 0.75f);

					ScanYamlFile(ap, scriptGuid, components, reverseRefs);
				}

				Debug.Log($"{TOOL_TAG} Found {components.Count} autoLocalize=1 components. " +
				          $"Cross-reference index has {reverseRefs.Count} entries.");

				// ── Phase 3: detect components that should have autoLocalize=0 ─────────
				EditorUtility.DisplayProgressBar("AutoLocalize Auditor", "Analysing components…", 0.75f);

				var fixMap = new Dictionary<string, List<long>>(); // assetPath → list of localIds to fix
				int total = 0;

				foreach (var comp in components)
				{
					bool shouldFix = false;
					string reason = null;

					// Strategy A: heuristic on text content
					string candidateKey = string.IsNullOrEmpty(comp.LocaKey) ? comp.Text : comp.LocaKey;
					if (LegacyTextToLocalizedTmpConverter.IsObviouslyRuntimeValue(candidateKey))
					{
						shouldFix = true;
						reason = $"runtime-placeholder text \"{Truncate(candidateKey, 50)}\"";
					}

					// Strategy B: a referencing script assigns .text directly
					if (!shouldFix)
					{
						if (reverseRefs.TryGetValue((comp.AssetGuid, comp.LocalId), out var refs))
						{
							foreach (var r in refs)
							{
								if (HasTextSetter(r.ScriptGuid, r.FieldName))
								{
									shouldFix = true;
									reason = $"'{r.FieldName}.text =' in {AssetDatabase.GUIDToAssetPath(r.ScriptGuid)}";
									break;
								}
							}
						}
					}

					if (!shouldFix)
						continue;

					Debug.Log($"{TOOL_TAG} Will fix {comp.AssetPath} (id={comp.LocalId}): {reason}");

					if (!fixMap.TryGetValue(comp.AssetPath, out var ids))
					{
						ids = new List<long>();
						fixMap[comp.AssetPath] = ids;
					}
					ids.Add(comp.LocalId);
					total++;
				}

				// ── Phase 4: apply fixes ───────────────────────────────────────────────
				EditorUtility.DisplayProgressBar("AutoLocalize Auditor", "Applying fixes…", 0.9f);

				int fixedFiles = 0;
				foreach (var kvp in fixMap)
				{
					string fullPath = YamlUtility.AssetPathToFullPath(kvp.Key);
					if (!File.Exists(fullPath))
						continue;

					string yaml = File.ReadAllText(fullPath);
					bool changed = false;

					foreach (long localId in kvp.Value)
					{
						string patched = PatchAutoLocalize(yaml, localId, enable: false);
						if (patched != null)
						{
							yaml = patched;
							changed = true;
						}
					}

					if (!changed)
						continue;

					File.WriteAllText(fullPath, yaml);
					AssetDatabase.ImportAsset(kvp.Key, ImportAssetOptions.ForceUpdate);
					fixedFiles++;
				}

				AssetDatabase.Refresh();
				Debug.Log($"{TOOL_TAG} Done. Fixed {total} component(s) across {fixedFiles} file(s).");
			}
			finally
			{
				EditorUtility.ClearProgressBar();
			}
		}

		// ── Data types ────────────────────────────────────────────────────────────────

		private struct ComponentEntry
		{
			public string AssetPath;  // Assets/… path
			public string AssetGuid;
			public long   LocalId;
			public string Text;       // m_text (TMP base text field)
			public string LocaKey;    // m_locaKey (our custom field; usually empty after conversion)
		}

		private struct RefEntry
		{
			public string ScriptGuid;  // GUID of the C# script that holds the reference
			public string FieldName;   // serialized field name that points to the component
		}

		// ── Compiled regexes ──────────────────────────────────────────────────────────

		private static readonly Regex s_blockSplitRx =
			new Regex(@"(?=^--- !u!114 &)", RegexOptions.Multiline | RegexOptions.Compiled);

		private static readonly Regex s_blockHeaderRx =
			new Regex(@"^--- !u!114 &(-?\d+)", RegexOptions.Compiled);

		private static readonly Regex s_scriptGuidRx =
			new Regex(@"m_Script:\s*\{[^}]*\bguid:\s*([a-fA-F0-9]+)[^}]*\}",
				RegexOptions.Compiled);

		private static readonly Regex s_autoLocalizeRx =
			new Regex(@"\bm_autoLocalize:\s*(\d)", RegexOptions.Compiled);

		private static readonly Regex s_textFieldRx =
			new Regex(@"^\s+m_text:\s*""((?:[^""\\]|\\.)*)""",
				RegexOptions.Multiline | RegexOptions.Compiled);

		private static readonly Regex s_locaKeyRx =
			new Regex(@"^\s+m_locaKey:\s*(.*)",
				RegexOptions.Multiline | RegexOptions.Compiled);

		// Same-file field reference: someField: {fileID: 123}
		private static readonly Regex s_sameFileRefRx =
			new Regex(@"^\s+(\w+):\s*\{\s*fileID:\s*(-?\d+)\s*\}$",
				RegexOptions.Multiline | RegexOptions.Compiled);

		// Cross-file field reference: someField: {fileID: 123, guid: abc..., type: N}
		private static readonly Regex s_crossFileRefRx =
			new Regex(@"^\s+(\w+):\s*\{\s*fileID:\s*(-?\d+)\s*,\s*guid:\s*([a-fA-F0-9]+)",
				RegexOptions.Multiline | RegexOptions.Compiled);

		// ── Scanning ─────────────────────────────────────────────────────────────────

		private static List<string> CollectProjectYamlAssetPaths()
		{
			var result = new List<string>();
			var searchFolders = new[] { "Assets" };
			foreach (string guid in AssetDatabase.FindAssets("t:Scene t:Prefab", searchFolders))
			{
				string ap = AssetDatabase.GUIDToAssetPath(guid);
				if (!string.IsNullOrEmpty(ap))
					result.Add(ap);
			}
			return result;
		}

		/// <summary>
		/// Reads one YAML file and:
		/// <list type="bullet">
		/// <item>appends any <see cref="UiLocalizedTextMeshProUGUI"/> blocks with
		///   <c>m_autoLocalize=1</c> to <paramref name="components"/>.</item>
		/// <item>records every serialized field reference (same-file and cross-file) found in
		///   any <c>MonoBehaviour</c> block into <paramref name="reverseRefs"/>.</item>
		/// </list>
		/// </summary>
		private static void ScanYamlFile(
			string assetPath,
			string targetScriptGuid,
			List<ComponentEntry> components,
			Dictionary<(string, long), List<RefEntry>> reverseRefs)
		{
			string fullPath = YamlUtility.AssetPathToFullPath(assetPath);
			if (!File.Exists(fullPath))
				return;

			string yaml;
			try { yaml = File.ReadAllText(fullPath); }
			catch { return; }

			string assetGuid = AssetDatabase.AssetPathToGUID(assetPath);
			if (string.IsNullOrEmpty(assetGuid))
				return;

			foreach (string block in s_blockSplitRx.Split(yaml))
			{
				if (!block.StartsWith("--- !u!114 &", System.StringComparison.Ordinal))
					continue;

				var headerM = s_blockHeaderRx.Match(block);
				if (!headerM.Success || !long.TryParse(headerM.Groups[1].Value, out long localId))
					continue;

				var scriptM = s_scriptGuidRx.Match(block);
				if (!scriptM.Success)
					continue;
				string scriptGuid = scriptM.Groups[1].Value;

				// ── Part A: collect UiLocalizedTextMeshProUGUI components ─────────────
				if (scriptGuid == targetScriptGuid)
				{
					var alM = s_autoLocalizeRx.Match(block);
					if (alM.Success && alM.Groups[1].Value == "1")
					{
						components.Add(new ComponentEntry
						{
							AssetPath = assetPath,
							AssetGuid = assetGuid,
							LocalId   = localId,
							Text      = ExtractQuotedField(block, s_textFieldRx),
							LocaKey   = ExtractSimpleField(block, s_locaKeyRx),
						});
					}
				}

				// ── Part B: record all field references for the reverse-ref index ─────
				// Same-file: {fileID: X}
				foreach (Match m in s_sameFileRefRx.Matches(block))
				{
					if (!long.TryParse(m.Groups[2].Value, out long refId))
						continue;
					AddRef(reverseRefs, (assetGuid, refId),
						new RefEntry { ScriptGuid = scriptGuid, FieldName = m.Groups[1].Value });
				}

				// Cross-file: {fileID: X, guid: Y, …}
				foreach (Match m in s_crossFileRefRx.Matches(block))
				{
					if (!long.TryParse(m.Groups[2].Value, out long refId))
						continue;
					AddRef(reverseRefs, (m.Groups[3].Value, refId),
						new RefEntry { ScriptGuid = scriptGuid, FieldName = m.Groups[1].Value });
				}
			}
		}

		private static void AddRef(
			Dictionary<(string, long), List<RefEntry>> dict,
			(string, long) key,
			RefEntry entry)
		{
			if (!dict.TryGetValue(key, out var list))
			{
				list = new List<RefEntry>();
				dict[key] = list;
			}
			list.Add(entry);
		}

		// ── Helpers ───────────────────────────────────────────────────────────────────

		private static string ExtractQuotedField(string block, Regex rx)
		{
			var m = rx.Match(block);
			if (!m.Success)
				return string.Empty;
			return m.Groups[1].Value
				.Replace("\\n",  "\n")
				.Replace("\\r",  "\r")
				.Replace("\\\"", "\"")
				.Replace("\\\\", "\\");
		}

		private static string ExtractSimpleField(string block, Regex rx)
		{
			var m = rx.Match(block);
			if (!m.Success)
				return string.Empty;
			string val = m.Groups[1].Value.Trim();
			if (val.Length >= 2 && val[0] == '"' && val[val.Length - 1] == '"')
				val = val.Substring(1, val.Length - 2);
			return val;
		}

		// Cache to avoid re-reading the same C# script for multiple field names.
		private static readonly Dictionary<string, string> s_scriptSourceCache
			= new Dictionary<string, string>();

		private static bool HasTextSetter(string scriptGuid, string fieldName)
		{
			string assetPath = AssetDatabase.GUIDToAssetPath(scriptGuid);
			if (string.IsNullOrEmpty(assetPath)
			    || !assetPath.EndsWith(".cs", System.StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}

			if (!s_scriptSourceCache.TryGetValue(assetPath, out string source))
			{
				string fullPath = YamlUtility.AssetPathToFullPath(assetPath);
				source = File.Exists(fullPath) ? File.ReadAllText(fullPath) : string.Empty;
				s_scriptSourceCache[assetPath] = source;
			}

			return new Regex($@"\b{Regex.Escape(fieldName)}\.text\s*=").IsMatch(source);
		}

		/// <summary>
		/// Returns a copy of <paramref name="yaml"/> with the <c>m_autoLocalize</c> field in the
		/// block identified by <paramref name="localId"/> set to 1 (enable) or 0 (disable).
		/// Returns <c>null</c> when no change was necessary.
		/// </summary>
		private static string PatchAutoLocalize(string yaml, long localId, bool enable)
		{
			string header = $"--- !u!114 &{localId}";
			int blockStart = yaml.IndexOf(header + "\n", System.StringComparison.Ordinal);
			if (blockStart < 0)
				blockStart = yaml.IndexOf(header + "\r\n", System.StringComparison.Ordinal);
			if (blockStart < 0)
				return null;

			int nextBlock = yaml.IndexOf("\n--- ", blockStart + 1, System.StringComparison.Ordinal);
			int blockEnd  = nextBlock >= 0 ? nextBlock + 1 : yaml.Length;

			string block = yaml.Substring(blockStart, blockEnd - blockStart);
			string from  = enable ? "  m_autoLocalize: 0\n" : "  m_autoLocalize: 1\n";
			string to    = enable ? "  m_autoLocalize: 1\n" : "  m_autoLocalize: 0\n";

			if (!block.Contains(from))
				return null;

			return yaml.Substring(0, blockStart) + block.Replace(from, to) + yaml.Substring(blockEnd);
		}

		private static string Truncate(string s, int max) =>
			s != null && s.Length > max ? s.Substring(0, max) + "…" : s ?? string.Empty;
	}
}
