#if UNITY_6000_0_OR_NEWER
#define UITK_USE_ROSLYN
#endif

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEditor.Compilation;
using System.Linq;
using GuiToolkit.Exceptions;

#if UITK_USE_ROSLYN
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using GuiToolkit.Editor.Roslyn;
using GuiToolkit.Style;
#endif

using OwnerAndPathList = System.Collections.Generic.List<(UnityEngine.Object owner, string propertyPath)>;
using OwnerAndPathListById = System.Collections.Generic.Dictionary<int, System.Collections.Generic.List<(UnityEngine.Object owner, string propertyPath)>>;
using TextSnapshotList = System.Collections.Generic.List<(GuiToolkit.Editor.EditorCodeUtility.TextSnapshot Snapshot, TMPro.TextMeshProUGUI NewComp)>;

namespace GuiToolkit.Editor
{
	/// <summary>
	/// Editor utilities for migrating UnityEngine.UI.Text to TextMeshProUGUI, including:
	/// - collecting serialized references,
	/// - replacing components in scenes,
	/// - rewriting C# source code using Roslyn,
	/// - and applying rewiring after domain reload.
	/// </summary>
	public static partial class EditorCodeUtility
	{
		/// <summary>
		/// Snapshot of important properties from a legacy UnityEngine.UI.Text
		/// used to restore equivalent values on a new TextMeshProUGUI component.
		/// </summary>
		public struct TextSnapshot
		{
			/// <summary>Text content.</summary>
			public string Text;
			/// <summary>Text color.</summary>
			public Color Color;
			/// <summary>Font size value.</summary>
			public float FontSize;
			/// <summary>Whether rich text is enabled on the legacy Text.</summary>
			public bool Rich;
			/// <summary>Whether auto sizing was enabled (best-fit).</summary>
			public bool AutoSize;
			public float AutoSizeMin;
			public float AutoSizeMax;

			/// <summary>Legacy Text alignment anchor.</summary>
			public TextAnchor Anchor;
			/// <summary>Whether the legacy Text was a raycast target.</summary>
			public bool Raycast;
			/// <summary>Legacy line spacing.</summary>
			public float LineSpacing;

			public string FontName;         // legacy Text.font?.name
			public FontStyle FontStyle;     // legacy Text.fontStyle

			/// <summary>
			/// Instance ID of the original Text component.
			/// Used to rewire serialized object references to the new component.
			/// </summary>
			public int OldId;

			public override string ToString()
			{
				var sb = new System.Text.StringBuilder(128);
				sb.Append("TextSnapshot\n");
				sb.Append("{\n");
				sb.Append("\t  Text='").Append(Preview(Text, 80)).Append("'\n");
				sb.Append("\t, Color=").Append(ColorToHex(Color)).Append('\n');
				sb.Append("\t, FontSize=").Append(FloatToString(FontSize)).Append('\n');
				sb.Append("\t, Rich=").Append(Rich).Append('\n');
				sb.Append("\t, AutoSize=").Append(AutoSize).Append('\n');
				sb.Append("\t, AutoSizeMin=").Append(FloatToString(AutoSizeMin)).Append('\n');
				sb.Append("\t, AutoSizeMax=").Append(FloatToString(AutoSizeMax)).Append('\n');
				sb.Append("\t, Anchor=").Append(Anchor).Append('\n');
				sb.Append("\t, Raycast=").Append(Raycast).Append('\n');
				sb.Append("\t, LineSpacing=").Append(FloatToString(LineSpacing)).Append('\n');
				sb.Append("\t, FontName=").Append(string.IsNullOrEmpty(FontName) ? "<null>" : FontName).Append('\n');
				sb.Append("\t, FontStyle=").Append(FontStyle).Append('\n');
				sb.Append("\t, OldId=").Append(OldId).Append('\n');
				sb.Append("}\n");
				return sb.ToString();
			}

			private static string FloatToString( float _f ) => _f.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);

			// --- helpers (ASCII-safe) ---
			private static string Preview( string s, int maxLen )
			{
				if (string.IsNullOrEmpty(s))
					return "<empty>";

				s = s.Replace("\r", "\\r").Replace("\n", "\\n");
				if (s.Length <= maxLen) return s;
				return s.Substring(0, maxLen) + "...";
			}

			private static string ColorToHex( Color c )
			{
				int r = Mathf.Clamp(Mathf.RoundToInt(c.r * 255f), 0, 255);
				int g = Mathf.Clamp(Mathf.RoundToInt(c.g * 255f), 0, 255);
				int b = Mathf.Clamp(Mathf.RoundToInt(c.b * 255f), 0, 255);
				int a = Mathf.Clamp(Mathf.RoundToInt(c.a * 255f), 0, 255);
				return string.Format("#{0:X2}{1:X2}{2:X2}{3:X2}", r, g, b, a);
			}
		}

		private static readonly Dictionary<string, (TMP_FontAsset font, Material mat)> s_tmpFontLookupCache = new(StringComparer.OrdinalIgnoreCase);

		private static string NormalizeFontKey( string name )
		{
			if (string.IsNullOrEmpty(name)) return "";
			// normalize aggressively: lower, remove spaces, underscores, hyphens and common SDF tags
			var key = new string(name.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
			key = key.Replace("sdf", "").Replace("tmp", "");
			return key;
		}

		private static (TMP_FontAsset font, Material mat) FindMatchingTMPFontAndMaterial( string legacyFontName )
		{
			if (string.IsNullOrEmpty(legacyFontName))
				return (null, null);

			if (s_tmpFontLookupCache.TryGetValue(legacyFontName, out var cached))
				return cached;

			var legacyKey = NormalizeFontKey(legacyFontName);

			TMP_FontAsset best = null;
			int bestScore = int.MinValue;

			// 1) scan all TMP_FontAsset assets once
			var guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
			foreach (var guid in guids)
			{
				var path = AssetDatabase.GUIDToAssetPath(guid);
				var fa = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
				if (!fa) continue;

				// candidates to compare
				var cand = new[]
				{
					fa.faceInfo.familyName,                    // most robust match
					fa.sourceFontFile ? fa.sourceFontFile.name : null,
					fa.name
				};

				int scoreHere = int.MinValue;
				for (int i = 0; i < cand.Length; i++)
				{
					var c = cand[i];
					if (string.IsNullOrEmpty(c)) continue;

					var key = NormalizeFontKey(c);

					// scoring: exact normalized match >> startswith/contains
					if (key == legacyKey) scoreHere = Math.Max(scoreHere, 100 - i); // prefer earlier fields
					else if (key.StartsWith(legacyKey)) scoreHere = Math.Max(scoreHere, 60 - i);
					else if (legacyKey.StartsWith(key)) scoreHere = Math.Max(scoreHere, 50 - i);
					else if (key.Contains(legacyKey)) scoreHere = Math.Max(scoreHere, 40 - i);
				}

				if (scoreHere > bestScore)
				{
					bestScore = scoreHere;
					best = fa;
				}
			}

			// 2) prefer the font asset's own (default) material; override only if a clearly better neutral preset exists
			Material mat = null;

			if (best)
			{
				// always start with the asset's own material (usually effect-free)
				var defaultMat = best.material;
				mat = defaultMat;

				// search for neutral presets in the same folder; avoid effecty ones like Outline/Shadow/Glow/Underlay
				var folder = System.IO.Path.GetDirectoryName(AssetDatabase.GetAssetPath(best)).Replace("\\", "/");
				if (!string.IsNullOrEmpty(folder))
				{
					var matGuids = AssetDatabase.FindAssets("t:Material", new[] { folder });

					// scoring helpers
					int ScoreMaterialName( string name, IEnumerable<string> keys )
					{
						if (string.IsNullOrEmpty(name)) return int.MinValue;
						var mk = NormalizeFontKey(name);

						// hard penalties for effect keywords
						// note: keep this set conservative; fontStyle handles bold/italic already
						string[] effectHints = { "outline", "shadow", "glow", "underlay", "stroke", "soft", "thick", "thin" };
						foreach (var eh in effectHints)
							if (mk.Contains(NormalizeFontKey(eh))) return -1000;

						int score = 0;

						// small bonuses for neutral keywords
						string[] neutralHints = { "regular", "normal", "default", "atlas", "sdf" };
						foreach (var nh in neutralHints)
							if (mk.Contains(NormalizeFontKey(nh))) score += 5;

						foreach (var k in keys)
						{
							if (string.IsNullOrEmpty(k)) continue;
							if (mk == k) score = Math.Max(score, score + 100);
							else if (mk.StartsWith(k)) score = Math.Max(score, score + 70);
							else if (mk.Contains(k)) score = Math.Max(score, score + 50);
						}

						return score;
					}

					// keys representing this font
					var keysToTry = new[]
					{
						NormalizeFontKey(best.faceInfo.familyName),
						NormalizeFontKey(best.name),
						NormalizeFontKey(best.sourceFontFile ? best.sourceFontFile.name : null)
					};

					var bestPreset = defaultMat;
					int bestPresetScore = -1;

					foreach (var mg in matGuids)
					{
						var mPath = AssetDatabase.GUIDToAssetPath(mg);
						var m = AssetDatabase.LoadAssetAtPath<Material>(mPath);
						if (!m || m == defaultMat) continue;

						// consider only TMP-compatible shaders
						var shaderName = m.shader ? m.shader.name : "";
						if (string.IsNullOrEmpty(shaderName) || (!shaderName.Contains("TextMeshPro") && !shaderName.Contains("TMP")))
							continue;

						int score = ScoreMaterialName(m.name, keysToTry);
						if (score > bestPresetScore)
						{
							bestPresetScore = score;
							bestPreset = m;
						}
					}

					// Only override the default if the preset is clearly better AND not an "effect" (penalties enforce that)
					// Threshold keeps us conservative; tweak if you like.
					const int OVERRIDE_THRESHOLD = 95;
					if (bestPreset != null && bestPreset != defaultMat && bestPresetScore >= OVERRIDE_THRESHOLD)
						mat = bestPreset;
					else
						mat = defaultMat; // stick to the asset's own material
				}
			}

			var result = (best, mat);
			s_tmpFontLookupCache[legacyFontName] = result;
			return result;
		}

		private static TMPro.FontStyles MapFontStyle( FontStyle fs )
		{
			// Legacy FontStyle has bitwise Bold/Italic, others are Normal/Bold/Italic/BoldAndItalic
			switch (fs)
			{
				case FontStyle.Bold: return TMPro.FontStyles.Bold;
				case FontStyle.Italic: return TMPro.FontStyles.Italic;
				case FontStyle.BoldAndItalic: return TMPro.FontStyles.Bold | TMPro.FontStyles.Italic;
				case FontStyle.Normal:
				default: return TMPro.FontStyles.Normal;
			}
		}
		/// <summary>
		/// Checks if the string segment at the given index is immediately followed
		/// by a known localization function call in the next code segment.
		/// </summary>
		/// <param name="_parts">Alternating list of code and string parts as produced by SeparateCodeAndStrings.</param>
		/// <param name="_index">Index pointing to a string entry inside the list.</param>
		/// <returns>True if the subsequent code contains a localization call, otherwise false.</returns>
		public static bool IsFollowedByLocalizationFunction( List<string> _parts, int _index )
		{
			if (_index % 2 != 1 || _index + 1 >= _parts.Count)
				return false;

			var nextCode = _parts[_index + 1];
			return Regex.IsMatch(nextCode, @"\b(_|__|_n|gettext|ngettext)\s*\(");
		}

		/// <summary>
		/// Returns the absolute file path of the caller source file (primarily used in tests).
		/// </summary>
		/// <param name="_path">Automatically provided by the compiler via CallerFilePath.</param>
		/// <returns>Absolute file path string of the caller source file.</returns>
		public static string GetThisFilePath( [CallerFilePath] string _path = null ) => _path;

		/// <summary>
		/// Splits a C# source string into alternating code and string segments.
		/// The result list always starts with a code segment and alternates:
		/// [code, string, code, string, ..., code]. If needed, an empty string
		/// is appended at the end to keep the even-pair structure.
		/// Code segments contain tokens stripped from trivia; string segments
		/// contain unescaped literal contents and interpolated string text parts.
		/// Interpolated expressions are emitted as code segments without braces.
		/// </summary>
		/// <param name="_sourceCode">Input C# source code.</param>
		/// <returns>Alternating list of code and string items starting with code.</returns>
		public static List<string> SeparateCodeAndStrings( string _sourceCode )
		{
#if UITK_USE_ROSLYN
			var result = new List<string>();

			var tree = CSharpSyntaxTree.ParseText(_sourceCode);
			var root = tree.GetRoot();

			ProcessNode(result, root);

			// If odd, add empty string to ensure always code/string pairs
			if (result.Count.IsOdd())
				result.Add("");

			return result;
#else
			throw new RoslynUnavailableException();
#endif
		}

		/// <summary>
		/// Applies a previously recorded rewiring registry, if present in the current context scene.
		/// This replaces UI.Text with TMP, then rewires all recorded object references
		/// to point to the new TMP components.
		/// </summary>
		/// <param name="_replaced">Number of Text components that were replaced by TMP components.</param>
		/// <param name="_rewired">Number of serialized object references successfully rewired to TMP.</param>
		/// <param name="_missing">Number of rewiring attempts that could not be completed (missing objects or types).</param>
		/// <returns>True if a registry was found and applied; false otherwise.</returns>
		public static bool ApplyRewireRegistryIfFound( out int _replaced, out int _rewired, out int _missing )
		{
#if UITK_USE_ROSLYN
			_replaced = 0;
			_rewired = 0;
			_missing = 0;

			var scene = GetCurrentContextScene(out bool isPrefab);
			if (!scene.IsValid())
				throw new InvalidOperationException("No valid scene or prefab stage.");

			if (!ReferencesRewireRegistry.TryGetRegistryWithEntries(scene, out ReferencesRewireRegistry reg))
				return false;

			LogReplacement("Apply Rewire Registry");
			// 1) Replace components (mapping only; no SerializedProperty writes in this step)
			var replacedList = ReplaceUITextWithTMPInActiveScene();
			_replaced = replacedList.Count;

			// Rewire from registry
			foreach (var e in reg.Entries)
			{
				if (!e.Owner || !e.TargetGameObject)
				{
					UiLog.LogError($"Missing: No owner or target game object found for property path '{e.PropertyPath}'");
					_missing++;
					continue;
				}

				var tmp = e.TargetGameObject.GetComponent<TextMeshProUGUI>();
				if (!tmp)
				{
					tmp = e.TargetGameObject.GetComponent<TMP_Text>() as TextMeshProUGUI;
					if (tmp == null)
					{
						UiLog.LogError($"Missing: No {nameof(TextMeshProUGUI)} found for property path '{e.PropertyPath}' on target object:'{e.TargetGameObject}'", e.TargetGameObject);
						_missing++;
						continue;
					}
				}

				EditorUtility.SetDirty(e.TargetGameObject);

				var so = new SerializedObject(e.Owner);
				var sp = so.FindProperty(e.PropertyPath);
				if (sp == null || sp.propertyType != SerializedPropertyType.ObjectReference)
				{
					UiLog.LogError($"Missing: No property found for property path '{e.PropertyPath}' on target object:'{e.TargetGameObject}'", e.TargetGameObject);
					_missing++;
					continue;
				}

				if (sp.objectReferenceValue != tmp)
				{
					Undo.RecordObject(e.Owner, "Rewire TMP reference");
					sp.objectReferenceValue = tmp;
					so.ApplyModifiedProperties();
					EditorUtility.SetDirty(e.Owner);
					_rewired++;
				}
			}

			// Remove registry GO
			Undo.DestroyObjectImmediate(reg.gameObject);
			EditorSceneManager.MarkSceneDirty(scene);
			return true;
#else
			throw new RoslynUnavailableException();
#endif
		}

		/// <summary>
		/// Roslyn-backed C# source rewriter that replaces all usages of TA with TB.
		/// Adds a using for TB's namespace if requested and not already present.
		/// </summary>
		/// <typeparam name="TA">Source component type to replace (e.g. UnityEngine.UI.Text).</typeparam>
		/// <typeparam name="TB">Target component type to use (e.g. TMPro.TMP_Text).</typeparam>
		/// <param name="_sourceCode">Full C# source code to rewrite.</param>
		/// <param name="_addUsing">If true, ensure a using directive exists for TB's namespace.</param>
		/// <param name="_extraTypes">Optional extra runtime types whose assemblies should be referenced.</param>
		/// <returns>Rewritten C# source code.</returns>
		/// <exception cref="RoslynUnavailableException">Thrown when Roslyn is not available.</exception>
		public static string ReplaceMonoBehaviour<TA, TB>( string _sourceCode, bool _addUsing = true, params Type[] _extraTypes )
			where TA : MonoBehaviour
			where TB : MonoBehaviour
		{
#if UITK_USE_ROSLYN
			var refs = new List<MetadataReference>
			{
				MetadataReference.CreateFromFile(typeof(object).Assembly.Location),             // CoreLib
                MetadataReference.CreateFromFile(typeof(UnityEngine.Object).Assembly.Location), // UnityEngine.CoreModule
                MetadataReference.CreateFromFile(typeof(TA).Assembly.Location),
				MetadataReference.CreateFromFile(typeof(TB).Assembly.Location),
				MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location),
				MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location)
			};

			if (_extraTypes != null)
			{
				foreach (var t in _extraTypes)
				{
					try
					{
						refs.Add(MetadataReference.CreateFromFile(t.Assembly.Location));
					}
					catch
					{
						UiLog.LogError($"Assembly for Type {t.Name} not found!");
					}
				}
			}

			return RoslynComponentReplacer.ReplaceComponent<TA, TB>(
				_sourceCode,
				refs,
				_addUsing
			);
#else
			throw new RoslynUnavailableException();
#endif
		}

		/// <summary>
		/// Replaces all UnityEngine.UI.Text components in the active scene with TextMeshProUGUI components.
		/// Captures relevant data before removal and applies it to the new components.
		/// Also rewires references that previously pointed to the old Text objects.
		/// </summary>
		/// <returns>List of tuples containing the captured snapshot and the new TMP component.</returns>
		public static TextSnapshotList ReplaceUITextWithTMPInActiveScene()
		{
#if UITK_USE_ROSLYN
			// collect all object-reference properties that currently point to Text
			var refGroups = CollectRefGroupsToTextInActiveScene();

			return ReplaceMonoBehavioursInActiveSceneWithMapping(
				_capture: ( Text t ) => new TextSnapshot
				{
					Text = t.text,
					Color = t.color,
					FontSize = t.fontSize,
					Rich = t.supportRichText,
					AutoSize = t.resizeTextForBestFit,
					AutoSizeMin = t.resizeTextMinSize,
					AutoSizeMax = t.resizeTextMaxSize,
					Anchor = t.alignment,
					Raycast = t.raycastTarget,
					LineSpacing = t.lineSpacing,
					FontName = t.font ? t.font.name : null,
					FontStyle = t.fontStyle,

					OldId = t.GetInstanceID(), // key to rewire later
				},
				_apply: ( TextSnapshot s, TextMeshProUGUI tmp ) =>
				{
					tmp.text = s.Text;
					tmp.color = s.Color;
					tmp.fontSize = s.FontSize;
					tmp.richText = s.Rich;
					tmp.enableAutoSizing = s.AutoSize;
					tmp.fontSizeMin = s.AutoSizeMin > 0 ? s.AutoSizeMin : 18;
					tmp.fontSizeMax = s.AutoSizeMax > 0 ? s.AutoSizeMax : 72;
					tmp.raycastTarget = s.Raycast;
					tmp.lineSpacing = ConvertLineSpacingFromTextToTmp(s.LineSpacing);

					// map alignment
					switch (s.Anchor)
					{
						case TextAnchor.UpperLeft: tmp.alignment = TextAlignmentOptions.TopLeft; break;
						case TextAnchor.UpperCenter: tmp.alignment = TextAlignmentOptions.Top; break;
						case TextAnchor.UpperRight: tmp.alignment = TextAlignmentOptions.TopRight; break;
						case TextAnchor.MiddleLeft: tmp.alignment = TextAlignmentOptions.Left; break;
						case TextAnchor.MiddleCenter: tmp.alignment = TextAlignmentOptions.Center; break;
						case TextAnchor.MiddleRight: tmp.alignment = TextAlignmentOptions.Right; break;
						case TextAnchor.LowerLeft: tmp.alignment = TextAlignmentOptions.BottomLeft; break;
						case TextAnchor.LowerCenter: tmp.alignment = TextAlignmentOptions.Bottom; break;
						case TextAnchor.LowerRight: tmp.alignment = TextAlignmentOptions.BottomRight; break;
						default: tmp.alignment = TextAlignmentOptions.Center; break;
					}

					// assign TMP font asset and material based on legacy font name
					if (!string.IsNullOrEmpty(s.FontName))
					{
						var (fa, mat) = FindMatchingTMPFontAndMaterial(s.FontName);
						if (fa)
						{
							tmp.font = fa;
							// Optional: preset material if found (keine harte Pflicht)
							if (mat) tmp.fontSharedMaterial = mat;
						}
					}

					// map legacy font style (Bold/Italic) to TMP fontStyle
					tmp.fontStyle = MapFontStyle(s.FontStyle);
					tmp.SetVerticesDirty();
					tmp.SetLayoutDirty();
					EditorUtility.SetDirty(tmp);

					// rewire references
					RewireRefsForOldId(refGroups, s.OldId, tmp);
				}
			);
#else
			throw new RoslynUnavailableException();
#endif
		}

		/// <summary>
		/// Generic two-phase replacement in the active scene:
		/// 1) capture(TA) runs while the old component still exists,
		/// 2) TA is destroyed and TB is ensured on the same GameObject,
		/// 3) apply(snapshot, TB) runs to apply migrated data.
		/// This avoids conflicts (e.g., multiple Graphics on the same object).
		/// </summary>
		/// <typeparam name="TA">Old component type to remove.</typeparam>
		/// <typeparam name="TB">New component type to add or reuse.</typeparam>
		/// <typeparam name="TSnapshot">Type holding captured state from TA.</typeparam>
		/// <param name="_capture">Delegate that captures TA state before removal.</param>
		/// <param name="_apply">Delegate that applies captured state to TB.</param>
		/// <returns>List of tuples (captured snapshot, new component).</returns>
		/// <exception cref="RoslynUnavailableException">Thrown when Roslyn-related compilation guards are required.</exception>
		public static List<(TSnapshot Snapshot, TB NewComp)> ReplaceMonoBehavioursInActiveSceneWithMapping
		<TA, TB, TSnapshot>
		(
			Func<TA, TSnapshot> _capture,
			Action<TSnapshot, TB> _apply
		)
		where TA : MonoBehaviour
		where TB : MonoBehaviour
		{
#if UITK_USE_ROSLYN
			var scene = GetCurrentContextScene(out bool isPrefab);
			if (!scene.IsValid())
				throw new ArgumentException($"No scene found.");
			if (typeof(TA).IsAbstract)
				throw new ArgumentException($"{typeof(TA).Name} is abstract.");
			if (typeof(TB).IsAbstract)
				throw new ArgumentException($"{typeof(TB).Name} is abstract.");

			var results = new List<(TSnapshot, TB)>();

			var targets = EditorAssetUtility.FindObjectsInCurrentEditedPrefabOrScene<TA>();
			if (targets == null || targets.Length == 0)
				return results;

			Undo.IncrementCurrentGroup();
			Undo.SetCurrentGroupName($"Replace {typeof(TA).Name} with {typeof(TB).Name}");

			foreach (var oldComp in targets)
			{
				if (!oldComp)
					continue;

				// 1) Capture data while TA is still present
				var snapshot = _capture != null ? _capture(oldComp) : default;
				LogReplacement($"Captured text properties from '{oldComp.GetType().Name}' on '{oldComp.GetPath()}':\n{snapshot}");

				var go = oldComp.gameObject;

				// 1a) Remove blockers ...
				var blockers = CaptureAndRemoveBlockers(go, oldComp);

				// 1b) Ensure TB exists early, so we can deep-copy shared serialized data before destroying TA
				var newComp = go.GetComponent<TB>();
				bool createdNow = false;

				if (!newComp)
				{
					newComp = Undo.AddComponent<TB>(go);
					createdNow = true;

					if (!go.activeInHierarchy)
					{
						var tmpGO = new GameObject("__tmp_defaults__", typeof(RectTransform));
						var def = tmpGO.AddComponent<TB>();
						UnityEditorInternal.ComponentUtility.CopyComponent(def);
						UnityEditorInternal.ComponentUtility.PasteComponentValues(newComp);
						UnityEngine.Object.DestroyImmediate(tmpGO);
					}
				}

				if (!newComp)
				{
					RestoreBlockers(go, blockers);
					UiLog.LogError($"Failed to add target component {typeof(TB).Name} to '{go.GetPath()}'.", go);
					continue;
				}

				// 1c) Deep copy shared serialized properties while oldComp still exists
				int sharedCopied = CopySharedSerializedPropertiesImmediate(oldComp, newComp);
				LogReplacement($"Copied {sharedCopied} shared serialized properties TA->TB on '{go.GetPath()}'");

				// 2) Now remove TA safely
				if (!oldComp.CanBeDestroyed(out string reasons))
				{
					string s = $"Can not replace '{go.GetPath()}'\nReason(s): {reasons}";
					UiLog.LogError(s, oldComp);
					LogReplacement($"Error:{s}");

					// rollback created TB if we created it now
					if (createdNow && newComp)
						Undo.DestroyObjectImmediate(newComp);

					RestoreBlockers(go, blockers);
					continue;
				}

				Undo.RegisterCompleteObjectUndo(go, "Remove Source MonoBehaviour");
				Undo.DestroyObjectImmediate(oldComp);

				// 4) Apply captured data / special mapping
				_apply?.Invoke(snapshot, newComp);

				// 5) Restore blockers
				RestoreBlockers(go, blockers);

				results.Add((snapshot, newComp));
				results.Add((snapshot, newComp));
			}

			EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
			Undo.CollapseUndoOperations(Undo.GetCurrentGroup());

			return results;
#else
			throw new RoslynUnavailableException();
#endif
		}

		/// <summary>
		/// Scans scripts referenced by MonoBehaviours in the current context scene that reference UnityEngine.UI.Text,
		/// and uses the Roslyn rewriter to replace usages with TMP, saving and requesting recompilation if needed.
		/// </summary>
		/// <returns>Number of source files that were modified.</returns>
		public static int ReplaceMonoBehavioursInContextScene<T1, T2>()
		where T1 : MonoBehaviour
		where T2 : MonoBehaviour
		{
#if UITK_USE_ROSLYN
			var scriptPaths = CollectScriptPathsInContextSceneReferencing<T1>();
			if (scriptPaths == null || scriptPaths.Count == 0)
			{
				LogReplacement("No referring scripts found");
				return 0;
			}

			int changed = 0;
			foreach (var path in scriptPaths)
			{
				if (!path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
					continue;

				LogReplacement($"Replace Text component references in '{path}' with Text Mesh Pro references");
				var src = System.IO.File.ReadAllText(path);
				var dst = ReplaceMonoBehaviour<T1, T2>(src, _addUsing: true);

				if (!string.Equals(src, dst, StringComparison.Ordinal))
				{
					System.IO.File.WriteAllText(path, dst);
					changed++;
				}
			}

			if (changed > 0)
			{
				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
				CompilationPipeline.RequestScriptCompilation();
				UiLog.LogInternal($"[Code Replace Text->TMP] Changed {changed} files, requested compilation.");
			}
			else
			{
				UiLog.LogInternal("[Code Replace Text->TMP] No changes in scripts.");
			}

			return changed;
#else
			throw new RoslynUnavailableException();
#endif
		}

		/// <summary>
		/// One-click helper that performs:
		/// 1) Prepare (collect references into registry)
		/// 2) Code replacement in project files
		/// 3) Request compilation
		/// After domain reload, ApplyRewireRegistryIfFound will finalize rewiring.
		/// </summary>
		public static void ReplaceTextWithTextMeshProInCurrentContext()
		{
#if UITK_USE_ROSLYN
			var scene = GetCurrentContextScene(out bool isPrefab);
			if (!scene.IsValid())
			{
				UiLog.LogError("Scene is invalid");
				return;
			}

			ComponentReplaceLog.LogCr(2);
			LogReplacement($"___ Starting replacement of scene '{ComponentReplaceLog.GetLogScenePath()}' ___");
			int prepared = PrepareRewiring<Text, TextMeshProUGUI>();
			int replacedReferences = ReplaceMonoBehavioursInContextScene<Text, TextMeshProUGUI>();

			if (prepared == 0 && replacedReferences == 0)
			{
				LogReplacement("No Text references found; replacing Text -> TextMeshPro directly without domain reload");
				ReplaceUITextWithTMPInActiveScene();
			}
#else
			throw new RoslynUnavailableException();
#endif

		}

		public static void ReplaceMonoBehaviourInCurrentContext<T1, T2>()
			where T1 : MonoBehaviour
			where T2 : MonoBehaviour
		{
#if UITK_USE_ROSLYN
			var scene = GetCurrentContextScene(out bool isPrefab);
			if (!scene.IsValid())
			{
				UiLog.LogError("Scene is invalid");
				return;
			}

			ComponentReplaceLog.LogCr(2);
			LogReplacement($"___ Starting replacement of scene '{ComponentReplaceLog.GetLogScenePath()}' ___");

			int prepared = PrepareRewiring<T1, T2>();
			int replacedReferences = ReplaceMonoBehavioursInContextScene<T1, T2>();

			if (prepared == 0 && replacedReferences == 0)
			{
				LogReplacement("No code references found; replacing components directly in scene/prefab context");
				ReplaceMonoBehavioursInActiveSceneGeneric<T1, T2>();
			}
#else
			throw new RoslynUnavailableException();
#endif
		}

		/// <summary>
		/// Returns the "current context" scene:
		/// - Prefab Stage scene if currently editing a prefab,
		/// - Otherwise the active scene.
		/// </summary>
		/// <returns>Scene to operate on.</returns>
		public static Scene GetCurrentContextScene( out bool _isPrefab )
		{
			_isPrefab = false;
			var stage = PrefabStageUtility.GetCurrentPrefabStage();
			if (stage != null && stage.scene.IsValid())
			{
				_isPrefab = true;
				return stage.scene;
			}

			return SceneManager.GetActiveScene();
		}

		public static Scene GetCurrentContextScene() => GetCurrentContextScene(out var _);

		/// <summary>
		/// Returns the root GameObjects of the current context scene.
		/// </summary>
		/// <returns>Array of root GameObjects.</returns>
		public static GameObject[] GetCurrentContextSceneRoots() => GetCurrentContextScene().GetRootGameObjects();


		/// <summary>
		/// Step 1 of the migration: collects all object-reference properties in the current context
		/// that reference UnityEngine.UI.Text. Stores them in a hidden registry GameObject inside
		/// the scene so the data survives domain reload and can be applied afterward.
		/// </summary>
		/// <returns>Number of collected references recorded into the registry.</returns>
		public static int PrepareRewiring<T1,T2>()
		where T1:MonoBehaviour
		where T2:MonoBehaviour
		{
#if UITK_USE_ROSLYN
			var scene = GetCurrentContextScene();

			if (!scene.IsValid())
				throw new InvalidOperationException("No valid scene or prefab stage.");

			var reg = ReferencesRewireRegistry.GetOrCreate(scene);
			if (reg == null)
				throw new Exception("Unexpected: could not create Registry object");

			reg.Entries.Clear();

			var components = CollectMonoBehavioursInContextSceneReferencing<Text>();

			if (components == null || components.Count == 0)
			{
				LogReplacement("No referring components found");
				return 0;
			}


			int count = 0;

			// Iterate all components in the context scene only
			foreach (var comp in components)
			{
				if (!comp) continue;

				var so = new SerializedObject(comp);
				var it = so.GetIterator();
				var enterChildren = true;

				while (it.NextVisible(enterChildren))
				{
					enterChildren = false;

					if (it.propertyType != SerializedPropertyType.ObjectReference)
						continue;

					var obj = it.objectReferenceValue;
					var monoBehaviour = obj as MonoBehaviour;
					if (!monoBehaviour)
						continue;

					reg.Entries.Add(new ReferencesRewireRegistry.Entry
					{
						Owner = comp,
						PropertyPath = it.propertyPath,
						TargetGameObject = monoBehaviour.gameObject,
						OldType = typeof(T1),
						NewType = typeof(T2)
					});
					count++;
				}
			}

			if (count > 0)
			{
				EditorSceneManager.MarkSceneDirty(scene);
				UiLog.LogInternal($"[Prepare {typeof(T1).Name} -> {typeof(T2).Name}] Recorded {count} references in context '{scene.path}'.");
			}
			else
			{
				UiLog.LogInternal($"[Prepare {typeof(T1).Name} -> {typeof(T2).Name}] No references found in current context.");
			}

			return count;
#else
			throw new RoslynUnavailableException();
#endif

		}

#if UITK_USE_ROSLYN

		/// <summary>
		/// Scans all components in the active scene for object reference properties
		/// that currently point to a UnityEngine.UI.Text, and groups them by the
		/// instance ID of the referenced Text component.
		/// </summary>
		/// <returns>Dictionary from old Text instance ID to a list of (owner, propertyPath) pairs.</returns>
		private static OwnerAndPathListById CollectRefGroupsToTextInActiveScene()
		{
			var result = new OwnerAndPathListById();

			var allComponents = EditorAssetUtility.FindObjectsInCurrentEditedPrefabOrScene<MonoBehaviour>();

			foreach (var comp in allComponents)
			{
				if (!comp)
					continue;

				var so = new SerializedObject(comp);
				var it = so.GetIterator();
				var enterChildren = true;

				while (it.NextVisible(enterChildren))
				{
					enterChildren = false;

					if (it.propertyType != SerializedPropertyType.ObjectReference)
						continue;

					var obj = it.objectReferenceValue;
					if (!obj)
						continue;

					var txt = obj as Text;
					if (!txt)
						continue;

					var id = txt.GetInstanceID();
					if (!result.TryGetValue(id, out var list))
					{
						list = new OwnerAndPathList();
						result.Add(id, list);
					}

					list.Add((comp, it.propertyPath));
				}
			}

			return result;
		}

		/// <summary>
		/// Rewires all serialized object references recorded for the given old instance ID
		/// to point at the provided new TMP_Text target.
		/// </summary>
		/// <param name="_groups">Group mapping from old instance IDs to (owner, propertyPath) lists.</param>
		/// <param name="_oldId">Instance ID of the original Text component.</param>
		/// <param name="_newTarget">The new TMP component that references should point to.</param>
		private static void RewireRefsForOldId
		(
			OwnerAndPathListById _groups,
			int _oldId,
			TMP_Text _newTarget
		)
		{
			if (_newTarget == null)
				return;

			if (_groups == null)
				return;

			if (!_groups.TryGetValue(_oldId, out var props) || props == null || props.Count == 0)
				return;

			foreach (var (owner, path) in props)
			{
				if (!owner)
					continue;

				var so = new SerializedObject(owner);
				var sp = so.FindProperty(path);
				if (sp == null || sp.propertyType != SerializedPropertyType.ObjectReference)
					continue;

				if (sp.objectReferenceValue != _newTarget)
				{
					Undo.RecordObject(owner, "Rewire TMP reference");
					sp.objectReferenceValue = _newTarget;
					so.ApplyModifiedProperties();
					EditorUtility.SetDirty(owner);
				}
			}
		}

		// Convert legacy uGUI lineSpacing (multiplier) to TMP (percent of font size)
		private static float ConvertLineSpacingFromTextToTmp( float _legacyMultiplier )
		{
			// uGUI: 1.0 = normal, 1.2 = +20%
			// TMP:  0   = normal, 20   = +20% (in % of point size)
			return (_legacyMultiplier - 1f) * 100f;
		}

		// Helper: detect components that require a Graphic on the same GameObject
		private static bool RequiresGraphic( Type t )
		{
			// Walk [RequireComponent] attributes (can be multiple)
			var reqs = (RequireComponent[])Attribute.GetCustomAttributes(t, typeof(RequireComponent), inherit: true);
			if (reqs == null || reqs.Length == 0) return false;

			foreach (var r in reqs)
			{
				if (IsGraphic(r.m_Type0) || IsGraphic(r.m_Type1) || IsGraphic(r.m_Type2))
					return true;
			}

			return false;

			bool IsGraphic( Type x ) => x != null && (x == typeof(Graphic) || x == typeof(MaskableGraphic));
		}

		// Helper: capture, destroy, later restore "blocker" components
		private struct BlockerSnapshot
		{
			public Type Type;
			public string Json;   // EditorJsonUtility snapshot
		}

		private static List<BlockerSnapshot> CaptureAndRemoveBlockers( GameObject go, MonoBehaviour savedMonoBehaviour )
		{
			var blockers = new List<BlockerSnapshot>();
			var monoBehaviours = go.GetComponents<MonoBehaviour>();

			foreach (var monoBehaviour in monoBehaviours)
			{
				if (!monoBehaviour)
					continue;
				if (monoBehaviour == savedMonoBehaviour)
					continue;

				var ct = monoBehaviour.GetType();
				if (!RequiresGraphic(ct))
					continue;

				// serialize state
				string json = EditorJsonUtility.ToJson(monoBehaviour, true);
				blockers.Add(new BlockerSnapshot { Type = ct, Json = json });

				LogReplacement($"Temporarily delete '{monoBehaviour.GetType().Name}' on '{monoBehaviour.GetPath()}' due to dependencies");
				// remove now (so Text can be removed without failing RequireComponent)
				Undo.DestroyObjectImmediate(monoBehaviour);
			}

			return blockers;
		}

		private static void RestoreBlockers( GameObject _go, List<BlockerSnapshot> _blockers )
		{
			if (_blockers == null) return;

			foreach (var blocker in _blockers)
			{
				if (blocker.Type == null) continue;
				var restored = Undo.AddComponent(_go, blocker.Type);
				if (restored == null)
				{
					LogReplacement($"Error: Can not restore '{blocker.Type.Name}' on '{_go.GetPath()}'");
					continue;
				}

				LogReplacement($"Restored '{blocker.Type.Name}' on '{_go.GetPath()}'");
				if (!string.IsNullOrEmpty(blocker.Json))
				{
					// restore serialized values
					LogReplacement($"Restoreding properties for '{blocker.Type.Name}' on '{_go.GetPath()}':\n{blocker.Json}");
					EditorJsonUtility.FromJsonOverwrite(blocker.Json, restored);
					EditorUtility.SetDirty(restored);
				}
			}
		}

		/// <summary>
		/// Recursively walks the syntax tree to build alternating code and string segments.
		/// String literal content and interpolated string text are appended to the current
		/// string slot (unescaped). Non-string tokens contribute to the code slot with
		/// minimal spacing to separate adjacent identifiers.
		/// </summary>
		/// <param name="_result">Accumulator list for alternating code/string parts.</param>
		/// <param name="_node">Current syntax node to process.</param>
		private static void ProcessNode( List<string> _result, SyntaxNode _node )
		{
			foreach (var child in _node.ChildNodesAndTokens())
			{
				if (child.IsNode)
				{
					var childNode = child.AsNode();

					switch (childNode)
					{
						case LiteralExpressionSyntax literalExpr
							when literalExpr.IsKind(SyntaxKind.StringLiteralExpression):
							EnsureStringSlot(_result);
							_result[_result.Count - 1] += literalExpr.Token.ValueText; // unescaped
							break;

						case InterpolatedStringTextSyntax interpText:
							EnsureStringSlot(_result);
							_result[_result.Count - 1] += interpText.TextToken.ValueText; // unescaped
							break;

						case InterpolationSyntax interp:
							// We are currently in a code slot; first, close it by inserting an empty string segment
							EnsureStringSlot(_result);

							// Now create a fresh code slot for the inner expression
							EnsureCodeSlot(_result);

							// Recurse only into the inner expression (no braces)
							ProcessNode(_result, interp.Expression);

							// And ensure a trailing empty string to separate from the following code/text
							EnsureStringSlot(_result);
							break;
						default:
							// descend into other nodes
							ProcessNode(_result, childNode);
							break;
					}
				}
				else
				{
					// TOKEN: use .Text (no trivia) -> strips comments and whitespace automatically
					var token = child.AsToken();
					var text = token.Text;
					if (!string.IsNullOrEmpty(text))
					{
						EnsureCodeSlot(_result);
						AppendTokenWithMinimalSpace(_result, text);
					}
				}
			}
		}

		/// <summary>
		/// Appends a token into the current code slot, inserting a single space only
		/// when both the trailing left char and the leading right char are "wordy"
		/// (letters, digits or underscore). Also trims quotes and '$' at boundaries
		/// that can appear around interpolated string tokens.
		/// </summary>
		/// <param name="_list">Alternating code/string list.</param>
		/// <param name="_tokenText">Token text to append.</param>
		private static void AppendTokenWithMinimalSpace( List<string> _list, string _tokenText )
		{
			// We are in a code slot by contract
			var idx = _list.Count - 1;
			var current = _list[idx];

			if (NeedsSpace(current, _tokenText))
				current += " ";

			current += _tokenText;
			_list[idx] = current.Trim('"', '$');
		}

		/// <summary>
		/// Determines whether a space is needed between two token strings to avoid
		/// merging adjacent identifiers (e.g., "int" + "x" -> "int x").
		/// </summary>
		/// <param name="_left">Current accumulated code segment.</param>
		/// <param name="_right">Next token to append.</param>
		/// <returns>True if a separating space is required; otherwise false.</returns>
		private static bool NeedsSpace( string _left, string _right )
		{
			if (string.IsNullOrEmpty(_left) || string.IsNullOrEmpty(_right))
				return false;

			char lc = _left[_left.Length - 1];
			char rc = _right[0];

			// add a space only when both sides are "wordy" (identifier-ish)
			bool wordLeft = char.IsLetterOrDigit(lc) || lc == '_';
			bool wordRight = char.IsLetterOrDigit(rc) || rc == '_';

			return wordLeft && wordRight;
		}

		/// <summary>
		/// Ensures the current slot is a code slot. If the list is empty or already
		/// ends with a code slot, a new empty code slot is appended.
		/// </summary>
		/// <param name="_result">Alternating code/string list.</param>
		private static void EnsureCodeSlot( List<string> _result )
		{
			// If list is empty, start with a code slot
			// Also, add an empty string entry if last slot was code
			if (_result.Count == 0 || _result.Count.IsEven())
				_result.Add("");
		}

		/// <summary>
		/// Ensures the current slot is a string slot. If the list ends with a string slot,
		/// appends an empty code slot to alternate properly.
		/// </summary>
		/// <param name="_list">Alternating code/string list.</param>
		private static void EnsureStringSlot( List<string> _list )
		{
			// Add an empty code entry if last slot was string
			if (_list.Count.IsOdd())
				_list.Add("");
		}

		private static void LogReplacement( string _msg )
		{
			ComponentReplaceLog.Log(_msg);
		}
#endif
	}
}
