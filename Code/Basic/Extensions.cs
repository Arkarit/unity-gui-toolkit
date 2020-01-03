using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
	}
}