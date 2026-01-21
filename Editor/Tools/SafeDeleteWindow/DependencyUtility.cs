// Assets/Editor/SafeDelete/DependencyUtility.cs
#if UNITY_2022_3_OR_NEWER
#define USE_UNITY_SEARCH
#endif

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

#if USE_UNITY_SEARCH
using UnityEditor.Search;
#endif

namespace GuiToolkit.Editor
{
	/// <summary>
	/// Builds downstream closures and checks for external referrers (blockers).
	/// Main-asset-only: subasset references are normalized to their main asset.
	/// </summary>
	public static class DependencyUtility
	{
		// ---------------------------------------------------------------------
		// Entry points
		// ---------------------------------------------------------------------

		/// <summary>
		/// Build a downstream closure for a root (scene object or asset).
		/// Returns a set including the root and all referenced items.
		/// Subassets are normalized to their main assets.
		/// </summary>
		public static HashSet<DependencyNode> CollectClosure( Object root )
		{
			var closure = new HashSet<DependencyNode>();
			var seenAssets = new HashSet<string>(); // GUIDs only (main assets)

			if (!root) return closure;
			var rootNode = MakeNode(root);
			if (rootNode == null) return closure;

			if (rootNode.IsSceneObject)
			{
				CollectClosureFromSceneObject(root, closure, seenAssets);
			}
			else
			{
				// Root is an asset -> ensure it's treated as main asset
				if (!string.IsNullOrEmpty(rootNode.Guid))
				{
					seenAssets.Add(rootNode.Guid);
					rootNode.LocalId = 0;
				}
				closure.Add(rootNode);
				CollectClosureFromAsset(rootNode, closure, seenAssets);
			}
			return closure;
		}

		/// <summary>
		/// True if at least one referrer exists outside of the given closure.
		/// </summary>
		public static bool HasExternalReferrers( DependencyNode node, HashSet<DependencyNode> closure )
		{
string s = $"---::: node:{node}\n";
foreach (var closureNode in closure)
s += $"\tclosureNode: {closureNode}\n";
UiLog.LogInternal(s);

			foreach (var r in EnumerateReferrers(node))
			{
UiLog.LogInternal($"---::: {r}");
				if (!closure.Contains(r))
					return true;
			}
			return false;
		}

		/// <summary>
		/// Referrers outside closure (diagnostics).
		/// </summary>
		public static IEnumerable<DependencyNode> EnumerateExternalReferrers( DependencyNode node, HashSet<DependencyNode> closure )
		{
			foreach (var r in EnumerateReferrers(node))
				if (!closure.Contains(r)) yield return r;
		}

		// ---------------------------------------------------------------------
		// Downstream collectors
		// ---------------------------------------------------------------------

		private static void CollectClosureFromSceneObject(
			Object root,
			HashSet<DependencyNode> closure,
			HashSet<string> seenAssets )
		{
			// DFS over stage; only GameObjects become closure nodes.
			var stack = new Stack<Object>();
			var visited = new HashSet<GlobalObjectId>();
			stack.Push(root);

			while (stack.Count > 0)
			{
				var current = stack.Pop();

				// Normalize to GO for stage identity and de-dup
				GameObject ownerGO = current as GameObject;
				if (ownerGO == null && current is Component cmp)
					ownerGO = cmp.gameObject;

				if (ownerGO != null)
				{
					var goid = GlobalObjectId.GetGlobalObjectIdSlow(ownerGO);
					if (visited.Add(goid))
					{
						var goNode = MakeNode(ownerGO);
						if (goNode != null) closure.Add(goNode);

						// children
						var t = ownerGO.transform;
						for (int i = 0; i < t.childCount; ++i)
							stack.Push(t.GetChild(i).gameObject);

						// components (for reference harvesting only)
						foreach (var c in ownerGO.GetComponents<Component>())
							if (c) stack.Push(c);
					}
				}

				// harvest references (adds main assets; schedules stage refs)
				CollectSerializedObjectReferences(current, closure, stack, seenAssets);
			}
		}

		private static void CollectClosureFromAsset(
			DependencyNode assetNode,
			HashSet<DependencyNode> closure,
			HashSet<string> seenAssets )
		{
			if (string.IsNullOrEmpty(assetNode.Path)) return;

			// Asset-level deps (cached by Unity). Normalize every path to main asset.
			var deps = AssetDatabase.GetDependencies(assetNode.Path, true);
			foreach (var depPath in deps)
			{
				if (string.Equals(depPath, assetNode.Path, StringComparison.Ordinal))
					continue;

				var main = AssetDatabase.LoadMainAssetAtPath(depPath);
				if (!main) continue;

				if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(main, out var g, out long _))
				{
					if (seenAssets.Add(g))
					{
						var depNode = MakeNode(main);
						if (depNode != null)
						{
							depNode.LocalId = 0; // mark as main asset
							closure.Add(depNode);
						}
					}
				}
			}
		}

		private static void CollectSerializedObjectReferences(
			Object obj,
			HashSet<DependencyNode> closure,
			Stack<Object> toVisit,
			HashSet<string> seenAssets )
		{
			// A) SerializedObject walk -> normalize asset refs to main asset
			try
			{
				using (var so = new SerializedObject(obj))
				{
					var sp = so.GetIterator();
					while (sp.NextVisible(true))
					{
						if (sp.propertyType != SerializedPropertyType.ObjectReference)
							continue;

						var refObj = sp.objectReferenceValue;
						if (!refObj) continue;

						if (AssetDatabase.Contains(refObj))
						{
							var path = AssetDatabase.GetAssetPath(refObj);
							if (path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
								continue;

							var main = AssetDatabase.LoadMainAssetAtPath(path);
							if (!main) continue;

							if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(main, out var g, out long _))
							{
								if (seenAssets.Add(g))
								{
									var depNode = MakeNode(main);
									if (depNode != null)
									{
										depNode.LocalId = 0;
										closure.Add(depNode);
									}
								}
							}
						}
						else
						{
							// Stage object or component -> schedule traversal
							toVisit.Push(refObj);
						}
					}
				}
			}
			catch { /* best-effort */ }

			// B) Robust sweep via CollectDependencies -> normalize to main assets
			var hardDeps = EditorUtility.CollectDependencies(new Object[] { obj });
			for (int i = 0; i < hardDeps.Length; i++)
			{
				var d = hardDeps[i];
				if (!d) continue;
				if (!AssetDatabase.Contains(d)) continue;

				var path = AssetDatabase.GetAssetPath(d);
				if (path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
					continue;

				var main = AssetDatabase.LoadMainAssetAtPath(path);
				if (!main) continue;

				if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(main, out var g, out long _))
				{
					if (seenAssets.Add(g))
					{
						var depNode = MakeNode(main);
						if (depNode != null)
						{
							depNode.LocalId = 0;
							closure.Add(depNode);
						}
					}
				}
			}
		}

		// ---------------------------------------------------------------------
		// Upstream (dependents) via Unity Search
		// ---------------------------------------------------------------------

		private static IEnumerable<DependencyNode> EnumerateReferrers( DependencyNode node )
		{
#if USE_UNITY_SEARCH
			// --- 1) Project assets by GUID (fast)
			if (!string.IsNullOrEmpty(node.Guid))
			{
				using (var ctx = SearchService.CreateContext(new[] { "assets" }, "ref=" + node.Guid, SearchFlags.None))
				using (var req = SearchService.Request(ctx, SearchFlags.None))
				{
					foreach (var item in req)
					{
						var o = item.ToObject();
						if (!o) continue;
						yield return MakeNode(o);
					}
				}
			}

			// --- 2) Hierarchy by GUID/GOID (may miss Prefab Stage; keep anyway)
			{
				string q = !string.IsNullOrEmpty(node.Guid) ? "ref=" + node.Guid : "ref=" + node.Goid.ToString();
				using (var ctx = SearchService.CreateContext(new[] { "hierarchy" }, q, SearchFlags.None))
				using (var req = SearchService.Request(ctx, SearchFlags.None))
				{
					foreach (var item in req)
					{
						var o = item.ToObject();
						if (!o) continue;
						yield return MakeNode(o);
					}
				}
			}
#endif

			// --- 3) Prefab Stage fallback: manual scan (covers unsaved prefab contents)
#if UNITY_2021_2_OR_NEWER
			var prefabStage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
			if (prefabStage != null)
			{
				// Build the set of GUIDs that should count as a "hit".
				// We normalize to main-asset GUID, so subasset refs (Sprites) count as well.
				var targetGuids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				if (!string.IsNullOrEmpty(node.Guid))
				{
					targetGuids.Add(node.Guid);
				}
				else if (!string.IsNullOrEmpty(node.Path))
				{
					// stage objects won't have a GUID; assets will.
					if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(AssetDatabase.LoadMainAssetAtPath(node.Path), out var g0, out long _))
						targetGuids.Add(g0);
				}

				// Enumerate all GameObjects in the current stage
				var currentStage = UnityEditor.SceneManagement.StageUtility.GetCurrentStageHandle();
				var allGos = Resources.FindObjectsOfTypeAll<GameObject>();
				foreach (var go in allGos)
				{
					if (UnityEditor.SceneManagement.StageUtility.GetStageHandle(go) != currentStage)
						continue;

					// Quick skip: the GO itself is the root in our closure? (the caller will filter)
					// We still yield; closure.Contains will decide.

					// Check all components for object-reference fields pointing to an asset whose MAIN GUID matches target
					var comps = go.GetComponents<Component>();
					foreach (var c in comps)
					{
						if (!c) continue;
						bool hit = false;

						try
						{
							using (var so = new SerializedObject(c))
							{
								var sp = so.GetIterator();
								while (sp.NextVisible(true))
								{
									if (sp.propertyType != SerializedPropertyType.ObjectReference)
										continue;

									var refObj = sp.objectReferenceValue;
									if (!refObj) continue;
									if (!AssetDatabase.Contains(refObj))
										continue;

									var path = AssetDatabase.GetAssetPath(refObj);
									if (string.IsNullOrEmpty(path)) continue;

									// Normalize subasset -> main asset GUID
									var main = AssetDatabase.LoadMainAssetAtPath(path);
									if (!main) continue;

									if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(main, out var g, out long _))
									{
										if (targetGuids.Contains(g))
										{
											hit = true;
											break; // stop scanning this component
										}
									}
								}
							}
						}
						catch
						{
							// ignore non-serialized editor components
						}

						if (hit)
						{
							yield return MakeNode(go);
							break; // one hit per GO is enough
						}
					}
				}
			}
#endif
		}

		// DependencyUtility.cs
		public static GameObject ResolveStageGameObject( GlobalObjectId goid )
		{
			// Try the direct lookup first
			var obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(goid) as GameObject;
			if (obj) return obj;

#if UNITY_2021_2_OR_NEWER
			// Fallback: Prefab Stage manual scan (covers unsaved prefab contents)
			var currentStage = UnityEditor.SceneManagement.StageUtility.GetCurrentStageHandle();
			if (!currentStage.IsValid()) return null;

			var all = Resources.FindObjectsOfTypeAll<GameObject>();
			foreach (var go in all)
			{
				if (UnityEditor.SceneManagement.StageUtility.GetStageHandle(go) != currentStage)
					continue;

				var gid = GlobalObjectId.GetGlobalObjectIdSlow(go);
				if (gid.Equals(goid))
					return go;
			}
#endif
			return null;
		}


		// ---------------------------------------------------------------------
		// Node factory
		// ---------------------------------------------------------------------

		// DependencyUtility.MakeNode(...)
		public static DependencyNode MakeNode( Object obj )
		{
			if (!obj) return null;

			// Normalize components to owning GameObject
			if (obj is Component comp)
				obj = comp.gameObject;

			var node = new DependencyNode
			{
				Name = obj.name,
				TypeName = obj.GetType().Name,
				Goid = GlobalObjectId.GetGlobalObjectIdSlow(obj),
				Path = AssetDatabase.GetAssetPath(obj),
				IsScript = obj is MonoScript
			};

			if (!string.IsNullOrEmpty(node.Path))
			{
				node.IsSceneObject = false;
				AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out node.Guid, out node.LocalId);
				node.IsScript |= node.Path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase);
				node.IsInResources = node.Path.IndexOf("/Resources/", StringComparison.OrdinalIgnoreCase) >= 0;
			}
			else
			{
				node.IsSceneObject = true;
				node.Guid = string.Empty;
				node.LocalId = 0;
				node.IsInResources = false;
			}

			return node;
		}
	}
}
