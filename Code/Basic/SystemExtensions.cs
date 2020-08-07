using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class SystemExtensions
{
	public static bool Empty<T>(this LinkedList<T> _this)
	{
		return _this.First == null;
	}

	public static void PushBack<T>(this LinkedList<T> _this, T _val)
	{
		_this.AddLast(_val);
	}

	public static bool PopFront<T>( this LinkedList<T> _this, ref T _val)
	{
		if (_this.Empty())
			return false;

		_val = _this.First.Value;
		_this.RemoveFirst();
		return true;
	}


}
