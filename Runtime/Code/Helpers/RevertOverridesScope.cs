using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
#endif
namespace GuiToolkit
{
	public class RevertOverridesScope : IDisposable
	{
#if UNITY_EDITOR
		private Object m_obj;
#endif

		public RevertOverridesScope(Object obj)
		{
#if UNITY_EDITOR
			if (obj == null)
				return;

			m_obj = obj;
#endif
		}

		public void Dispose()
		{
#if UNITY_EDITOR
			if (m_obj == null) 
				return;

			var prefabInstanceRoot = PrefabUtility.GetNearestPrefabInstanceRoot(m_obj);
			if (prefabInstanceRoot == null)
				return;
			var mods = PrefabUtility.GetPropertyModifications(prefabInstanceRoot);
			List<PropertyModification> newMods = new ();
			foreach (var mod in mods)
			{
				if (PrefabUtility.IsDefaultOverride(mod) || mod.propertyPath == "running")
				{
					newMods.Add(mod);
					continue;
				}

				Debug.Log($"{mod.propertyPath}: {mod.value}");
			}

			PrefabUtility.SetPropertyModifications(prefabInstanceRoot, newMods.ToArray());

#endif
		}
	}
}