using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor.AiSupport
{
	/// <summary>
	/// Mirrors the library's prefab inheritance INTO the project, instead of flattening it.
	///
	/// The plain bulk run makes one variant per library prefab, each hanging off its own original. That
	/// gives the project ownership but loses the shape: the library's OkButton is a variant of its
	/// StandardButton, and the project's copies of the two are related to the package, not to each other.
	/// Add a frame to the project's StandardButton and the project's OkButton does not get it — the
	/// inheritance runs sideways into the package instead of down through the project.
	///
	/// So: create the roots as variants of the library prefab, then create each dependent as a variant of
	/// the PROJECT copy of its base, and transplant the library variant's own overrides onto it. The result
	/// is the same graph, one level lower, and a structural change to a project root reaches everything
	/// below it.
	///
	/// What made this hard before is gone: the standard-element identity travels in the overrides, so the
	/// rebuilt dependents keep their registry keys and every existing reference still resolves.
	/// </summary>
	public static class UiVariantGraph
	{
		private const string VariantSuffix = " Variant";

		/// <summary>
		/// Where the library's prefabs live, which is not one fixed path: installed as a package they sit
		/// under Packages/, and in the toolkit's own dev app the same folders are symlinked into Assets/.
		/// A hardcoded default silently finds nothing in one of the two.
		/// </summary>
		private static string DefaultSourceFolder()
		{
			var package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(UiThing).Assembly);
			if (package != null && !string.IsNullOrEmpty(package.assetPath))
				return package.assetPath + "/Runtime/Prefabs";

			string root = UiToolkitConfiguration.Instance.GetUiToolkitRootProjectDir()?.TrimEnd('/');
			foreach (var candidate in new[] { root + "/Runtime/Prefabs", root + "/Prefabs" })
				if (AssetDatabase.IsValidFolder(candidate))
					return candidate;

			throw new Exception("Could not locate the toolkit's prefab folder — pass 'sourceFolder' explicitly.");
		}

		private class Node
		{
			public string SourcePath;
			public string Name;
			public GameObject SourcePrefab;
			public Node Base;                       // null for a root
			public string TargetPath;
			/// <summary>Where a copy of this prefab already sits, when that is not where it belongs.</summary>
			public string MoveFrom;
			public int PropertyMods;
			public int AddedGameObjects;
			public int AddedComponents;
			public int RemovedComponents;
			public bool TargetExists;
			public string Error;
		}

		/// <summary>
		/// Payload: <c>{ "sourceFolder", "targetFolder", "dryRun": true, "replaceExisting": false }</c>.
		/// Returns the graph, what would be written, and — after a real run — a verification report.
		/// </summary>
		public static JObject Mirror( JObject _request )
		{
			string sourceFolder = (string)_request["sourceFolder"] ?? DefaultSourceFolder();
			string targetFolder = (string)_request["targetFolder"]
				?? UiToolkitConfiguration.Instance.PrefabVariantsPath?.TrimEnd('/');
			bool dryRun = (bool?)_request["dryRun"] ?? true;

			// Not a bool, because the useful answer is usually neither "keep everything" nor "replace
			// everything": the roots are where a project puts its own work — the frame someone added to the
			// standard button — while the dependents are exactly what this tool exists to rebuild on top of
			// them. Throwing away the first to fix the second is the one outcome nobody wants.
			string replace = ReplaceMode(_request["replaceExisting"]);
			bool mirrorHierarchy = (bool?)_request["mirrorHierarchy"] ?? true;

			if (string.IsNullOrWhiteSpace(targetFolder))
				throw new Exception("No target folder: pass 'targetFolder', or set the Prefab Variants Path in "
					+ "Gui Toolkit -> Configuration.");
			targetFolder = targetFolder.TrimEnd('/');
			if (!targetFolder.StartsWith("Assets/", StringComparison.Ordinal))
				throw new Exception($"'{targetFolder}' is not inside Assets/ — the project's own copies have to be.");

			var nodes = BuildGraph(sourceFolder, targetFolder, mirrorHierarchy);
			var ordered = TopologicalOrder(nodes);
			var toMove = ordered.Where(_n => !string.IsNullOrEmpty(_n.MoveFrom)).ToList();

			var result = new JObject
			{
				["sourceFolder"] = sourceFolder,
				["targetFolder"] = targetFolder,
				["dryRun"] = dryRun,
				["replaceExisting"] = replace,
				["mirrorHierarchy"] = mirrorHierarchy,
				["toMove"] = new JArray(toMove.Select(_n => (object)new JObject
				{
					["from"] = _n.MoveFrom,
					["to"] = _n.TargetPath,
				}).ToArray()),
				["counts"] = new JObject
				{
					["total"] = ordered.Count,
					["roots"] = ordered.Count(_n => _n.Base == null),
					["dependents"] = ordered.Count(_n => _n.Base != null),
					["alreadyPresent"] = ordered.Count(_n => _n.TargetExists),
					["structural"] = ordered.Count(_n => _n.Base != null && _n.HasStructure()),
				},
				["graph"] = new JArray(ordered.Where(_n => _n.Base != null).Select(_n => (object)new JObject
				{
					["name"] = _n.Name,
					["base"] = _n.Base.Name,
					["propertyMods"] = _n.PropertyMods,
					["addedGameObjects"] = _n.AddedGameObjects,
					["addedComponents"] = _n.AddedComponents,
					["removedComponents"] = _n.RemovedComponents,
				}).ToArray()),
			};

			if (dryRun)
			{
				result["hint"] = "Nothing was written. Re-run with dryRun:false to create what is missing. "
					+ "Anything under 'toMove' is an existing copy that sits in the wrong folder; a real run "
					+ "MOVES it (keeping its GUID, its place in the chain and any hand edits) rather than "
					+ "rebuilding it. replaceExisting:'dependents' also rebuilds the ones that already exist AND "
					+ "have a base, so hand-edited roots survive; 'all' rebuilds everything. A rebuilt asset "
					+ "gets a new GUID — cheap now, expensive once anything references it.";
				return result;
			}

			var written = new JArray();
			var failed = new JArray();
			var verification = new JArray();

			// Relocate before anything else, so the rest of the run sees every existing copy where it belongs.
			// MoveAsset keeps the GUID, which is what makes this a reorganisation rather than a rebuild:
			// references survive, the variant chain survives, and so does whatever a human edited into them.
			// Every folder first, while the target still looks the way it will look. Creating them one by one
			// during the moves means creating a folder next to files that are on their way out of it, which
			// is exactly the situation that makes Unity mangle the name.
			foreach (var node in ordered)
			{
				string wanted = Path.GetDirectoryName(node.TargetPath)?.Replace('\\', '/');
				string actual = EnsureFolder(wanted);
				if (actual != wanted)
					node.TargetPath = actual + "/" + Path.GetFileName(node.TargetPath);
			}

			var moved = new JArray();
			foreach (var node in toMove)
			{
				string error = AssetDatabase.MoveAsset(node.MoveFrom, node.TargetPath);
				if (string.IsNullOrEmpty(error))
					moved.Add(new JObject { ["from"] = node.MoveFrom, ["to"] = node.TargetPath });
				else
					failed.Add(new JObject { ["name"] = node.Name, ["error"] = $"move failed: {error}" });
			}

			if (moved.Count > 0)
			{
				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh();
			}

			bool Replaces( Node _node ) => _node.TargetExists
				&& (replace == "all" || (replace == "dependents" && _node.Base != null));

			// Delete everything that is being replaced FIRST, and bottom-up. A folder holding a variant chain
			// cannot be taken apart in any order: remove a base while its dependents are still on disk and
			// Unity re-imports them parentless, filling the console with "Missing Prefab Variant parent" —
			// which reads like data loss and is only a half-demolished chain. Dependents go first, so nothing
			// is ever orphaned, not even for one import.
			foreach (var node in Enumerable.Reverse(ordered).Where(Replaces))
				AssetDatabase.DeleteAsset(node.TargetPath);

			// Deliberately NOT batched with StartAssetEditing: each dependent is built on the asset created
			// one step earlier, and inside a batch that asset is not importable yet — every dependent then
			// fails with "base is missing" while the roots look fine. The chain needs each write to land.
			try
			{
				foreach (var node in ordered)
				{
					if (node.TargetExists && !Replaces(node))
						continue;

					try
					{
						Create(node);
						written.Add(node.TargetPath);
					}
					catch (Exception e)
					{
						node.Error = e.Message;
						failed.Add(new JObject { ["name"] = node.Name, ["error"] = e.Message });
					}
				}
			}
			finally
			{
				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh();
			}

			// Verify against the ORIGINAL, not against the plan: the plan is what we believed, the library
			// prefab is what the project has to end up matching.
			foreach (var node in ordered.Where(_n => _n.Base != null && _n.Error == null))
			{
				var differences = Verify(node);
				if (differences.Count > 0)
					verification.Add(new JObject
					{
						["name"] = node.Name,
						["differences"] = differences.Count,
						["examples"] = new JArray(differences.Take(8).Cast<object>().ToArray()),
					});
			}

			result["written"] = written;
			result["moved"] = moved;
			if (failed.Count > 0)
				result["failed"] = failed;
			result["verified"] = new JObject
			{
				["checked"] = ordered.Count(_n => _n.Base != null && _n.Error == null),
				["withDifferences"] = verification.Count,
				["details"] = verification,
			};
			result["hint"] = verification.Count == 0
				? "Every rebuilt dependent matches its library original property for property."
				: "Some rebuilt dependents differ from their library original — read 'verified.details'. "
					+ "A difference is not automatically wrong (a project root may deliberately differ), but "
					+ "anything unexpected there is a transplant that did not land.";
			return result;
		}

		/// <summary>Accepts the string form and the older bool, so a caller cannot get "true" wrong.</summary>
		private static string ReplaceMode( JToken _token )
		{
			if (_token == null)
				return "none";

			if (_token.Type == JTokenType.Boolean)
				return (bool)_token ? "all" : "none";

			string value = ((string)_token ?? "none").ToLowerInvariant();
			return value switch
			{
				"none" or "dependents" or "all" => value,
				_ => throw new Exception($"'replaceExisting' is 'none', 'dependents' or 'all', not '{value}'."),
			};
		}

		private static bool HasStructure( this Node _node ) =>
			_node.AddedGameObjects > 0 || _node.AddedComponents > 0 || _node.RemovedComponents > 0;

		#region Graph

		/// <summary>The prefab's folder path relative to the library's prefab root, or "" at the top.</summary>
		private static string RelativeFolder( string _assetPath, string _sourceFolder )
		{
			string folder = Path.GetDirectoryName(_assetPath)?.Replace('\\', '/') ?? "";
			if (!folder.StartsWith(_sourceFolder, StringComparison.Ordinal))
				return "";

			return folder.Substring(_sourceFolder.Length).Trim('/');
		}

		/// <summary>Finds a variant of this name anywhere under the target root, whatever folder it sits in.</summary>
		private static string FindExisting( string _targetFolder, string _assetName )
		{
			if (!AssetDatabase.IsValidFolder(_targetFolder))
				return null;

			foreach (var guid in AssetDatabase.FindAssets($"\"{_assetName}\" t:Prefab", new[] { _targetFolder }))
			{
				string path = AssetDatabase.GUIDToAssetPath(guid);
				if (Path.GetFileNameWithoutExtension(path) == _assetName)
					return path;
			}

			return null;
		}

		private static List<Node> BuildGraph( string _sourceFolder, string _targetFolder, bool _mirrorHierarchy )
		{
			var byPrefab = new Dictionary<GameObject, Node>();
			var nodes = new List<Node>();

			foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { _sourceFolder }))
			{
				string path = AssetDatabase.GUIDToAssetPath(guid);

				// A prefab the library loads by name from its own Resources folder: a copy elsewhere is never
				// found by that lookup, so it would be dead weight under a misleading name.
				if (path.Contains("/Resources/"))
					continue;

				var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
				if (prefab == null)
					continue;

				// The library's own folders are carried over, because 65 prefabs in one flat folder is a list
				// nobody can read: "Buttons/OkButton Variant" says what it is, "OkButton Variant" between 64
				// neighbours does not.
				string subFolder = _mirrorHierarchy ? RelativeFolder(path, _sourceFolder) : "";
				string targetFolder = string.IsNullOrEmpty(subFolder) ? _targetFolder : $"{_targetFolder}/{subFolder}";
				string targetPath = $"{targetFolder}/{prefab.name}{VariantSuffix}.prefab";

				var node = new Node
				{
					SourcePath = path,
					Name = prefab.name,
					SourcePrefab = prefab,
					TargetPath = targetPath,
					TargetExists = AssetDatabase.LoadAssetAtPath<GameObject>(targetPath) != null,
				};

				// A copy that already exists SOMEWHERE ELSE under the target root is moved, never rebuilt:
				// moving keeps its GUID, its place in the variant chain and whatever a human has since done
				// to it. Rebuilding would quietly throw all three away — and the hand-made things are exactly
				// what lives in these files.
				if (!node.TargetExists)
				{
					string elsewhere = FindExisting(_targetFolder, prefab.name + VariantSuffix);
					if (!string.IsNullOrEmpty(elsewhere))
					{
						node.MoveFrom = elsewhere;
						node.TargetExists = true;
					}
				}

				byPrefab[prefab] = node;
				nodes.Add(node);
			}

			foreach (var node in nodes)
			{
				if (PrefabUtility.GetPrefabAssetType(node.SourcePrefab) != PrefabAssetType.Variant)
					continue;

				var basePrefab = PrefabUtility.GetCorrespondingObjectFromSource(node.SourcePrefab);
				if (basePrefab != null && byPrefab.TryGetValue(basePrefab, out var baseNode))
					node.Base = baseNode;

				Weigh(node);
			}

			return nodes;
		}

		private static void Weigh( Node _node )
		{
			var mods = PrefabUtility.GetPropertyModifications(_node.SourcePrefab);
			_node.PropertyMods = mods?.Length ?? 0;

			var contents = PrefabUtility.LoadPrefabContents(_node.SourcePath);
			try
			{
				_node.AddedGameObjects = PrefabUtility.GetAddedGameObjects(contents).Count;
				_node.AddedComponents = PrefabUtility.GetAddedComponents(contents).Count;
				_node.RemovedComponents = PrefabUtility.GetRemovedComponents(contents).Count;
			}
			catch { /* counted as zero; the verification pass is what actually decides */ }
			finally
			{
				PrefabUtility.UnloadPrefabContents(contents);
			}
		}

		/// <summary>Bases before dependents, so a dependent can be built on the project copy of its base.</summary>
		private static List<Node> TopologicalOrder( List<Node> _nodes )
		{
			var result = new List<Node>();
			var placed = new HashSet<Node>();

			void Place( Node _node, HashSet<Node> _onPath )
			{
				if (placed.Contains(_node))
					return;
				if (!_onPath.Add(_node))
					throw new Exception($"Cyclic variant chain at '{_node.Name}'.");

				if (_node.Base != null)
					Place(_node.Base, _onPath);

				placed.Add(_node);
				result.Add(_node);
			}

			foreach (var node in _nodes)
				Place(node, new HashSet<Node>());

			return result;
		}

		#endregion

		#region Create

		private static void Create( Node _node )
		{
			string folder = EnsureFolder(Path.GetDirectoryName(_node.TargetPath)?.Replace('\\', '/'));
			_node.TargetPath = folder + "/" + Path.GetFileName(_node.TargetPath);

			// A root hangs off the library prefab; a dependent hangs off the PROJECT copy of its base, which
			// exists already because the nodes are processed bases-first.
			GameObject baseAsset = _node.Base == null
				? _node.SourcePrefab
				: AssetDatabase.LoadAssetAtPath<GameObject>(_node.Base.TargetPath);

			if (baseAsset == null)
				throw new Exception($"Base '{_node.Base?.TargetPath}' is missing — it should have been created first.");

			var instance = PrefabUtility.InstantiatePrefab(baseAsset) as GameObject;
			if (instance == null)
				throw new Exception($"Could not instantiate '{AssetDatabase.GetAssetPath(baseAsset)}'.");

			try
			{
				if (_node.Base != null)
					Transplant(_node, instance);

				instance.name = _node.Name;
				PrefabUtility.SaveAsPrefabAsset(instance, _node.TargetPath, out bool success);
				if (!success)
					throw new Exception($"SaveAsPrefabAsset failed for '{_node.TargetPath}'.");
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(instance);
			}
		}

		/// <summary>
		/// Copies what the library's variant adds to its base onto our instance: first structure (added
		/// objects and components, removed components), then the property overrides — in that order, because
		/// a modification can target something that only the structural pass creates.
		/// </summary>
		private static void Transplant( Node _node, GameObject _instance )
		{
			var source = PrefabUtility.LoadPrefabContents(_node.SourcePath);
			try
			{
				var targetByPath = MapByPath(_instance);
				var doomedComponents = new List<Component>();

				foreach (var removed in PrefabUtility.GetRemovedComponents(source))
				{
					var component = removed.assetComponent;
					if (component == null)
						continue;

					// The path has to be read off the INSTANCE side. assetComponent lives in the base prefab,
					// a different tree entirely, so walking up from it never reaches this source's root and
					// the lookup quietly found nothing — every removal was skipped in silence. The visible
					// result was a variant carrying both the component its base has and the one that replaces
					// it: a radio row that still had the plain toggle underneath, writing a bool into a
					// string setting the moment it was used.
					var owner = removed.containingInstanceGameObject;
					if (owner == null)
						continue;

					string path = PathOf(owner.transform, source.transform);
					if (!targetByPath.TryGetValue(path, out var targetGo))
						continue;

					var counterpart = targetGo.GetComponent(component.GetType());
					if (counterpart != null)
						doomedComponents.Add(counterpart);
				}

				// Removed in dependency order, and only what is actually removable: Unity refuses to delete a
				// CanvasRenderer while an Image still needs it and logs an error for the attempt, so the
				// naive loop filled the console with failures that were merely the wrong order.
				for (int pass = 0; pass < 4 && doomedComponents.Count > 0; pass++)
				{
					for (int i = doomedComponents.Count - 1; i >= 0; i--)
					{
						var doomed = doomedComponents[i];
						if (doomed == null)
						{
							doomedComponents.RemoveAt(i);
							continue;
						}

						if (IsRequiredByAnotherComponent(doomed, doomedComponents))
							continue;

						UnityEngine.Object.DestroyImmediate(doomed, true);
						doomedComponents.RemoveAt(i);
					}
				}

				// A variant can also DELETE something its base has — the language dropdown replaces the plain
				// dropdown its base carries rather than sitting next to it. Without this the copy keeps both,
				// which the one-directional verification could not see either.
				foreach (var removed in PrefabUtility.GetRemovedGameObjects(source))
				{
					var gameObject = removed.assetGameObject;
					if (gameObject == null || removed.parentOfRemovedGameObjectInInstance == null)
						continue;

					string parentPath = PathOf(removed.parentOfRemovedGameObjectInInstance.transform, source.transform);
					if (!targetByPath.TryGetValue(parentPath, out var parent))
						continue;

					var doomedObject = parent.transform.Find(gameObject.name);
					if (doomedObject != null)
						UnityEngine.Object.DestroyImmediate(doomedObject.gameObject, true);
				}

				// Re-map: the deletions changed which paths exist.
				targetByPath = MapByPath(_instance);

				foreach (var added in PrefabUtility.GetAddedGameObjects(source))
				{
					var go = added.instanceGameObject;
					if (go == null || go.transform.parent == null)
						continue;

					string parentPath = PathOf(go.transform.parent, source.transform);
					if (!targetByPath.TryGetValue(parentPath, out var parent))
						continue;

					var copy = UnityEngine.Object.Instantiate(go, parent.transform, false);
					copy.name = go.name;
					copy.transform.SetSiblingIndex(go.transform.GetSiblingIndex());
				}

				// Re-map: the structural pass may have created new paths.
				targetByPath = MapByPath(_instance);

				foreach (var added in PrefabUtility.GetAddedComponents(source))
				{
					var component = added.instanceComponent;
					if (component == null)
						continue;

					string path = PathOf(component.transform, source.transform);
					if (!targetByPath.TryGetValue(path, out var targetGo))
						continue;

					// Counts, not presence: a variant can add a SECOND component of a type the base already
					// has — two style appliers on one object is the library's own pattern — and "does it have
					// one already" silently drops the addition.
					var type = component.GetType();
					if (targetGo.GetComponents(type).Length >= component.gameObject.GetComponents(type).Length)
						continue;

					UnityEditorInternal.ComponentUtility.CopyComponent(component);
					UnityEditorInternal.ComponentUtility.PasteComponentAsNew(targetGo);
				}

				CopyPropertyValues(source, _instance);
				RemapInternalReferences(source, _instance);
			}
			finally
			{
				PrefabUtility.UnloadPrefabContents(source);
			}
		}

		/// <summary>
		/// Copies every serialized value from the library variant onto our instance, object by object. Done
		/// as a value copy rather than by re-targeting the library's PropertyModification list: those entries
		/// point at objects inside the PACKAGE base, and every one of them would have to be re-aimed at the
		/// corresponding object under our base. Copying the values leaves Unity to work out which of them
		/// differ from our base and therefore deserve to be overrides — which is the same answer, arrived at
		/// by the party that owns the definition of "differs".
		/// </summary>
		private static void CopyPropertyValues( GameObject _source, GameObject _target )
		{
			var targetByPath = MapByPath(_target);

			foreach (var sourceTransform in _source.GetComponentsInChildren<Transform>(true))
			{
				string path = PathOf(sourceTransform, _source.transform);
				if (!targetByPath.TryGetValue(path, out var targetGo))
					continue;

				var sourceGo = sourceTransform.gameObject;
				targetGo.name = sourceGo.name;
				targetGo.SetActive(sourceGo.activeSelf);
				targetGo.layer = sourceGo.layer;
				targetGo.tag = sourceGo.tag;

				var counted = new Dictionary<Type, int>();
				foreach (var sourceComponent in sourceGo.GetComponents<Component>())
				{
					if (sourceComponent == null)
						continue;

					var type = sourceComponent.GetType();
					counted.TryGetValue(type, out int index);
					counted[type] = index + 1;

					var targetComponents = targetGo.GetComponents(type);
					if (index >= targetComponents.Length)
						continue;

					EditorUtility.CopySerializedIfDifferent(sourceComponent, targetComponents[index]);
				}
			}
		}

		/// <summary>
		/// Re-aims every reference that points INSIDE the copied hierarchy at the corresponding object in our
		/// copy. Without this the transplant looks like it worked and is quietly gutted: CopySerialized copies
		/// an object reference verbatim, so a button's reference to its own Image still names the source
		/// tree's Image, and saving the instance as an asset drops every reference that leads outside it. The
		/// result is a prefab full of nulls — the animation with no target, the toggle with no checkmark —
		/// and nothing says so until someone clicks it.
		///
		/// References to real assets (sprites, fonts, materials) are left alone: they are not part of the
		/// hierarchy and are meant to point where they point.
		/// </summary>
		private static void RemapInternalReferences( GameObject _source, GameObject _target )
		{
			var targetByPath = MapByPath(_target);
			var sourceRoot = _source.transform;

			// Everything that belongs to the copied tree, so an external asset reference can be told apart
			// from an internal wiring reference.
			var internalObjects = new HashSet<UnityEngine.Object>();
			foreach (var transform in _source.GetComponentsInChildren<Transform>(true))
			{
				internalObjects.Add(transform.gameObject);
				foreach (var component in transform.GetComponents<Component>())
					if (component != null)
						internalObjects.Add(component);
			}

			UnityEngine.Object Corresponding( UnityEngine.Object _value )
			{
				var gameObject = _value as GameObject;
				var component = _value as Component;
				var transform = gameObject != null ? gameObject.transform : component?.transform;
				if (transform == null)
					return null;

				string path = PathOf(transform, sourceRoot);
				if (!targetByPath.TryGetValue(path, out var counterpart))
					return null;

				if (gameObject != null)
					return counterpart;

				// Index among same-typed components, so two appliers on one object stay distinguishable.
				var type = component.GetType();
				var sourceSiblings = component.gameObject.GetComponents(type);
				int index = Array.IndexOf(sourceSiblings, component);
				var targetSiblings = counterpart.GetComponents(type);
				return index >= 0 && index < targetSiblings.Length ? targetSiblings[index] : null;
			}

			foreach (var transform in _target.GetComponentsInChildren<Transform>(true))
			{
				foreach (var component in transform.GetComponents<Component>())
				{
					if (component == null)
						continue;

					var serialized = new SerializedObject(component);
					var iterator = serialized.GetIterator();
					bool changed = false;

					while (iterator.NextVisible(true))
					{
						if (iterator.propertyType != SerializedPropertyType.ObjectReference)
							continue;

						var value = iterator.objectReferenceValue;
						if (value == null || !internalObjects.Contains(value))
							continue;

						iterator.objectReferenceValue = Corresponding(value);
						changed = true;
					}

					if (changed)
						serialized.ApplyModifiedPropertiesWithoutUndo();
				}
			}
		}

		#endregion

		#region Verify

		/// <summary>
		/// Compares the created variant against the library original property for property. This is the part
		/// that makes the operation reviewable rather than hopeful: a transplant that quietly dropped
		/// something shows up here as a named property, not as a bug three weeks later.
		///
		/// It checks BOTH directions, which it did not at first: only asking "is everything from the original
		/// present in the copy" passed a copy that carried components the original does not have. A variant
		/// that replaces its base's toggle with a radio kept both, the extra one wrote a bool into a string
		/// setting, and the verification reported no differences at all while it happened.
		/// </summary>
		private static List<string> Verify( Node _node )
		{
			var differences = new List<string>();
			var original = PrefabUtility.LoadPrefabContents(_node.SourcePath);
			var created = PrefabUtility.LoadPrefabContents(_node.TargetPath);

			try
			{
				var createdByPath = MapByPath(created);
				CompareExtras(original, created, differences);

				foreach (var originalTransform in original.GetComponentsInChildren<Transform>(true))
				{
					string path = PathOf(originalTransform, original.transform);
					if (!createdByPath.TryGetValue(path, out var createdGo))
					{
						differences.Add($"missing object: {path}");
						continue;
					}

					var originalGo = originalTransform.gameObject;
					var counted = new Dictionary<Type, int>();

					foreach (var originalComponent in originalGo.GetComponents<Component>())
					{
						if (originalComponent == null)
							continue;

						var type = originalComponent.GetType();
						counted.TryGetValue(type, out int index);
						counted[type] = index + 1;

						var createdComponents = createdGo.GetComponents(type);
						if (index >= createdComponents.Length)
						{
							differences.Add($"missing component: {path} / {type.Name}");
							continue;
						}

						CompareSerialized(originalComponent, createdComponents[index], path, differences,
							original.transform, created.transform);
						if (differences.Count > 200)
							return differences;
					}
				}
			}
			finally
			{
				PrefabUtility.UnloadPrefabContents(original);
				PrefabUtility.UnloadPrefabContents(created);
			}

			return differences;
		}

		/// <summary>
		/// Whether some component that is STAYING declares it needs this one. Unity refuses the removal in
		/// that case and logs it, so asking first turns a console full of failures into a later pass.
		/// </summary>
		/// <summary>
		/// Components of EXACTLY this type. GetComponents(type) also counts subclasses, and that leniency
		/// hides the very thing this comparison is for: a variant whose base class component was supposed to
		/// be replaced by a derived one kept both, and the check saw the derived one and called it even.
		/// </summary>
		private static int CountExact( GameObject _gameObject, Type _type )
		{
			int count = 0;
			foreach (var component in _gameObject.GetComponents<Component>())
				if (component != null && component.GetType() == _type)
					count++;

			return count;
		}

		private static bool IsRequiredByAnotherComponent( Component _component, List<Component> _alsoGoing )
		{
			var type = _component.GetType();
			foreach (var other in _component.gameObject.GetComponents<Component>())
			{
				if (other == null || other == _component || _alsoGoing.Contains(other))
					continue;

				foreach (RequireComponent requirement in other.GetType()
					         .GetCustomAttributes(typeof(RequireComponent), true))
				{
					if (requirement.m_Type0 == type || requirement.m_Type1 == type || requirement.m_Type2 == type)
						return true;
				}
			}

			return false;
		}

		/// <summary>
		/// The other direction: objects and components the COPY has and the original does not.
		/// </summary>
		private static void CompareExtras( GameObject _original, GameObject _created, List<string> _differences )
		{
			var originalByPath = MapByPath(_original);

			foreach (var transform in _created.GetComponentsInChildren<Transform>(true))
			{
				string path = PathOf(transform, _created.transform);
				if (!originalByPath.TryGetValue(path, out var counterpart))
				{
					_differences.Add($"extra object: {path}");
					continue;
				}

				var counted = new Dictionary<Type, int>();
				foreach (var component in transform.GetComponents<Component>())
				{
					if (component == null)
						continue;

					var type = component.GetType();
					counted.TryGetValue(type, out int index);
					counted[type] = index + 1;

					if (CountExact(counterpart, type) <= index)
						_differences.Add($"extra component: {(path.Length == 0 ? "<root>" : path)} / {type.Name}");
				}
			}
		}

		private static void CompareSerialized( Component _original, Component _created, string _path,
			List<string> _differences, Transform _originalRoot, Transform _createdRoot )
		{
			var a = new SerializedObject(_original).GetIterator();
			var b = new SerializedObject(_created).GetIterator();

			// Descends into children on purpose. A property that CONTAINS references — an array of slave
			// animations, say — compares unequal as a whole no matter what, because the elements are
			// different instances of the same thing; only its leaves can be judged by where they point.
			while (a.NextVisible(true) && b.NextVisible(true))
			{
				// Identities differ by definition: they point at different assets and different instances.
				if (a.propertyPath is "m_Script" or "m_GameObject" or "m_CorrespondingSourceObject"
				    or "m_PrefabInstance" or "m_PrefabAsset" or "m_Father" or "m_Children")
					continue;

				// Containers are judged by their leaves, except an object reference, which is a leaf that
				// happens to have children.
				if (a.hasVisibleChildren && a.propertyType != SerializedPropertyType.ObjectReference)
					continue;

				if (SerializedProperty.DataEquals(a, b))
					continue;

				// An object reference into the prefab's own hierarchy is a different object here and there, so
				// identity says nothing. Compare WHERE it points, not what it is called: a variant renames its
				// root, and comparing names then reports every self-reference as a difference.
				if (a.propertyType == SerializedPropertyType.ObjectReference)
				{
					string an = Describe(a.objectReferenceValue, _originalRoot);
					string bn = Describe(b.objectReferenceValue, _createdRoot);
					if (an == bn)
						continue;
					_differences.Add($"{_path} / {_original.GetType().Name}.{a.propertyPath}: {an} != {bn}");
					continue;
				}

				_differences.Add($"{_path} / {_original.GetType().Name}.{a.propertyPath}");
			}
		}

		/// <summary>
		/// How a reference is compared across the two trees: a hierarchy path when it points inside, the
		/// asset's own name when it points outside (a sprite, a font), "null" when it points nowhere.
		/// </summary>
		private static string Describe( UnityEngine.Object _value, Transform _root )
		{
			if (_value == null)
				return "null";

			var gameObject = _value as GameObject;
			var component = _value as Component;
			var transform = gameObject != null ? gameObject.transform : component?.transform;

			if (transform == null || !transform.IsChildOf(_root))
				return _value.name;

			string path = PathOf(transform, _root);
			string where = string.IsNullOrEmpty(path) ? "<root>" : path;
			return component != null ? $"{where}:{component.GetType().Name}" : where;
		}

		#endregion

		#region Helpers

		private static Dictionary<string, GameObject> MapByPath( GameObject _root )
		{
			var result = new Dictionary<string, GameObject>(StringComparer.Ordinal);
			foreach (var transform in _root.GetComponentsInChildren<Transform>(true))
			{
				string path = PathOf(transform, _root.transform);
				if (!result.ContainsKey(path))
					result[path] = transform.gameObject;
			}

			return result;
		}

		/// <summary>
		/// Hierarchy path relative to the root, used to pair objects across two different instances. Names
		/// rather than object identity, because the two trees have none in common — and the root is "" so a
		/// renamed root (which every variant does) does not shift every path below it.
		/// </summary>
		private static string PathOf( Transform _transform, Transform _root )
		{
			if (_transform == _root)
				return "";

			var parts = new List<string>();
			var current = _transform;
			while (current != null && current != _root)
			{
				parts.Add(Segment(current));
				current = current.parent;
			}

			parts.Reverse();
			return string.Join("/", parts);
		}

		/// <summary>
		/// A path segment, disambiguated when siblings share a name. Unity allows that, and the library uses
		/// it — two children called "Image" under one parent. A plain name path then maps both to the first
		/// one, and everything the second carries is silently dropped.
		/// </summary>
		private static string Segment( Transform _transform )
		{
			var parent = _transform.parent;
			if (parent == null)
				return _transform.name;

			int ordinal = 0;
			int sameName = 0;
			foreach (Transform sibling in parent)
			{
				if (sibling == _transform)
					ordinal = sameName;
				if (sibling.name == _transform.name)
					sameName++;
			}

			return sameName > 1 ? $"{_transform.name}#{ordinal}" : _transform.name;
		}

		/// <summary>
		/// Creates the folder and returns where it ACTUALLY ended up, which is not always what was asked for.
		///
		/// Measured, and reproducible: `AssetDatabase.CreateFolder(parent, "Dialogs")` returns
		/// "parent/DialogS" when the parent already holds a file whose name starts with that same prefix in a
		/// different case — here "DialogStub Variant.prefab". Unity resolves the new folder's casing against
		/// the existing sibling and hands back a name nobody asked for. Without that sibling the same call is
		/// correct. So the requested name cannot be trusted, and a caller that assumes it silently writes into
		/// "DialogS" for the rest of the project's life.
		///
		/// Renaming afterwards does work, because it takes a different path through the asset database.
		/// </summary>
		private static string EnsureFolder( string _folder )
		{
			if (string.IsNullOrEmpty(_folder) || AssetDatabase.IsValidFolder(_folder))
				return _folder;

			string parent = EnsureFolder(Path.GetDirectoryName(_folder)?.Replace('\\', '/'));
			string wanted = Path.GetFileName(_folder);

			string guid = AssetDatabase.CreateFolder(parent, wanted);
			string actual = AssetDatabase.GUIDToAssetPath(guid);
			if (string.IsNullOrEmpty(actual) || actual == _folder)
				return _folder;

			string error = AssetDatabase.RenameAsset(actual, wanted);
			if (string.IsNullOrEmpty(error) && AssetDatabase.IsValidFolder(_folder))
				return _folder;

			// Could not be corrected: return the truth rather than a path that does not exist.
			return actual;
		}

		#endregion
	}
}
