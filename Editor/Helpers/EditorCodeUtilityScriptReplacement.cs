using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace GuiToolkit.Editor
{
	/// <summary>
	/// Editor utilities for collecting MonoBehaviours and their scripts,
	/// resolving asset paths, and checking serialized references to specific types.
	/// </summary>
	public static partial class EditorCodeUtility
	{
		/// <summary>
		/// Collects all MonoBehaviours in the current context scene that contain
		/// serialized references to type T (or GameObjects containing T).
		/// </summary>
		/// <typeparam name="T">Target component type to search for.</typeparam>
		/// <returns>List of MonoBehaviours that reference T.</returns>
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

		/// <summary>
		/// Returns the MonoScript assets corresponding to the given MonoBehaviours.
		/// </summary>
		/// <param name="_monoBehaviours">Collection of MonoBehaviours.</param>
		/// <returns>List of MonoScript assets (non-null).</returns>
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

		/// <summary>
		/// Returns the asset paths of given MonoScript assets.
		/// Only includes valid project assets starting with "Assets/".
		/// </summary>
		/// <param name="_scripts">Enumerable of MonoScript assets.</param>
		/// <returns>List of asset paths under the Assets/ folder.</returns>
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

		/// <summary>
		/// Collects script asset paths from MonoBehaviours in the current context scene
		/// that reference type T.
		/// </summary>
		/// <typeparam name="T">Target type to search for (must be a MonoBehaviour).</typeparam>
		/// <returns>List of C# script asset paths.</returns>
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
		/// Filters a collection of MonoBehaviours and yields only those that have
		/// serialized references to T. Can optionally treat a reference to a GameObject
		/// containing T as valid.
		/// </summary>
		/// <typeparam name="T">Component type to look for.</typeparam>
		/// <param name="_sources">Enumerable of MonoBehaviours to scan.</param>
		/// <param name="_treatGameObjectWithTAsRef">If true, a GameObject reference with T counts as a reference.</param>
		/// <param name="_onlySceneObjects">If true, ignores project asset references.</param>
		/// <returns>Enumerable of MonoBehaviours referencing T.</returns>
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
		/// Checks whether the given component has any serialized field that references
		/// type T, or a GameObject carrying T.
		/// </summary>
		/// <typeparam name="T">Component type to search for.</typeparam>
		/// <param name="_source">Component to inspect.</param>
		/// <param name="_treatGameObjectWithTAsRef">If true, GameObject with T counts as a reference.</param>
		/// <param name="_onlySceneObjects">If true, ignores project assets and only considers scene objects.</param>
		/// <returns>True if a reference to T (or GameObject with T) is found.</returns>
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
	}
}
