#if UNITY_6000_0_OR_NEWER
#define UITK_USE_ROSLYN
#endif

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

#if UITK_USE_ROSLYN
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using GuiToolkit.Editor.Roslyn;
using UnityEditor;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;


#endif

namespace GuiToolkit.Editor
{
	public sealed class RoslynUnavailableException : NotSupportedException
	{
		public RoslynUnavailableException()
			: base($"Roslyn-based parsing is not available in this Unity version.\n" +
				   $"Install Roslyn via menu '{StringConstants.ROSLYN_INSTALL_HACK}' " +
					"or run this on Unity 6+ where Roslyn in package is supported.")
		{ }
	}

	public static class EditorCodeUtility
	{
		/// <summary>
		/// Checks if the string at a given index is followed by a localization function.
		/// </summary>
		public static bool IsFollowedByLocalizationFunction( List<string> _parts, int _index )
		{
			if (_index % 2 != 1 || _index + 1 >= _parts.Count)
				return false;

			var nextCode = _parts[_index + 1];
			return Regex.IsMatch(nextCode, @"\b(_|__|_n|gettext|ngettext)\s*\(");
		}

		/// <summary>
		/// Return the file path of caller.
		/// </summary>
		/// <param name="_path"></param>
		/// <returns></returns>
		public static string GetThisFilePath( [CallerFilePath] string _path = null ) => _path;

		/// <summary>
		/// Splits a C# source string into alternating code and string segments.
		/// The resulting list always starts with a code segment and alternates:
		/// [code, string, code, string, ..., code]. If necessary, an empty string
		/// is appended at the end to ensure this pattern.
		/// 
		/// - Code segments are raw source code.
		/// - String segments are unescaped literal or interpolated string parts.
		/// - Interpolated expressions (e.g. {value}) are included as code without braces.
		/// </summary>
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

		public static string ReplaceComponent<TA, TB>( string _sourceCode, bool _addUsing = true, params Type[] _extraTypes )
			where TA : Component
			where TB : Component
		{
#if UITK_USE_ROSLYN
			var refs = new List<MetadataReference>
			{
				MetadataReference.CreateFromFile(typeof(object).Assembly.Location),           // CoreLib
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
						Debug.LogError($"Assembly for Type {t.Name} not found!");
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

		// Public entry – specialized helper for UI.Text -> TextMeshProUGUI
		private struct TextSnapshot
		{
			public string text;
			public Color color;
			public float fontSize;
			public bool rich;
			public bool autoSize;
			public TextAnchor anchor;
			public bool raycast;
			public float lineSpacing;
		}

		public static List<(Text OldComp, TextMeshProUGUI NewComp)>
			ReplaceUITextWithTMPInActiveScene()
		{
			return ReplaceComponentsInActiveSceneWithMapping<Text, TextMeshProUGUI>(
				capture: ( Text t ) => new TextSnapshot
				{
					text = t.text,
					color = t.color,
					fontSize = t.fontSize,
					rich = t.supportRichText,
					autoSize = t.resizeTextForBestFit,
					anchor = t.alignment,
					raycast = t.raycastTarget,
					lineSpacing = t.lineSpacing,
				},
				apply: ( snapObj, tmp ) =>
				{
					var s = (TextSnapshot)snapObj;

					tmp.text = s.text;
					tmp.color = s.color;
					tmp.fontSize = s.fontSize;
					tmp.richText = s.rich;
					tmp.enableAutoSizing = s.autoSize;
					tmp.raycastTarget = s.raycast;
					tmp.lineSpacing = s.lineSpacing;

					switch (s.anchor)
					{
						case TextAnchor.UpperLeft: tmp.alignment = TMPro.TextAlignmentOptions.TopLeft; break;
						case TextAnchor.UpperCenter: tmp.alignment = TMPro.TextAlignmentOptions.Top; break;
						case TextAnchor.UpperRight: tmp.alignment = TMPro.TextAlignmentOptions.TopRight; break;
						case TextAnchor.MiddleLeft: tmp.alignment = TMPro.TextAlignmentOptions.Left; break;
						case TextAnchor.MiddleCenter: tmp.alignment = TMPro.TextAlignmentOptions.Center; break;
						case TextAnchor.MiddleRight: tmp.alignment = TMPro.TextAlignmentOptions.Right; break;
						case TextAnchor.LowerLeft: tmp.alignment = TMPro.TextAlignmentOptions.BottomLeft; break;
						case TextAnchor.LowerCenter: tmp.alignment = TMPro.TextAlignmentOptions.Bottom; break;
						case TextAnchor.LowerRight: tmp.alignment = TMPro.TextAlignmentOptions.BottomRight; break;
						default: tmp.alignment = TMPro.TextAlignmentOptions.Center; break;
					}
				});
		}

		/// <summary>
		/// Generic replace in active scene with a two-phase mapping:
		/// 1) capture(TA) is invoked while the old component still exists.
		/// 2) old is destroyed, new TB is added.
		/// 3) apply(captured, TB) is invoked to restore/migrate data.
		/// This avoids co-existence conflicts (e.g., multiple Graphics on one GO).
		/// </summary>
		public static List<(TA OldComp, TB NewComp)> ReplaceComponentsInActiveSceneWithMapping<TA, TB>(
			Func<TA, object> capture,
			Action<object, TB> apply )
			where TA : Component
			where TB : Component
		{
			if (typeof(TA).IsAbstract)
				throw new ArgumentException($"{typeof(TA).Name} is abstract.");
			if (typeof(TB).IsAbstract)
				throw new ArgumentException($"{typeof(TB).Name} is abstract.");

			var results = new List<(TA, TB)>();

			var targets = UnityEngine.Object.FindObjectsByType<TA>(FindObjectsInactive.Include, FindObjectsSortMode.None);

			if (targets == null || targets.Length == 0)
				return results;

			Undo.IncrementCurrentGroup();
			Undo.SetCurrentGroupName($"Replace {typeof(TA).Name} ? {typeof(TB).Name}");

			foreach (var oldComp in targets)
			{
				if (!oldComp) continue;
				var go = oldComp.gameObject;

				// 1) Capture data while TA is still present
				object snapshot = capture?.Invoke(oldComp);

				// 2) Remove TA first (prevents co-existence conflicts like multiple Graphics)
				Undo.RegisterCompleteObjectUndo(go, "Remove Source Component");
				Undo.DestroyObjectImmediate(oldComp);

				// 3) Ensure TB exists (reuse if already present; otherwise add)
				var newComp = go.GetComponent<TB>();
				if (!newComp)
				{
					Undo.RegisterCompleteObjectUndo(go, "Add Target Component");
					newComp = Undo.AddComponent<TB>(go);
				}

				// 4) Apply captured data to TB
				apply?.Invoke(snapshot, newComp);

				results.Add((null, newComp)); // oldComp no longer exists; set to null in tuple
			}

			EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
			Undo.CollapseUndoOperations(Undo.GetCurrentGroup());

			return results;
		}

		public static List<(TA OldComp, TB NewComp)> ReplaceComponentsInActiveScene<TA, TB>()
			where TA : Component
			where TB : Component
		{
			var results = new List<(TA, TB)>();

			var targets = UnityEngine.Object.FindObjectsOfType<TA>(true);
			if (targets == null || targets.Length == 0)
				return results;

			Undo.SetCurrentGroupName($"Replace {typeof(TA).Name} with {typeof(TB).Name}");

			foreach (var oldComp in targets)
			{
				var go = oldComp.gameObject;
				Undo.RegisterCompleteObjectUndo(go, "Replace Component");

				// Kopie der relevanten Werte/Felder kann später hier rein
				Undo.DestroyObjectImmediate(oldComp);

				var newComp = Undo.AddComponent<TB>(go);

				results.Add((oldComp, newComp));
			}

			UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
				UnityEngine.SceneManagement.SceneManager.GetActiveScene()
			);

			return results;
		}


#if UITK_USE_ROSLYN
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

		// Append token text into current code slot, inserting a single space only if needed
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

		private static void EnsureCodeSlot( List<string> _result )
		{
			// If list is empty, start with a code slot
			// Also, add an empty string entry if last slot was code
			if (_result.Count == 0 || _result.Count.IsEven())
				_result.Add("");
		}

		private static void EnsureStringSlot( List<string> _list )
		{
			// Add an empty code entry if last slot was string
			if (_list.Count.IsOdd())
				_list.Add("");
		}
#endif
	}
}