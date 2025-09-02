using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GuiToolkit.Editor
{
	[Serializable]
	public class DependencyNodeTree
	{
		[SerializeField] private DependencyNode m_root;
		[SerializeField] private readonly HashSet<DependencyNode> m_dependencies = new ();
		[SerializeField] private readonly HashSet<DependencyNode> m_dependendants = new ();
		
		public DependencyNode Root
		{
			get => m_root;
			set => m_root = value;
		}

		public DependencyNodeTree(Object _object)
		{
			m_root = new DependencyNode(_object, this, null);
		}
	}
}
