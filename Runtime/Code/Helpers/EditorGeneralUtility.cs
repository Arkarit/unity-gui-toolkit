#if UNITY_EDITOR

// Caution! Does not work properly yet!
//#define SCENE_DEPENDENCY_WIP
using GuiToolkit.Debugging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;


namespace GuiToolkit
{
	public static class EditorGeneralUtility
	{
		private static readonly List<GameObject> SearchList = new();

#if SCENE_DEPENDENCY_WIP
		[Flags]
		public enum SceneDependencyFlags
		{
			ExcludeSelf			= 0x0001,
			OnlyGameObjects		= 0x0002,
			NoPrefabs			= 0x0004,
		}
		
		public const SceneDependencyFlags AllFlags = (SceneDependencyFlags) 0x7ffffff;
		public const SceneDependencyFlags NoFlags = 0;
		
		
		private static readonly Dictionary<Object, HashSet<Object>> DependenciesByObject = new();
		private static readonly Dictionary<GameObject, HashSet<Object>> DependenciesByGameObject = new();

		public static void InitSceneDependencyQuery(SceneDependencyFlags _flags = NoFlags)
		{
			DependenciesByObject.Clear();
			DependenciesByGameObject.Clear();
			
			var allGameObjects = Object.FindObjectsOfType<GameObject>();

			foreach (var go in allGameObjects)
			{
				Iterate(go, go, _flags);
				var components = go.GetComponents<Component>();
				foreach (var component in components)
					Iterate(component, go, _flags);
			}

			UiLog.Log(DumpDependencyCache());
		}
		
		public static List<GameObject> QueryGameObjectSceneReferences(GameObject _gameObject, SearchableEditorWindow.SearchMode _searchMode = SearchableEditorWindow.SearchMode.All, bool _excludeSelf = true)
		{
			SearchList.Clear();
			if (DependenciesByGameObject.TryGetValue(_gameObject, out HashSet<Object> dependencies))
			{
				foreach (var dependency in dependencies)
					if (dependency is GameObject go)
						SearchList.Add(go);
			}
			
			return SearchList;
		}


		public static string DumpDependencyCache()
		{
			StringBuilder sb = new();
			
			sb.Append("Dependency cache contents:\n\n");
			
			foreach (var kv in DependenciesByObject)
			{
				sb.Append($"{kv.Key.name}:{kv.Key.GetType()} depends on:\n");
				foreach (var dependency in kv.Value)
					sb.Append($"\t{dependency.name}:{dependency.GetType()}\n");
				sb.Append("\n");
			}
			
			sb.Append("\nGame Object Dependency cache contents:\n\n");
			
			foreach (var kv in DependenciesByGameObject)
			{
				sb.Append($"{kv.Key.name}:{kv.Key.GetType()} depends on:\n");
				foreach (var dependency in kv.Value)
					sb.Append($"\t{dependency.name}:{dependency.GetType()}\n");

				sb.Append("\n");
			}
			
			return sb.ToString();
		}
		
		private static bool HasFlag(SceneDependencyFlags _flags, SceneDependencyFlags _flag) => (_flags & _flag) != 0;

		private static void Iterate(Object _obj, GameObject _go, SceneDependencyFlags _flags)
		{
			if (!_obj || !_go)
				return;

			using (var serializedObject = new SerializedObject(_obj))
			{
				SerializedProperty iterator = serializedObject.GetIterator();
				iterator.Next(true);

				do
				{
					if (iterator.name == "m_FirstField")
						break;

					if (iterator.isArray)
					{
						HandleArray(iterator, _obj, _go, _flags);
						continue;
					}

					Object dependency = GetDependency(iterator);
					if (dependency)
						AddToDependencyCaches(_obj, dependency, _go, _flags);
				}
				while (iterator.Next(false));
			}
		}

		private static bool HandleArray(SerializedProperty _iterator, Object _obj, GameObject _go, SceneDependencyFlags _flags)
		{
			if (_iterator.arraySize == 0)
				return false;
			
			var elem = _iterator.GetArrayElementAtIndex(0);
			if (!IsDependencySupported(elem))
				return false;
			
			bool result = false;

			for (int i = 0; i < _iterator.arraySize; i++)
			{
				elem = _iterator.GetArrayElementAtIndex(i);
				Object dependency = GetDependency(elem);
				if (dependency)
				{
					AddToDependencyCaches(_obj, dependency, _go, _flags);
					result = true;
				}
			}

			return result;
		}

		//TODO: boxed value
		private static bool IsDependencySupported(SerializedProperty _iterator) => GetDependency(_iterator);

		private static Object GetDependency(SerializedProperty _iterator)
		{
			switch (_iterator.propertyType)
			{
				case SerializedPropertyType.ObjectReference:
					return _iterator.objectReferenceValue;
				case SerializedPropertyType.ExposedReference:
					return _iterator.exposedReferenceValue;
				case SerializedPropertyType.ManagedReference:
					return _iterator.managedReferenceValue as Object;

				default:
					return null;
			}
		}

		private static void AddToDependencyCaches(Object _obj, Object _dependency, GameObject _go, SceneDependencyFlags _flags)
		{
			if (!_obj || !_go)
				return;
			
			if (HasFlag(_flags, SceneDependencyFlags.ExcludeSelf) && _obj == _dependency )
				return;
			if (HasFlag(_flags, SceneDependencyFlags.OnlyGameObjects) && !(_dependency is GameObject))
				return;
			if (HasFlag(_flags, SceneDependencyFlags.NoPrefabs))
				if (_dependency is GameObject dependencyGo)
					if(dependencyGo.scene.name == null)
						return;
			
			if (!DependenciesByObject.TryGetValue(_obj, out HashSet<Object> dependencies))
			{
				dependencies = new HashSet<Object>();
				DependenciesByObject.Add(_obj, dependencies);
			}

			dependencies.Add(_dependency);
			
			if (HasFlag(_flags, SceneDependencyFlags.ExcludeSelf) && _dependency == _go )
				return;
			
			if (!DependenciesByGameObject.TryGetValue(_go, out HashSet<Object> gameObjectDependencies))
			{
				gameObjectDependencies = new HashSet<Object>();
				DependenciesByGameObject.Add(_go, gameObjectDependencies);
			}

			gameObjectDependencies.Add(_dependency);
		}
#endif

		public static bool IsInternal = EditorFileUtility.GetApplicationDataDir().ToLower().Contains(".dev-app");

		public static void SetStaticEditorFlagsInHierarchy( Transform _transform, StaticEditorFlags _flags, bool _replace = false, bool _log = true )
		{
			if (_transform == null)
				return;

			var go = _transform.gameObject;

			var currentFlags = GameObjectUtility.GetStaticEditorFlags(_transform.gameObject);
			if ((currentFlags & _flags) != _flags)
			{
				if (_log)
					UiLog.Log($"Setting static editor Flags '{_flags}'\n'{_transform.GetPath()}'\ncurrent flags:{currentFlags}");

				if (_replace)
					GameObjectUtility.SetStaticEditorFlags(go, _flags);
				else
					GameObjectUtility.SetStaticEditorFlags(go, currentFlags | _flags);
			}

			foreach (Transform child in _transform)
				SetStaticEditorFlagsInHierarchy(child, _flags, _replace);
		}

		public static void SetStaticEditorFlagsInHierarchy( GameObject _gameObject, StaticEditorFlags _flags, bool _replace = false, bool _log = true )
		{
			if (_gameObject == null)
				return;

			SetStaticEditorFlagsInHierarchy(_gameObject.transform, _flags, _replace, _log);
		}

		public static void ClearStaticEditorFlagsInHierarchy( Transform _transform, StaticEditorFlags _flags, bool _log = true )
		{
			if (_transform == null)
				return;

			var currentFlags = GameObjectUtility.GetStaticEditorFlags(_transform.gameObject);
			if ((currentFlags & _flags) != 0)
			{
				if (_log)
					UiLog.Log($"Clearing static editor Flags '{_flags}'\n'{_transform.GetPath()}'\ncurrent flags:{currentFlags}");

				GameObjectUtility.SetStaticEditorFlags(_transform.gameObject, currentFlags & ~_flags);
				EditorGeneralUtility.SetDirty(_transform.gameObject);
			}

			foreach (Transform child in _transform)
				ClearStaticEditorFlagsInHierarchy(child, _flags, _log);
		}

		public static void ClearStaticEditorFlagsInHierarchy( GameObject _gameObject, StaticEditorFlags _flags, bool _log = true )
		{
			if (_gameObject == null)
				return;

			ClearStaticEditorFlagsInHierarchy(_gameObject.transform, _flags, _log);
		}

		public static object GetInstancePropertyValueByReflection( object _instance, string _propertyName, bool _public = true )
		{
			var flags = BindingFlags.Instance | (_public ? BindingFlags.Public : BindingFlags.NonPublic);
			var propertyInfo = _instance.GetType().GetProperty(_propertyName, flags);
			if (propertyInfo == null)
				return null;

			return propertyInfo.GetValue(_instance, null);
		}

		public static void DestroyStrayMeshes( bool _hiddenOnly = true )
		{
			List<Mesh> meshesToDestroy = new();

			var allFlaggedMeshes = Object.FindObjectsOfType<Object>(true)
				.Where(obj => obj is Mesh)
				.Where(obj => !_hiddenOnly || (obj.hideFlags & (HideFlags.HideInHierarchy | HideFlags.HideInInspector)) != 0)
				.ToList();

			var allMeshFilters = Object.FindObjectsOfType<MeshFilter>(true)
				.Where(filter => filter.sharedMesh != null)
				.ToList();

			HashSet<Mesh> usedMeshes = new();
			foreach (var meshFilter in allMeshFilters)
				usedMeshes.Add(meshFilter.sharedMesh);

			foreach (var mesh in allFlaggedMeshes)
			{
				if (!usedMeshes.Contains(mesh))
					meshesToDestroy.Add(mesh as Mesh);
			}

			foreach (var mesh in meshesToDestroy)
			{
				mesh.SafeDestroy(true);
			}
		}

		public static void UnpackAllPrefabInstancesInChildren( Transform tf )
		{
			var transforms = tf.GetComponentsInChildren<Transform>(true);
			foreach (var descendant in transforms)
			{
				if (PrefabUtility.IsPartOfAnyPrefab(descendant.gameObject))
				{
					try
					{
						var outermostPrefab = PrefabUtility.GetOutermostPrefabInstanceRoot(descendant.gameObject);
						PrefabUtility.UnpackPrefabInstance(outermostPrefab, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
					}
					catch (Exception e)
					{
						UiLog.LogWarning($"Could not unpack prefab instance '{descendant.name}'\nException:{e.Message}\n{descendant.GetPath()}");
					}
				}
			}
		}

		private static readonly HashSet<string> s_emptyStringHashSet = new();

		public static string GetProjectName()
		{
			string path = Application.dataPath;
			return new DirectoryInfo(Path.GetDirectoryName(path)).Name;
		}

		public static string GetPlayerPrefsProjectKey( string _postfix, string _entryName )
		{
			return $"{StringConstants.PLAYER_PREFS_PREFIX}{GetProjectName()}_{_postfix}.{_entryName}";
		}

		public static void DrawInspectorExceptField( SerializedObject _serializedObject, string _fieldToSkip = null, string _header = null )
		{
			if (string.IsNullOrEmpty(_fieldToSkip))
			{
				DrawInspectorExceptFields(_serializedObject, s_emptyStringHashSet, _header);
			}

			DrawInspectorExceptFields(_serializedObject, new HashSet<string>() { _fieldToSkip }, _header);
		}

		public static void DrawInspectorExceptFields( SerializedObject _serializedObject, HashSet<string> _fieldsToSkip = null, string _header = null )
		{
			if (_serializedObject == null || _serializedObject.targetObject == null)
			{
				UiLog.LogWarning($"serialized object or target object is null");
				return;
			}

			_serializedObject.Update();

			if (!string.IsNullOrEmpty(_header))
				EditorGUILayout.LabelField(_header, EditorStyles.boldLabel);
			if (_fieldsToSkip == null)
				_fieldsToSkip = s_emptyStringHashSet;

			ForeachProperty(_serializedObject, prop =>
			{
				if (_fieldsToSkip.Contains(prop.name))
					return;

				EditorGUILayout.PropertyField(_serializedObject.FindProperty(prop.name), true);
			});
		}

		public static void ForeachProperty( SerializedObject _serializedObject, Action<SerializedProperty> _action )
		{
			if (_serializedObject == null || _serializedObject.targetObject == null)
			{
				UiLog.LogWarning($"serialized object or target object is null");
				return;
			}

			_serializedObject.Update();
			SerializedProperty prop = _serializedObject.GetIterator();
			if (prop.NextVisible(true))
			{
				do
				{
					_action.Invoke(_serializedObject.FindProperty(prop.name));
				}
				while (prop.NextVisible(false));
			}

			_serializedObject.ApplyModifiedProperties();
		}

		public static void ForeachPropertyHierarchical( SerializedObject _serializedObject, Action<SerializedProperty> _action )
		{
			if (_serializedObject == null || _serializedObject.targetObject == null)
			{
				UiLog.LogWarning($"serialized object or target object is null");
				return;
			}

			_serializedObject.Update();

			SerializedProperty prop = _serializedObject.GetIterator();
			if (prop.NextVisible(true))
			{
				do
				{
					var prop2 = _serializedObject.FindProperty(prop.name);
					_action.Invoke(prop2);

					SerializedProperty it = prop2.Copy();
					while (it.Next(true))
						_action.Invoke(it);
				}
				while (prop.NextVisible(false));
			}

			_serializedObject.ApplyModifiedProperties();
		}

		public static void ForeachProperty( Object _object, Action<SerializedProperty> _action ) => ForeachProperty(new SerializedObject(_object), _action);
		public static void ForeachPropertyHierarchical( Object _object, Action<SerializedProperty> _action ) => ForeachPropertyHierarchical(new SerializedObject(_object), _action);

		public static SerializedProperty GetParentProperty( this SerializedProperty _property )
		{
			var propertyPaths = _property.propertyPath.Split('.');
			if (propertyPaths.Length <= 1)
			{
				return default;
			}

			var parentSerializedProperty = _property.serializedObject.FindProperty(propertyPaths.First());
			for (var i = 1; i < propertyPaths.Length - 1; i++)
			{
				if (propertyPaths[i] == "Array")
				{
					if (i + 1 == propertyPaths.Length - 1)
					{
						// reached the end
						break;
					}
					if (propertyPaths.Length > i + 1 && Regex.IsMatch(propertyPaths[i + 1], "^data\\[\\d+\\]$"))
					{
						var match = Regex.Match(propertyPaths[i + 1], "^data\\[(\\d+)\\]$");
						var arrayIndex = int.Parse(match.Groups[1].Value);
						parentSerializedProperty = parentSerializedProperty.GetArrayElementAtIndex(arrayIndex);
						i++;
					}
				}
				else
				{
					parentSerializedProperty = parentSerializedProperty.FindPropertyRelative(propertyPaths[i]);
				}
			}
			return parentSerializedProperty;
		}

		public static bool TryGetCustomAttribute<TA>( this SerializedProperty _property, out TA _value, bool _checkArray = false ) where TA : Attribute
		{
			if (_checkArray)
			{
				var parentProperty = _property.GetParentProperty();
				if (parentProperty != null && parentProperty.isArray)
					return TryGetCustomAttribute<TA>(parentProperty, out _value, false);
			}

			_value = default;
			var obj = _property.serializedObject.targetObject;
			if (obj == null)
				return false;

			FieldInfo field = obj.GetType().GetField(_property.name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
			if (field == null)
				return false;

			_value = field.GetCustomAttribute<TA>();
			return _value != null;
		}

		public static GameObject FindMatchingChildInPrefab( GameObject prefab, GameObject partOfPrefab )
		{
			var partOfPrefabPath = partOfPrefab.GetPath();
			var transforms = prefab.GetComponentsInChildren<Transform>();
			foreach (var transform in transforms)
			{
				if (partOfPrefabPath.EndsWith(transform.GetPath(1)))
					return transform.gameObject;
			}

			return null;
		}

		public static bool SetSceneHierarchySearchFilter( object _searchObj, SearchableEditorWindow.SearchMode _searchMode = SearchableEditorWindow.SearchMode.All )
		{
			Type type = typeof(SearchableEditorWindow).Assembly.GetType("UnityEditor.SceneHierarchyWindow");
			EditorWindow sceneHierarchyWindow = EditorWindow.GetWindow(type, false, null, false);
			BindingFlags nonPublicInstanceFlags = BindingFlags.NonPublic | BindingFlags.Instance;

			MethodInfo setSearchFilterMethod = type.GetMethod("SetSearchFilter", nonPublicInstanceFlags);
			if (setSearchFilterMethod == null)
			{
				UiLog.LogError($"Unity internal API has changed, please fix!");
				return false;
			}

			setSearchFilterMethod.Invoke(sceneHierarchyWindow, new object[] { _searchObj, _searchMode, true, false });
			return true;
		}

		public static bool SetSceneHierarchySearchFilter( string _searchStr, SearchableEditorWindow.SearchMode _searchMode = SearchableEditorWindow.SearchMode.All ) =>
			SetSceneHierarchySearchFilter((object)_searchStr, _searchMode);

		public static object GetSceneHierarchySearchFilter()
		{
			Type type = typeof(SearchableEditorWindow).Assembly.GetType("UnityEditor.SceneHierarchyWindow");
			EditorWindow sceneHierarchyWindow = EditorWindow.GetWindow(type, false, null, false);
			BindingFlags nonPublicInstanceFlags = BindingFlags.NonPublic | BindingFlags.Instance;

			object sceneHierarchy = GetInstancePropertyValueByReflection(sceneHierarchyWindow, "sceneHierarchy");
			if (sceneHierarchy == null)
				goto ReflectionError;

			FieldInfo searchFilterMember = sceneHierarchy.GetType().GetField("m_SearchFilter", nonPublicInstanceFlags);
			if (searchFilterMember == null)
				goto ReflectionError;

			return searchFilterMember.GetValue(sceneHierarchy);

ReflectionError:
			UiLog.LogError($"Unity internal API has changed, please fix!");
			return null;
		}

		/// <summary>
		/// Unity has got a very powerful search.
		/// Unfortunately and as usual, this very essential feature is secretly hidden in internal and private crap.
		/// We are using reflection here to overcome this.
		/// !!! Caution: This is guaranteed to be extremely slow. Don't even think about iterating a scene with 10000 game objects. !!!
		/// </summary>
		/// <param name="_searchString">Common Unity search string as used in scene hierarchy window</param>
		/// <param name="_searchMode"></param>
		/// <returns>List of found game objects.</returns>
		public static List<GameObject> SceneHierarchySearch( string _searchString, SearchableEditorWindow.SearchMode _searchMode = SearchableEditorWindow.SearchMode.All )
		{
			SearchList.Clear();

			Type type = typeof(SearchableEditorWindow).Assembly.GetType("UnityEditor.SceneHierarchyWindow");
			EditorWindow sceneHierarchyWindow = EditorWindow.GetWindow(type, false, null, false);

			object savedSearchFilter = GetSceneHierarchySearchFilter();
			SetSceneHierarchySearchFilter(_searchString, _searchMode);

			object sceneHierarchy = GetInstancePropertyValueByReflection(sceneHierarchyWindow, "sceneHierarchy");
			if (sceneHierarchy == null)
				goto ReflectionError;

			object treeViewProperty = GetInstancePropertyValueByReflection(sceneHierarchy, "treeView", false);
			object data = GetInstancePropertyValueByReflection(treeViewProperty, "data");
			if (treeViewProperty == null || data == null)
				goto ReflectionError;

			MethodInfo getRowsMethod = data.GetType().GetMethod("GetRows");
			if (getRowsMethod == null)
				goto ReflectionError;

			IEnumerable rows = getRowsMethod.Invoke(data, null) as IEnumerable;
			if (rows == null)
				goto ReflectionError;

			foreach (var row in rows)
			{
				var id = (int)GetInstancePropertyValueByReflection(row, "id");
				var gameObject = EditorUtility.InstanceIDToObject(id) as GameObject;
				if (gameObject)
					SearchList.Add(gameObject);
			}

			SetSceneHierarchySearchFilter(savedSearchFilter, SearchableEditorWindow.SearchMode.Name);
			return SearchList;

ReflectionError:
			UiLog.LogError($"Unity internal API has changed, please fix!");
			return SearchList;
		}

		/// <summary>
		/// Collect scene references of an object by reflection.
		/// !!! Caution: This is guaranteed to be extremely slow. Don't even think about iterating a scene with 10000 game objects. !!!
		/// </summary>
		/// <param name="_obj">Object to find references to</param>
		/// <param name="_searchMode"></param>
		/// <param name="_excludeSelf">if true, the common self-reference of game objects is removed from result.</param>
		/// <returns>List of found referencing game objects</returns>
		public static List<GameObject> CollectSceneReferences( Object _obj, SearchableEditorWindow.SearchMode _searchMode = SearchableEditorWindow.SearchMode.All, bool _excludeSelf = true )
		{
			SearchList.Clear();
			if (_obj == null)
				return SearchList;

			int id = _obj.GetInstanceID();
			var result = SceneHierarchySearch(string.Format("ref:{0}:", id), _searchMode);

			if (result != null && _excludeSelf)
			{
				for (int i = 0; i < result.Count; i++)
				{
					var reference = result[i];
					if (reference == _obj)
					{
						result.RemoveAt(i);
						return result;
					}
				}
			}

			return result;
		}

		public static List<T> ToListBoxed<T>( this SerializedProperty _property ) => ToList(_property, serializedProperty => (T)serializedProperty.boxedValue);

		public static List<string> ToListString( this SerializedProperty _property ) => ToList(_property, serializedProperty => serializedProperty.stringValue);

		public static List<T> ToList<T>( this SerializedProperty _property, Func<SerializedProperty, T> _setter )
		{
			List<T> result = new();

			if (!_property.isArray)
				return result;

			int count = _property.arraySize;
			for (int i = 0; i < count; i++)
			{
				SerializedProperty elemProp = _property.GetArrayElementAtIndex(i);
				T elem = _setter.Invoke(elemProp);
				result.Add(elem);
			}

			return result;
		}


		public static void FromListBoxed<T>( this SerializedProperty _property, List<T> _list ) => FromList(_property, _list, ( property, t ) => property.boxedValue = t);
		public static void FromListString( this SerializedProperty _property, List<string> _list ) => FromList(_property, _list, ( property, t ) => property.stringValue = t);

		public static void FromList<T>( this SerializedProperty _property, List<T> _list, Action<SerializedProperty, T> _setter )
		{
			if (!_property.isArray)
				return;

			_property.arraySize = _list.Count;
			int count = _property.arraySize;
			for (int i = 0; i < count; i++)
			{
				SerializedProperty elemProp = _property.GetArrayElementAtIndex(i);
				_setter.Invoke(elemProp, _list[i]);
			}
		}

		public static void RemoveArrayElementAtIndex( SerializedProperty _list, int _idx )
		{
			if (!ValidateListAndIndex(_list, _idx))
				return;

			for (int i = _idx; i < _list.arraySize - 1; i++)
				_list.MoveArrayElement(i + 1, i);
			_list.arraySize--;
		}

		public static void SwapArrayElement( SerializedProperty _list, int _idx1, int _idx2 )
		{
			if (_idx1 == _idx2 || !ValidateListAndIndex(_list, _idx1) || !ValidateListAndIndex(_list, _idx2))
				return;
			SerializedProperty pt = _list.GetArrayElementAtIndex(_idx1);
			_list.MoveArrayElement(_idx2, _idx1);
			SerializedProperty p = _list.GetArrayElementAtIndex(_idx2);
			p = pt;
		}

		public static bool AreMultipleBitsSet( int _n )
		{
			return (_n & (_n - 1)) != 0;
		}

		// Supports interfaces
		// Caution! Clear result before usage!
		// (It is not cleared here on purpose, to be able to do multiple FindObjectsOfType() after another)
		public static void FindObjectsOfType<T>( List<T> _result, Scene _scene, bool _includeInactive = true )
		{
			GameObject[] roots = _scene.GetRootGameObjects();
			foreach (GameObject root in roots)
			{
				T[] components = root.GetComponentsInChildren<T>(_includeInactive);
				if (components == null || components.Length == 0)
					continue;

				foreach (T component in components)
					_result.Add(component);
			}
		}

		public static List<T> FindObjectsOfType<T>( Scene _scene, bool _includeInactive = true )
		{
			List<T> result = new();
			FindObjectsOfType<T>(result, _scene, _includeInactive);
			return result;
		}

		public static void FindObjectsOfType<T>( List<T> _result, bool _includeInactive = true )
		{
			_result.Clear();
			for (int i = 0; i < SceneManager.loadedSceneCount; i++)
			{
				Scene scene = EditorSceneManager.GetSceneAt(i);
				FindObjectsOfType<T>(_result, scene, _includeInactive);
			}
		}

		public static List<T> FindObjectsOfType<T>( bool _includeInactive = true )
		{
			List<T> result = new List<T>();
			FindObjectsOfType<T>(result, _includeInactive);
			return result;
		}

		public static T InstantiateIfNotAnAsset<T>( T _obj, Dictionary<T, T> _clonedCache = null, Action<T, T> _onCloned = null ) where T : Object
		{
			if (!_obj)
				return null;

			var assetPath = AssetDatabase.GetAssetPath(_obj);
			if (!string.IsNullOrEmpty(assetPath))
				return _obj;

			if (_clonedCache != null && _clonedCache.TryGetValue(_obj, out T cloned))
				return cloned;

			T result = Object.Instantiate<T>(_obj);
			_onCloned?.Invoke(_obj, result);

			if (_clonedCache != null)
				_clonedCache.Add(_obj, result);

			return result;
		}

		public static bool DestroyComponents<T>( Transform _t ) where T : Component
		{
			if (_t == null)
				return false;

			var components = _t.GetComponents<T>();
			foreach (var component in components)
				component.SafeDestroy();

			return components.Length > 0;
		}

		public static void CreateAsset( Object _obj, string _path )
		{
			string directory = EditorFileUtility.GetDirectoryName(_path);
			EditorFileUtility.EnsureUnityFolderExists(directory);
			AssetDatabase.CreateAsset(_obj, _path);
		}

		public static void SetDirty( Object _obj )
		{
			// Never set package assets dirty; leads to "Saving Prefab to immutable folder is not allowed" errors
			if (PrefabUtility.IsPartOfImmutablePrefab(_obj))
				return;

			EditorUtility.SetDirty(_obj);
		}

		private static bool ValidateListAndIndex( SerializedProperty _list, int _idx )
		{
			if (!ValidateList(_list))
				return false;

			return ValidateListIndex(_idx, _list);
		}

		private static bool ValidateList( SerializedProperty _list )
		{
			if (!_list.isArray)
			{
				UiLog.LogError("Attempt to access array element from a SerializedProperty which isn't an array");
				return false;
			}
			return true;
		}

		private static bool ValidateListIndex( int _idx, SerializedProperty _list )
		{
			if (_idx < 0 || _idx >= _list.arraySize)
			{
				UiLog.LogError("Out of Bounds when accessing an array element");
				return false;
			}
			return true;
		}

		public static void ForceRefreshEditorUi()
		{
			EditorApplication.QueuePlayerLoopUpdate();

			SceneView.RepaintAll();
			EditorApplication.RepaintHierarchyWindow();
			EditorApplication.RepaintProjectWindow();

			EditorWindow[] windows = Resources.FindObjectsOfTypeAll<EditorWindow>();
			foreach (EditorWindow window in windows)
			{
				if (window == null)
				{
					continue;
				}

				VisualElement root = window.rootVisualElement;
				if (root != null)
					root.MarkDirtyRepaint();

				window.Repaint();
			}

			EditorApplication.delayCall += () =>
			{
				SceneView.RepaintAll();
				EditorApplication.QueuePlayerLoopUpdate();
			};
		}
	}
}

#endif
