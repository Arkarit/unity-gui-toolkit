using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GuiToolkit
{
	public static class Extensions
	{
		public static IList<T> Clone<T>( this IList<T> listToClone ) where T : ICloneable
		{
			return listToClone.Select(item => (T)item.Clone()).ToList();
		}

		public static Transform Destroy( this Transform transform )
		{
#if UNITY_EDITOR
			UnityEditor.EditorApplication.delayCall += () =>
			{
				if (transform && transform.gameObject)
					GameObject.DestroyImmediate(transform.gameObject);
			};
#else
			GameObject.Destroy(transform.gameObject);
#endif
			return transform;
		}

		public static Transform Clear( this Transform transform )
		{
			foreach (Transform child in transform)
				child.Destroy();
			return transform;
		}
	}
}