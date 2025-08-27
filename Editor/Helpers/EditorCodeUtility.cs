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

#if UITK_USE_ROSLYN
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using GuiToolkit.Editor.Roslyn;
#endif

using OwnerAndPathList = System.Collections.Generic.List<(UnityEngine.Object owner, string propertyPath)>;
using OwnerAndPathListById = System.Collections.Generic.Dictionary<int, System.Collections.Generic.List<(UnityEngine.Object owner, string propertyPath)>>;
using TextSnapshotList = System.Collections.Generic.List<(GuiToolkit.Editor.EditorCodeUtility.TextSnapshot Snapshot, TMPro.TextMeshProUGUI NewComp)>;

namespace GuiToolkit.Editor
{
	/// <summary>
	/// Exception thrown when Roslyn-based parsing or rewriting is not available
	/// for the current Unity version or environment.
	/// </summary>
	public sealed class RoslynUnavailableException : NotSupportedException
	{
		/// <summary>
		/// Creates a new instance that explains how to enable Roslyn support.
		/// </summary>
		public RoslynUnavailableException()
			: base($"Roslyn-based parsing is not available in this Unity version.\n" +
				   $"Install Roslyn via menu '{StringConstants.ROSLYN_INSTALL_HACK}' " +
					"or run this on Unity 6+ where Roslyn in package is supported.")
		{ }
	}

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
			/// <summary>Legacy Text alignment anchor.</summary>
			public TextAnchor Anchor;
			/// <summary>Whether the legacy Text was a raycast target.</summary>
			public bool Raycast;
			/// <summary>Legacy line spacing.</summary>
			public float LineSpacing;

			/// <summary>
			/// Instance ID of the original Text component.
			/// Used to rewire serialized object references to the new component.
			/// </summary>
			public int OldId;
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
			_replaced = 0;
			_rewired = 0;
			_missing = 0;

			var scene = GetCurrentContextScene();
			if (!scene.IsValid())
				throw new InvalidOperationException("No valid scene or prefab stage.");

			if (!ReferencesRewireRegistry.TryGetRegistryWithEntries(scene, out ReferencesRewireRegistry reg))
				return false;

			// 1) Replace components (mapping only; no SerializedProperty writes in this step)
			var replacedList = ReplaceUITextWithTMPInActiveScene();
			_replaced = replacedList.Count;

			// Rewire from registry
			foreach (var e in reg.Entries)
			{
				if (!e.Owner || !e.TargetGameObject)
				{
					Debug.LogError($"Missing: No owner or target game object found for property path '{e.PropertyPath}'");
					_missing++;
					continue;
				}

				var tmp = e.TargetGameObject.GetComponent<TextMeshProUGUI>();
				if (!tmp)
				{
					tmp = e.TargetGameObject.GetComponent<TMP_Text>() as TextMeshProUGUI;
					if (tmp == null)
					{
						Debug.LogError($"Missing: No {nameof(TextMeshProUGUI)} found for property path '{e.PropertyPath}' on target object:'{e.TargetGameObject}'", e.TargetGameObject);
						_missing++;
						continue;
					}
				}

				var so = new SerializedObject(e.Owner);
				var sp = so.FindProperty(e.PropertyPath);
				if (sp == null || sp.propertyType != SerializedPropertyType.ObjectReference)
				{
					Debug.LogError($"Missing: No property found for property path '{e.PropertyPath}' on target object:'{e.TargetGameObject}'", e.TargetGameObject);
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

		/// <summary>
		/// Replaces all UnityEngine.UI.Text components in the active scene with TextMeshProUGUI components.
		/// Captures relevant data before removal and applies it to the new components.
		/// Also rewires references that previously pointed to the old Text objects.
		/// </summary>
		/// <returns>List of tuples containing the captured snapshot and the new TMP component.</returns>
		public static TextSnapshotList ReplaceUITextWithTMPInActiveScene()
		{
			// collect all object-reference properties that currently point to Text
			var refGroups = CollectRefGroupsToTextInActiveScene();

			return ReplaceComponentsInActiveSceneWithMapping(
				_capture: ( Text t ) => new TextSnapshot
				{
					Text = t.text,
					Color = t.color,
					FontSize = t.fontSize,
					Rich = t.supportRichText,
					AutoSize = t.resizeTextForBestFit,
					Anchor = t.alignment,
					Raycast = t.raycastTarget,
					LineSpacing = t.lineSpacing,
					OldId = t.GetInstanceID(), // key to rewire later
				},
				_apply: ( TextSnapshot s, TextMeshProUGUI tmp ) =>
				{
					// map fields
					tmp.text = s.Text;
					tmp.color = s.Color;
					tmp.fontSize = s.FontSize;
					tmp.richText = s.Rich;
					tmp.enableAutoSizing = s.AutoSize;
					tmp.raycastTarget = s.Raycast;
					tmp.lineSpacing = s.LineSpacing;

					switch (s.Anchor)
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
					RewireRefsForOldId(refGroups, s.OldId, tmp);
				}
			);
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
		public static List<(TSnapshot Snapshot, TB NewComp)> ReplaceComponentsInActiveSceneWithMapping
		<TA, TB, TSnapshot>
		(
			Func<TA, TSnapshot> _capture,
			Action<TSnapshot, TB> _apply
		)
		where TA : Component
		where TB : Component
		{
#if UITK_USE_ROSLYN
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
				
				var go = oldComp.gameObject;

				// 1) Capture data while TA is still present
				var snapshot = _capture != null ? _capture(oldComp) : default;

				// 2) Remove TA first (prevents co-existence conflicts like multiple Graphics)
				if (!oldComp.CanBeDestroyed(out string reasons))
				{
					Debug.LogError($"Can not replace '{go.GetPath()}'\nReason(s): {reasons}", oldComp);
					continue;
				}
				
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
				_apply?.Invoke(snapshot, newComp);

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
		public static int ReplaceTextInContextSceneWithTextMeshPro()
		{
			var scriptPaths = CollectScriptPathsInContextSceneReferencing<Text>();
			if (scriptPaths == null || scriptPaths.Count == 0)
				return 0;

			int changed = 0;
			foreach (var path in scriptPaths)
			{
				if (!path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
					continue;

				var src = System.IO.File.ReadAllText(path);
				var dst = ReplaceComponent<Text, TMP_Text>(src, _addUsing: true);

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
				Debug.Log($"[Code Replace Text->TMP] Changed {changed} files, requested compilation.");
			}
			else
			{
				Debug.Log("[Code Replace Text->TMP] No changes in scripts.");
			}

			return changed;
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
			PrepareUITextToTMPInContextScene();
			ReplaceTextInContextSceneWithTextMeshPro();
		}

		/// <summary>
		/// Returns the "current context" scene:
		/// - Prefab Stage scene if currently editing a prefab,
		/// - Otherwise the active scene.
		/// </summary>
		/// <returns>Scene to operate on.</returns>
		public static Scene GetCurrentContextScene()
		{
			var stage = PrefabStageUtility.GetCurrentPrefabStage();
			if (stage != null && stage.scene.IsValid())
				return stage.scene;
			return SceneManager.GetActiveScene();
		}

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
		public static int PrepareUITextToTMPInContextScene()
		{
			var scene = GetCurrentContextScene();
			if (!scene.IsValid())
				throw new InvalidOperationException("No valid scene or prefab stage.");

			var reg = ReferencesRewireRegistry.GetOrCreate(scene);
			if (reg == null)
				throw new Exception("Unexpected: could not create Registry object");

			reg.Entries.Clear();

			var components = CollectMonoBehavioursInContextSceneReferencing<Text>();
			if (components == null || components.Count == 0)
				return 0;


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
					Text txt = obj as Text;
					if (!txt)
						continue;

					reg.Entries.Add(new ReferencesRewireRegistry.Entry
					{
						Owner = comp,
						PropertyPath = it.propertyPath,
						TargetGameObject = txt.gameObject,
						OldType = typeof(Text),
						NewType = typeof(TextMeshProUGUI)
					});
					count++;
				}
			}

			if (count > 0)
			{
				EditorSceneManager.MarkSceneDirty(scene);
				Debug.Log($"[Prepare Text->TMP] Recorded {count} references in context '{scene.path}'.");
			}
			else
			{
				Debug.Log("[Prepare Text->TMP] No references found in current context.");
			}

			return count;
		}

		/// <summary>
		/// Scans all components in the active scene for object reference properties
		/// that currently point to a UnityEngine.UI.Text, and groups them by the
		/// instance ID of the referenced Text component.
		/// </summary>
		/// <returns>Dictionary from old Text instance ID to a list of (owner, propertyPath) pairs.</returns>
		private static OwnerAndPathListById CollectRefGroupsToTextInActiveScene()
		{
			var result = new OwnerAndPathListById();

			var allComponents = EditorAssetUtility.FindObjectsInCurrentEditedPrefabOrScene<Component>();;

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
