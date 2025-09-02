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
		
		public readonly HashSet<DependencyNode> DirectDependencies = new ();
		public readonly HashSet<DependencyNode> DirectDependants = new ();
		public DependencyNodeTree Tree;

		public DependencyNode() { } // Deprecated
		
		public DependencyNode(Object _object, DependencyNodeTree _tree, DependencyNode _parent)
		{
			Object = _object;
			Tree = _tree;
			Parent = _parent;
			
			SetProperties(_object);
			CollectDirectDependencies();
		}

		private void CollectDirectDependencies()
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
			//TODO
		}
		
		private void CollectDependenciesFromAsset()
		{
			//TODO
		}
		

		private void SetProperties(Object _object)
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
