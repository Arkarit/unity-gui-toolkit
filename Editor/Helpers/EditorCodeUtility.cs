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
		public struct TextSnapshot
		{
			public string text;
			public Color color;
			public float fontSize;
			public bool rich;
			public bool autoSize;
			public TextAnchor anchor;
			public bool raycast;
			public float lineSpacing;

			// used to rewire serialized references to the new component if needed
			public int oldId;
		}

		public static List<(TextSnapshot Snapshot, TextMeshProUGUI NewComp)>
			ReplaceUITextWithTMPInActiveScene()
		{
			// collect all object-reference properties that currently point to Text
			var refGroups = CollectRefGroupsToTextInActiveScene();

			return ReplaceComponentsInActiveSceneWithMapping<Text, TextMeshProUGUI, TextSnapshot>(
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
					oldId = t.GetInstanceID(), // key to rewire later
				},
				apply: ( TextSnapshot s, TextMeshProUGUI tmp ) =>
				{
					// map fields
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

					// rewire any serialized references that pointed to the old Text (by oldId)
					RewireRefsForOldId(refGroups, s.oldId, tmp);
				}
			);
		}

		/// <summary>
		/// Generic replace in active scene with a two-phase mapping:
		/// 1) capture(TA) is invoked while the old component still exists.
		/// 2) old is destroyed, new TB is added.
		/// 3) apply(captured, TB) is invoked to restore/migrate data.
		/// This avoids co-existence conflicts (e.g., multiple Graphics on one GO).
		/// Returns a list of (Snapshot, NewComp).
		/// </summary>
		public static List<(TSnapshot Snapshot, TB NewComp)> ReplaceComponentsInActiveSceneWithMapping<TA, TB, TSnapshot>(
			Func<TA, TSnapshot> capture,
			Action<TSnapshot, TB> apply )
			where TA : Component
			where TB : Component
		{
#if UITK_USE_ROSLYN
			if (typeof(TA).IsAbstract)
				throw new ArgumentException($"{typeof(TA).Name} is abstract.");
			if (typeof(TB).IsAbstract)
				throw new ArgumentException($"{typeof(TB).Name} is abstract.");

			var results = new List<(TSnapshot, TB)>();

			var targets = UnityEngine.Object.FindObjectsByType<TA>(FindObjectsInactive.Include, FindObjectsSortMode.None);
			if (targets == null || targets.Length == 0)
				return results;

			Undo.IncrementCurrentGroup();
			Undo.SetCurrentGroupName($"Replace {typeof(TA).Name} with {typeof(TB).Name}");

			foreach (var oldComp in targets)
			{
				if (!oldComp) continue;
				var go = oldComp.gameObject;

				// 1) Capture data while TA is still present
				var snapshot = capture != null ? capture(oldComp) : default;

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
		/// Convenience wrapper that preserves the previous object-based mapping signature.
		/// Returns a list of (object Snapshot, NewComp).
		/// </summary>
		public static List<(object Snapshot, TB NewComp)> ReplaceComponentsInActiveSceneWithMapping<TA, TB>(
			Func<TA, object> capture,
			Action<object, TB> apply )
			where TA : Component
			where TB : Component
		{
			return ReplaceComponentsInActiveSceneWithMapping<TA, TB, object>(
				capture,
				apply
			);
		}

		public static List<(TA OldComp, TB NewComp)> ReplaceComponentsInActiveScene<TA, TB>()
			where TA : Component
			where TB : Component
		{
#if UITK_USE_ROSLYN
			var results = new List<(TA, TB)>();

			var targets = UnityEngine.Object.FindObjectsOfType<TA>(true);
			if (targets == null || targets.Length == 0)
				return results;

			Undo.SetCurrentGroupName($"Replace {typeof(TA).Name} with {typeof(TB).Name}");

			foreach (var oldComp in targets)
			{
				var go = oldComp.gameObject;
				Undo.RegisterCompleteObjectUndo(go, "Replace Component");

				// NOTE: simple swap without mapping
				Undo.DestroyObjectImmediate(oldComp);
				var newComp = Undo.AddComponent<TB>(go);

				results.Add((oldComp, newComp));
			}

			UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
				UnityEngine.SceneManagement.SceneManager.GetActiveScene()
			);

			return results;
#else
            throw new RoslynUnavailableException();
#endif
		}

		private static Dictionary<int, List<(UnityEngine.Object owner, string propertyPath)>> CollectRefGroupsToTextInActiveScene()
		{
#if UITK_USE_ROSLYN
			var map = new Dictionary<int, List<(UnityEngine.Object, string)>>();

#if UNITY_2023_1_OR_NEWER
			var allComponents = UnityEngine.Object.FindObjectsByType<Component>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
            var allComponents = UnityEngine.Object.FindObjectsOfType<Component>(true);
#endif

			foreach (var comp in allComponents)
			{
				if (!comp) continue;

				var so = new UnityEditor.SerializedObject(comp);
				var it = so.GetIterator();
				var enterChildren = true;

				while (it.NextVisible(enterChildren))
				{
					enterChildren = false;

					if (it.propertyType != UnityEditor.SerializedPropertyType.ObjectReference)
						continue;

					var obj = it.objectReferenceValue;
					if (!obj) continue;

					var txt = obj as UnityEngine.UI.Text;
					if (!txt) continue;

					var id = txt.GetInstanceID();
					if (!map.TryGetValue(id, out var list))
					{
						list = new List<(UnityEngine.Object, string)>();
						map.Add(id, list);
					}
					list.Add((comp, it.propertyPath));
				}
			}

			return map;
#else
            throw new RoslynUnavailableException();
#endif
		}

		private static void RewireRefsForOldId
		(
			Dictionary<int, List<(UnityEngine.Object owner, string propertyPath)>> groups,
			int oldId,
			TMPro.TMP_Text newTarget
		)
		{
#if UITK_USE_ROSLYN
			if (newTarget == null) return;
			if (groups == null) return;

			if (!groups.TryGetValue(oldId, out var props) || props == null || props.Count == 0)
				return;

			foreach (var (owner, path) in props)
			{
				if (!owner) continue;

				var so = new UnityEditor.SerializedObject(owner);
				var sp = so.FindProperty(path);
				if (sp == null || sp.propertyType != UnityEditor.SerializedPropertyType.ObjectReference)
					continue;

				if (sp.objectReferenceValue != (UnityEngine.Object)newTarget)
				{
					Undo.RecordObject(owner, "Rewire TMP reference");
					sp.objectReferenceValue = newTarget;
					so.ApplyModifiedProperties();
					UnityEditor.EditorUtility.SetDirty(owner);
				}
			}
#else
            throw new RoslynUnavailableException();
#endif
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
