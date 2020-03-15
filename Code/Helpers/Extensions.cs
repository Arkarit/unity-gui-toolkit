using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	public static class Extensions
	{
		public const BindingFlags DEFAULT_COPY_FLAGS = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Default | BindingFlags.DeclaredOnly;

		// From http://answers.unity.com/answers/641022/view.html
		public static T CopyValuesFrom<T>( this T _this, T _other, BindingFlags _bindingFlags = DEFAULT_COPY_FLAGS ) where T : Component
		{
			Type type = _this.GetType();
			if (type != _other.GetType())
				return null; // type mis-match

			PropertyInfo[] propertyInfos = type.GetProperties(_bindingFlags);

			foreach (var propertyInfo in propertyInfos)
			{
				if (propertyInfo.CanWrite)
				{
					try
					{
						propertyInfo.SetValue(_this, propertyInfo.GetValue(_other, null), null);
					}
					catch { } // In case of NotImplementedException being thrown. For some reason specifying that exception didn't seem to catch it, so I didn't catch anything specific.
				}
			}

			FieldInfo[] fieldInfos = type.GetFields(_bindingFlags);
			foreach (var fieldInfo in fieldInfos)
			{
				fieldInfo.SetValue(_this, fieldInfo.GetValue(_other));
			}

			if (_this is MonoBehaviour thisMonoBehaviour)
				thisMonoBehaviour.SetDirty();

			return _this as T;
		}

		public static T CopyValuesFrom<T>( this T _this, ComponentMemberInfo _memberInfo, BindingFlags _bindingFlags = DEFAULT_COPY_FLAGS ) where T : Component
		{
			Type type = _this.GetType();
			if (_memberInfo.m_component == null || type != _memberInfo.m_component.GetType())
				return null;

			Component other = _memberInfo.m_component;

			for (int i=0; i<_memberInfo.Count; i++)
			{
				if (_memberInfo.m_isProperty[i])
				{
					PropertyInfo propertyInfo = type.GetProperty(_memberInfo.m_names[i], _bindingFlags);
					if (propertyInfo == null)
						continue;
					propertyInfo.SetValue(_this, propertyInfo.GetValue(other, null), null);
				}
				else
				{
					FieldInfo fieldInfo = type.GetField(_memberInfo.m_names[i], _bindingFlags);
					if (fieldInfo == null)
						continue;

					fieldInfo.SetValue(_this, fieldInfo.GetValue(other));
				}
			}

			if (_this is MonoBehaviour thisMonoBehaviour)
				thisMonoBehaviour.SetDirty();

			return _this as T;
		}


#if UNITY_EDITOR
		public static EditorComponentMemberInfo GetEditorComponentMemberInfo<T>( this T _this, BindingFlags _bindingFlags = DEFAULT_COPY_FLAGS ) where T : Component
		{
			Type type = _this.GetType();
			PropertyInfo[] propertyInfos = type.GetProperties(_bindingFlags);
			FieldInfo[] fieldInfos = type.GetFields(_bindingFlags);
			return new EditorComponentMemberInfo(_this, propertyInfos, fieldInfos);
		}
#endif

		// Note: This is not performing very good and - worse - creates GC allocations.
		// Better cache the values in simple bools after using this to evaluate at least in runtime code. 
		public static bool HasFlags<T>( this T _this, T _flags) where T : Enum
		{
			return ((int)(object)_this & (int)(object)_flags) != 0;
		}

		public static IList<T> Clone<T>( this IList<T> _listToClone ) where T : ICloneable
		{
			return _listToClone.Select(item => (T)item.Clone()).ToList();
		}

		public static Transform Destroy( this Transform _this )
		{
#if UNITY_EDITOR
			UnityEditor.EditorApplication.delayCall += () =>
			{
				if (_this && _this.gameObject)
					GameObject.DestroyImmediate(_this.gameObject);
			};
#else
			GameObject.Destroy(_this.gameObject);
#endif
			return _this;
		}

		public static Transform Clear( this Transform _this )
		{
			foreach (Transform child in _this)
				child.Destroy();
			return _this;
		}

		public static List<T> ToList<T>( this HashSet<T> _this )
		{
			List<T> result = new List<T>(_this.Count);
			foreach( T elem in _this )
				result.Add(elem);

			return result;
		}

		public static void SetDirty( this MonoBehaviour _this )
		{
			// enabling and disabling the MonoBehaviour makes it dirty.
			_this.enabled = !_this.enabled;
			_this.enabled = !_this.enabled;
		}

		public static List<T> ToList<T>( this SortedSet<T> _this )
		{
			List<T> result = new List<T>(_this.Count);
			foreach( T elem in _this )
				result.Add(elem);

			return result;
		}

		public static Rect GetWorldRect2D( this Rect _this, RectTransform _rt)
		{
			Vector2 tl = _rt.TransformPoint( _this.TopLeft() );
			Vector2 br = _rt.TransformPoint( _this.BottomRight() );
			float x = tl.x;
			float y = tl.y;
			float w = br.x - x;
			float h = br.y - y;
			
			return new Rect( x,y,w,h );
		}

		public static Vector2[] GetWorldCorners2D( this Rect _this, RectTransform _rt)
		{
			return new Vector2[] {
				_rt.TransformPoint(_this.BottomLeft()),
				_rt.TransformPoint(_this.TopLeft()),
				_rt.TransformPoint(_this.TopRight()),
				_rt.TransformPoint(_this.BottomRight()),
			};
		}

		public static Vector3[] GetWorldCorners( this Rect _this, RectTransform _rt)
		{
			return new Vector3[] {
				_rt.TransformPoint(_this.BottomLeft()),
				_rt.TransformPoint(_this.TopLeft()),
				_rt.TransformPoint(_this.TopRight()),
				_rt.TransformPoint(_this.BottomRight()),
			};
		}

		public static Rect GetWorldRect2D( this RectTransform _this )
		{
			return _this.rect.GetWorldRect2D(_this);
		}

		public static Rect GetWorldRect2D( this RectTransform _this, Canvas _canvas )
		{
			Rect result = GetWorldRect2D( _this );
			result = result.ScaleBy( 1.0f / _canvas.scaleFactor );
			return result;
		}

		public static Rect GetWorldRect2D( this Rect _this, RectTransform _rt, Canvas _canvas)
		{
			Rect result = GetWorldRect2D( _this, _rt );
			result = result.ScaleBy( 1.0f / _canvas.scaleFactor );
			return result;
		}

		public static (Rect rect, Vector2 offset) BringToCenter( this Rect _this )
		{
			Vector2 offset = -_this.center;
			Rect rect = _this;
			rect.center = Vector2.zero;
			return (rect, offset);
		}

		public static Rect Absolute( this Rect _this)
		{
			Rect result = _this;
			if (_this.height < 0 || _this.width < 0)
			{
				Vector2 min = result.min;
				Vector2 max = result.max;
				float t;

				if (_this.width < 0)
				{
					t = min.x;
					min.x = max.x;
					max.x = t;
				}
				if (_this.height < 0)
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

		public static Vector3 Size3( this Rect _this)
		{
			return _this.size;
		}

		public static Rect GetWorldRect2D( this RectTransform _this, Vector3[] _worldRectPositions)
		{
			Debug.Assert( _worldRectPositions != null && _worldRectPositions.Length == 4, "Needs to be world corners array as provided by RectTransform..GetWorldCorners()");
			if (_worldRectPositions == null || _worldRectPositions.Length != 4)
				return new Rect();

			return new Rect(_worldRectPositions[1].x, _worldRectPositions[1].y, _worldRectPositions[3].x - _worldRectPositions[0].x, _worldRectPositions[1].y - _worldRectPositions[0].y);
		}

		public static Vector2 GetWorldPosition2D( this RectTransform _this )
		{
			return _this.TransformPoint( _this.rect.TopLeft() );
		}

		public static Vector2 GetWorldPosition2D( this RectTransform _this, Canvas _canvas )
		{
			Vector2 result = _this.TransformPoint( _this.rect.TopLeft() );
			result = result * (1.0f / _canvas.scaleFactor);
			return result;
		}

		public static Vector2 GetWorldCenter2D( this RectTransform _this )
		{
			Rect rt = GetWorldRect2D( _this );
			return rt.center;
		}

		public static Vector2 GetWorldCenter2D( this RectTransform _this, Canvas _canvas )
		{
			Rect rt = GetWorldRect2D( _this, _canvas );
			return rt.center;
		}

		public static Vector2 TopLeft( this Rect _this )
		{
			return new Vector2(_this.xMin, _this.yMax );
		}
		public static Vector2 BottomLeft( this Rect _this )
		{
			return new Vector2(_this.xMin, _this.yMin );
		}
		public static Vector2 TopRight( this Rect _this )
		{
			return new Vector2(_this.xMax, _this.yMax );
		}
		public static Vector2 BottomRight( this Rect _this )
		{
			return new Vector2(_this.xMax, _this.yMin );
		}

		public static Vector3 TopLeft3( this Rect _this )
		{
			return new Vector3(_this.xMin, _this.yMax );
		}
		public static Vector3 BottomLeft3( this Rect _this )
		{
			return new Vector3(_this.xMin, _this.yMin );
		}
		public static Vector3 TopRight3( this Rect _this )
		{
			return new Vector3(_this.xMax, _this.yMax );
		}
		public static Vector3 BottomRight3( this Rect _this )
		{
			return new Vector3(_this.xMax, _this.yMin );
		}

		public static Rect ScaleBy( this Rect _this, float _val )
		{
			_this.x *= _val;
			_this.y *= _val;
			_this.width *= _val;
			_this.height *= _val;
			return _this;
		}

		public static Vector2 Xy( this Vector3 _this )
		{
			return new Vector2(_this.x, _this.y);
		}
		public static Vector2 Xz( this Vector3 _this )
		{
			return new Vector2(_this.x, _this.z);
		}
		public static Vector2 Yz( this Vector3 _this )
		{
			return new Vector2(_this.y, _this.z);
		}

		public static Vector2 Swap( this Vector2 _this )
		{
			float t = _this.x;
			_this.x = _this.y;
			_this.y = t;
			return _this;
		}

		public static bool IsFlagSet<T>( this T _this, T _flag ) where T : Enum
		{
			return ((int) (object) _this & (int) (object) _flag) != 0;
		}

	}
}