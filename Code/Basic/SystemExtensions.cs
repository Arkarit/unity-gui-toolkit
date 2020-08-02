using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class SystemExtensions
{
	// Support for Empty(), which is usually much faster than checking Count == 0
	public static bool Empty<T>(this LinkedList<T> _this)
	{
		return _this.First == null;
	}
}
