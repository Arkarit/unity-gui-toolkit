using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace GuiToolkit.Editor
{
	// Script conversions, collecting and loading (no reference handling)
	public static partial class EditorCodeUtility
	{
		public static List<MonoBehaviour> CollectMonoBehavioursInContextSceneReferencing<T>() where T : MonoBehaviour
		{
			HashSet<MonoBehaviour> hashSet = new();
			var roots = GetCurrentContextSceneRoots();

			foreach (var root in roots)
			{
				var components = root.GetComponentsInChildren<MonoBehaviour>();
				hashSet.UnionWith(FilterReferencingType<T>(components));
			}

			return hashSet.ToList();
		}

		public static List<MonoScript> GetScriptsFromMonoBehaviours( IEnumerable<MonoBehaviour> _monoBehaviours )
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

		public static List<string> GetScriptPathsFromScripts( IEnumerable<MonoScript> _scripts )
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
		
		public static List<string> CollectScriptPathsInContextSceneReferencing<T>() where T : MonoBehaviour
		{
			var monoBehaviours = CollectMonoBehavioursInContextSceneReferencing<T>();
			if (monoBehaviours == null || monoBehaviours.Count == 0)
				return new List<string>();
			
			var scripts = GetScriptsFromMonoBehaviours(monoBehaviours);
			if (scripts == null || scripts.Count == 0)
				return new List<string>();
			
			return GetScriptPathsFromScripts(scripts);
		}

		/// <summary>
		/// Returns all components from 'sources' that contain a serialized reference to any T
		/// (optionally also counts a GameObject reference that has a T on it).
		/// </summary>
		public static IEnumerable<MonoBehaviour> FilterReferencingType<T>
		(
			IEnumerable<MonoBehaviour> _sources,
			bool _treatGameObjectWithTAsRef = true,
			bool _onlySceneObjects = true
		)
			where T : MonoBehaviour
		{
			foreach (var c in _sources)
			{
				if (c == null) continue;
				if (DoesComponentReferenceType<T>(c, _treatGameObjectWithTAsRef, _onlySceneObjects))
					yield return c;
			}
		}

		/// <summary>
		/// True if 'source' has any serialized reference to T (or to a GO that has T).
		/// </summary>
		public static bool DoesComponentReferenceType<T>
		(
			Component _source,
			bool _treatGameObjectWithTAsRef = true,
			bool _onlySceneObjects = true
		)
			where T : Component
		{
			var so = new SerializedObject(_source);
			var it = so.GetIterator();
			bool enterChildren = true;

			while (it.NextVisible(enterChildren))
			{
				enterChildren = false;

				if (it.propertyType != SerializedPropertyType.ObjectReference)
					continue;

				var obj = it.objectReferenceValue;
				if (obj == null)
					continue;

				// Ignore project assets if we only care about scene/prefab-stage objects
				if (_onlySceneObjects && EditorUtility.IsPersistent(obj))
					continue;

				// Direct Component reference of type T
				if (obj is T)
					return true;

				// A GameObject reference that carries a T
				if (_treatGameObjectWithTAsRef && obj is GameObject go && go.TryGetComponent<T>(out _))
					return true;
			}

			return false;
		}

		/// <summary>
		/// Variant: does 'source' reference this specific target object?
		/// </summary>
		public static bool DoesComponentReferenceObject( Component _source, UnityEngine.Object _target, bool _onlySceneObjects = true )
		{
			if (_source == null || _target == null) return false;

			var so = new SerializedObject(_source);
			var it = so.GetIterator();
			bool enterChildren = true;

			while (it.NextVisible(enterChildren))
			{
				enterChildren = false;

				if (it.propertyType != SerializedPropertyType.ObjectReference)
					continue;

				var obj = it.objectReferenceValue;
				if (obj == null)
					continue;

				if (_onlySceneObjects && EditorUtility.IsPersistent(obj))
					continue;

				if (ReferenceEquals(obj, _target))
					return true;
			}

			return false;
		}

	}
}
