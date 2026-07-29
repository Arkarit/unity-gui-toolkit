using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using GuiToolkit.Style;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit.Editor.AiSupport
{
	/// <summary>
	/// Milestone 2 of the AI screen-authoring effort: turns a JSON screen description (authored against
	/// the <see cref="UiScreenCatalog"/> vocabulary) into a real <c>.prefab</c> asset that the team can
	/// hand-edit afterward.
	///
	/// The toolkit's widgets are not self-contained, so screens are composed from two node kinds:
	/// <list type="bullet">
	/// <item><b>template</b> — instantiates a ready-made palette prefab (StandardButton, panel background,
	/// ...) as a nested prefab instance, keeping the link so template edits propagate.</item>
	/// <item><b>element</b> — creates a bare GameObject and adds a catalogued component (a UiView root,
	/// a layout group, a plain panel).</item>
	/// </list>
	///
	/// Runs entirely in Edit Mode (no Play Mode → no <see cref="EditorApplication.isPlaying"/> side
	/// effects, and the baked prefab can be screenshotted for the AI preview loop).
	///
	/// Marked <c>[EditorAware]</c>: baking touches toolkit singletons (e.g.
	/// <see cref="UiToolkitConfiguration"/>) which are gated behind editor-awareness. The baker is only
	/// ever entered from a menu item or an MCP request on the main thread, i.e. when assets are ready.
	/// </summary>
	[EditorAware]
	public static class UiScreenBaker
	{
		private const string OutputDir = "Assets/AiSupport/Generated";

		// Literal-text escape: bypasses localization on an otherwise-localized text component.
		private const string LiteralTextPrefix = "@text:";
		// Optional, purely cosmetic prefix an author may put on a loca key.
		private const string LocaKeyPrefix = "@loca:";

		private static Dictionary<string, Type> s_componentTypesByName;
		private static Dictionary<Type, Type> s_applierByTargetType;

		// Non-fatal issues collected during a single bake (dropped props, redirected/short-of-ideal
		// templates, unresolved text, …) so the caller can surface them instead of the author having to
		// grep the Editor log. Reset at the start of each Bake; the main-thread single-caller model makes
		// a plain static safe.
		private static List<string> s_warnings;

		// Two-pass wiring state (reset per bake): id -> the node's GameObject, and the "#id" reference
		// props deferred until the whole tree exists (references can point forward to later nodes).
		private static Dictionary<string, GameObject> s_nodesById;
		private static List<DeferredRef> s_deferredRefs;

		private struct DeferredRef
		{
			public Component component;
			public MemberRef member;
			public JToken value;
			public string ownerName;
		}

		// A settable member on a component — either a serialized field (the overwhelming majority) or a C#
		// property (native components like CanvasGroup have no reflectable fields, only properties). Lets the
		// baker write both through one path.
		private readonly struct MemberRef
		{
			public readonly FieldInfo Field;
			public readonly PropertyInfo Property;

			public MemberRef( FieldInfo _field ) { Field = _field; Property = null; }
			public MemberRef( PropertyInfo _property ) { Field = null; Property = _property; }

			public bool IsValid => Field != null || Property != null;
			public Type ValueType => Field != null ? Field.FieldType : Property.PropertyType;
			public string Name => Field != null ? Field.Name : Property.Name;

			public void SetValue( object _target, object _value )
			{
				if (Field != null)
					Field.SetValue(_target, _value);
				else
					Property.SetValue(_target, _value);
			}
		}

		private static void Warn( string _message )
		{
			s_warnings?.Add(_message);
			UiLog.LogWarning(_message);
		}

		/// <summary>Result of a bake: the written prefab path plus any non-fatal warnings.</summary>
		public class BakeResult
		{
			public string path;
			public List<string> warnings = new();

			/// <summary>Paths of the companion prefabs baked from the screen's "prefabs" array, in order.</summary>
			public List<string> companions = new();
		}

		/// <summary>Project-relative folder the baked prefabs are written to by default.</summary>
		public static string GeneratedDir => OutputDir;

		#region Public API

		/// <summary>
		/// Bakes a screen described by <paramref name="_screenJson"/> into a prefab asset and returns
		/// its project-relative path. Throws on malformed input so the caller (menu / MCP bridge) can
		/// surface a precise message.
		/// </summary>
		public static BakeResult Bake( string _screenJson )
		{
			if (string.IsNullOrWhiteSpace(_screenJson))
				throw new ArgumentException("Empty screen JSON.");

			JObject screen;
			try
			{
				screen = JObject.Parse(_screenJson);
			}
			catch (Exception e)
			{
				throw new ArgumentException($"Screen JSON is not valid JSON: {e.Message}");
			}

			string name = (string)screen["name"];
			if (string.IsNullOrEmpty(name))
				throw new ArgumentException("Screen JSON must have a non-empty \"name\".");

			var rootNode = screen["root"] as JObject;
			if (rootNode == null)
				throw new ArgumentException("Screen JSON must have a \"root\" node object.");

			// Companion prefabs, baked BEFORE this screen so it can reference them by name as templates:
			// one authoring call produces the repeated part as its own asset plus the screen that uses it,
			// instead of the screen carrying N copies of it. Each entry is a full screen description.
			// Done before the per-bake state below is initialised, because a nested Bake resets it.
			var companionPaths = new List<string>();
			var companionWarnings = new List<string>();
			if (screen["prefabs"] is JArray companions)
			{
				foreach (var companion in companions.OfType<JObject>())
				{
					var companionResult = Bake(companion.ToString());
					companionPaths.Add(companionResult.path);
					foreach (var w in companionResult.warnings)
						companionWarnings.Add($"[{(string)companion["name"]}] {w}");
				}
			}

			ResetCaches();
			s_warnings = new List<string>(companionWarnings);
			s_nodesById = new Dictionary<string, GameObject>(StringComparer.Ordinal);
			s_deferredRefs = new List<DeferredRef>();

			string path = ResolveOutputPath(screen, name);

			// Edit-preserving re-bake (opt-in): before rebuilding, fold hand edits made to the existing prefab
			// since the last bake back into this screen JSON, so a re-bake doesn't clobber them. Done at the
			// JSON level (then baked fresh) — no in-place prefab surgery, so no corruption risk.
			bool preserveEdits = (bool?)screen["preserveEdits"] ?? false;
			if (preserveEdits)
				ApplyEditPreservation(rootNode, path);

			WarnOnRepeatedSubtrees(rootNode);

			GameObject rootGo = null;
			try
			{
				rootGo = BuildNode(rootNode, null);
				ResolveDeferredRefs();

				EditorFileUtility.EnsureUnityFolderExists(ParentFolder(path));

				var saved = PrefabUtility.SaveAsPrefabAsset(rootGo, path, out bool success);
				if (!success || saved == null)
					throw new Exception($"PrefabUtility.SaveAsPrefabAsset failed for '{path}'.");

				// Sidecar = the (possibly merged) screen we actually baked → the new baseline for next time.
				WriteSourceSidecar(path, screen.ToString());

				AssetDatabase.Refresh();
				UiLog.LogInternal($"Baked screen '{name}' → '{path}'" +
					(s_warnings.Count > 0 ? $" ({s_warnings.Count} warning(s))." : "."));
				return new BakeResult { path = path, warnings = s_warnings, companions = companionPaths };
			}
			finally
			{
				if (rootGo != null)
					UnityEngine.Object.DestroyImmediate(rootGo);
			}
		}

		#region Repeated-subtree detection

		// Two siblings that look the same are already a copy — a human would have made the second one an
		// instance of the first. Deliberately strict: redundancy of this kind is cheap to create, invisible
		// afterwards, and a project drowns in it long before anyone decides to clean it up.
		private const int RepeatedSubtreeThreshold = 2;

		// Below this a "duplicate" is a bare graphic or label, where a prefab would cost more than it saves.
		private const int RepeatedSubtreeMinNodes = 3;

		/// <summary>
		/// Warns when a node has siblings that are structurally and visually the same subtree (same component
		/// or template, same styles, same prop keys, same child shape — only values differ), which means the
		/// author copied a subtree instead of authoring it once and referencing it. Template nodes are exempt:
		/// they already ARE references to one prefab, so repeating them is the intended pattern.
		/// </summary>
		private static void WarnOnRepeatedSubtrees( JObject _node )
		{
			if (_node["children"] is not JArray children)
				return;

			var groups = new Dictionary<string, List<JObject>>(StringComparer.Ordinal);
			foreach (var child in children.OfType<JObject>())
			{
				if (!string.IsNullOrEmpty((string)child["template"]))
					continue;

				string signature = StructuralSignature(child);
				if (!groups.TryGetValue(signature, out var list))
					groups[signature] = list = new List<JObject>();
				list.Add(child);
			}

			foreach (var kv in groups)
			{
				if (kv.Value.Count < RepeatedSubtreeThreshold)
					continue;
				if (CountNodes(kv.Value[0]) < RepeatedSubtreeMinNodes)
					continue;

				var names = kv.Value.Select(NodeLabel).ToList();
				Warn($"Node '{NodeLabel(_node)}' has {kv.Value.Count} structurally identical children " +
				     $"({string.Join(", ", names)}) — that is a copied subtree. Author it ONCE as its own prefab " +
				     "(add it to the screen's \"prefabs\" array), then reference it per instance with " +
				     "\"template\": \"<its name>\" and vary the parts via \"overrides\". Repeated rows/cards that " +
				     "are filled from data at runtime need only ONE authored instance, or none plus a container.");
			}

			foreach (var child in children.OfType<JObject>())
				WarnOnRepeatedSubtrees(child);
		}

		// Identity of a subtree's SHAPE and LOOK: component/template, stacked components, style, the set of
		// prop keys (not their values) and the same recursively for children. Values are excluded on purpose —
		// six cards differing only in title, icon and amount are exactly the case worth reporting.
		private static string StructuralSignature( JObject _node )
		{
			var sb = new StringBuilder();

			string template = (string)_node["template"];
			sb.Append(!string.IsNullOrEmpty(template) ? $"T:{template}" : $"C:{(string)_node["type"]}");

			if (_node["components"] is JArray components)
			{
				sb.Append('[');
				sb.Append(string.Join(",", components.Select(c => c is JObject o ? (string)o["type"] : (string)c)));
				sb.Append(']');
			}

			string style = (string)_node["style"];
			if (!string.IsNullOrEmpty(style))
				sb.Append("|s:").Append(style);

			if (_node["props"] is JObject props)
			{
				sb.Append("|p:");
				sb.Append(string.Join(",", props.Properties().Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal)));
			}

			if (_node["text"] != null)
				sb.Append("|t");

			if (_node["scroll"] != null)
				sb.Append("|sc");

			if (_node["children"] is JArray children)
			{
				sb.Append('(');
				foreach (var child in children.OfType<JObject>())
					sb.Append(StructuralSignature(child)).Append(';');
				sb.Append(')');
			}

			return sb.ToString();
		}

		private static int CountNodes( JObject _node )
		{
			int count = 1;
			if (_node["children"] is JArray children)
				foreach (var child in children.OfType<JObject>())
					count += CountNodes(child);
			return count;
		}

		private static string NodeLabel( JObject _node )
		{
			string id = (string)_node["id"];
			if (!string.IsNullOrEmpty(id))
				return id;
			string template = (string)_node["template"];
			if (!string.IsNullOrEmpty(template))
				return template;
			return (string)_node["type"] ?? "?";
		}

		#endregion

		// The output prefab path: an explicit "outputPath" on the screen (a full ".prefab" path used
		// verbatim, or a folder the screen name is appended to) wins over the default Generated folder.
		// Letting the author pin the path keeps the "edit → re-bake" loop intact after a prefab is moved.
		private static string ResolveOutputPath( JObject _screen, string _name )
		{
			string safeName = EditorFileUtility.GetSafeFileName(_name);
			string outputPath = ((string)_screen["outputPath"])?.Replace('\\', '/').TrimEnd('/');

			if (string.IsNullOrEmpty(outputPath))
				return $"{OutputDir}/{safeName}.prefab";
			if (outputPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
				return outputPath;
			return $"{outputPath}/{safeName}.prefab";
		}

		private static string ParentFolder( string _assetPath )
		{
			int slash = _assetPath.LastIndexOf('/');
			return slash > 0 ? _assetPath.Substring(0, slash) : "Assets";
		}

		// Project-relative suffix of the source-JSON sidecar written next to each baked prefab.
		private const string SidecarSuffix = ".screen.src.json";

		/// <summary>The source-JSON sidecar path for a baked prefab (e.g. Foo.prefab → Foo.screen.src.json).</summary>
		public static string SidecarPathFor( string _prefabPath )
			=> _prefabPath.Substring(0, _prefabPath.Length - ".prefab".Length) + SidecarSuffix;

		// Writes the screen JSON next to the baked prefab. This is the authoritative "last generated" baseline
		// the edit-preserving re-bake diffs against to tell hand edits apart from generated structure.
		private static void WriteSourceSidecar( string _prefabPath, string _screenJson )
		{
			try
			{
				string content;
				try { content = JObject.Parse(_screenJson).ToString(Newtonsoft.Json.Formatting.Indented); }
				catch { content = _screenJson; }
				System.IO.File.WriteAllText(System.IO.Path.GetFullPath(SidecarPathFor(_prefabPath)), content);
			}
			catch (Exception e)
			{
				Warn($"Could not write source sidecar for '{_prefabPath}': {e.Message}");
			}
		}

		[MenuItem(StringConstants.AI_BAKE_TEST_DIALOG_MENU_NAME)]
		private static void BakeTestDialogMenu()
		{
			try
			{
				string path = Bake(TestDialogJson).path;
				var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
				EditorGUIUtility.PingObject(asset);
				Selection.activeObject = asset;
			}
			catch (Exception e)
			{
				UiLog.LogError($"Bake Test Dialog failed: {e.Message}\n{e.StackTrace}");
			}
		}

		#endregion

		#region Edit-preserving re-bake

		// Folds hand edits made to an existing baked prefab back into the new screen JSON before it is rebuilt.
		// "Hand edit" = a prop/text on a node that differs from the sidecar baseline (what the baker last
		// generated) and that the NEW JSON does not itself specify. Such edits are copied into the new JSON so
		// the fresh bake keeps them; anything the new JSON specifies wins (the author's re-bake is authoritative).
		// Matching is by node id. No-op on a first bake (no prefab / no sidecar yet).
		private static void ApplyEditPreservation( JObject _newRoot, string _prefabPath )
		{
			try
			{
				if (AssetDatabase.LoadAssetAtPath<GameObject>(_prefabPath) == null)
					return; // first bake — nothing to preserve

				string sidecarPath = SidecarPathFor(_prefabPath);
				if (!System.IO.File.Exists(System.IO.Path.GetFullPath(sidecarPath)))
				{
					Warn($"preserveEdits: no baseline sidecar for '{_prefabPath}'; cannot tell hand edits apart, skipping preservation.");
					return;
				}

				var baselineRoot = JObject.Parse(System.IO.File.ReadAllText(System.IO.Path.GetFullPath(sidecarPath)))["root"] as JObject;
				var currentRoot = UiScreenReader.Read(_prefabPath, "structural").screen["root"] as JObject;
				if (baselineRoot == null || currentRoot == null)
					return;

				MergePreservedEdits(_newRoot, baselineRoot, currentRoot);
			}
			catch (Exception e)
			{
				Warn($"preserveEdits failed ('{e.Message}'); baking the screen as given without preservation.");
			}
		}

		/// <summary>
		/// The pure JSON merge behind <c>preserveEdits</c> (exposed for testing): folds hand edits — props/text/
		/// style present in <paramref name="_currentRoot"/> that differ from <paramref name="_baselineRoot"/>
		/// and that <paramref name="_newRoot"/> does not itself specify — into <paramref name="_newRoot"/>,
		/// matching nodes by id. The new JSON always wins where it specifies a value.
		/// </summary>
		public static void MergePreservedEdits( JObject _newRoot, JObject _baselineRoot, JObject _currentRoot )
		{
			var baselineById = new Dictionary<string, JObject>(StringComparer.Ordinal);
			var currentById = new Dictionary<string, JObject>(StringComparer.Ordinal);
			IndexById(_baselineRoot, baselineById);
			IndexById(_currentRoot, currentById);

			MergeNode(_newRoot, baselineById, currentById);
		}

		private static void IndexById( JObject _node, Dictionary<string, JObject> _map )
		{
			string id = (string)_node["id"];
			if (!string.IsNullOrEmpty(id))
				_map[id] = _node;
			if (_node["children"] is JArray children)
				foreach (var child in children.OfType<JObject>())
					IndexById(child, _map);
		}

		private static void MergeNode( JObject _newNode, Dictionary<string, JObject> _baselineById, Dictionary<string, JObject> _currentById )
		{
			string id = (string)_newNode["id"];
			if (!string.IsNullOrEmpty(id) && _currentById.TryGetValue(id, out var current))
			{
				_baselineById.TryGetValue(id, out var baseline);
				PreserveProps(_newNode, baseline, current, id);
				PreserveScalar(_newNode, baseline, current, "text", id);
				PreserveScalar(_newNode, baseline, current, "style", id);
			}

			if (_newNode["children"] is JArray children)
				foreach (var child in children.OfType<JObject>())
					MergeNode(child, _baselineById, _currentById);
		}

		// Copies props that were hand-edited (differ from baseline) and that the new node does not specify.
		private static void PreserveProps( JObject _newNode, JObject _baseline, JObject _current, string _id )
		{
			if (_current["props"] is not JObject currentProps)
				return;
			var baselineProps = _baseline?["props"] as JObject;
			var newProps = _newNode["props"] as JObject;

			foreach (var pair in currentProps)
			{
				if (newProps != null && newProps.ContainsKey(pair.Key))
					continue; // the re-bake specifies it — author wins

				bool handEdited = baselineProps == null
					|| !baselineProps.ContainsKey(pair.Key)
					|| !JToken.DeepEquals(baselineProps[pair.Key], pair.Value);
				if (!handEdited)
					continue; // unchanged from what the baker generated — not a hand edit

				if (newProps == null)
				{
					newProps = new JObject();
					_newNode["props"] = newProps;
				}
				newProps[pair.Key] = pair.Value.DeepClone();
				Warn($"preserveEdits: kept hand-edited prop '{pair.Key}' on node '{_id}'.");
			}
		}

		private static void PreserveScalar( JObject _newNode, JObject _baseline, JObject _current, string _field, string _id )
		{
			var currentVal = _current[_field];
			if (currentVal == null || currentVal.Type == JTokenType.Null)
				return;
			if (_newNode[_field] != null)
				return; // author specified it — wins
			if (_baseline != null && JToken.DeepEquals(_baseline[_field], currentVal))
				return; // unchanged from baseline — generated, not hand-edited

			_newNode[_field] = currentVal.DeepClone();
			Warn($"preserveEdits: kept hand-edited '{_field}' on node '{_id}'.");
		}

		#endregion

		#region Node building

		private static GameObject BuildNode( JObject _node, Transform _parent )
		{
			string template = (string)_node["template"];
			string type = (string)_node["type"];

			if (!string.IsNullOrEmpty(template) && !string.IsNullOrEmpty(type))
				throw new ArgumentException($"Node declares both \"template\" ('{template}') and \"type\" ('{type}'); pick one.");
			if (string.IsNullOrEmpty(template) && string.IsNullOrEmpty(type))
				throw new ArgumentException("Node must declare either \"template\" or \"type\".");

			GameObject go = !string.IsNullOrEmpty(template)
				? CreateTemplateNode(template)
				: CreateElementNode(type);

			string id = (string)_node["id"];
			string displayName = (string)_node["name"] ?? id ?? go.name;
			go.name = displayName;

			// Register the id so later "#id" reference props (resolved after the whole tree is built) can
			// find this node's GameObject.
			if (!string.IsNullOrEmpty(id))
			{
				if (s_nodesById.ContainsKey(id))
					Warn($"Duplicate node id '{id}'; the later node wins for '#{id}' references.");
				s_nodesById[id] = go;
			}

			// Parent before configuring so [ExecuteAlways] style appliers resolve against the hierarchy.
			if (_parent != null)
			{
				var rt = go.transform as RectTransform;
				if (rt != null)
					rt.SetParent(_parent, false);
				else
					go.transform.SetParent(_parent, false);
			}

			// A UiView carries a Canvas + CanvasScaler that the toolkit configures at runtime (render
			// mode, reference resolution via the global template). That never runs while baking, so do
			// it here — otherwise the view keeps Unity's defaults (WorldSpace + constant-pixel scaler)
			// and every child renders at the wrong size.
			ConfigureViewCanvasIfPresent(go);

			// Extra components stacked on the same GameObject (e.g. a UiView that is also a UiSimpleAnimation),
			// so no wrapper node is needed. Added before props so node-level props can target their fields too.
			if (_node["components"] is JArray extraComponents)
				AddExtraComponents(go, extraComponents);

			if (_node["props"] is JObject props)
				ApplyProps(go, props);

			string style = (string)_node["style"];
			if (!string.IsNullOrEmpty(style))
				ApplyStyle(go, style);

			// Props run before styles and a style wins, so the draw mode can only be judged once both are in.
			WarnOnStretchedSlicedSprite(go);

			string text = (string)_node["text"];
			if (text != null)
				ApplyText(go, text);

			// Per-instance variation of a TEMPLATE's internal parts. Without this, a template node can only
			// be configured at its root, so anything that differs per instance (a card's title, icon, amount,
			// state) forces the author to hand-copy the whole subtree instead of referencing one prefab.
			if (_node["overrides"] is JObject overrides)
				ApplyPartOverrides(go, overrides, !string.IsNullOrEmpty(template));

			// Layout: an explicit "rect" wins; otherwise a root gets a sane full-stretch default so it
			// isn't left at the 100x100 centered default of a fresh RectTransform.
			if (_node["rect"] is JObject rect)
				ApplyRect(go, rect);
			else if (_parent == null)
				ApplyFullStretch(go);

			// A ScrollRect needs a Viewport/Content structure to clip and scroll its children; a bare
			// UiScrollRect element (RequireComponent(ScrollRect)) has none, so build it here.
			bool scaffoldedScroll = ScaffoldScrollRectIfPresent(go);

			// Content sizing/layout so the ScrollRect actually scrolls — otherwise its Content stays 0-sized
			// and nothing moves. An explicit "scroll" node configures direction + layout group + fitter; a
			// bare (scaffolded) ScrollRect gets a sensible vertical-list default even without one.
			if (_node["scroll"] is JObject scrollNode)
				ConfigureScrollContent(go, scrollNode, scaffoldedScroll);
			else if (scaffoldedScroll)
				ConfigureScrollContent(go, null, true);

			if (_node["children"] is JArray children)
			{
				// Children go into the node's content container (a ScrollRect's Content, or a component's
				// serialized content/container transform), falling back to the node's own transform.
				Transform contentParent = ResolveContentParent(go);
				foreach (var child in children.OfType<JObject>())
					BuildNode(child, contentParent);
			}

			// Only meaningful once the children exist.
			if (_node["scroll"] is JObject || scaffoldedScroll)
				WarnOnCollapsingScrollChildren(go);

			return go;
		}

		/// <summary>
		/// Applies per-part overrides to an instantiated node's internals: <c>{ "Header/Title": { props, style,
		/// text, rect, id } }</c>, keyed by a child transform path relative to this node. On a template node
		/// these become prefab-instance overrides, so the instance keeps its link to the source prefab —
		/// the difference between referencing one prefab N times and copying a subtree N times.
		/// An <c>id</c> registers the internal part for <c>"#id"</c> wiring, which is otherwise impossible
		/// (a reference into a template's interior had no way to be addressed).
		/// </summary>
		private static void ApplyPartOverrides( GameObject _root, JObject _overrides, bool _isTemplate )
		{
			if (!_isTemplate)
			{
				Warn($"Node '{_root.name}' declares \"overrides\" but is not a template node — overrides address " +
				     "the internals of an instantiated prefab; on an element node, author the children directly.");
			}

			foreach (var pair in _overrides)
			{
				string childPath = pair.Key;
				if (pair.Value is not JObject spec)
				{
					Warn($"Override '{childPath}' on '{_root.name}' is not an object; skipped.");
					continue;
				}

				var target = _root.transform.Find(childPath);
				if (target == null)
				{
					Warn($"Override path '{childPath}' does not exist under '{_root.name}'; skipped. " +
					     "Use read_screen on the template prefab to see its internal node names.");
					continue;
				}

				var go = target.gameObject;

				if (spec["props"] is JObject props)
					ApplyProps(go, props);

				string style = (string)spec["style"];
				if (!string.IsNullOrEmpty(style))
					ApplyStyle(go, style);

				WarnOnStretchedSlicedSprite(go);

				string text = (string)spec["text"];
				if (text != null)
					ApplyText(go, text);

				if (spec["rect"] is JObject rect)
					ApplyRect(go, rect);

				string id = (string)spec["id"];
				if (string.IsNullOrEmpty(id))
					continue;

				if (s_nodesById.ContainsKey(id))
					Warn($"Duplicate node id '{id}' (override '{childPath}'); the later node wins for '#{id}'.");
				s_nodesById[id] = go;
			}
		}

		private static GameObject CreateTemplateNode( string _templateName )
		{
			var prefab = ResolveTemplatePrefab(_templateName);
			if (prefab == null)
				throw new ArgumentException($"Unknown template '{_templateName}'. It must be a standard-element " +
				                            $"identity from the registry or the name of an existing prefab — see the " +
				                            $"catalog's 'palette' (run Generate Screen Catalog to refresh it).");

			var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
			if (instance == null)
				throw new Exception($"Could not instantiate template prefab '{_templateName}'.");

			return instance;
		}

		private static GameObject CreateElementNode( string _typeName )
		{
			Type type = ResolveComponentType(_typeName);
			if (type == null)
				throw new ArgumentException($"Unknown component type '{_typeName}'. It must be a catalogued " +
				                            $"component (run Generate Screen Catalog to see available types).");

			// Start with a RectTransform (UI object); AddComponent auto-adds [RequireComponent]s.
			var go = new GameObject(_typeName, typeof(RectTransform));
			go.AddComponent(type);
			return go;
		}

		#endregion

		#region Canvas / Layout

		private static void ConfigureViewCanvasIfPresent( GameObject _go )
		{
			var canvas = _go.GetComponent<Canvas>();
			var scaler = _go.GetComponent<CanvasScaler>();
			if (canvas == null || scaler == null)
				return;

			// Overlay renders correctly in the prefab stage without needing a camera reference; UiMain
			// re-inits the render mode when the view is actually shown at runtime.
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;

			var config = UiToolkitConfiguration.Instance;
			var template = config != null ? config.GlobalCanvasScalerTemplate : null;
			if (template != null)
			{
				template.CopyTo(scaler);
			}
			else
			{
				// Fallback mirrors the toolkit's usual authoring resolution.
				scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
				scaler.referenceResolution = new Vector2(1920, 1080);
				scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
				scaler.matchWidthOrHeight = 0.5f;
			}

			EditorGeneralUtility.SetDirty(canvas);
			EditorGeneralUtility.SetDirty(scaler);
		}

		private static void ApplyFullStretch( GameObject _go )
		{
			if (_go.transform is RectTransform rt)
			{
				rt.anchorMin = Vector2.zero;
				rt.anchorMax = Vector2.one;
				rt.pivot = new Vector2(0.5f, 0.5f);
				rt.offsetMin = Vector2.zero;
				rt.offsetMax = Vector2.zero;
				EditorGeneralUtility.SetDirty(rt);
			}
		}

		private static void ApplyRect( GameObject _go, JObject _rect )
		{
			if (_go.transform is not RectTransform rt)
			{
				Warn($"'rect' set on '{_go.name}' but it has no RectTransform; skipped.");
				return;
			}

			Vector2 min = rt.anchorMin, max = rt.anchorMax, pivot = rt.pivot;

			string preset = (string)_rect["anchor"];
			if (!string.IsNullOrEmpty(preset) && TryAnchorPreset(preset, out var pMin, out var pMax, out var pPivot))
			{
				min = pMin; max = pMax; pivot = pPivot;
			}

			if (_rect["anchorMin"] is JArray aMin) min = Vec2(aMin, min);
			if (_rect["anchorMax"] is JArray aMax) max = Vec2(aMax, max);
			if (_rect["pivot"] is JArray piv) pivot = Vec2(piv, pivot);

			rt.anchorMin = min;
			rt.anchorMax = max;
			rt.pivot = pivot;

			// sizeDelta / anchoredPosition first, then explicit stretch offsets win if given.
			if (_rect["size"] is JArray size) rt.sizeDelta = Vec2(size, rt.sizeDelta);
			if (_rect["position"] is JArray pos) rt.anchoredPosition = Vec2(pos, rt.anchoredPosition);
			if (_rect["offsetMin"] is JArray oMin) rt.offsetMin = Vec2(oMin, rt.offsetMin);
			if (_rect["offsetMax"] is JArray oMax) rt.offsetMax = Vec2(oMax, rt.offsetMax);

			EditorGeneralUtility.SetDirty(rt);
		}

		// Unity's anchor-preset grid, plus "stretch"/"fill". Returns anchorMin/Max and a matching pivot.
		private static bool TryAnchorPreset( string _name, out Vector2 _min, out Vector2 _max, out Vector2 _pivot )
		{
			(Vector2 min, Vector2 max, Vector2 pivot)? preset = _name.Trim().ToLowerInvariant() switch
			{
				"stretch" or "fill"   => (new Vector2(0, 0),    new Vector2(1, 1),    new Vector2(0.5f, 0.5f)),
				"center"              => (new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f)),
				"top"                 => (new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1)),
				"bottom"              => (new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0)),
				"left"                => (new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f)),
				"right"               => (new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f)),
				"top-left"            => (new Vector2(0, 1),    new Vector2(0, 1),    new Vector2(0, 1)),
				"top-right"           => (new Vector2(1, 1),    new Vector2(1, 1),    new Vector2(1, 1)),
				"bottom-left"         => (new Vector2(0, 0),    new Vector2(0, 0),    new Vector2(0, 0)),
				"bottom-right"        => (new Vector2(1, 0),    new Vector2(1, 0),    new Vector2(1, 0)),
				"top-stretch"         => (new Vector2(0, 1),    new Vector2(1, 1),    new Vector2(0.5f, 1)),
				"bottom-stretch"      => (new Vector2(0, 0),    new Vector2(1, 0),    new Vector2(0.5f, 0)),
				"left-stretch"        => (new Vector2(0, 0),    new Vector2(0, 1),    new Vector2(0, 0.5f)),
				"right-stretch"       => (new Vector2(1, 0),    new Vector2(1, 1),    new Vector2(1, 0.5f)),
				"stretch-horizontal"  => (new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0.5f, 0.5f)),
				"stretch-vertical"    => (new Vector2(0.5f, 0), new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f)),
				_ => ((Vector2, Vector2, Vector2)?)null,
			};

			if (preset == null)
			{
				_min = _max = _pivot = new Vector2(0.5f, 0.5f);
				Warn($"Unknown anchor preset '{_name}'; ignored.");
				return false;
			}

			(_min, _max, _pivot) = preset.Value;
			return true;
		}

		private static Vector2 Vec2( JArray _arr, Vector2 _fallback )
		{
			var v = _fallback;
			if (_arr.Count > 0) v.x = (float)_arr[0];
			if (_arr.Count > 1) v.y = (float)_arr[1];
			return v;
		}

		// Resolves where a node's children should be parented: a ScrollRect's Content, else a component's
		// serialized content/container transform (same heuristic the catalog generator exposes as
		// "contentField"), else the node's own transform.
		private static Transform ResolveContentParent( GameObject _go )
		{
			var scrollRect = _go.GetComponent<ScrollRect>();
			if (scrollRect != null && scrollRect.content != null)
				return scrollRect.content;

			var contentTransform = FindContentReference(_go);
			if (contentTransform != null)
				return contentTransform;

			return _go.transform;
		}

		// Mirrors UiScreenCatalogGenerator.ResolveContentField: a serialized Transform/GameObject field
		// whose name mentions "content"/"container" and that already points at a child of this node
		// (the case for template prefabs that carry a real content area). Most-derived declarations win.
		private static Transform FindContentReference( GameObject _go )
		{
			foreach (var component in _go.GetComponents<Component>())
			{
				if (component == null)
					continue;

				for (var t = component.GetType(); t != null && t != typeof(object); t = t.BaseType)
				{
					var fields = t.GetFields(BindingFlags.Instance | BindingFlags.Public |
					                         BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
					foreach (var f in fields)
					{
						bool contenty = f.Name.IndexOf("content", StringComparison.OrdinalIgnoreCase) >= 0
						             || f.Name.IndexOf("container", StringComparison.OrdinalIgnoreCase) >= 0;
						if (!contenty)
							continue;
						if (!typeof(Transform).IsAssignableFrom(f.FieldType) && !typeof(GameObject).IsAssignableFrom(f.FieldType))
							continue;

						Transform tr = f.GetValue(component) switch
						{
							Transform x  => x,
							GameObject g => g.transform,
							_            => null,
						};
						if (tr != null && tr != _go.transform && tr.IsChildOf(_go.transform))
							return tr;
					}
				}
			}
			return null;
		}

		// Builds the standard Viewport→Content structure for a ScrollRect that has none (a bare
		// UiScrollRect element), and wires the ScrollRect's viewport/content refs. A template prefab that
		// already ships a Content is left untouched. Returns true iff it actually scaffolded a Content, so
		// the caller can apply a default layout/fitter to a Content the baker fully owns. Layout group +
		// ContentSizeFitter are added by ConfigureScrollContent.
		private static bool ScaffoldScrollRectIfPresent( GameObject _go )
		{
			var scrollRect = _go.GetComponent<ScrollRect>();
			if (scrollRect == null || scrollRect.content != null)
				return false;

			if (_go.transform is not RectTransform rootRt)
				return false;

			// Viewport: clips the content (RectMask2D) and, via an invisible Image, is a raycast target so
			// drag-to-scroll works over empty areas too.
			var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D), typeof(Image));
			var viewportRt = (RectTransform)viewportGo.transform;
			viewportRt.SetParent(rootRt, false);
			FullStretch(viewportRt);
			viewportRt.pivot = new Vector2(0f, 1f);
			viewportGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);

			// Content: anchored to the top edge, width follows the viewport, height free to grow.
			var contentGo = new GameObject("Content", typeof(RectTransform));
			var contentRt = (RectTransform)contentGo.transform;
			contentRt.SetParent(viewportRt, false);
			contentRt.anchorMin = new Vector2(0f, 1f);
			contentRt.anchorMax = new Vector2(1f, 1f);
			contentRt.pivot = new Vector2(0.5f, 1f);
			contentRt.anchoredPosition = Vector2.zero;
			contentRt.sizeDelta = Vector2.zero;

			scrollRect.viewport = viewportRt;
			scrollRect.content = contentRt;
			EditorGeneralUtility.SetDirty(scrollRect);
			return true;
		}

		// Makes a ScrollRect's Content actually scroll: sets the horizontal/vertical flags, a layout group
		// that arranges the children, and a ContentSizeFitter that grows the Content along the scroll axis
		// (its Content otherwise stays 0-sized). For a scaffolded Content the baker also sets
		// direction-appropriate anchors; an existing template Content's anchors are respected. `_scroll` may
		// be null (defaults only, for a bare scaffolded ScrollRect).
		//
		// "scroll": {
		//   "direction": "vertical" | "horizontal" | "both",   // default vertical; sets the scroll flags
		//   "layout":    "vertical" | "horizontal" | "grid" | "none", // layout group on Content
		//   "fit":       true,                    // ContentSizeFitter along the scroll axis (default true)
		//   "spacing":   8,                       // number (or [x,y] for grid)
		//   "padding":   [left, right, top, bottom],
		//   "cellSize":  [w, h],                  // grid only
		//   "childAlignment": "UpperCenter",      // TextAnchor name
		//   "childControlWidth": true,            // default true — see the note in ConfigureLinearLayout:
		//   "childControlHeight": true,           //   children without a preferred size collapse to 0
		//   "childForceExpandWidth": null,        // default: true for a vertical list
		//   "childForceExpandHeight": null        // default: true for a horizontal list
		// }
		private static void ConfigureScrollContent( GameObject _go, JObject _scroll, bool _scaffolded )
		{
			var scrollRect = _go.GetComponent<ScrollRect>();
			if (scrollRect == null || scrollRect.content == null)
				return;

			var content = scrollRect.content;

			string direction = ((string)_scroll?["direction"])?.Trim().ToLowerInvariant() ?? "vertical";
			if (direction != "vertical" && direction != "horizontal" && direction != "both")
			{
				Warn($"Unknown scroll direction '{direction}' on '{_go.name}'; using vertical.");
				direction = "vertical";
			}

			scrollRect.horizontal = direction is "horizontal" or "both";
			scrollRect.vertical = direction is "vertical" or "both";
			EditorGeneralUtility.SetDirty(scrollRect);

			if (_scaffolded)
				ApplyScrollContentAnchors(content, direction);

			string layout = ((string)_scroll?["layout"])?.Trim().ToLowerInvariant() ?? DefaultScrollLayout(direction);
			ConfigureScrollLayoutGroup(content, layout, _scroll);

			bool fit = _scroll?["fit"] == null || (bool)_scroll["fit"];
			if (fit)
				ConfigureContentFitter(content, direction);

			EditorGeneralUtility.SetDirty(content);
		}

		// vertical → arrange top-to-bottom, width follows the viewport; horizontal → left-to-right, height
		// follows the viewport; both → top-left origin (paired with a grid).
		private static void ApplyScrollContentAnchors( RectTransform _content, string _direction )
		{
			switch (_direction)
			{
				case "horizontal":
					_content.anchorMin = new Vector2(0f, 0f); _content.anchorMax = new Vector2(0f, 1f); _content.pivot = new Vector2(0f, 0.5f);
					break;
				case "both":
					_content.anchorMin = new Vector2(0f, 1f); _content.anchorMax = new Vector2(0f, 1f); _content.pivot = new Vector2(0f, 1f);
					break;
				default: // vertical
					_content.anchorMin = new Vector2(0f, 1f); _content.anchorMax = new Vector2(1f, 1f); _content.pivot = new Vector2(0.5f, 1f);
					break;
			}
			_content.anchoredPosition = Vector2.zero;
			_content.sizeDelta = Vector2.zero;
		}

		private static string DefaultScrollLayout( string _direction ) => _direction switch
		{
			"horizontal" => "horizontal",
			"both"       => "grid",
			_            => "vertical",
		};

		private static void ConfigureScrollLayoutGroup( RectTransform _content, string _layout, JObject _scroll )
		{
			switch (_layout)
			{
				case "none":
					break;
				case "grid":
					ConfigureGridLayout(_content, _scroll);
					break;
				case "horizontal":
					ConfigureLinearLayout(_content, false, _scroll);
					break;
				case "vertical":
					ConfigureLinearLayout(_content, true, _scroll);
					break;
				default:
					Warn($"Unknown scroll layout '{_layout}' on '{_content.name}'; skipped.");
					break;
			}
		}

		private static void ConfigureLinearLayout( RectTransform _content, bool _vertical, JObject _scroll )
		{
			var group = _vertical
				? (HorizontalOrVerticalLayoutGroup)EnsureComponent<VerticalLayoutGroup>(_content.gameObject)
				: EnsureComponent<HorizontalLayoutGroup>(_content.gameObject);

			if (_scroll?["spacing"] != null && _scroll["spacing"].Type != JTokenType.Array)
				group.spacing = (float)_scroll["spacing"];
			var padding = ParsePadding(_scroll?["padding"]);
			if (padding != null)
				group.padding = padding;
			if (TryParseTextAnchor((string)_scroll?["childAlignment"], out var anchor))
				group.childAlignment = anchor;

			// Items keep their preferred size on the scroll axis and stretch across the cross axis.
			// NOTE: with childControl on, a child that declares no preferred size (no LayoutElement and no
			// ILayoutElement of its own) is driven to ZERO along that axis and all children end up stacked
			// on one spot. Authors hit this constantly, so the flags are overridable and WarnOnCollapsing-
			// ScrollChildren() reports it after the children exist.
			group.childControlWidth = Bool(_scroll?["childControlWidth"], true);
			group.childControlHeight = Bool(_scroll?["childControlHeight"], true);
			group.childForceExpandWidth = Bool(_scroll?["childForceExpandWidth"], _vertical);
			group.childForceExpandHeight = Bool(_scroll?["childForceExpandHeight"], !_vertical);

			static bool Bool( JToken _t, bool _default )
				=> _t == null || _t.Type == JTokenType.Null ? _default : (bool)_t;
		}

		/// <summary>
		/// After the children exist: warn about children a childControl-driven layout group will collapse to
		/// zero because they declare no preferred size. Silent collapse otherwise looks like "layout is
		/// broken" and costs a long debugging detour.
		/// </summary>
		private static void WarnOnCollapsingScrollChildren( GameObject _go )
		{
			var scrollRect = _go.GetComponent<ScrollRect>();
			var content = scrollRect != null ? scrollRect.content : null;
			if (content == null)
				return;

			var group = content.GetComponent<HorizontalOrVerticalLayoutGroup>();
			if (group == null)
				return;

			bool horizontal = group is HorizontalLayoutGroup;
			bool controls = horizontal ? group.childControlWidth : group.childControlHeight;
			if (!controls)
				return;

			var collapsing = new List<string>();
			foreach (Transform childTransform in content)
			{
				// Not every child is guaranteed to be a RectTransform; don't throw on an odd one.
				if (childTransform is not RectTransform child)
					continue;
				float preferred = horizontal
					? LayoutUtility.GetPreferredWidth(child)
					: LayoutUtility.GetPreferredHeight(child);
				if (preferred <= 0f)
					collapsing.Add(child.name);
			}

			if (collapsing.Count > 0)
				Warn($"Scroll content of '{_go.name}': {collapsing.Count} child(ren) declare no preferred " +
				     $"{(horizontal ? "width" : "height")} and will be collapsed to 0 by the layout group " +
				     $"(they will all sit on the same spot): {string.Join(", ", collapsing)}. Give them a " +
				     $"LayoutElement (preferredWidth/preferredHeight), or set " +
				     $"\"{(horizontal ? "childControlWidth" : "childControlHeight")}\": false in \"scroll\".");
		}

		private static void ConfigureGridLayout( RectTransform _content, JObject _scroll )
		{
			var grid = EnsureComponent<GridLayoutGroup>(_content.gameObject);
			if (_scroll?["cellSize"] is JArray cell)
				grid.cellSize = Vec2(cell, grid.cellSize);
			if (_scroll?["spacing"] is JArray sp)
				grid.spacing = Vec2(sp, grid.spacing);
			else if (_scroll?["spacing"] != null)
			{
				float s = (float)_scroll["spacing"];
				grid.spacing = new Vector2(s, s);
			}
			var padding = ParsePadding(_scroll?["padding"]);
			if (padding != null)
				grid.padding = padding;
			if (TryParseTextAnchor((string)_scroll?["childAlignment"], out var anchor))
				grid.childAlignment = anchor;
		}

		private static void ConfigureContentFitter( RectTransform _content, string _direction )
		{
			var fitter = EnsureComponent<ContentSizeFitter>(_content.gameObject);
			fitter.horizontalFit = DirectionCoversAxis(_direction, "horizontal") ? ContentSizeFitter.FitMode.PreferredSize : ContentSizeFitter.FitMode.Unconstrained;
			fitter.verticalFit   = DirectionCoversAxis(_direction, "vertical")   ? ContentSizeFitter.FitMode.PreferredSize : ContentSizeFitter.FitMode.Unconstrained;
		}

		private static bool DirectionCoversAxis( string _direction, string _axis ) => _direction == _axis || _direction == "both";

		private static RectOffset ParsePadding( JToken _token )
		{
			if (_token is not JArray arr)
				return null;
			int Get( int i ) => arr.Count > i ? (int)arr[i] : 0;
			return new RectOffset(Get(0), Get(1), Get(2), Get(3)); // left, right, top, bottom
		}

		private static bool TryParseTextAnchor( string _value, out TextAnchor _anchor )
		{
			_anchor = TextAnchor.UpperLeft;
			return !string.IsNullOrEmpty(_value) && Enum.TryParse(_value, true, out _anchor);
		}

		private static T EnsureComponent<T>( GameObject _go ) where T : Component
		{
			var existing = _go.GetComponent<T>();
			return existing != null ? existing : _go.AddComponent<T>();
		}

		private static void FullStretch( RectTransform _rt )
		{
			_rt.anchorMin = Vector2.zero;
			_rt.anchorMax = Vector2.one;
			_rt.offsetMin = Vector2.zero;
			_rt.offsetMax = Vector2.zero;
		}

		#endregion

		#region Props

		private static void ApplyProps( GameObject _go, JObject _props )
		{
			foreach (var pair in _props)
			{
				string key = pair.Key;
				JToken value = pair.Value;

				var (component, member) = FindSerializedField(_go, key);
				if (!member.IsValid)
				{
					Warn($"Prop '{key}' not found on '{_go.name}'; skipped.");
					continue;
				}

				// A "#id" value (or an array of them) is a reference to another node's component/GameObject.
				// Those are resolved in a second pass once every node exists, so defer them here — but ONLY
				// when the target is actually a reference type. A "#" string bound to a value field is not a
				// ref: e.g. an html color "#FF0000" on a Color field, or a literal on a string field.
				if (IsRefToken(value) && IsReferenceMember(member.ValueType))
				{
					s_deferredRefs.Add(new DeferredRef
					{
						component = component,
						member = member,
						value = value,
						ownerName = _go.name,
					});
					continue;
				}

				if (!TryConvert(value, member.ValueType, out object converted))
				{
					Warn($"Prop '{key}' on '{_go.name}': cannot convert value to {member.ValueType.Name}; skipped.");
					continue;
				}

				member.SetValue(component, converted);
				EditorGeneralUtility.SetDirty(component);
			}
		}

		// Stacks extra components on the SAME node's GameObject, so an author doesn't need a wrapper node
		// just to combine behaviours (e.g. a UiView that is also a UiSimpleAnimation). Each entry is either
		// a type-name string or an object { "type": "...", "props": { … } }; per-entry props are applied to
		// that specific component (disambiguating a field name that several components share). Node-level
		// props/style/text still search all components on the GameObject, so the common case needs no props
		// here. A component already present (the primary type, a RequireComponent, or a duplicate entry) is
		// reused, not added twice.
		private static void AddExtraComponents( GameObject _go, JArray _components )
		{
			foreach (var token in _components)
			{
				string typeName;
				JObject props = null;

				if (token.Type == JTokenType.String)
				{
					typeName = (string)token;
				}
				else if (token is JObject obj)
				{
					typeName = (string)obj["type"];
					props = obj["props"] as JObject;
				}
				else
				{
					Warn($"Extra component entry on '{_go.name}' is neither a type name nor a {{ type, props }} object; skipped.");
					continue;
				}

				if (string.IsNullOrEmpty(typeName))
				{
					Warn($"Extra component entry on '{_go.name}' has no 'type'; skipped.");
					continue;
				}

				Type type = ResolveComponentType(typeName);
				if (type == null)
				{
					Warn($"Extra component '{typeName}' on '{_go.name}': unknown component type; skipped.");
					continue;
				}

				var component = _go.GetComponent(type);
				if (component == null)
					component = _go.AddComponent(type);

				if (props != null)
					SetFieldsOnComponent(component, props);
			}
		}

		// The single-component half of ApplyProps: sets props on ONE specific component (used by
		// AddExtraComponents for per-entry props). Shares the deferred-ref + TryConvert machinery.
		private static void SetFieldsOnComponent( Component _component, JObject _props )
		{
			foreach (var pair in _props)
			{
				var member = FindMemberOnComponent(_component, pair.Key);
				if (!member.IsValid)
				{
					Warn($"Prop '{pair.Key}' not found on {_component.GetType().Name} ('{_component.gameObject.name}'); skipped.");
					continue;
				}

				if (IsRefToken(pair.Value) && IsReferenceMember(member.ValueType))
				{
					s_deferredRefs.Add(new DeferredRef
					{
						component = _component,
						member = member,
						value = pair.Value,
						ownerName = _component.gameObject.name,
					});
					continue;
				}

				if (!TryConvert(pair.Value, member.ValueType, out object converted))
				{
					Warn($"Prop '{pair.Key}' on {_component.GetType().Name} ('{_component.gameObject.name}'): cannot convert value to {member.ValueType.Name}; skipped.");
					continue;
				}

				member.SetValue(_component, converted);
				EditorGeneralUtility.SetDirty(_component);
			}
		}

		// True when a member holds a Unity object reference (GameObject/Component/Object) — or an array / List<>
		// of them. Only such members participate in "#id" deferred wiring; value members (Color, string, ...)
		// take a "#..." token as a literal instead.
		private static bool IsReferenceMember( Type _type )
		{
			Type t = _type;
			if (t.IsArray)
				t = t.GetElementType();
			else if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>))
				t = t.GetGenericArguments()[0];

			return t != null && typeof(UnityEngine.Object).IsAssignableFrom(t);
		}

		// A "#id" string (or an array whose elements are all "#id" strings) denotes a reference to another
		// node's component/GameObject, resolved in the second pass.
		private static bool IsRefToken( JToken _token )
		{
			if (_token.Type == JTokenType.String)
				return ((string)_token).StartsWith("#", StringComparison.Ordinal);
			if (_token is JArray arr && arr.Count > 0)
				return arr.All(e => e.Type == JTokenType.String && ((string)e).StartsWith("#", StringComparison.Ordinal));
			return false;
		}

		// Second pass: every node now exists in s_nodesById, so resolve the deferred "#id" reference props
		// into real object references (single, or an array / List<T> of them).
		private static void ResolveDeferredRefs()
		{
			foreach (var deferred in s_deferredRefs)
			{
				if (deferred.value is JArray arr)
					ResolveRefList(deferred, arr);
				else
					ResolveSingleRef(deferred, (string)deferred.value);
			}
		}

		private static void ResolveSingleRef( DeferredRef _deferred, string _ref )
		{
			var resolved = ResolveRef(_ref, _deferred.member.ValueType, _deferred.ownerName);
			if (resolved == null)
				return;
			_deferred.member.SetValue(_deferred.component, resolved);
			EditorGeneralUtility.SetDirty(_deferred.component);
		}

		private static void ResolveRefList( DeferredRef _deferred, JArray _refs )
		{
			Type fieldType = _deferred.member.ValueType;
			Type elementType =
				fieldType.IsArray ? fieldType.GetElementType() :
				fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>) ? fieldType.GetGenericArguments()[0] :
				null;

			if (elementType == null)
			{
				Warn($"Reference list on '{_deferred.ownerName}.{_deferred.member.Name}' skipped: field is not an array or List<>.");
				return;
			}

			var resolved = new List<UnityEngine.Object>();
			foreach (var token in _refs)
			{
				var obj = ResolveRef((string)token, elementType, _deferred.ownerName);
				if (obj != null)
					resolved.Add(obj);
			}

			if (fieldType.IsArray)
			{
				var array = Array.CreateInstance(elementType, resolved.Count);
				for (int i = 0; i < resolved.Count; i++)
					array.SetValue(resolved[i], i);
				_deferred.member.SetValue(_deferred.component, array);
			}
			else
			{
				var list = (System.Collections.IList)Activator.CreateInstance(fieldType);
				foreach (var obj in resolved)
					list.Add(obj);
				_deferred.member.SetValue(_deferred.component, list);
			}
			EditorGeneralUtility.SetDirty(_deferred.component);
		}

		// Resolves a single "#id" reference to the target node's GameObject or a component on it, matching
		// the requested field/element type. Warns (and returns null) when the id or the component is missing.
		private static UnityEngine.Object ResolveRef( string _ref, Type _wantType, string _ownerName )
		{
			string id = _ref.StartsWith("#", StringComparison.Ordinal) ? _ref.Substring(1) : _ref;

			if (!s_nodesById.TryGetValue(id, out var target) || target == null)
			{
				Warn($"Reference '#{id}' on '{_ownerName}': no node with that id; skipped.");
				return null;
			}

			if (typeof(GameObject).IsAssignableFrom(_wantType))
				return target;

			if (typeof(Component).IsAssignableFrom(_wantType))
			{
				var component = target.GetComponent(_wantType);
				if (component == null)
					Warn($"Reference '#{id}' on '{_ownerName}': node '{target.name}' has no {_wantType.Name}; skipped.");
				return component;
			}

			Warn($"Reference '#{id}' on '{_ownerName}': field type {_wantType.Name} is not a GameObject/Component reference; skipped.");
			return null;
		}

		// Resolves an authoring name ("layer") or raw field name ("m_layer") to a settable member (serialized
		// field or property) on any component of the GameObject, searching the most-derived declarations first.
		private static (Component, MemberRef) FindSerializedField( GameObject _go, string _key )
		{
			foreach (var component in _go.GetComponents<Component>())
			{
				if (component == null)
					continue;

				var member = FindMemberOnComponent(component, _key);
				if (member.IsValid)
					return (component, member);
			}
			return (null, default);
		}

		// The single-component half of FindSerializedField: resolves an authoring name ("layer") or raw
		// field name ("m_layer") to a settable member on this one component, most-derived declaration first.
		// A serialized field is preferred; a C# property is the fallback for native components (CanvasGroup).
		private static MemberRef FindMemberOnComponent( Component _component, string _key )
		{
			string mKey = _key.StartsWith("m_", StringComparison.Ordinal) ? _key : "m_" + _key;

			for (var t = _component.GetType(); t != null && t != typeof(object); t = t.BaseType)
			{
				var fields = t.GetFields(BindingFlags.Instance | BindingFlags.Public |
				                         BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
				foreach (var f in fields)
				{
					if (f.Name == _key || f.Name == mKey)
						return new MemberRef(f);
				}
			}

			// Property fallback: match the authoring name directly (properties carry no "m_" prefix), e.g.
			// CanvasGroup.alpha / interactable / blocksRaycasts. Only writable, non-indexed properties.
			for (var t = _component.GetType(); t != null && t != typeof(object); t = t.BaseType)
			{
				var properties = t.GetProperties(BindingFlags.Instance | BindingFlags.Public |
				                                 BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
				foreach (var p in properties)
				{
					if (!p.CanWrite || p.GetIndexParameters().Length > 0)
						continue;
					if (string.Equals(p.Name, _key, StringComparison.Ordinal))
						return new MemberRef(p);
				}
			}

			return default;
		}

		private static bool TryConvert( JToken _token, Type _type, out object _result )
		{
			_result = null;
			try
			{
				if (_type == typeof(string)) { _result = (string)_token; return true; }
				if (_type == typeof(bool)) { _result = (bool)_token; return true; }
				if (_type.IsEnum) { _result = Enum.Parse(_type, (string)_token, true); return true; }

				if (_type == typeof(int) || _type == typeof(short) || _type == typeof(byte)
				    || _type == typeof(sbyte) || _type == typeof(ushort))
				{ _result = Convert.ChangeType((int)_token, _type, CultureInfo.InvariantCulture); return true; }
				if (_type == typeof(long) || _type == typeof(uint) || _type == typeof(ulong))
				{ _result = Convert.ChangeType((long)_token, _type, CultureInfo.InvariantCulture); return true; }
				if (_type == typeof(float)) { _result = (float)_token; return true; }
				if (_type == typeof(double)) { _result = (double)_token; return true; }

				if (_type == typeof(Color) || _type == typeof(Color32))
				{
					var c = ParseColor(_token);
					_result = _type == typeof(Color32) ? (object)(Color32)c : c;
					return true;
				}

				if (_type == typeof(Vector2)) { var v = Floats(_token, 2); _result = new Vector2(v[0], v[1]); return true; }
				if (_type == typeof(Vector3)) { var v = Floats(_token, 3); _result = new Vector3(v[0], v[1], v[2]); return true; }
				if (_type == typeof(Vector4)) { var v = Floats(_token, 4); _result = new Vector4(v[0], v[1], v[2], v[3]); return true; }

				if (_type == typeof(AnimationCurve)) { _result = ParseAnimationCurve(_token); return _result != null; }

				if (typeof(Sprite).IsAssignableFrom(_type))
				{
					_result = AssetDatabase.LoadAssetAtPath<Sprite>((string)_token);
					return _result != null;
				}

				// Any other asset reference given as a project-relative path — most importantly a PREFAB
				// reference (a container's item prefab, a spawner's template), which is how a screen points
				// at the single prefab it instantiates per data row at runtime instead of holding N authored
				// copies. "#id" tokens never reach here; those are node references resolved in a later pass.
				if (typeof(UnityEngine.Object).IsAssignableFrom(_type) && _token.Type == JTokenType.String)
				{
					string assetPath = (string)_token;
					if (assetPath.StartsWith("Assets/", StringComparison.Ordinal) ||
					    assetPath.StartsWith("Packages/", StringComparison.Ordinal))
					{
						_result = AssetDatabase.LoadAssetAtPath(assetPath, _type);

						// A Component-typed field pointed at a prefab path: take the component off its root,
						// which is what the author means (e.g. an itemPrefab field typed as the item's class).
						if (_result == null && typeof(Component).IsAssignableFrom(_type))
						{
							var go = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
							if (go != null)
								_result = go.GetComponent(_type);
						}
						return _result != null;
					}
				}

				// Nested structs are not supported by the baker.
				return false;
			}
			catch
			{
				return false;
			}
		}

		private static Color ParseColor( JToken _token )
		{
			if (_token.Type == JTokenType.String)
			{
				string s = (string)_token;
				if (ColorUtility.TryParseHtmlString(s, out var c))
					return c;
				throw new FormatException($"'{s}' is not a valid HTML color.");
			}
			var f = Floats(_token, 4, defaultAlpha: 1f);
			return new Color(f[0], f[1], f[2], f[3]);
		}

		private static float[] Floats( JToken _token, int _count, float defaultAlpha = 0f )
		{
			var result = new float[_count];
			if (_count == 4)
				result[3] = defaultAlpha;

			if (_token is JArray arr)
			{
				for (int i = 0; i < _count && i < arr.Count; i++)
					result[i] = (float)arr[i];
				return result;
			}
			throw new FormatException("Expected a JSON array of numbers.");
		}

		// Parses an AnimationCurve from one of three author-friendly shapes:
		//   • a preset name string: "linear" / "easeInOut" / "constant" (default over the 0→1 range);
		//   • an object { "preset": "...", "from": [time, value], "to": [time, value], "preWrapMode"?, "postWrapMode"? };
		//   • a keyframe list — a bare array [ { "time", "value", "inTangent"?, "outTangent"? }, … ],
		//     or an object { "keys": [ … ], "preWrapMode"?, "postWrapMode"? } for full control.
		// [time, value] pairs map x→time, y→value. Throws (→ non-fatal "cannot convert" warning) on
		// malformed input, matching the other TryConvert helpers.
		private static AnimationCurve ParseAnimationCurve( JToken _token )
		{
			if (_token.Type == JTokenType.String)
				return PresetCurve((string)_token, new Vector2(0f, 0f), new Vector2(1f, 1f));

			if (_token is JArray keyArray)
				return CurveFromKeys(keyArray);

			if (_token is JObject obj)
			{
				if (obj["keys"] is JArray keys)
				{
					var byKeys = CurveFromKeys(keys);
					ApplyWrapModes(byKeys, obj);
					return byKeys;
				}

				string preset = (string)obj["preset"];
				if (!string.IsNullOrEmpty(preset))
				{
					Vector2 from = obj["from"] is JArray f ? Vec2(f, new Vector2(0f, 0f)) : new Vector2(0f, 0f);
					Vector2 to = obj["to"] is JArray t ? Vec2(t, new Vector2(1f, 1f)) : new Vector2(1f, 1f);
					var byPreset = PresetCurve(preset, from, to);
					ApplyWrapModes(byPreset, obj);
					return byPreset;
				}
			}

			throw new FormatException("AnimationCurve must be a preset name, a { preset, from, to } object, or a keyframe list.");
		}

		private static AnimationCurve PresetCurve( string _preset, Vector2 _from, Vector2 _to )
		{
			string p = new string(_preset.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
			return p switch
			{
				"linear"                          => AnimationCurve.Linear(_from.x, _from.y, _to.x, _to.y),
				"easeinout" or "ease" or "smooth" => AnimationCurve.EaseInOut(_from.x, _from.y, _to.x, _to.y),
				"constant"                        => AnimationCurve.Constant(_from.x, _to.x, _to.y),
				_ => throw new FormatException($"Unknown AnimationCurve preset '{_preset}' (use linear / easeInOut / constant)."),
			};
		}

		private static AnimationCurve CurveFromKeys( JArray _keys )
		{
			var frames = new List<Keyframe>();
			foreach (var k in _keys.OfType<JObject>())
			{
				float time = k["time"] != null ? (float)k["time"] : 0f;
				float value = k["value"] != null ? (float)k["value"] : 0f;
				float inTangent = k["inTangent"] != null ? (float)k["inTangent"] : 0f;
				float outTangent = k["outTangent"] != null ? (float)k["outTangent"] : 0f;
				frames.Add(new Keyframe(time, value, inTangent, outTangent));
			}

			if (frames.Count == 0)
				throw new FormatException("AnimationCurve keyframe list is empty.");

			return new AnimationCurve(frames.ToArray());
		}

		private static void ApplyWrapModes( AnimationCurve _curve, JObject _obj )
		{
			if (TryParseWrapMode((string)_obj["preWrapMode"], out var pre))
				_curve.preWrapMode = pre;
			if (TryParseWrapMode((string)_obj["postWrapMode"], out var post))
				_curve.postWrapMode = post;
		}

		private static bool TryParseWrapMode( string _value, out WrapMode _mode )
		{
			_mode = WrapMode.Default;
			return !string.IsNullOrEmpty(_value) && Enum.TryParse(_value, true, out _mode);
		}

		#endregion

		#region Style

		private static void ApplyStyle( GameObject _go, string _styleName )
		{
			BuildApplierMap();

			bool applied = false;
			foreach (var kv in s_applierByTargetType)
			{
				Type targetType = kv.Key;
				Type applierType = kv.Value;

				if (_go.GetComponent(targetType) == null)
					continue;

				var applier = (UiAbstractApplyStyleBase)_go.AddComponent(applierType);
				applier.Name = _styleName; // setter resolves + applies

				if (applier.Style != null)
				{
					applier.Apply();
					EditorGeneralUtility.SetDirty(applier);
					applied = true;
				}
				else
				{
					UnityEngine.Object.DestroyImmediate(applier);
				}
			}

			if (!applied)
				Warn($"Style '{_styleName}' matched no component on '{_go.name}'; skipped.");
		}

		/// <summary>
		/// Warns when an <see cref="Image"/> carries a 9-slice sprite (non-zero border) while its draw mode is
		/// still <see cref="Image.Type.Simple"/>: the borders get stretched instead of sliced, which is the most
		/// common "the authored screen looks broken" cause. It is invisible in the screen JSON, because a style
		/// sets the sprite while the draw mode stays whatever the freshly added component defaulted to — a style
		/// never sets it. Not an error: a 9-slice sprite drawn Simple is legal, just almost never intended.
		/// </summary>
		private static void WarnOnStretchedSlicedSprite( GameObject _go )
		{
			foreach (var image in _go.GetComponents<Image>())
			{
				if (image == null || image.type != Image.Type.Simple)
					continue;

				var sprite = image.sprite;
				if (sprite == null || sprite.border == Vector4.zero)
					continue;

				Warn($"Image on '{_go.name}' has the 9-slice sprite '{sprite.name}' (border " +
				     $"{sprite.border.x},{sprite.border.y},{sprite.border.z},{sprite.border.w}) but draw mode " +
				     $"Simple, so its borders are stretched — add \"type\": \"Sliced\" (or \"Tiled\") to this node.");
			}
		}

		#endregion

		#region Text

		private static void ApplyText( GameObject _go, string _text )
		{
			var localized = _go.GetComponentInChildren<UiLocalizedTextMeshProUGUI>(true);
			if (localized != null)
			{
				if (_text.StartsWith(LiteralTextPrefix, StringComparison.Ordinal))
				{
					string literal = _text.Substring(LiteralTextPrefix.Length);
					SetPrivateField(localized, "m_isTranslated", false);
					localized.text = literal;
				}
				else
				{
					string key = _text.StartsWith(LocaKeyPrefix, StringComparison.Ordinal)
						? _text.Substring(LocaKeyPrefix.Length)
						: _text;
					SetPrivateField(localized, "m_isTranslated", true);
					SetPrivateField(localized, "m_locaKey", key);

					// Also seed the visible TMP text with the key so the bake/preview shows something
					// meaningful instead of the template's leftover placeholder (LocaManager overwrites it
					// at runtime). The .text property re-asserts the placeholder on a localized component, so
					// write the backing m_text field via SerializedObject — that is what sticks.
					var so = new SerializedObject(localized);
					var textProp = so.FindProperty("m_text");
					if (textProp != null)
					{
						textProp.stringValue = key;
						so.ApplyModifiedPropertiesWithoutUndo();
					}
				}
				EditorGeneralUtility.SetDirty(localized);
				return;
			}

			var tmp = _go.GetComponentInChildren<TMPro.TMP_Text>(true);
			if (tmp != null)
			{
				tmp.text = StripPrefix(_text, LiteralTextPrefix);
				EditorGeneralUtility.SetDirty(tmp);
				return;
			}

			Warn($"Text set requested on '{_go.name}' but no TMP text component was found; skipped.");
		}

		private static string StripPrefix( string _value, string _prefix )
			=> _value.StartsWith(_prefix, StringComparison.Ordinal) ? _value.Substring(_prefix.Length) : _value;

		private static void SetPrivateField( object _target, string _fieldName, object _value )
		{
			for (var t = _target.GetType(); t != null && t != typeof(object); t = t.BaseType)
			{
				var f = t.GetField(_fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
				if (f != null)
				{
					f.SetValue(_target, _value);
					return;
				}
			}
		}

		#endregion

		#region Type resolution

		private static void ResetCaches()
		{
			s_componentTypesByName = null;
			s_applierByTargetType = null;
		}

		private static GameObject ResolveTemplatePrefab( string _name )
		{
			// Variant resolution — ONLY when the template name is a standard-element IDENTITY (a registry
			// key, e.g. "OkButton"): resolve it to the winning prefab, so the registry's client-over-library
			// ranking builds the screen with a client variant when one exists. Falls back to a name search
			// when the registry is absent (bakes still work before the catalog is generated).
			var registry = UiToolkitConfiguration.Instance != null
				? UiToolkitConfiguration.Instance.StandardElementRegistry
				: null;
			var byKey = registry != null ? registry.Resolve(_name) : null;
			if (byKey != null)
				return byKey;

			// Otherwise the author named a SPECIFIC prefab (a particular variant, a client widget, a
			// non-standard palette entry) — honour it exactly. We deliberately do NOT re-resolve it through
			// its inherited marker: several distinct client prefabs can inherit the same key (e.g. ButtonOk
			// and ButtonCancel both inherit StandardButton), so re-resolving would silently swap the named
			// prefab for the key's single winner. Naming a specific prefab means that prefab.
			return FindPrefabByName(_name);
		}

		private static GameObject FindPrefabByName( string _name )
		{
			// Prefer a StandardElements match (the library anchor) so a name resolves deterministically even
			// before the catalog/registry exist; the registry re-resolution above then upgrades to a client
			// variant when one is registered.
			foreach (var guid in AssetDatabase.FindAssets($"{_name} t:Prefab"))
			{
				string path = AssetDatabase.GUIDToAssetPath(guid);
				var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
				if (prefab != null && prefab.name == _name &&
				    path.Replace('\\', '/').Contains("/Prefabs/StandardElements/"))
					return prefab;
			}

			// Second pass: any prefab with that exact name anywhere (client widgets / extra folders).
			foreach (var guid in AssetDatabase.FindAssets($"{_name} t:Prefab"))
			{
				string path = AssetDatabase.GUIDToAssetPath(guid);
				var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
				if (prefab != null && prefab.name == _name)
					return prefab;
			}
			return null;
		}

		private static Type ResolveComponentType( string _shortName )
		{
			if (s_componentTypesByName == null)
			{
				s_componentTypesByName = new Dictionary<string, Type>(StringComparer.Ordinal);
				Assembly toolkit = typeof(UiThing).Assembly;

				foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
				{
					Type[] types;
					try { types = asm.GetTypes(); }
					catch (ReflectionTypeLoadException e) { types = e.Types.Where(t => t != null).ToArray(); }
					catch { continue; }

					foreach (var t in types)
					{
						if (t == null || t.IsAbstract || !typeof(Component).IsAssignableFrom(t))
							continue;

						// On a short-name collision, prefer the toolkit's own type.
						if (s_componentTypesByName.TryGetValue(t.Name, out var existing))
						{
							if (existing.Assembly == toolkit)
								continue;
						}
						s_componentTypesByName[t.Name] = t;
					}
				}
			}

			s_componentTypesByName.TryGetValue(_shortName, out var result);
			return result;
		}

		// target Unity component type -> concrete UiApplyStyle* applier type.
		private static void BuildApplierMap()
		{
			if (s_applierByTargetType != null)
				return;

			s_applierByTargetType = new Dictionary<Type, Type>();
			Assembly toolkit = typeof(UiThing).Assembly;

			foreach (var t in SafeTypes(toolkit))
			{
				if (t.IsAbstract || !typeof(UiAbstractApplyStyleBase).IsAssignableFrom(t))
					continue;

				Type target = ApplierTargetType(t);
				if (target != null)
					s_applierByTargetType[target] = t;
			}
		}

		private static Type ApplierTargetType( Type _applierType )
		{
			for (var b = _applierType.BaseType; b != null; b = b.BaseType)
			{
				if (b.IsGenericType && b.GetGenericTypeDefinition().Name.StartsWith("UiAbstractApplyStyle", StringComparison.Ordinal))
				{
					var args = b.GetGenericArguments();
					if (args.Length >= 1)
						return args[0];
				}
			}
			return null;
		}

		private static IEnumerable<Type> SafeTypes( Assembly _asm )
		{
			try { return _asm.GetTypes(); }
			catch (ReflectionTypeLoadException e) { return e.Types.Where(t => t != null); }
			catch { return Array.Empty<Type>(); }
		}

		#endregion

		#region Test fixture

		// The Milestone-2 proof: a dialog composed from templates + a UiView element root.
		private const string TestDialogJson = @"
{
  ""name"": ""AiTestDialog"",
  ""root"": {
    ""type"": ""UiView"",
    ""id"": ""root"",
    ""props"": { ""layer"": ""Dialog"", ""isFullScreen"": false },
    ""children"": [
      {
        ""template"": ""StandardPanelBackgroundWithHeadline"",
        ""id"": ""panel"",
        ""rect"": { ""anchor"": ""center"", ""size"": [900, 600] },
        ""children"": [
          {
            ""template"": ""StandardButtonBar"",
            ""id"": ""buttons"",
            ""rect"": { ""anchor"": ""bottom-stretch"", ""size"": [0, 140], ""position"": [0, 40] },
            ""children"": [
              { ""template"": ""OkButton"", ""id"": ""okButton"", ""text"": ""@text:OK"" },
              { ""template"": ""CancelButton"", ""id"": ""cancelButton"", ""text"": ""@text:Cancel"" }
            ]
          }
        ]
      }
    ]
  }
}";

		#endregion
	}
}
