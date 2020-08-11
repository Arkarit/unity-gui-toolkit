using System;
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

	public static List<T> ToList<T>( this LinkedList<T> _this )
	{
		List<T> result = new List<T>();
		foreach( var elem in _this)
		{
			result.Add(elem);
		}
		return result;
	}


	public static bool Empty<T>(this List<T> _this)
	{
		return _this.Count == 0;
	}

	public static T Back<T>( this List<T> _this )
	{
		if (_this.Empty())
			throw new InvalidOperationException("Access Back() of empty list");

		return _this[_this.Count-1];
	}

	// rgba
	public static byte[] ToBytes(this Color[] _colors, bool _useAlpha = false)
	{
		int byteWidth = _useAlpha ? 4 : 3;
		int byteLength = _colors.Length * byteWidth;
		byte[] result = new byte[byteLength];
		for (int i=0; i<_colors.Length; i++)
		{
			int byteIdx = i * byteWidth;
			result[byteIdx] =   (byte) (255.0f * _colors[i].r);
			result[byteIdx+1] = (byte) (255.0f * _colors[i].g);
			result[byteIdx+2] = (byte) (255.0f * _colors[i].b);
			if (_useAlpha)
				result[byteIdx+3] = (byte) (255.0f * _colors[i].a);
		}

		return result;
	}

	public static Color[] ToColors(this byte[] _bytes, bool _useAlpha = false)
	{
		int byteWidth = _useAlpha ? 4 : 3;
		int colorLength = _bytes.Length / byteWidth; // surplus bytes are simply skipped
		Color[] result = new Color[colorLength];
		for (int i=0; i<colorLength; i++)
		{
			int byteIdx = i * byteWidth;
			result[i].r = (float) _bytes[byteIdx] / 255.0f;
			result[i].g = (float) _bytes[byteIdx+1] / 255.0f;
			result[i].b = (float) _bytes[byteIdx+2] / 255.0f;
			result[i].a = _useAlpha ? (float) _bytes[byteIdx+3] / 255.0f : 1;
		}
		return result;
	}

	public static void SaveAsPNG(this Texture2D _texture, string _fullPath)
    {
        byte[] _bytes =_texture.EncodeToPNG();
        System.IO.File.WriteAllBytes(_fullPath, _bytes);
    }
}
