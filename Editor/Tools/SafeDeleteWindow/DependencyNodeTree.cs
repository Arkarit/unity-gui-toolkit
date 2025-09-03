using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GuiToolkit.Editor
{
	[Serializable]
	public class DependencyNodeTree
	{
		[SerializeField] private DependencyNode m_root;
		[SerializeField] private readonly HashSet<DependencyNode> m_allNodes = new();
		private readonly Dictionary<string, DependencyNode> m_byKey = new(StringComparer.Ordinal);

		public DependencyNode Root
		{
			get => m_root;
			private set => m_root = value;
		}

		public DependencyNodeTree( Object _object )
		{
			Root = BuildFromRoot(_object);
		}

		
		// stable key: Stage -> GOID; Asset -> GUID (Main-Asset-Policy)
		private static string MakeKey( Object _object )
		{
			if (!_object) return null;

			// Normalize: components -> GameObject
			if (_object is Component c) _object = c.gameObject;

			var path = AssetDatabase.GetAssetPath(_object);
			if (!string.IsNullOrEmpty(path))
			{
				// Normalize subassets -> main asset
				var main = AssetDatabase.LoadMainAssetAtPath(path) ?? _object;
				AssetDatabase.TryGetGUIDAndLocalFileIdentifier(main, out var guid, out _);
				return guid; // main asset key
			}

			var goid = GlobalObjectId.GetGlobalObjectIdSlow(_object);
			return goid.ToString(); // stage key
		}

		public DependencyNode GetOrCreateNode( Object _obj )
		{
			var key = MakeKey(_obj);
			if (string.IsNullOrEmpty(key)) return null;

			if (m_byKey.TryGetValue(key, out var existing))
				return existing;

			var node = new DependencyNode(_obj, this);
			m_byKey[key] = node;
			m_allNodes.Add(node);
			return node;
		}
		
		public DependencyNode BuildFromRoot( Object _root )
		{
			if (_root is Component component)
				_root = component.gameObject;
			
			var visited = new HashSet<string>(StringComparer.Ordinal);
			var stack = new Stack<DependencyNode>();
			var rootNode = GetOrCreateNode(_root);
			stack.Push(rootNode);

			while (stack.Count > 0)
			{
				var n = stack.Pop();
				var key = MakeKey(n.Object);
				if (!visited.Add(key)) continue;

				// fill direct deps now
				n.CollectDirectDependencies();

				foreach (var d in n.DirectDependencies)
					stack.Push(d);
			}
			
			return rootNode;
		}
	}
}
