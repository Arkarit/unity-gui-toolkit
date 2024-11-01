// Note: don't move this file to an Editor folder, since it needs to be available
// for inplace editor code
#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor.SceneManagement;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace GuiToolkit
{
	/// \brief General Asset Utility
	/// 
	/// This is a collection of _asset helper functions.
	/// 
	/// Note: This file must reside outside of an "Editor" folder, since it must be accessible
	/// from mixed game/editor classes (even though all accesses are in #if UNITY_EDITOR clauses)
	/// See https://answers.unity.com/questions/426184/acces-script-in-the-editor-folder.html for reasons.
	public static class EditorAssetUtility
	{
		private const string CachePrefix = "UIECache_";
		private static readonly Dictionary<string, Component> s_cachedComponents = new();

		public delegate void ComponentAssetFoundDelegate<T>(T _component)  where T:Component;
		public delegate bool ComponentAssetFoundDelegateWithAsset<T>(T _component, GameObject _asset, string _assetPath)  where T:Component;
		public delegate bool ComponentAssetFoundDelegateWithSceneAsset<T>(T _component, Scene _scene, string _scenePath)  where T:Component;
		public delegate void ScriptableObjectFoundDelegate<T>(T _scriptableObject)  where T:ScriptableObject;
		public delegate void ScriptFoundDelegate(string _path, string _content);

		/// <summary>
		/// Finds all components in all prefabs.
		/// Warning: Extremely slow if no search string is provided (or if a search string is provided, but very many assets are found)
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="_foundFn"></param>
		/// <param name="_includeInactive"></param>
		/// <param name="_searchString"></param>
		public static void FindAllComponentsInAllPrefabs<T>(ComponentAssetFoundDelegateWithAsset<T> _foundFn, bool _includeInactive = true, string _searchString = null, bool _showProgressBar = true) where T:Component
		{
			try
			{
				if (_searchString == null)
					_searchString = string.Empty;
	
				string[] allAssetPathGuids = AssetDatabase.FindAssets($"t:GameObject {_searchString}");
	
				for (int i=0; i<allAssetPathGuids.Length; i++)
				{
					string guid = allAssetPathGuids[i];
					string _assetPath = AssetDatabase.GUIDToAssetPath(guid);
					
					if (_showProgressBar)
					{
						float done = (float) (i+1) / allAssetPathGuids.Length;
						EditorUtility.DisplayProgressBar($"Searching prefabs for components of type {typeof(T).Name}", $"Searching prefab '{Path.GetFileName(_assetPath)}' ", done);
					}
					
					GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(_assetPath);
					T[] components = go.GetComponentsInChildren<T>(_includeInactive);
					if (components == null || components.Length == 0)
						continue;
	
					foreach (T _component in components)
					{
						if (!_foundFn(_component, go, _assetPath))
							break;
					}
				}
			}
			finally
			{
				if (_showProgressBar)
					EditorUtility.ClearProgressBar();
			}
		}

		/// <summary>
		/// Finds all components in all prefabs.
		/// Warning: Extremely slow if no search string is provided (or if a search string is provided, but very many assets are found)
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="_foundFn"></param>
		/// <param name="_includeInactive"></param>
		/// <param name="_searchString"></param>
		public static void FindAllComponentsInAllPrefabs<T>(ComponentAssetFoundDelegate<T> _foundFn, bool _includeInactive = true, string _searchString = null, bool _showProgressBar = true) where T:Component
		{
			FindAllComponentsInAllPrefabs<T>((_component, _, __) =>
			{
				_foundFn(_component);
				return true;
			}, _includeInactive, _searchString, _showProgressBar);
		}

		/// <summary>
		/// Finds all components in all prefabs.
		/// Warning: Extremely slow if no search string is provided (or if a search string is provided, but very many assets are found)
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="_result"></param>
		/// <param name="_searchString"></param>
		public static void FindAllComponentsInAllPrefabs<T>(List<T> _result, bool _includeInactive = true, string _searchString = null, bool _showProgressBar = true) where T:Component
		{
			_result.Clear();

			FindAllComponentsInAllPrefabs<T>(_component =>
			{
				_result.Add(_component);
			}, _includeInactive, _searchString, _showProgressBar);
		}

		/// <summary>
		/// Finds all components in all prefabs.
		/// Warning: Extremely slow if no search string is provided (or if a search string is provided, but very many assets are found)
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="_searchString"></param>
		/// <returns></returns>
		public static List<T> FindAllComponentsInAllPrefabs<T>(string _searchString = null) where T:Component
		{
			List<T> result = new();

			FindAllComponentsInAllPrefabs<T>((_component) =>
			{
				result.Add(_component);
			}, true, _searchString);

			return result;
		}

		/// <summary>
		/// Finds a single _component in all prefabs.
		/// Warning: Extremely slow if no search string is provided (or if a search string is provided, but very many assets are found)
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="_searchString"></param>
		/// <param name="_errorIfZero">output error message if no _asset was found</param>
		/// <param name="_errorIfMoreThanOne">output error message if more than one _asset was found</param>
		/// <returns>Asset if found or null</returns>
		public static T FindComponentInAllPrefabs<T>(string _searchString = null, bool _errorIfZero = false, bool _errorIfMoreThanOne = false) where T:Component
		{
			string nameLogStr = string.IsNullOrEmpty(_searchString) ? "" : $", search string: '{_searchString}'";
			
			List<T> found = FindAllComponentsInAllPrefabs<T>(_searchString);
			int numFound = found.Count;
			if (numFound == 0)
			{
				if (_errorIfZero)
					Debug.LogError($"Didn't find any game objects with _component type '{typeof(T).Name}'{nameLogStr}");

				return null;
			}

			if (_errorIfMoreThanOne && numFound > 1)
				Debug.LogError($"Found multiple game objects ({numFound}) with _component type '{typeof(T).Name}'{nameLogStr}, but there may be only one");

			return found[0];
		}

		/// <summary>
		/// This function tries to find a single _component in all prefabs.
		/// It issues an error message if none or more than one components were found.
		/// As this is a super long operation, it then adds the found _component to a 2-way cache:
		/// 1st cache: local non-persistent dictionary, super fast
		/// 2nd cache: cached persistent _asset path in editor prefs, fast
		/// not cached: fast if search string is provided and not too many assets are found, extremely slow if no search string is provided.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="_searchString"></param>
		/// <returns>Asset if found or null</returns>
		public static T FindSingleComponentInAllPrefabsAndCache<T>(string _searchString = null) where T : Component
		{
			string cacheKey = GetCacheKey<T>(_searchString);
			T result = null;

			// Attempt 1: Find _asset in local dictionary
			if (s_cachedComponents.TryGetValue(cacheKey, out Component found))
			{
				result = found as T;
				if (!result)
				{
					s_cachedComponents.Remove(cacheKey);
				}
				else
				{
					return result;
				}
			}

			// Attempt 2: Find _asset path in editor prefs
			string _assetPath = EditorPrefs.GetString(cacheKey);
			if (!string.IsNullOrEmpty(_assetPath))
			{
				GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(_assetPath);
				if (!go)
				{
					EditorPrefs.DeleteKey(cacheKey);
				}
				else
				{
					result = go.GetComponentInChildren<T>();
					if (!result)
					{
						EditorPrefs.DeleteKey(cacheKey);
					}
					else
					{
						s_cachedComponents.Add(cacheKey, result);
						return result;
					}
				}
			}

			// Attempt 3: Search manually. Super slow.
			result = FindComponentInAllPrefabs<T>(_searchString, true, true);
			if (!result)
				return null;

			_assetPath = AssetDatabase.GetAssetPath(result.gameObject.GetRoot());
			if (string.IsNullOrEmpty(_assetPath))
			{
				Debug.LogError("Game Object _asset not found. This shouldn't happen under normal circumstances. Please contact achilles@funatics.de.");
				return null;
			}

			EditorPrefs.SetString(cacheKey, _assetPath);
			s_cachedComponents.Add(cacheKey, result);
			return result;
		}

		/// <summary>
		/// Clear a previously cached _component.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="_searchString">Needs to be exactly the same search string as used previously in FindSingleComponentInAllPrefabsAndCache()</param>
		public static void ClearCachedComponent<T>(string _searchString = null)
		{
			string cacheKey = GetCacheKey<T>(_searchString);
			EditorPrefs.DeleteKey(cacheKey);
			s_cachedComponents.Remove(cacheKey);
		}

		/// <summary>
		/// Find all scriptable objects of a given type
		/// Warning: Slow if no search string is provided (or if a search string is provided, but very many assets are found)
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="_foundFn"></param>
		/// <param name="_searchString"></param>
		public static void FindAllScriptableObjects<T>(ScriptableObjectFoundDelegate<T> _foundFn, string _searchString = null) where T:ScriptableObject
		{
			if (_searchString == null)
				_searchString = string.Empty;

			string[] allAssetPathGuids = AssetDatabase.FindAssets($"t:ScriptableObject {_searchString}");

			foreach (string guid in allAssetPathGuids)
			{
				string _assetPath = AssetDatabase.GUIDToAssetPath(guid);
				ScriptableObject scriptableObject = AssetDatabase.LoadAssetAtPath<ScriptableObject>(_assetPath);
				if (scriptableObject == null || !(scriptableObject is T))
					continue;

				_foundFn( (T)(object) scriptableObject);
			}
		}

		/// <summary>
		/// Find all scriptable objects of a given type
		/// Warning: Slow if no search string is provided (or if a search string is provided, but very many assets are found)
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="_result"></param>
		/// <param name="_searchString"></param>
		public static void FindAllScriptableObjects<T>(List<T> _result, string _searchString = null) where T : ScriptableObject
		{
			_result.Clear();
			FindAllScriptableObjects<T>((so) =>
			{
				_result.Add(so);
			}, _searchString);
		}

		/// <summary>
		/// Find all scriptable objects of a given type
		/// Warning: Slow if no search string is provided (or if a search string is provided, but very many assets are found)
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="_searchString"></param>
		/// <returns></returns>
		public static List<T> FindAllScriptableObjects<T>(string _searchString = null) where T : ScriptableObject
		{
			List<T> result = new();
			FindAllScriptableObjects<T>((so) =>
			{
				result.Add(so);
			}, _searchString);

			return result;
		}

		/// <summary>
		/// Finds a single scriptable object.
		/// Warning: Extremely slow if no search string is provided (or if a search string is provided, but very many assets are found)
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="_searchString"></param>
		/// <param name="_errorIfZero">output error message if no _asset was found</param>
		/// <param name="errorIfMoreThanOne">output error message if more than one _asset was found</param>
		/// <returns></returns>
		public static T FindScriptableObject<T>(string _searchString = null, bool _errorIfZero = false,
			bool errorIfMoreThanOne = false) where T:ScriptableObject
		{
			string nameLogStr = string.IsNullOrEmpty(_searchString) ? "" : $", search string: '{_searchString}'";
			var found = FindAllScriptableObjects<T>(_searchString);
			var numFound = found.Count;
			if (numFound == 0)
			{
				if (_errorIfZero)
					Debug.LogError($"Didn't find any scriptable objects of type '{typeof(T).Name}'{nameLogStr}");

				return null;
			}

			if (errorIfMoreThanOne && numFound > 1)
				Debug.LogError($"Found multiple scriptable objects ({numFound}) of type '{typeof(T).Name}'{nameLogStr}, but there may be only one");

			return found[0];
		}

		/// <summary>
		/// Finds all components in all scenes.
		/// Note that scenes in Packages are not handled.
		/// https://discussions.unity.com/t/it-is-not-allowed-to-open-a-_scene-in-a-read-only-package-why/850077
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="_foundFn"></param>
		/// <param name="_includeInactive"></param>
		/// <param name="_searchString"></param>
		public static void FindAllComponentsInAllScenes<T>(ComponentAssetFoundDelegateWithSceneAsset<T> _foundFn, bool _includeInactive = true, string _searchString = null, bool _showProgressBar = true) where T:Component
		{
			try
			{
				if (_searchString == null)
					_searchString = string.Empty;
	
				string[] allAssetPathGuids = AssetDatabase.FindAssets($"t:Scene {_searchString}");
				if (allAssetPathGuids.Length == 0)
					return;
				
				for (int i=0; i<allAssetPathGuids.Length; i++)
				{
					string guid = allAssetPathGuids[i];
					string _assetPath = AssetDatabase.GUIDToAssetPath(guid);
					if (_showProgressBar)
					{
						float done = (float) (i+1) / allAssetPathGuids.Length;
						EditorUtility.DisplayProgressBar($"Searching scenes for components of type {typeof(T).Name}", $"Searching _scene '{Path.GetFileName(_assetPath)}' ", done);
					}
					
					// Avoid dreaded "It is not allowed..." 
					// Skip all packages scenes altogether
					if (_assetPath.StartsWith("Packages"))
						continue;
					
					Scene scene;
					bool wasLoaded;
					
					try
					{
						scene = EditorSceneManager.GetSceneByPath(_assetPath);
						wasLoaded = scene.isLoaded;
						if (!wasLoaded)
							scene = EditorSceneManager.OpenScene(_assetPath, OpenSceneMode.Additive);
					}
					catch
					{
						continue;
					}
	
					GameObject[] roots = scene.GetRootGameObjects();
					foreach(GameObject root in roots)
					{
						T[] components = root.GetComponentsInChildren<T>(_includeInactive);
						if (components == null || components.Length == 0)
							continue;
	
						foreach (T _component in components)
						{
							if (!_foundFn(_component, scene, _assetPath))
								goto exitLoop;
						}
					}
					
					exitLoop:
	
					if (!wasLoaded)
						EditorSceneManager.CloseScene(scene, true);
				}
			}
			finally
			{
				if (_showProgressBar)
					EditorUtility.ClearProgressBar();
			}
		}

		/// <summary>
		/// Finds all components in all scenes
		/// Note that scenes in Packages are not handled.
		/// https://discussions.unity.com/t/it-is-not-allowed-to-open-a-_scene-in-a-read-only-package-why/850077
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="_foundFn"></param>
		/// <param name="_includeInactive"></param>
		/// <param name="_searchString"></param>
		public static void FindAllComponentsInAllScenes<T>(ComponentAssetFoundDelegate<T> _foundFn, bool _includeInactive = true, string _searchString = null, bool _showProgressBar = true) where T:Component
		{
			FindAllComponentsInAllScenes<T>((_component, _, __) =>
			{
				_foundFn(_component);
				return true;
			}, _includeInactive, _searchString, _showProgressBar);
		}

		/// <summary>
		/// Finds all scripts
		/// </summary>
		/// <param name="_foundFn"></param>
		/// <param name="_excludePackages"></param>
		public static void FindAllScripts(ScriptFoundDelegate _foundFn, bool _excludePackages = true)
		{
			string[] allAssetPathGuids = AssetDatabase.FindAssets("t:Script");

			foreach (string guid in allAssetPathGuids)
			{
				string _assetPath = AssetDatabase.GUIDToAssetPath(guid);

				if (_excludePackages && _assetPath.StartsWith("Packages/", StringComparison.OrdinalIgnoreCase))
					continue;

				TextAsset textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(_assetPath);
				if (textAsset == null)
					continue;

				_foundFn( _assetPath, textAsset.text );
			}
		}

		/// <summary>
		/// Gets the count of all scripts
		/// </summary>
		/// <param name="_excludePackages"></param>
		/// <returns></returns>
		public static int FindAllScriptsCount(bool _excludePackages = true)
		{
			string[] allAssetPathGuids = AssetDatabase.FindAssets("t:Script");

			int result = 0;
			foreach (string guid in allAssetPathGuids)
			{
				string _assetPath = AssetDatabase.GUIDToAssetPath(guid);

				if (_excludePackages && _assetPath.StartsWith("Packages/", StringComparison.OrdinalIgnoreCase))
					continue;

				result++;
			}
			return result;
		}

		/// <summary>
		/// Create an _asset and ensure folder exists
		/// </summary>
		/// <param name="_obj"></param>
		/// <param name="_path"></param>
		public static void CreateAsset( UnityEngine.Object _obj, string _path )
		{
			string directory = EditorFileUtility.GetDirectoryName(_path);
			EditorFileUtility.EnsureFolderExists(directory);
			AssetDatabase.CreateAsset(_obj, _path);
		}


		private static string GetCacheKey<T>(string _searchString) => CachePrefix + typeof(T).FullName + (string.IsNullOrEmpty(_searchString) ? "" : _searchString);
	}
}
#endif

