using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	public static class Extensions
	{
		private static readonly Vector3[] s_worldCorners = new Vector3[4];

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
			GameObject.Destroy(transform.gameObject);
#endif
			return _this;
		}

		public static Transform Clear( this Transform _this )
		{
			foreach (Transform child in _this)
				child.Destroy();
			return _this;
		}

		public static BaseMeshEffect SetDirty( this BaseMeshEffect _this)
		{
			_this.GetComponent<Graphic>()?.SetVerticesDirty();
			return _this;
		}

		public static List<T> ToList<T>( this HashSet<T> _this )
		{
			List<T> result = new List<T>(_this.Count);
			foreach( T elem in _this )
				result.Add(elem);

			return result;
		}

		public static List<T> ToList<T>( this SortedSet<T> _this )
		{
			List<T> result = new List<T>(_this.Count);
			foreach( T elem in _this )
				result.Add(elem);

			return result;
		}

		public static Rect GetWorldRect2D( this RectTransform _this )
		{
			_this.GetWorldCorners( s_worldCorners );
			float x = s_worldCorners[1].x;
			float y = s_worldCorners[1].y;
			float w = s_worldCorners[2].x - x;
			float h = s_worldCorners[3].y - y;
			return new Rect( x,y,w,h );
		}

		public static Vector2 GetWorldPosition2D( this RectTransform _this )
		{
			_this.GetWorldCorners( s_worldCorners );
			float x = s_worldCorners[1].x;
			float y = s_worldCorners[1].y;
			return new Vector2( x,y );
		}

		public static Vector2 GetWorldCenter2D( this RectTransform _this )
		{
			Rect rt = GetWorldRect2D( _this );
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