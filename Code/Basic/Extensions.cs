using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	public static class Extensions
	{
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
			Graphic graphic = _this.GetComponent<Graphic>();
			if (graphic)
				graphic.SetVerticesDirty();
			return _this;
		}

		public static float Bilerp(float v0, float v1, float v2, float v3, float a, float b)
		{
			float top = Mathf.Lerp(v1, v2, a);
			float bottom = Mathf.Lerp(v0, v3, a);
			return Mathf.Lerp(bottom, top, b);
		}

		public static Vector2 Bilerp(Vector2 v0, Vector2 v1, Vector2 v2, Vector2 v3, float a, float b)
		{
			Vector2 top = Vector2.Lerp(v1, v2, a);
			Vector2 bottom = Vector2.Lerp(v0, v3, a);
			return Vector2.Lerp(bottom, top, b);
		}

		public static Vector3 Bilerp(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3, float a, float b)
		{
			Vector3 top = Vector3.Lerp(v1, v2, a);
			Vector3 bottom = Vector3.Lerp(v0, v3, a);
			return Vector3.Lerp(bottom, top, b);
		}

		public static Vector4 Bilerp(Vector4 v0, Vector4 v1, Vector4 v2, Vector4 v3, float a, float b)
		{
			Vector4 top = Vector4.Lerp(v1, v2, a);
			Vector4 bottom = Vector4.Lerp(v0, v3, a);
			return Vector4.Lerp(bottom, top, b);
		}

		public static Color Bilerp(Color v0, Color v1, Color v2, Color v3, float a, float b)
		{
			Color top = Color.Lerp(v1, v2, a);
			Color bottom = Color.Lerp(v0, v3, a);
			return Color.Lerp(bottom, top, b);
		}

	}
}