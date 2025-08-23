using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace GuiToolkit.Editor
{
	// Script conversions, collecting and loading (no reference handling)
	public static partial class EditorCodeUtility
	{
		public static HashSet<MonoBehaviour> CollectComponentsInCtxSceneReferencing<T>() where T : MonoBehaviour
		{
			HashSet<MonoBehaviour> result = new ();
			var roots = GetCurrentContextSceneRoots();
			
			foreach (var root in roots)
			{
				var components = root.GetComponentsInChildren<MonoBehaviour>();
				result.UnionWith(FilterReferencingType<T>(components));
			}
			
			return result;
		}
		
		public static List<MonoScript> GetScriptsFromComponents(IEnumerable<MonoBehaviour> _monoBehaviours)
		{
			List<MonoScript> result = new();
			foreach (var monoBehaviour in _monoBehaviours)
			{
				var ms = MonoScript.FromMonoBehaviour(monoBehaviour);
                if (ms == null)
                    continue;
                
                result.Add(ms);
			}
			
			return result;
		}
		
		public static List<string> GetScriptPathsFromScripts(IEnumerable<MonoScript> _scripts)
		{
			List<string> result = new();
			
			if (_scripts == null)
				return result;

			foreach (var monoScript in _scripts)
			{
				var path = AssetDatabase.GetAssetPath(monoScript);
                    if (string.IsNullOrEmpty(path) || !path.StartsWith("Assets/"))
                        continue;
                    
                result.Add(path);
			}
			
			return result;
		}
	}
}
