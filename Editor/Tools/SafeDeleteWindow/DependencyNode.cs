// Assets/Editor/SafeDelete/DependencyNode.cs

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GuiToolkit.Editor
{
	/// <summary>
	/// Data holder that uniquely identifies an object (asset or stage object).
	/// Assets/subassets are keyed by (GUID, LocalId). Stage objects by GlobalObjectId.
	/// </summary>
	[Serializable]
	public class DependencyNode : IEquatable<DependencyNode>
	{
		public string Name;
		public string Path;
		public string Guid;
		public long LocalId;
		public GlobalObjectId Goid;
		public string TypeName;
		public bool IsSceneObject;
		public bool IsScript;
		public bool IsInResources;
		public bool IsInPackages;
		public DependencyNode Parent;
		public Object Object;

		public readonly HashSet<DependencyNode> DirectDependencies = new();
		public readonly HashSet<DependencyNode> DirectDependants = new();
		public DependencyNodeTree Tree;

		public DependencyNode() { } // Deprecated

		public DependencyNode( Object _object, DependencyNodeTree _tree, DependencyNode _parent )
		{
			Object = _object;
			Tree = _tree;
			Parent = _parent;

			SetProperties(_object);
		}

		public void CollectDirectDependencies()
		{
			if (IsSceneObject)
			{
				CollectDependenciesFromSceneObject();
				return;
			}

			CollectDependenciesFromAsset();
		}

		private void CollectDependenciesFromSceneObject()
		{
			var go = Object as GameObject;
			if (!go)
			{
				if (Object is Component comp) go = comp.gameObject;
				if (!go) return;
			}

			// 1) Children are structural dependencies (deleting parent deletes children)
			var t = go.transform;
			for (int i = 0; i < t.childCount; ++i)
			{
				var childGo = t.GetChild(i).gameObject;
				var childNode = Tree.GetOrCreateNode(childGo, this);
				Link(this, childNode);
			}

			// 2) Component object-references (assets and stage objects)
			foreach (var c in go.GetComponents<Component>())
			{
				if (!c) continue;

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

							// Asset reference?
							if (AssetDatabase.Contains(refObj))
							{
								var path = AssetDatabase.GetAssetPath(refObj);
								if (string.IsNullOrEmpty(path)) continue;
								if (path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)) continue; // ignore scripts

								// Normalize subassets -> main asset
								var main = AssetDatabase.LoadMainAssetAtPath(path);
								if (!main) continue;

								var depNode = Tree.GetOrCreateNode(main, this);
								Link(this, depNode);
							}
							else
							{
								// Stage-side reference (component or GO)
								var refGo = (refObj as GameObject) ?? (refObj as Component)?.gameObject;
								if (!refGo) continue;

								var depNode = Tree.GetOrCreateNode(refGo, this);
								Link(this, depNode);
							}
						}
					}
				}
				catch
				{
					// non-serializable editor components etc. – ignore in MVP
				}
			}
		}

		private void CollectDependenciesFromAsset()
		{
			if (string.IsNullOrEmpty(Path)) return;

			// Unity's cached dependency listing. Always normalize to main asset.
			var deps = AssetDatabase.GetDependencies(Path, true);
			foreach (var depPath in deps)
			{
				if (string.Equals(depPath, Path, StringComparison.Ordinal))
					continue; // skip self

				var main = AssetDatabase.LoadMainAssetAtPath(depPath);
				if (!main) continue;

				var ext = System.IO.Path.GetExtension(depPath);
				if (string.Equals(ext, ".cs", StringComparison.OrdinalIgnoreCase))
					continue; // ignore scripts

				var depNode = Tree.GetOrCreateNode(main, this);
				Link(this, depNode);
			}
		}
		

		// Small helper to wire the edge both ways and rely on HashSet dedup.
		private static void Link( DependencyNode from, DependencyNode to )
		{
			if (to == null || from == null) 
				return;
			if (ReferenceEquals(from, to))
				return;

			from.DirectDependencies.Add(to);
			to.DirectDependants.Add(from);
		}

		private void SetProperties( Object _object )
		{
			// Normalize components to owning GameObject
			if (_object is Component comp)
				_object = comp.gameObject;

			Name = _object.name;
			TypeName = _object.GetType().Name;
			Goid = GlobalObjectId.GetGlobalObjectIdSlow(_object);
			Path = AssetDatabase.GetAssetPath(_object);
			IsScript = _object is MonoScript;

			if (!string.IsNullOrEmpty(Path))
			{
				IsSceneObject = false;
				AssetDatabase.TryGetGUIDAndLocalFileIdentifier(_object, out Guid, out LocalId);
				IsScript |= Path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase);
				IsInResources = Path.IndexOf("/Resources/", StringComparison.OrdinalIgnoreCase) >= 0;
				IsInPackages = Path.StartsWith("Packages", StringComparison.OrdinalIgnoreCase);
				return;
			}

			IsSceneObject = true;
			Guid = string.Empty;
			LocalId = 0;
			IsInResources = false;
			IsInPackages = false;
		}

		public bool Equals( DependencyNode other )
		{
			if (other == null) return false;
			if (!string.IsNullOrEmpty(Guid) || !string.IsNullOrEmpty(other.Guid))
				return Guid == other.Guid && LocalId == other.LocalId;
			return Goid.Equals(other.Goid);
		}

		public override bool Equals( object obj ) => Equals(obj as DependencyNode);

		public override int GetHashCode()
		{
			unchecked
			{
				if (!string.IsNullOrEmpty(Guid))
					return (Guid.GetHashCode() * 397) ^ LocalId.GetHashCode();
				return Goid.GetHashCode();
			}
		}

		public override string ToString()
		{
			if (!string.IsNullOrEmpty(Path)) return Path;
			if (!string.IsNullOrEmpty(Name)) return Name;
			return "<DependencyNode>";
		}
	}
}
