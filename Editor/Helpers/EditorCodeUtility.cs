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
		private static readonly Dictionary<string, (TMP_FontAsset font, Material mat)> s_tmpFontLookupCache = new(StringComparer.OrdinalIgnoreCase);

		private static string NormalizeFontKey( string name )
		{
			if (string.IsNullOrEmpty(name)) return "";
			// normalize aggressively: lower, remove spaces, underscores, hyphens and common SDF tags
			var key = new string(name.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
			key = key.Replace("sdf", "").Replace("tmp", "");
			return key;
		}

		internal static (TMP_FontAsset font, Material mat) FindMatchingTMPFontAndMaterial( string legacyFontName )
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

		internal static TMPro.FontStyles MapFontStyle( FontStyle fs )
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

		// Convert legacy uGUI lineSpacing (multiplier) to TMP (percent of font size)
		internal static float ConvertLineSpacingFromTextToTmp( float _legacyMultiplier )
		{
			// uGUI: 1.0 = normal, 1.2 = +20%
			// TMP:  0   = normal, 20   = +20% (in % of point size)
			return (_legacyMultiplier - 1f) * 100f;
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
		public static string ReplaceComponent<TA, TB>( string _sourceCode, bool _addUsing = true, params Type[] _extraTypes )
			where TA : Component
			where TB : Component
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

#if UITK_USE_ROSLYN

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

#endif
	}
}
