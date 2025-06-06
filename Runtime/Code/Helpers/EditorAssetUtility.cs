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
	/// This is a collection of asset helper functions.
	/// 
	/// Note: This file must reside outside of an "Editor" folder, since it must be accessible
	/// from mixed game/editor classes (even though all accesses are in #if UNITY_EDITOR clauses)
	/// See https://answers.unity.com/questions/426184/acces-script-in-the-editor-folder.html for reasons.
	public static class EditorAssetUtility
	{
		private const string CachePrefix = "UIECache_";
		private static readonly Dictionary<string, Component> s_cachedComponents = new();
		private static readonly string[] DefaultFolders = new []{"Assets"};
		private static readonly AssetSearchOptions DefaultSearchOptions = new();
		
		public class AssetSearchOptions
		{
			[Flags]
			public enum EErrorCondition
			{
				NoError = 0,
				ErrorIfNotFound = 1,
				ErrorIfMultipleFound = 2,
			}
			
			public enum EErrorType
			{
				LogWarning,
				LogError,
				Throw,
				Dialog,
			}
			
			public string SearchString = string.Empty;
			public bool ShowProgressBar = true;
			public bool IncludeInactive = true;
			public EErrorCondition ErrorCondition = EErrorCondition.NoError;
			public EErrorType ErrorType = EErrorType.LogError;
			public string[] Folders = new []{"Assets"};
		}

		public delegate void ComponentAssetFoundDelegate<T>(T _component);
		public delegate bool ComponentAssetFoundDelegateWithAsset<T>(T _component, GameObject _asset, string _assetPath);
		public delegate bool ComponentAssetFoundDelegateWithSceneAsset<T>(T _component, Scene _scene, string _scenePath);
		public delegate void ScriptableObjectFoundDelegate<T>(T _scriptableObject);
		public delegate void ScriptFoundDelegate(string _path, string _content);

		/// <summary>
		/// Finds all components in all prefabs.
		/// Warning: Extremely slow if no search string is provided (or if a search string is provided, but very many assets are found)
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="_foundFn"></param>
		/// <param name="_options"></param>
		public static void FindAllComponentsInAllPrefabs<T>(ComponentAssetFoundDelegateWithAsset<T> _foundFn, AssetSearchOptions _options = null)
		{
			if (_options == null)
				_options = DefaultSearchOptions;
			
			var searchString = _options.SearchString;
			var showProgressBar = _options.ShowProgressBar;
			var includeInactive = _options.IncludeInactive;
			int numFound = 0;
			
			try
			{
				if (searchString == null)
					searchString = string.Empty;
	
				string[] allAssetPathGuids = AssetDatabase.FindAssets($"t:GameObject {searchString}", _options.Folders);
	
				for (int i=0; i<allAssetPathGuids.Length; i++)
				{
					string guid = allAssetPathGuids[i];
					string assetPath = AssetDatabase.GUIDToAssetPath(guid);
					
					if (showProgressBar)
					{
						float done = (float) (i+1) / allAssetPathGuids.Length;
						EditorUtility.DisplayProgressBar($"Searching prefabs for components of type {typeof(T).Name}", $"Searching prefab '{Path.GetFileName(assetPath)}' ", done);
					}
					
					GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
					T[] components = go.GetComponentsInChildren<T>(includeInactive);
					if (components == null || components.Length == 0)
						continue;
	
					foreach (T component in components)
					{
						if (!_foundFn(component, go, assetPath))
							break;
						
						numFound++;
					}
				}
				
				ShowErrorIfNecessary<T>(numFound, _options);
			}
			finally
			{
				if (showProgressBar)
					EditorUtility.ClearProgressBar();
			}
		}
		
		/// <summary>
		/// Finds all components in all prefabs.
		/// Warning: Extremely slow if no search string is provided (or if a search string is provided, but very many assets are found)
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="_foundFn"></param>
		public static void FindAllComponentsInAllPrefabs<T>(ComponentAssetFoundDelegate<T> _foundFn, AssetSearchOptions _options = null)
		{
			FindAllComponentsInAllPrefabs<T>((_component, _, __) =>
			{
				_foundFn(_component);
				return true;
			}, _options);
		}

		/// <summary>
		/// Finds all components in all prefabs.
		/// Warning: Extremely slow if no search string is provided (or if a search string is provided, but very many assets are found)
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="_result"></param>
		/// <param name="_searchString"></param>
		public static void FindAllComponentsInAllPrefabs<T>(List<T> _result, AssetSearchOptions _options = null)
		{
			_result.Clear();

			FindAllComponentsInAllPrefabs<T>(_component =>
			{
				_result.Add(_component);
			}, _options);
		}

		/// <summary>
		/// Finds all components in all prefabs.
		/// Warning: Extremely slow if no search string is provided (or if a search string is provided, but very many assets are found)
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="_searchString"></param>
		/// <returns></returns>
		public static List<T> FindAllComponentsInAllPrefabs<T>(string _searchString = null)
		{
			List<T> result = new();
			
			var assetSearchOptions = new AssetSearchOptions() {SearchString = _searchString};
			
			FindAllComponentsInAllPrefabs<T>((_component) =>
			{
				result.Add(_component);
			}, assetSearchOptions);

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
		public static T FindComponentInAllPrefabs<T>(AssetSearchOptions _options)
		{
			List<T> found = FindAllComponentsInAllPrefabs<T>(_options.SearchString);
			bool error = ShowErrorIfNecessary<T>(found.Count, _options);
			if (error || found.Count == 0)
				return default;
			
			return found[0];
		}

		/// <summary>
		/// This function tries to find a single component in all prefabs.
		/// It issues an error message if none or more than one components were found.
		/// As this is a super long operation, it then adds the found _component to a 2-way cache:
		/// 1st cache: local non-persistent dictionary, super fast
		/// 2nd cache: cached persistent asset path in editor prefs, fast
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
			var options = new AssetSearchOptions() {SearchString = _searchString, ErrorCondition = AssetSearchOptions.EErrorCondition.ErrorIfNotFound | AssetSearchOptions.EErrorCondition.ErrorIfMultipleFound};
			result = FindComponentInAllPrefabs<T>(options);
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
		/// <param name="_options"></param>
		public static void FindAllScriptableObjects<T>(ScriptableObjectFoundDelegate<T> _foundFn, AssetSearchOptions _options = null)
		{
			if (_options == null)
				_options = DefaultSearchOptions;

			string[] allAssetPathGuids = AssetDatabase.FindAssets($"t:ScriptableObject {_options.SearchString}", _options.Folders);
			int numFound = 0;
			foreach (string guid in allAssetPathGuids)
			{
				string _assetPath = AssetDatabase.GUIDToAssetPath(guid);
				ScriptableObject scriptableObject = AssetDatabase.LoadAssetAtPath<ScriptableObject>(_assetPath);
				if (scriptableObject == null || !(scriptableObject is T))
					continue;

				numFound++;
				_foundFn( (T)(object) scriptableObject);
			}
			
			ShowErrorIfNecessary<T>(numFound, _options);
		}

		/// <summary>
		/// Find all scriptable objects of a given type
		/// Warning: Slow if no search string is provided (or if a search string is provided, but very many assets are found)
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="_result"></param>
		/// <param name="_options"></param>
		public static void FindAllScriptableObjects<T>(List<T> _result, AssetSearchOptions _options = null) where T : ScriptableObject
		{
			_result.Clear();
			FindAllScriptableObjects<T>((so) =>
			{
				_result.Add(so);
			}, _options);
		}

		/// <summary>
		/// Find all scriptable objects of a given type
		/// Warning: Slow if no search string is provided (or if a search string is provided, but very many assets are found)
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="_options"></param>
		/// <returns></returns>
		public static List<T> FindAllScriptableObjects<T>(AssetSearchOptions _options = null) where T : ScriptableObject
		{
			List<T> result = new();
			FindAllScriptableObjects<T>((so) =>
			{
				result.Add(so);
			}, _options);

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
		public static T FindScriptableObject<T>(AssetSearchOptions _options = null) where T:ScriptableObject
		{
			var found = FindAllScriptableObjects<T>(_options);
			bool error = ShowErrorIfNecessary<T>(found.Count, _options);
			if (error || found.Count == 0)
				return default;

			return found[0];
		}

		/// <summary>
		/// Finds all components in all scenes.
		/// Note that scenes in Packages are not handled.
		/// https://discussions.unity.com/t/it-is-not-allowed-to-open-a-_scene-in-a-read-only-package-why/850077
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="_foundFn"></param>
		/// <param name="_options"></param>
		public static void FindAllComponentsInAllScenes<T>(ComponentAssetFoundDelegateWithSceneAsset<T> _foundFn, AssetSearchOptions _options = null)
		{
			if (_options == null)
				_options = DefaultSearchOptions;
			
			var searchString = _options.SearchString == null ? string.Empty : _options.SearchString;
			var showProgressBar = _options.ShowProgressBar;
			var includeInactive = _options.IncludeInactive;
			
			int objectsFound = 0;

			try
			{
				string[] allAssetPathGuids = AssetDatabase.FindAssets($"t:Scene {searchString}", _options.Folders);
				if (allAssetPathGuids.Length == 0)
					return;
				
				for (int i=0; i<allAssetPathGuids.Length; i++)
				{
					string guid = allAssetPathGuids[i];
					string assetPath = AssetDatabase.GUIDToAssetPath(guid);
					if (showProgressBar)
					{
						float done = (float) (i+1) / allAssetPathGuids.Length;
						EditorUtility.DisplayProgressBar($"Searching scenes for components of type {typeof(T).Name}", $"Searching _scene '{Path.GetFileName(assetPath)}' ", done);
					}
					
					// Avoid dreaded "It is not allowed..." 
					// Skip all packages scenes altogether
					if (assetPath.StartsWith("Packages"))
						continue;
					
					Scene scene;
					bool wasLoaded;
					
					try
					{
						scene = EditorSceneManager.GetSceneByPath(assetPath);
						wasLoaded = scene.isLoaded;
						if (!wasLoaded)
							scene = EditorSceneManager.OpenScene(assetPath, OpenSceneMode.Additive);
					}
					catch
					{
						continue;
					}
	
					GameObject[] roots = scene.GetRootGameObjects();
					foreach(GameObject root in roots)
					{
						T[] components = root.GetComponentsInChildren<T>(includeInactive);
						if (components == null || components.Length == 0)
							continue;
	
						foreach (T _component in components)
						{
							objectsFound++;
							if (!_foundFn(_component, scene, assetPath))
								goto exitLoop;
						}
					}
					
					exitLoop:
	
					if (!wasLoaded)
						EditorSceneManager.CloseScene(scene, true);
				}
				
				ShowErrorIfNecessary<T>(objectsFound, _options);
			}
			finally
			{
				if (showProgressBar)
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
		public static void FindAllComponentsInAllScenes<T>(ComponentAssetFoundDelegate<T> _foundFn, AssetSearchOptions _options = null)
		{
			FindAllComponentsInAllScenes<T>((_component, _, __) =>
			{
				_foundFn(_component);
				return true;
			}, _options);
		}

		/// <summary>
		/// Finds all scripts
		/// </summary>
		/// <param name="_foundFn"></param>
		/// <param name="_options"></param>
		public static void FindAllScripts(ScriptFoundDelegate _foundFn, AssetSearchOptions _options = null)
		{
			if (_options == null)
				_options = DefaultSearchOptions;
			
			string[] allAssetPathGuids = AssetDatabase.FindAssets("t:Script", _options.Folders);
			int objectsFound = 0;

			foreach (string guid in allAssetPathGuids)
			{
				string _assetPath = AssetDatabase.GUIDToAssetPath(guid);

				TextAsset textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(_assetPath);
				if (textAsset == null)
					continue;

				_foundFn( _assetPath, textAsset.text );
				objectsFound++;
			}
			
			ShowErrorIfNecessary<TextAsset>(objectsFound, _options);
		}

		/// <summary>
		/// Gets the count of all scripts
		/// </summary>
		/// <param name="_excludePackages"></param>
		/// <returns></returns>
		public static int FindAllScriptsCount(AssetSearchOptions _options = null)
		{
			if (_options == null)
				_options = DefaultSearchOptions;
			
			string[] allAssetPathGuids = AssetDatabase.FindAssets("t:Script", _options.Folders);

			int result = 0;
			foreach (string guid in allAssetPathGuids)
			{
				string _assetPath = AssetDatabase.GUIDToAssetPath(guid);

				result++;
			}
			return result;
		}

		/// <summary>
		/// Find a matching object in a game object hierarchy by inspecting the paths of the two.
		/// This method attempts to find a game object or component in an unrelated game object hierarchy.
		/// This is very useful e.g. for cloning prefab variants or fixing references after cloning.
		/// </summary>
		/// <typeparam name="T">GameObject or Component subclass</typeparam>
		/// <param name="_gameObjectHierarchy"></param>
		/// <param name="_objectToFind"></param>
		/// <returns>found object or null if not found or error</returns>
		public static T FindMatchingObject<T>(GameObject _gameObjectHierarchy, T _objectToFind) where T : Object
		{
			if (_objectToFind is GameObject gameObjectToFind)
				return FindMatchingGameObject(_gameObjectHierarchy, gameObjectToFind) as T;

			if (_objectToFind is Component componentToFind)
				return FindMatchingComponent(_gameObjectHierarchy, componentToFind) as T;

			return null;
		}

		/// <summary>
		/// Find a matching component in a game object hierarchy by inspecting the paths of the two.
		/// This method attempts to find a component in an unrelated game object hierarchy.
		/// Multiple components of the same type on a single game object are supported.
		/// </summary>
		/// <typeparam name="T">Component subclass</typeparam>
		/// <param name="_gameObjectHierarchy"></param>
		/// <param name="_componentToFind"></param>
		/// <returns>found component or null if not found or error</returns>
		public static T FindMatchingComponent<T>(GameObject _gameObjectHierarchy, T _componentToFind) where T : Component
		{
			GameObject matchingGo = FindMatchingGameObject(_gameObjectHierarchy, _componentToFind.gameObject);
			if (matchingGo == null)
				return null;
	
			int componentIndex = FindIndexOfMultipleSameTypeComponents(_componentToFind);

			int currentComponentIndex = 0;
			var components = matchingGo.GetComponents<Component>();
			foreach (var matchingComponent in components)
			{
				if (matchingComponent.GetType() == _componentToFind.GetType())
				{
					if (currentComponentIndex < componentIndex)
					{
						currentComponentIndex++;
						continue;
					}

					return matchingComponent as T;
				}
			}
	
			return null;
		}

		/// <summary>
		/// Find the index for multiple components of the same type.
		/// Multiple components of the same type are quite rare, but clearly allowed and should thus be supported.
		/// Warning: NOT to be confused with Component.GetComponentIndex() (which relates to ALL components, not only those of the same kind)
		/// </summary>
		/// <param name="_component"></param>
		/// <returns>Index or -1 if no component of the given type is found</returns>
		public static int FindIndexOfMultipleSameTypeComponents(Component _component)
		{
			int result = 0;
			var allSourceComponents = _component.gameObject.GetComponents<Component>();
			foreach (var sourceComponent in allSourceComponents)
			{
				if (sourceComponent.GetType() != _component.GetType())
					continue;

				if (sourceComponent != _component)
				{
					result++;
					continue;
				}

				return result;
			}

			return -1;
		}
			
		/// <summary>
		/// Find a matching game object in a game object hierarchy by inspecting the paths of the two.
		/// This method attempts to find a game object in an unrelated game object hierarchy.
		/// </summary>
		/// <param name="_gameObjectHierarchy"></param>
		/// <param name="_gameObjectToFind"></param>
		/// <returns>found game object or null if not found or error</returns>
		public static GameObject FindMatchingGameObject(GameObject _gameObjectHierarchy, GameObject _gameObjectToFind)
		{
			if (_gameObjectHierarchy.transform.parent == _gameObjectToFind.transform.parent)
				return _gameObjectHierarchy;
	
			var toFindPath = _gameObjectToFind.GetPath(-1);
			var transforms = _gameObjectHierarchy.GetComponentsInChildren<Transform>(true);
			foreach (var transform in transforms)
			{
				if (transform.GetPath(-1).EndsWith(toFindPath))
					return transform.gameObject;
			}
	
			return null;
		}

		public static void FixReferencesInClone(GameObject _original, GameObject _clone)
		{
string s = $"---::: Fix {_clone.name}\n";
			FixReferencesInCloneInternal(_original, _clone, 1, ref s);
Debug.Log(s);
		}

		private static void FixReferencesInCloneInternal(GameObject _original, GameObject _clone, int _numTabs, ref string _s)
		{
_s += $"{new string('\t', _numTabs)}Fix Game Object '{_clone.name}'\n";
string s = string.Empty;
			var components = _clone.GetComponents<Component>();
			foreach (var component in components)
			{
_s += $"{new string('\t', _numTabs)}Fix Component '{component.GetType().Name}'\n";
				var cloneComponentSerObj = new SerializedObject(component);
				
				EditorGeneralUtility.ForeachProperty(cloneComponentSerObj, property =>
				{
					if (property.propertyType != SerializedPropertyType.ObjectReference)
						return;
	
					var matchingObject = FindMatchingObject(_clone, property.objectReferenceValue);
					if (matchingObject != null)
					{
s += $"{new string('\t', _numTabs)}Fixed Property '{property.propertyPath}'\n";
						property.objectReferenceValue = matchingObject;
					}
				});
_s += s + "\n";

				cloneComponentSerObj.ApplyModifiedProperties();
			}


			foreach (Transform child in _clone.transform)
			{
				FixReferencesInCloneInternal(_original, child.gameObject, _numTabs+1, ref _s);
			}
		}

		/// <summary>
		/// Create an asset and ensure folder exists
		/// </summary>
		/// <param name="_obj"></param>
		/// <param name="_path"></param>
		public static void CreateAsset( Object _obj, string _path )
		{
			string directory = EditorFileUtility.GetDirectoryName(_path);
			EditorFileUtility.EnsureUnityFolderExists(directory);
			AssetDatabase.CreateAsset(_obj, _path);
		}

		/// <summary>
		/// Check if an asset is currently being imported for the first time.
		/// Pretty hacky, but couldn't find a more sane way.
		/// </summary>
		/// <param name="_obj"></param>
		/// <param name="_path"></param>
		/// <returns></returns>
		public static bool IsBeingImportedFirstTime( string _path )
		{
			// Asset database can't (yet) load the object
			if (AssetDatabase.LoadAssetAtPath(_path, typeof(Object)))
				return false;
			
			// but the file already exists -> importing
			return File.Exists(_path);
		}
		
		public static bool IsPackagesOrInternalAsset(Object _obj)
		{
			if (IsInternalAsset(_obj))
				return true;
			
			return IsPackagesAsset(_obj);
		}

		public static bool IsInternalAsset(Object _obj)
		{
			if (!_obj)
				return false;
			
			var path = AssetDatabase.GetAssetPath(_obj);
			if (string.IsNullOrEmpty(path))
				return false;
			
			var fullPath = Path.GetFullPath(path).Replace('\\', '/').ToLower();
			return fullPath.Contains(".dev-app/unity/assets/external/unity-gui-toolkit");
		}

		public static bool IsPackagesAsset(Object _obj)
		{
			if (!_obj)
				return false;
			
			var path = AssetDatabase.GetAssetPath(_obj);
			if (string.IsNullOrEmpty(path))
				return false;
			
			return path.StartsWith("Packages");
		}

		private static string GetCacheKey<T>(string _searchString) => CachePrefix + typeof(T).FullName + (string.IsNullOrEmpty(_searchString) ? "" : _searchString);
		
		private static bool ShowErrorIfNecessary<T>(int numFound, AssetSearchOptions _options)
		{
			if (_options.ErrorCondition == AssetSearchOptions.EErrorCondition.NoError)
				return false;
			
			string nameLogStr = string.IsNullOrEmpty(_options.SearchString) ? "" : $", search string: '{_options.SearchString}'";
			
			if ((_options.ErrorCondition & AssetSearchOptions.EErrorCondition.ErrorIfNotFound) != 0 && numFound == 0)
			{
				string msg = $"Didn't find any objects of type '{typeof(T).Name}' with search string '{nameLogStr}'";
				ShowError<T>(msg, _options);
				return true;
			}
			else if ((_options.ErrorCondition & AssetSearchOptions.EErrorCondition.ErrorIfMultipleFound) != 0 && numFound > 1)
			{
				string msg = $"Found multiple game objects ({numFound}) with _component type '{typeof(T).Name}'{nameLogStr}, but there may be only one";
				ShowError<T>(msg, _options);
				return true;
			}
			
			return false;
		}

		private static void ShowError<T>(string _msg, AssetSearchOptions _options)
		{
			switch (_options.ErrorType)
			{
				case AssetSearchOptions.EErrorType.LogWarning:
					Debug.LogWarning(_msg);
					break;
				case AssetSearchOptions.EErrorType.LogError:
					Debug.LogError(_msg);
					break;
				case AssetSearchOptions.EErrorType.Throw:
					throw new Exception(_msg);
				case AssetSearchOptions.EErrorType.Dialog:
					EditorUtility.DisplayDialog("Asset Error", _msg, "Ok");
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}
}
#endif

