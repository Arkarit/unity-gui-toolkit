using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;
using System.Runtime.CompilerServices;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GuiToolkit
{
	public static class Extensions
	{
		private static readonly HashSet<string> s_excludedMembers = new HashSet<string> { "name", "_parent", "parentInternal" };
		private static readonly HashSet<Type> s_excludedTypes = new HashSet<Type> { typeof(Component), typeof(Transform), typeof(MonoBehaviour) };
		private static readonly List<Transform> s_tempTransformList = new();
		private static bool s_isQuitting = false;
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void Init()
		{
			Application.quitting += () => s_isQuitting = true;
		}

		public static bool IsQuitting( this Application _ ) => s_isQuitting;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe bool HasFlags<T>( this T _self, T _flags ) where T : unmanaged, Enum
		{
			var s = *(uint*)&_self;
			var f = *(uint*)&_flags;
			return (s & f) != 0;
		}

		public static IList<T> Clone<T>( this IList<T> _listToClone ) where T : ICloneable
		{
			return _listToClone.Select(item => (T)item.Clone()).ToList();
		}

		public static bool EndsWithViceVersa( this string _self, string _other )
		{
			int lenSelf = _self.Length;
			int lenOther = _other.Length;
			bool selfGreaterThanOther = lenSelf > lenOther;
			string a = selfGreaterThanOther ? _self : _other;
			string b = selfGreaterThanOther ? _other : _self;

			return a.EndsWith(b);
		}

		/// <summary>
		/// Returns the path of a transform, game object or component.
		/// </summary>
		/// <param name="_self">Transform, game object or component. May be null, in that case a string containing @"@<null@>@" is returned</param>
		/// <param name="_depth">
		/// If 0, complete path is returned
		/// If > 0, path up to depth n is returned (e.g. MyOuterPrefab/MyPrefab/MyContainer/MyObject with depth 2 would return MyContainer/MyObject)
		/// If < 0, complete path with -n parts removed at the left side (e.g. MyOuterPrefab/MyPrefab/MyContainer/MyObject with depth -1 would return MyPrefab/MyContainer/MyObject)
		/// </param>
		/// <param name="_separator"></param>
		/// <returns></returns>
		public static string GetPath( this Transform _self, int _depth = 0, char _separator = '/' )
		{
			if (_self == null)
				return "<null>";

			if (_depth < 0)
				_depth = GetPathDepth(_self) + _depth;

			if (_depth <= 0)
				_depth = 100000;

			string result = _self.name;
			while (_self.parent != null && _depth > 1)
			{
				_self = _self.parent;
				result = _self.name + _separator + result;
				_depth--;
			}

			return result;
		}

		public static int GetPathDepth( this Transform _self )
		{
			if (_self == null)
				return 0;

			int result = 1;
			while (_self.parent != null)
			{
				result++;
				_self = _self.parent;
			}

			return result;
		}

		public static void GetComponentsInDirectChildren<T>( this Transform _self, List<T> _list, bool _includeInactive = true ) where T : Component
		{
			_list.Clear();

			foreach (Transform child in _self)
			{
				if (!_includeInactive && !child.gameObject.activeInHierarchy)
					continue;

				T component = child.GetComponent<T>();
				if (component)
					_list.Add(component);
			}
		}

		public static void GetComponentsInDirectChildren<T>( this GameObject _self, List<T> _list, bool _includeInactive = true ) where T : Component =>
			GetComponentsInDirectChildren<T>(_self.transform, _list, _includeInactive);

		public static List<T> GetComponentsInDirectChildren<T>( this Transform _self, bool _includeInactive = true ) where T : Component
		{
			List<T> result = new();
			GetComponentsInDirectChildren<T>(_self, result, _includeInactive);
			return result;
		}

		public static List<T> GetComponentsInDirectChildren<T>( this GameObject _self, bool _includeInactive = true ) where T : Component =>
			GetComponentsInDirectChildren<T>(_self.transform, _includeInactive);

		public static string GetPath( this GameObject _self, int _depth = 0, char _separator = '/' )
		{
			if (_self == null)
				return "<null>";

			return GetPath(_self.transform, _depth, _separator);
		}

		public static int GetPathDepth( this GameObject _self ) => _self == null ? 0 : GetPathDepth(_self.transform);

		public static string GetPath( this Component _self, int _depth = 0, char _separator = '/' )
		{
			if (_self == null)
				return "<null>";

			return GetPath(_self.transform, _depth, _separator);
		}

		public static int GetPathDepth( this Component _self ) => _self == null ? 0 : GetPathDepth(_self.transform);

		public static string GetRelativePathOfDescendant( this Transform _self, Transform _descendant, char _separator = '/' )
		{
			if (_descendant == null)
				return "<null>";

			if (_self == _descendant)
				return string.Empty;

			var path = _descendant.GetPath(0, _separator);

			if (_self == null || !_self.IsMyDescendant(_descendant.transform))
				return path;

			return path.Substring(_self.GetPath(0, _separator).Length + 1);
		}

		public static string GetRelativePathOfDescendant( this Component _self, Component _descendant, char _separator = '/' )
		{
			if (_descendant == null)
				return "<null>";

			if (_self == null)
				return _descendant.transform.GetPath(0, _separator);

			return _self.transform.GetRelativePathOfDescendant(_descendant.transform, _separator);
		}

		public static string GetRelativePathOfDescendant( this GameObject _self, GameObject _descendant, char _separator = '/' )
		{
			if (_descendant == null)
				return "<null>";

			if (_self == null)
				return _descendant.transform.GetPath(0, _separator);

			return _self.transform.GetRelativePathOfDescendant(_descendant.transform, _separator);
		}

		public static void DestroyEmptyChildren( this Transform _self, bool _includeSelf = false, bool _onlyIfNoComponents = true )
		{
			// We can not simply iterate, since children are possibly destroyed
			var childCount = _self.childCount;
			for (int i = childCount - 1; i >= 0; --i)
				DestroyEmptyChildren(_self.GetChild(i), true, _onlyIfNoComponents);

			if (!_includeSelf)
				return;

			if (_onlyIfNoComponents && _self.gameObject.GetComponentCount() > 1)
				return;

			if (_self.childCount == 0)
				_self.gameObject.SafeDestroy();
		}

		private static Transform s_DisabledParent;

		public static GameObject InstantiateDisabled( this GameObject original, Transform parent, bool worldPositionStays )
		{
			if (original == null)
				return null;

			if (!original.activeSelf)
				return UnityEngine.Object.Instantiate(original, parent, worldPositionStays);

			GameObject result;

			if (parent != null && !parent.gameObject.activeInHierarchy)
			{
				result = UnityEngine.Object.Instantiate(original, parent, worldPositionStays);
				result.SetActive(false);
				return result;
			}

			if (s_DisabledParent == null)
			{
				GameObject go = new GameObject("Disabled");
				UnityEngine.Object.DontDestroyOnLoad(go);
				go.SetActive(false);
				s_DisabledParent = go.transform;
			}

			result = UnityEngine.Object.Instantiate(original, s_DisabledParent, worldPositionStays);
			result.SetActive(false);
			result.transform.SetParent(parent, true);
			return result;
		}

		public static GameObject InstantiateDisabled( this GameObject original, Transform parent ) => InstantiateDisabled(original, parent, false);
		public static GameObject InstantiateDisabled( this GameObject original ) => InstantiateDisabled(original, null, false);

		public static void SafeDestroyDelayed( this UnityEngine.Object _self )
		{
			if (GeneralUtility.IsQuitting)
				return;

			if (_self == null)
				return;

			if (!Application.isPlaying)
			{
				SafeDestroy(_self, false);
				return;
			}

			CoRoutineRunner.Instance.StartCoroutine(DestroyDelayed(_self));
		}

		private static IEnumerator DestroyDelayed( UnityEngine.Object _self )
		{
			yield return 0;

			// Application stopped during delay
			if (!Application.isPlaying || _self == null)
				yield break;

			Object.Destroy(_self);
		}


		public static bool SafeDestroy( this UnityEngine.Object _self, bool _supportUndoIfPossible = true, bool _logText = false )
		{
			if (_self == null)
				return true;

#if UNITY_EDITOR
			if (!CanBeDestroyed(_self))
				return false;

			if (_supportUndoIfPossible && !Application.isPlaying)
			{
				Undo.DestroyObjectImmediate(_self);
				return true;
			}
#endif

			// We want the object to be immediately detached from its parent if it is a game object
			GameObject go = _self as GameObject;
#if UNITY_EDITOR
			if (go && !PrefabUtility.IsPartOfImmutablePrefab(go))
#else
			if (go)
#endif
				go.transform.SetParent(null);

#if UNITY_EDITOR

			// Sometimes, objects are not actually deleted in editor (especially when starting game).
			// We make sure, that these objects are not visible in editor and won't be saved.
			if (go)
			{
				var transforms = go.GetComponentsInChildren<Transform>();
				foreach (var transform in transforms)
				{
					transform.gameObject.hideFlags = HideFlags.HideAndDontSave;
				}

				go.SetActive(false);
			}

			if (Application.isPlaying)
			{
				Object.Destroy(_self);
				return true;
			}

			EditorApplication.delayCall += () =>
			{
				Object.DestroyImmediate(_self);
			};

			return true;
#else
			Object.Destroy(_self);
			return true;
#endif
		}

		public static bool CanBeDestroyed( this Object _self, out string _issues )
		{
			_issues = string.Empty;
#if UNITY_EDITOR
			if (Application.isPlaying)
				return true;

			if (!(_self is Component component))
				return true;

			bool result = EditorGameObjectUtility.CanRemoveComponent(component, out var reasons);
			if (!result)
				_issues = string.Join(", ", reasons);

			return result;
#else
			return true;
#endif
		}

		public static bool CanBeDestroyed( this Object _self ) => CanBeDestroyed(_self, out var _);
		public static MethodInfo GetPublicOrNonPublicStaticMethod( this Type _type, string _name, bool _logError = true )
		{
			// We also find public methods in case a method has been made public in an update
			var result = _type.GetMethod(_name, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

			if (_logError && result == null)
				UiLog.LogError($"Internal API for private static method '{_type.Name}.{_name}()' has changed, please fix!");

			return result;
		}

		public static object CallPublicOrNonPublicStaticMethod( this Type _type, string _name, out bool _success, bool _catchExceptions = false, bool _logError = true, params object[] _params )
		{
			_success = true;
			MethodInfo mi = GetPublicOrNonPublicStaticMethod(_type, _name, _logError);
			if (mi == null)
			{
				_success = false;
				return null;
			}

			try
			{
				return mi.Invoke(null, _params);
			}
			catch (Exception e)
			{
				_success = false;
				if (!_catchExceptions)
					throw;

				if (_logError)
					UiLog.LogError($"Exception: Internal API for private static method '{_type.Name}.{_name}()' has changed, or other (parameter) error, please fix!\n{e.Message}");

				return null;
			}
		}

		public static object CallPublicOrNonPublicStaticMethod( this Type _type, string _name, params object[] _params ) =>
			CallPublicOrNonPublicStaticMethod(_type, _name, out bool _, _catchExceptions: false, _logError: true, _params);

		public static List<T> ToList<T>( this HashSet<T> _self )
		{
			List<T> result = new List<T>(_self.Count);
			foreach (T elem in _self)
				result.Add(elem);

			return result;
		}

		public static List<T> ToList<T>( this SortedSet<T> _self )
		{
			List<T> result = new List<T>(_self.Count);
			foreach (T elem in _self)
				result.Add(elem);

			return result;
		}

		public static List<T> ToList<T>( this SortedSet<T> _self, List<T> _result )
		{
			foreach (T elem in _self)
				_result.Add(elem);

			return _result;
		}

		public static Rect GetWorldRect2D( this Rect _self, RectTransform _rt )
		{
			Vector2 tl = _rt.TransformPoint(_self.TopLeft());
			Vector2 br = _rt.TransformPoint(_self.BottomRight());
			float x = tl.x;
			float y = tl.y;
			float w = br.x - x;
			float h = br.y - y;

			return new Rect(x, y, w, h);
		}

		public static Vector2[] GetWorldCorners2D( this Rect _self, RectTransform _rt )
		{
			return new Vector2[] {
				_rt.TransformPoint(_self.BottomLeft()),
				_rt.TransformPoint(_self.TopLeft()),
				_rt.TransformPoint(_self.TopRight()),
				_rt.TransformPoint(_self.BottomRight()),
			};
		}

		public static Vector3[] GetWorldCorners( this Rect _self, RectTransform _rt )
		{
			return new Vector3[] {
				_rt.TransformPoint(_self.BottomLeft()),
				_rt.TransformPoint(_self.TopLeft()),
				_rt.TransformPoint(_self.TopRight()),
				_rt.TransformPoint(_self.BottomRight()),
			};
		}

		public static Rect GetWorldRect2D( this RectTransform _self )
		{
			return _self.rect.GetWorldRect2D(_self);
		}

		public static Rect GetWorldRect2D( this RectTransform _self, Canvas _canvas )
		{
			Rect result = GetWorldRect2D(_self);
			result = result.ScaleBy(1.0f / _canvas.scaleFactor);
			return result;
		}

		public static Rect GetWorldRect2D( this Rect _self, RectTransform _rt, Canvas _canvas )
		{
			Rect result = GetWorldRect2D(_self, _rt);
			result = result.ScaleBy(1.0f / _canvas.scaleFactor);
			return result;
		}

		public static Rect GetScreenRect( this RectTransform _self )
		{
			Vector2 size = Vector2.Scale(_self.rect.size, _self.lossyScale);
			Rect rect = new Rect(_self.position.x, Screen.height - _self.position.y, size.x, size.y);
			rect.x -= (_self.pivot.x * size.x);
			rect.y -= ((1.0f - _self.pivot.y) * size.y);
			return rect;
		}

		public static (Rect rect, Vector2 offset) BringToCenter( this Rect _self )
		{
			Vector2 offset = -_self.center;
			Rect rect = _self;
			rect.center = Vector2.zero;
			return (rect, offset);
		}

		public static Rect Absolute( this Rect _self )
		{
			Rect result = _self;
			if (_self.height < 0 || _self.width < 0)
			{
				Vector2 min = result.min;
				Vector2 max = result.max;
				float t;

				if (_self.width < 0)
				{
					t = min.x;
					min.x = max.x;
					max.x = t;
				}
				if (_self.height < 0)
				{
					t = min.y;
					min.y = max.y;
					max.y = t;
				}

				result.min = min;
				result.max = max;
			}
			return result;
		}

		public static Vector3 Size3( this Rect _self )
		{
			return _self.size;
		}

		public static Rect GetWorldRect2D( this RectTransform _self, Vector3[] _worldRectPositions )
		{
			Debug.Assert(_worldRectPositions != null && _worldRectPositions.Length == 4, "Needs _to be world corners _array as provided by RectTransform..GetWorldCorners()");
			if (_worldRectPositions == null || _worldRectPositions.Length != 4)
				return new Rect();

			return new Rect(_worldRectPositions[1].x, _worldRectPositions[1].y, _worldRectPositions[3].x - _worldRectPositions[0].x, _worldRectPositions[1].y - _worldRectPositions[0].y);
		}

		public static Vector2 GetWorldPosition2D( this RectTransform _self )
		{
			return _self.TransformPoint(_self.rect.TopLeft());
		}

		public static Vector2 GetWorldPosition2D( this RectTransform _self, Canvas _canvas )
		{
			Vector2 result = _self.TransformPoint(_self.rect.TopLeft());
			result = result * (1.0f / _canvas.scaleFactor);
			return result;
		}

		public static Vector2 GetWorldCenter2D( this RectTransform _self )
		{
			Rect _rt = GetWorldRect2D(_self);
			return _rt.center;
		}

		public static Vector2 GetWorldCenter2D( this RectTransform _self, Canvas _canvas )
		{
			Rect _rt = GetWorldRect2D(_self, _canvas);
			return _rt.center;
		}

		public static Vector2 TopLeft( this Rect _self )
		{
			return new Vector2(_self.xMin, _self.yMax);
		}
		public static Vector2 BottomLeft( this Rect _self )
		{
			return new Vector2(_self.xMin, _self.yMin);
		}
		public static Vector2 TopRight( this Rect _self )
		{
			return new Vector2(_self.xMax, _self.yMax);
		}
		public static Vector2 BottomRight( this Rect _self )
		{
			return new Vector2(_self.xMax, _self.yMin);
		}

		public static Vector3 TopLeft3( this Rect _self )
		{
			return new Vector3(_self.xMin, _self.yMax);
		}
		public static Vector3 BottomLeft3( this Rect _self )
		{
			return new Vector3(_self.xMin, _self.yMin);
		}
		public static Vector3 TopRight3( this Rect _self )
		{
			return new Vector3(_self.xMax, _self.yMax);
		}
		public static Vector3 BottomRight3( this Rect _self )
		{
			return new Vector3(_self.xMax, _self.yMin);
		}

		public static Rect ScaleBy( this Rect _self, float _val )
		{
			_self.x *= _val;
			_self.y *= _val;
			_self.width *= _val;
			_self.height *= _val;
			return _self;
		}

		public static Vector2 Xy( this Vector3 _self )
		{
			return new Vector2(_self.x, _self.y);
		}
		public static Vector2 Xz( this Vector3 _self )
		{
			return new Vector2(_self.x, _self.z);
		}
		public static Vector2 Yz( this Vector3 _self )
		{
			return new Vector2(_self.y, _self.z);
		}

		public static Vector2 Xy( this Vector4 _self )
		{
			return new Vector2(_self.x, _self.y);
		}

		public static Vector2 Zw( this Vector4 _self )
		{
			return new Vector2(_self.z, _self.w);
		}

		public static Vector3 Xyz( this Vector4 _self )
		{
			return new Vector3(_self.x, _self.y, _self.z);
		}

		public static Vector2 Swap( this Vector2 _self )
		{
			float t = _self.x;
			_self.x = _self.y;
			_self.y = t;
			return _self;
		}

		public static float Min( this Vector3 _self ) => Mathf.Min(_self.x, Mathf.Min(_self.y, _self.z));
		public static float Max( this Vector3 _self ) => Mathf.Max(_self.x, Mathf.Max(_self.y, _self.z));


		public static bool IsFlagSet<T>( this T _self, T _flag ) where T : unmanaged, Enum
		{
			return (_self.HasFlags(_flag));
		}

		public static bool IsSimilar( this Color _self, Color _other )
		{
			return
				   Mathf.Approximately(_self.r, _other.r)
				&& Mathf.Approximately(_self.g, _other.g)
				&& Mathf.Approximately(_self.b, _other.b)
				&& Mathf.Approximately(_self.a, _other.a);
		}

		public static void ScrollToTop( this ScrollRect _self, MonoBehaviour _coroutineHolder = null )
		{
			if (_coroutineHolder)
			{
				_coroutineHolder.StartCoroutine(ScrollToTopDelayed(_self));
				return;
			}

			if (_self.vertical)
				_self.verticalNormalizedPosition = 1;
			else if (_self.horizontal)
				_self.horizontalNormalizedPosition = 1;
		}

		private static IEnumerator ScrollToTopDelayed( ScrollRect _scrollRect )
		{
			yield return 0;
			_scrollRect.ScrollToTop();
		}

		public static void ForceRefresh( this MonoBehaviour _self )
		{
			if (!_self.enabled || !_self.gameObject.activeInHierarchy)
				return;

			_self.enabled = false;
			// ReSharper disable once Unity.InefficientPropertyAccess
			_self.enabled = true;
		}

		public static T GetInParentOrCreateComponent<T>( this Component _self ) where T : Component
		{
			T result = _self.GetComponentInParent<T>();

			if (result)
				return result;

			return _self.gameObject.AddComponent<T>();
		}

		public static T GetOrCreateComponent<T>( this Component _self ) where T : Component
		{
			T result = _self.GetComponent<T>();

			if (result)
				return result;

			return _self.gameObject.AddComponent<T>();
		}

		public static Component GetOrCreateComponent( this Component _self, Type t )
		{
			Component result = _self.GetComponent(t);

			if (result)
				return result;

			return _self.gameObject.AddComponent(t);
		}

		public static T GetOrCreateComponent<T>( this GameObject _self ) where T : Component
		{
			return GetOrCreateComponent<T>(_self.transform);
		}

		public static T GetOrCreateComponentOnChild<T>( this Component _self, GameObject _parent, string _childName ) where T : Component
		{
			Transform child = _self.transform.Find(_childName);
			if (child == null)
			{
				GameObject go = new GameObject(_childName);
				go.transform.SetParent(_parent.transform);
				LayoutElement le = go.AddComponent<LayoutElement>();
				le.ignoreLayout = true;
				child = go.transform;
			}

			return child.GetOrCreateComponent<T>();
		}


		public static T CopyTo<T>( this Component _self, T _other, HashSet<string> _excludedMembers = null, HashSet<Type> _excludedTypes = null ) where T : Component
		{
			return (T)InternalCopyTo(_self, _other, _excludedMembers, _excludedTypes);
		}

		private static Component InternalCopyTo( this Component _self, Component _other, HashSet<string> _excludedMembers = null, HashSet<Type> _excludedTypes = null )
		{
			if (_excludedMembers == null)
				_excludedMembers = s_excludedMembers;
			else
				_excludedMembers.UnionWith(s_excludedMembers);

			if (_excludedTypes == null)
				_excludedTypes = s_excludedTypes;
			else
				_excludedTypes.UnionWith(s_excludedTypes);

			Type type = _other.GetType();

			while (!_excludedTypes.Contains(type))
			{
				//UiLog.Log($"type: {type}");
				_other.GetInstanceID();
				BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;

				FieldInfo[] finfos = type.GetFields(flags);
				foreach (var finfo in finfos)
				{
					if (!_excludedMembers.Contains(finfo.Name))
					{
						try
						{
							//UiLog.Log($"finfo {finfo.Name}");
							finfo.SetValue(_other, finfo.GetValue(_self));
						}
						catch { }
					}
				}

				PropertyInfo[] pinfos = type.GetProperties(flags);
				foreach (var pinfo in pinfos)
				{
					if (!_excludedMembers.Contains(pinfo.Name) && pinfo.CanWrite)
					{
						try
						{
							//UiLog.Log($"pinfo {pinfo.Name}");
							pinfo.SetValue(_other, pinfo.GetValue(_self, null), null);
						}
						catch { }
					}
				}

				type = type.BaseType;
			}

			return _other;
		}

		public static T CloneTo<T>( this Component _self, GameObject _to, bool _alsoIfExists = false, HashSet<string> _excludedMembers = null, HashSet<Type> _excludedTypes = null ) where T : Component
		{
			return CloneTo(_self, _to, _alsoIfExists, _excludedMembers, _excludedTypes) as T;
		}

		public static Component CloneTo( this Component _self, GameObject _to, bool _alsoIfExists = false, HashSet<string> _excludedMembers = null, HashSet<Type> _excludedTypes = null )
		{
			Component componentOnTo = _to.GetComponent(_self.GetType());
			if (componentOnTo && !_alsoIfExists)
				return componentOnTo;

			if (!componentOnTo)
				componentOnTo = _to.AddComponent(_self.GetType());

			Component result = _self.InternalCopyTo(componentOnTo, _excludedMembers, _excludedTypes);

			return result;
		}

		public static void CloneAllComponents( GameObject _from, GameObject _to, bool _alsoIfExists = false, HashSet<string> _excludedMembers = null, HashSet<Type> _excludedTypes = null )
		{
			List<Component> components = new();
			_from.GetComponents(components);
			foreach (var component in components)
			{
				CloneTo(component, _to, _alsoIfExists, _excludedMembers, _excludedTypes);
			}
		}

		public static void GetChildren( this Transform _this, ICollection<Transform> _list )
		{
			_list.Clear();
			foreach (Transform child in _this)
				_list.Add(child);
		}

		public static List<Transform> GetChildrenList( this Transform _this )
		{
			List<Transform> result = new List<Transform>();
			GetChildren(_this, result);
			return result;
		}

		public static Transform[] GetChildrenArray( this Transform _this )
		{
			GetChildren(_this, s_tempTransformList);
			return s_tempTransformList.ToArray();
		}

		public static void DestroyAllChildren( this Transform _self, bool _includeHidden = true, bool _supportUndoIfPossible = true, Func<Transform, bool> _filter = null )
		{
			var childCount = _self.childCount;
			for (int i = childCount - 1; i >= 0; --i)
			{
				var child = _self.GetChild(i).gameObject;
				if (!child.activeSelf && !_includeHidden)
					continue;

				if (_filter != null)
				{
					if (!_filter.Invoke(child.transform))
						continue;
				}

#if UNITY_EDITOR
				int check = _self.childCount;
				string childName = child.name;
#endif

				child.SafeDestroy(_supportUndoIfPossible);

#if UNITY_EDITOR
				if (check == _self.childCount)
				{
					UiLog.LogError($"Game Object '{childName}' not properly destroyed!");
				}
#endif
			}
		}

		public static void DestroyAllChildren( this GameObject _self, bool _includeHidden = true, bool _supportUndoIfPossible = true, Func<Transform, bool> _filter = null )
		{
			DestroyAllChildren(_self.transform, _includeHidden, _supportUndoIfPossible, _filter);
		}

		public static Vector3 GetComponentWiseDivideBy( this Vector3 _dividend, Vector3 _divisor )
		{
			return new Vector3
			(
				_dividend.x / _divisor.x,
				_dividend.y / _divisor.y,
				_dividend.z / _divisor.z
			);
		}

		public static Vector3 GetComponentWiseMultiply( this Vector3 _factor1, Vector3 _factor2 )
		{
			return new Vector3
			(
				_factor1.x * _factor2.x,
				_factor1.y * _factor2.y,
				_factor1.z * _factor2.z
			);
		}

		public static Vector3 GetScaled( this Vector3 _factor1, Vector3 _factor2 ) => GetComponentWiseMultiply(_factor1, _factor2);

		/// <summary>
		/// Quaternion.eulerAngles always returns a positive degree.
		/// This function fixes this by returning negative degrees for degrees over 180.
		/// This is sometimes necessary for interpolating.
		/// </summary>
		/// <param name="_val"></param>
		/// <returns></returns>
		public static float GetMinusDegrees( this float _val )
		{
			if (_val > 180.0f)
				return _val - 360;

			return _val;
		}

		public static bool IsEven( this int _self ) => (_self & 1) == 0;
		public static bool IsOdd( this int _self ) => (_self & 1) != 0;

		/// <summary>
		/// Clone a single game object without its children.
		/// Horribly inefficient, but afaik the only way;
		/// Detaching children temporarily doesn't work for persistent objects
		/// </summary>
		/// <param name="_gameObject"></param>
		/// <param name="_newParent"></param>
		/// <returns></returns>
		public static GameObject CloneWithoutChildren( this GameObject _gameObject, Transform _newParent = null )
		{
			GameObject result = Object.Instantiate(_gameObject, _newParent, false);
			result.DestroyAllChildren();
			return result;
		}

		public static GameObject GetRoot( this GameObject _self )
		{
			if (!_self)
				return null;

			return _self.transform.root.gameObject;
		}

		public static T ShallowClone<T>( this T _this )
		{
			var inst = _this.GetType().GetMethod("MemberwiseClone", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
			return (T)inst?.Invoke(_this, null);
		}

		private class CloneHelper : ScriptableObject
		{
			public UnityEngine.Object m_unityObject;
			[SerializeReference] public object m_object;
		}

		public static T DeepClone<T>( this T _source )
		{
			var unityObject = _source as UnityEngine.Object;
			bool isUnityObject = unityObject != null;

			var helper = ScriptableObject.CreateInstance<CloneHelper>();
			if (isUnityObject)
				helper.m_unityObject = unityObject;
			else
				helper.m_object = _source;

			var clone = UnityEngine.Object.Instantiate(helper);
			var result = isUnityObject ? clone.m_unityObject : clone.m_object;

			helper.SafeDestroy();
			clone.SafeDestroy();
			return (T)result;
		}

		public static Transform FindDescendantByName( this Transform _self, string _name, bool _includeSelf = false )
		{
			if (_includeSelf && _self.name == _name)
				return _self;

			foreach (Transform child in _self)
			{
				var found = FindDescendantByName(child, _name, true);
				if (found)
					return found;
			}

			return null;
		}

		public static GameObject FindDescendantByName( this GameObject _self, string _name, bool _includeSelf = false )
		{
			var result = FindDescendantByName(_self.transform, _name, _includeSelf);
			return result ? result.gameObject : null;
		}

		public static bool IsMyDescendant( this Transform _self, Transform _potentialDescendant, bool _includeSelf = false )
		{
			if (_includeSelf && _potentialDescendant == _self)
				return true;

			return IsMyDescendantInternal(_self, _potentialDescendant);
		}

		private static bool IsMyDescendantInternal( Transform _self, Transform _potentialDescendant )
		{
			for (var p = _potentialDescendant.parent; p != null; p = p.parent)
			{
				if (p == _self)
					return true;
			}

			return false;
		}

		public static bool IsInRange( this RangeInt _self, int _value, bool _excludeMin = false, bool _excludeMax = false )
		{
			if (_excludeMin && _value <= _self.start || _value < _self.start)
				return false;

			if (_excludeMax && _value >= _self.end || _value > _self.end)
				return false;

			return true;
		}

		public static bool CompareTagEx( this GameObject _go, string _tag )
		{
			if (!_go)
				return false;

			if (_go.TryGetComponent(typeof(AdditionalTags), out Component additionalTags))
				return ((AdditionalTags)additionalTags).CompareTag(_tag);

			return _go.CompareTag(_tag);
		}

		public static Transform FindDescendantWithTag( this Transform _parent, string _tag, bool _includeInactive = false )
		{
			if (_parent == null) 
				return null;
			
			if (!_includeInactive && !_parent.gameObject.activeInHierarchy)
				return null;
			
			if (_parent.CompareTag(_tag))
				return _parent;
			
			foreach (Transform child in _parent)
			{
				var result = child.FindDescendantWithTag(_tag, _includeInactive);
				if (result != null)
					return result;
			}
			
			return null;
		}
		
		public static GameObject FindDescendantWithTag(this GameObject _parent, string _tag, bool _includeInactive = false )
		{
			var found = _parent.transform.FindDescendantWithTag(_tag, _includeInactive);
			return found != null ? found.gameObject : null;
		}
		
		public static bool CompareTagEx( this Transform _transform, string _tag ) => _transform && CompareTagEx(_transform.gameObject, _tag);

		/// <summary>
		/// Returns a random long from min (inclusive) to max (exclusive)
		/// </summary>
		/// <param name="random">The given random instance</param>
		/// <param name="min">The inclusive minimum bound</param>
		/// <param name="max">The exclusive maximum bound.  Must be greater than min</param>
		public static long NextLong( this System.Random random, long min, long max )
		{
			if (max <= min)
				throw new ArgumentOutOfRangeException("max", "max must be > min!");

			//Working with ulong so that modulo works correctly with values > long.MaxValue
			ulong uRange = (ulong)(max - min);

			//Prevent a modolo bias; see https://stackoverflow.com/a/10984975/238419
			//for more information.
			//In the worst case, the expected number of calls is 2 (though usually it's
			//much closer to 1) so this loop doesn't really hurt performance at all.
			ulong ulongRand;
			do
			{
				byte[] buf = new byte[8];
				random.NextBytes(buf);
				ulongRand = (ulong)BitConverter.ToInt64(buf, 0);
			} while (ulongRand > ulong.MaxValue - ((ulong.MaxValue % uRange) + 1) % uRange);

			return (long)(ulongRand % uRange) + min;
		}

		/// <summary>
		/// Returns a random long from 0 (inclusive) to max (exclusive)
		/// </summary>
		/// <param name="random">The given random instance</param>
		/// <param name="max">The exclusive maximum bound.  Must be greater than 0</param>
		public static long NextLong( this System.Random random, long max )
		{
			return random.NextLong(0, max);
		}

		/// <summary>
		/// Returns a random long over all possible values of long (except long.MaxValue, similar to
		/// random.Next())
		/// </summary>
		/// <param name="random">The given random instance</param>
		public static long NextLong( this System.Random random )
		{
			return random.NextLong(long.MinValue, long.MaxValue);
		}

		public static void SetPasswordDisplay
		(
			this InputField _inputField,
			bool _isPassword,
			InputField.ContentType _nonPasswordType = InputField.ContentType.Standard,
			bool _activate = true
		)
		{
			_inputField.contentType = _isPassword ?
				InputField.ContentType.Password :
				_nonPasswordType;

			if (_activate)
				_inputField.ActivateInputField();
		}

		public static bool GetPasswordDisplay( this InputField _inputField ) => _inputField.contentType == InputField.ContentType.Password;

		public static void InvokeDelayed( this Action _action )
		{
			if (_action == null)
				return;

			CoRoutineRunner.Instance.StartCoroutine(InvokeActionDelayedCoroutine(_action));
		}

		private static IEnumerator InvokeActionDelayedCoroutine( Action _action )
		{
			yield return null;
			_action?.Invoke();
		}
	}



	public static class GameObjectHelper
	{
		// Caution! Guaranteed _to be extremely slow when include inactive.
		// Use only if unavoidable and use only once and cache!
		public static GameObject Find( string _name, bool _includeInactive = false )
		{
			GameObject go = GameObject.Find(_name);
			if (go != null || !_includeInactive)
				return go;

			var transforms = Resources.FindObjectsOfTypeAll<Transform>();
			foreach (var t in transforms)
				if (t.gameObject.name == _name)
					return t.gameObject;

			return null;
		}
	}

	public static class ArrayHelper
	{
		public static void Append<T>( ref T[] _array, T[] _toAppend )
		{
			int len = _array.Length;

			Array.Resize(ref _array, _array.Length + _toAppend.Length);

			for (int i = len; i < _array.Length; i++)
			{
				_array[i] = _toAppend[i - len];
			}
		}

		public static void Append<T>( ref T[] _array, T _toAppend )
		{
			Append<T>(ref _array, new T[] { _toAppend });
		}
	}

	public static class EnumHelper
	{
		public static IEnumerable<T> GetValues<T>()
		{
			return Enum.GetValues(typeof(T)).Cast<T>();
		}

		public static IEnumerable<T> GetValues<T>( out int _numItems )
		{
			var arr = Enum.GetValues(typeof(T));
			_numItems = arr.Length;
			return arr.Cast<T>();
		}
	}
}
