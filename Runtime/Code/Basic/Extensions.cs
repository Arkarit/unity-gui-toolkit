using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GuiToolkit
{
	public static class Extensions
	{
		private static readonly HashSet<string> s_excludedMembers = new HashSet<string> {"name", "_parent", "parentInternal"};
		private static readonly HashSet<Type> s_excludedTypes = new HashSet<Type> {typeof(Component), typeof(Transform), typeof(MonoBehaviour)};

		// Note: This is not super performant and - worse - creates GC allocations
		// Better cache the values in simple bools after evaluating at least in runtime code. 
		public static bool HasFlags<T>( this T _self, T _flags) where T : Enum
		{
			return ((int)(object)_self & (int)(object)_flags) != 0;
		}

		public static IList<T> Clone<T>( this IList<T> _listToClone ) where T : ICloneable
		{
			return _listToClone.Select(item => (T)item.Clone()).ToList();
		}

		public static string GetPath(this Transform _self, char _separator = '/')
		{
			if (_self == null)
				return "<null>";
			
			string result = _self.name;
			while (_self.parent != null)
			{
				_self = _self.parent;
				result = _self.name + _separator + result;
			}
			return result;
		}

		public static void GetComponentsInDirectChildren<T>(this Transform _self, List<T> _list) where T : Component
		{
			_list.Clear();

			foreach (Transform child in _self)
			{
				T component = child.GetComponent<T>();
				if (component)
					_list.Add(component);
			}
		}

		public static void GetComponentsInDirectChildren<T>(this GameObject _self, List<T> _list) where T : Component =>
			GetComponentsInDirectChildren<T>(_self.transform, _list);

		public static List<T> GetComponentsInDirectChildren<T>(this Transform _self) where T : Component
		{
			List<T> result = new();
			GetComponentsInDirectChildren<T>(_self, result);
			return result;
		}

		public static List<T> GetComponentsInDirectChildren<T>(this GameObject _self) where T : Component =>
			GetComponentsInDirectChildren<T>(_self.transform);
		public static string GetPath(this GameObject _self)
		{
			if (_self == null)
				return "<null>";
			
			return GetPath(_self.transform);
		}

		public static string GetPath(this Component _self)
		{
			if (_self == null)
				return "<null>";
			
			return GetPath(_self.transform);
		}

		public static void SafeDestroy( this UnityEngine.Object _self, bool _supportUndoIfPossible = true )
		{
			if (_self == null)
				return;

#if UNITY_EDITOR
			if (_supportUndoIfPossible && !Application.isPlaying)
			{
				Undo.DestroyObjectImmediate(_self);
				return;
			}
#endif

			// We want the object _to be immediately detached _from its _parent if it is a game object
			GameObject go = _self as GameObject;
			if (go)
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
				UnityEngine.Object.Destroy(_self);
				return;
			}

			UnityEditor.EditorApplication.delayCall += () =>
			{
				UnityEngine.Object.DestroyImmediate(_self);
			};
#else
			UnityEngine.Object.Destroy(_self);
#endif
		}

		public static List<T> ToList<T>( this HashSet<T> _self )
		{
			List<T> result = new List<T>(_self.Count);
			foreach( T elem in _self )
				result.Add(elem);

			return result;
		}

		public static List<T> ToList<T>( this SortedSet<T> _self )
		{
			List<T> result = new List<T>(_self.Count);
			foreach( T elem in _self )
				result.Add(elem);

			return result;
		}

		public static Rect GetWorldRect2D( this Rect _self, RectTransform _rt)
		{
			Vector2 tl = _rt.TransformPoint( _self.TopLeft() );
			Vector2 br = _rt.TransformPoint( _self.BottomRight() );
			float x = tl.x;
			float y = tl.y;
			float w = br.x - x;
			float h = br.y - y;
			
			return new Rect( x,y,w,h );
		}

		public static Vector2[] GetWorldCorners2D( this Rect _self, RectTransform _rt)
		{
			return new Vector2[] {
				_rt.TransformPoint(_self.BottomLeft()),
				_rt.TransformPoint(_self.TopLeft()),
				_rt.TransformPoint(_self.TopRight()),
				_rt.TransformPoint(_self.BottomRight()),
			};
		}

		public static Vector3[] GetWorldCorners( this Rect _self, RectTransform _rt)
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
			Rect result = GetWorldRect2D( _self );
			result = result.ScaleBy( 1.0f / _canvas.scaleFactor );
			return result;
		}

		public static Rect GetWorldRect2D( this Rect _self, RectTransform _rt, Canvas _canvas)
		{
			Rect result = GetWorldRect2D( _self, _rt );
			result = result.ScaleBy( 1.0f / _canvas.scaleFactor );
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

		public static Vector3 Size3( this Rect _self)
		{
			return _self.size;
		}

		public static Rect GetWorldRect2D( this RectTransform _self, Vector3[] _worldRectPositions)
		{
			Debug.Assert( _worldRectPositions != null && _worldRectPositions.Length == 4, "Needs _to be world corners _array as provided by RectTransform..GetWorldCorners()");
			if (_worldRectPositions == null || _worldRectPositions.Length != 4)
				return new Rect();

			return new Rect(_worldRectPositions[1].x, _worldRectPositions[1].y, _worldRectPositions[3].x - _worldRectPositions[0].x, _worldRectPositions[1].y - _worldRectPositions[0].y);
		}

		public static Vector2 GetWorldPosition2D( this RectTransform _self )
		{
			return _self.TransformPoint( _self.rect.TopLeft() );
		}

		public static Vector2 GetWorldPosition2D( this RectTransform _self, Canvas _canvas )
		{
			Vector2 result = _self.TransformPoint( _self.rect.TopLeft() );
			result = result * (1.0f / _canvas.scaleFactor);
			return result;
		}

		public static Vector2 GetWorldCenter2D( this RectTransform _self )
		{
			Rect _rt = GetWorldRect2D( _self );
			return _rt.center;
		}

		public static Vector2 GetWorldCenter2D( this RectTransform _self, Canvas _canvas )
		{
			Rect _rt = GetWorldRect2D( _self, _canvas );
			return _rt.center;
		}

		public static Vector2 TopLeft( this Rect _self )
		{
			return new Vector2(_self.xMin, _self.yMax );
		}
		public static Vector2 BottomLeft( this Rect _self )
		{
			return new Vector2(_self.xMin, _self.yMin );
		}
		public static Vector2 TopRight( this Rect _self )
		{
			return new Vector2(_self.xMax, _self.yMax );
		}
		public static Vector2 BottomRight( this Rect _self )
		{
			return new Vector2(_self.xMax, _self.yMin );
		}

		public static Vector3 TopLeft3( this Rect _self )
		{
			return new Vector3(_self.xMin, _self.yMax );
		}
		public static Vector3 BottomLeft3( this Rect _self )
		{
			return new Vector3(_self.xMin, _self.yMin );
		}
		public static Vector3 TopRight3( this Rect _self )
		{
			return new Vector3(_self.xMax, _self.yMax );
		}
		public static Vector3 BottomRight3( this Rect _self )
		{
			return new Vector3(_self.xMax, _self.yMin );
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

		public static bool IsFlagSet<T>( this T _self, T _flag ) where T : Enum
		{
			return ((int) (object) _self & (int) (object) _flag) != 0;
		}

		public static bool IsSimilar( this Color _self, Color _other )
		{
			return
				   Mathf.Approximately(_self.r, _other.r)
				&& Mathf.Approximately(_self.g, _other.g)
				&& Mathf.Approximately(_self.b, _other.b)
				&& Mathf.Approximately(_self.a, _other.a);
		}

		public static void ScrollToTop(this ScrollRect _self, MonoBehaviour _coroutineHolder = null)
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

		private static IEnumerator ScrollToTopDelayed(ScrollRect _scrollRect)
		{
			yield return 0;
			_scrollRect.ScrollToTop();
		}

		public static void ForceRefresh(this MonoBehaviour _self)
		{
			if (!_self.enabled || !_self.gameObject.activeInHierarchy)
				return;

			_self.enabled = false;
			// ReSharper disable once Unity.InefficientPropertyAccess
			_self.enabled = true;
		}

		public static T GetInParentOrCreateComponent<T>(this Component _self) where T : Component
		{
			T result = _self.GetComponentInParent<T>();

			if (result)
				return result;

			return _self.gameObject.AddComponent<T>();
		}

		public static T GetOrCreateComponent<T>(this Component _self) where T : Component
		{
			T result = _self.GetComponent<T>();

			if (result)
				return result;

			return _self.gameObject.AddComponent<T>();
		}

		public static Component GetOrCreateComponent(this Component _self, Type t)
		{
			Component result = _self.GetComponent(t);

			if (result)
				return result;

			return _self.gameObject.AddComponent(t);
		}

		public static T GetOrCreateComponentOnChild<T>(this Component _self, GameObject _parent, string _childName ) where T : Component
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
			return (T) InternalCopyTo(_self, _other, _excludedMembers, _excludedTypes);
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
				//Debug.Log($"type: {type}");
				_other.GetInstanceID();
				BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
				
				FieldInfo[] finfos = type.GetFields(flags);
				foreach (var finfo in finfos)
				{
					if (!_excludedMembers.Contains(finfo.Name))
					{
						try
						{
							//Debug.Log($"finfo {finfo.Name}");
							finfo.SetValue(_other, finfo.GetValue(_self));
						} catch {}
					}
				}

				PropertyInfo[] pinfos = type.GetProperties(flags);
				foreach (var pinfo in pinfos)
				{
					if (!_excludedMembers.Contains(pinfo.Name) && pinfo.CanWrite)
					{
						try
						{
							//Debug.Log($"pinfo {pinfo.Name}");
							pinfo.SetValue(_other, pinfo.GetValue(_self, null), null);
						}
						catch {}
					}
				}

				type = type.BaseType;
			}

			return _other;
		}

		public static T CloneTo<T>(this Component _self, GameObject _to, bool _alsoIfExists = false, HashSet<string> _excludedMembers = null, HashSet<Type> _excludedTypes = null ) where T : Component
		{
			return CloneTo(_self, _to, _alsoIfExists, _excludedMembers, _excludedTypes) as T;
		}

		public static Component CloneTo(this Component _self, GameObject _to, bool _alsoIfExists = false, HashSet<string> _excludedMembers = null, HashSet<Type> _excludedTypes = null )
		{
			Component componentOnTo = _to.GetComponent(_self.GetType());
			if (componentOnTo && !_alsoIfExists)
				return componentOnTo;

			if (!componentOnTo)
				componentOnTo = _to.AddComponent(_self.GetType());

			Component result = _self.InternalCopyTo(componentOnTo, _excludedMembers, _excludedTypes);

			return result;
		}

		public static void CloneAllComponents(GameObject _from, GameObject _to, bool _alsoIfExists = false, HashSet<string> _excludedMembers = null, HashSet<Type> _excludedTypes = null)
		{
			List<Component> components = new();
			_from.GetComponents(components);
			foreach (var component in components)
			{
				CloneTo(component, _to, _alsoIfExists, _excludedMembers, _excludedTypes);
			}
		}
		
		
		public static Vector3 GetComponentWiseDivideBy(this Vector3 _dividend, Vector3 _divisor)
		{
			return new Vector3
			(
				_dividend.x / _divisor.x,
				_dividend.y / _divisor.y,
				_dividend.z / _divisor.z
			);
		}
		
		public static Vector3 GetComponentWiseMultiply(this Vector3 _factor1, Vector3 _factor2)
		{
			return new Vector3
			(
				_factor1.x * _factor2.x,
				_factor1.y * _factor2.y,
				_factor1.z * _factor2.z
			);
		}
		
		public static Vector3 GetScaled(this Vector3 _factor1, Vector3 _factor2) => GetComponentWiseMultiply(_factor1, _factor2);	
		
		/// <summary>
		/// Quaternion.eulerAngles always returns a positive degree.
		/// This function fixes this by returning negative degrees for degrees over 180.
		/// This is sometimes necessary for interpolating.
		/// </summary>
		/// <param name="_val"></param>
		/// <returns></returns>
		public static float GetMinusDegrees(this float _val)
		{
			if (_val > 180.0f)
				return _val - 360;
			
			return _val;
		}
	
		public static bool IsEven(this int _self) => _self % 2 == 0;
		public static bool IsOdd(this int _self) => _self % 2 != 0;
		
		public static GameObject GetRoot(this GameObject _self)
		{
			if (!_self)
				return null;

			return _self.transform.root.gameObject;
		}
		
		private class CloneHelper : ScriptableObject
		{
			public UnityEngine.Object m_unityObject;
			[SerializeReference] public object m_object;
		}

		public static T DeepClone<T>(this T _source)
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
			return (T) result;
		}
		
		public static Transform FindDescendantByName(this Transform _self, string _name, bool _includeSelf = false)
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

		public static GameObject FindDescendantByName(this GameObject _self, string _name, bool _includeSelf = false)
		{
			var result = FindDescendantByName(_self.transform, _name, _includeSelf);
			return result ? result.gameObject : null;
		}
	}



	public static class GameObjectHelper
	{
		// Caution! Guaranteed _to be extremely slow when include inactive.
		// Use only if unavoidable and use only once and cache!
		public static GameObject Find(string _name, bool _includeInactive = false)
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
		public static void Append<T>(ref T[] _array, T[] _toAppend)
		{
			int len = _array.Length;

			Array.Resize(ref _array, _array.Length + _toAppend.Length);

			for (int i=len; i<_array.Length; i++)
			{
				_array[i] = _toAppend[i-len];
			}
		}

		public static void Append<T>(ref T[] _array, T _toAppend)
		{
			Append<T>(ref _array, new T[] {_toAppend});
		}
	}

	public static class EnumHelper
	{
		public static IEnumerable<T> GetValues<T>()
		{
			return Enum.GetValues(typeof(T)).Cast<T>();
		}

		public static IEnumerable<T> GetValues<T>(out int _numItems)
		{
			var arr = Enum.GetValues(typeof(T));
			_numItems = arr.Length;
			return arr.Cast<T>();
		}
	}
}
