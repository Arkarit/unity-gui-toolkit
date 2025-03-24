using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace GuiToolkit
{
	public static class UiMathUtility
	{
		private static readonly List<int[]> s_binomialLUT = new List<int[]>();

		// Note: In this implementation, only the absolute necessary values position, color and uv0 are lerped due to performance reasons,
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static UIVertex Bilerp( ref UIVertex _bl, ref UIVertex _tl, ref UIVertex _tr, ref UIVertex _br, float _h, float _v )
		{
			UIVertex result = _bl;
			result.position = Bilerp(ref _bl.position, ref _tl.position, ref _tr.position, ref _br.position, _h, _v);
			result.color = Bilerp(ref _bl.color, ref _tl.color, ref _tr.color, ref _br.color, _h, _v);
			result.uv0 = Bilerp(ref _bl.uv0, ref _tl.uv0, ref _tr.uv0, ref _br.uv0, _h, _v);
			return result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Bilerp( float _bl, float _tl, float _tr, float _br, float _h, float _v )
		{
			float top = Mathf.Lerp(_tl, _tr, _h);
			float bottom = Mathf.Lerp(_bl, _br, _h);
			return Mathf.Lerp(bottom, top, _v);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector2 Bilerp( Vector2 _bl, Vector2 _tl, Vector2 _tr, Vector2 _br, float _h, float _v )
		{
			Vector2 top = Vector2.Lerp(_tl, _tr, _h);
			Vector2 bottom = Vector2.Lerp(_bl, _br, _h);
			return Vector2.Lerp(bottom, top, _v);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector3 Bilerp( ref Vector3 _bl, ref Vector3 _tl, ref Vector3 _tr, ref Vector3 _br, float _h, float _v )
		{
			Vector3 top = Vector3.Lerp(_tl, _tr, _h);
			Vector3 bottom = Vector3.Lerp(_bl, _br, _h);
			return Vector3.Lerp(bottom, top, _v);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector4 Bilerp(ref Vector4 _bl, ref Vector4 _tl, ref Vector4 _tr, ref Vector4 _br, float _h, float _v )
		{
			Vector4 top = Vector4.Lerp(_tl, _tr, _h);
			Vector4 bottom = Vector4.Lerp(_bl, _br, _h);
			return Vector4.Lerp(bottom, top, _v);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Color Bilerp( ref Color _bl, ref Color _tl, ref Color _tr, ref Color _br, float _h, float _v )
		{
			Color top = Color.Lerp(_tl, _tr, _h);
			Color bottom = Color.Lerp(_bl, _br, _h);
			return Color.Lerp(bottom, top, _v);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Color32 Bilerp( ref Color32 _bl, ref Color32 _tl, ref Color32 _tr, ref Color32 _br, float _h, float _v )
		{
			Color32 top = LerpColor32(ref _tl, ref _tr, _h);
			Color32 bottom = LerpColor32(ref _bl, ref _br, _h);
			return LerpColor32(ref bottom, ref top, _v);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Color32 LerpColor32(ref Color32 _a, ref Color32 _b, float _t)
		{
			Color32 r = _a;
			r.r = (byte)Mathf.Clamp((int)(_a.r + (_b.r - _a.r) * _t), 0, 255);
			r.g = (byte)Mathf.Clamp((int)(_a.g + (_b.g - _a.g) * _t), 0, 255);
			r.b = (byte)Mathf.Clamp((int)(_a.b + (_b.b - _a.b) * _t), 0, 255);
			r.a = (byte)Mathf.Clamp((int)(_a.a + (_b.a - _a.a) * _t), 0, 255);
			return r;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Swap<T>(ref T _lhs, ref T _rhs)
		{
			T temp = _lhs;
			_lhs = _rhs;
			_rhs = temp;
		}

		public static float Inverse(float _f) => Mathf.Approximately(0, _f) ? 1 : 1 / _f;
		public static Vector3 Inverse(Vector3 _v) => new Vector3(Inverse(_v.x), Inverse(_v.y), Inverse(_v.z));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector2 Clamp01(Vector2 _input)
		{
			for (int i=0; i<2; i++)
				_input[i] = Mathf.Clamp01(_input[i]);
			return _input;
		}
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector2 Clamp01(Vector3 _input)
		{
			for (int i=0; i<3; i++)
				_input[i] = Mathf.Clamp01(_input[i]);
			return _input;
		}
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector2 Lerp4P( Vector2 _tl, Vector2 _tr, Vector2 _bl, Vector2 _br, ref Vector2 _normP)
		{
			return Vector2.LerpUnclamped(Vector2.LerpUnclamped(_tl, _tr, _normP.x), Vector2.LerpUnclamped(_bl, _br, _normP.x), _normP.y );
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector2 Lerp4P( ref Vector2 _tl, ref Vector2 _tr, ref Vector2 _bl, ref Vector2 _br, ref Vector2 _normP, bool _oneMinusX, bool _oneMinusY)
		{
			if (_oneMinusX)
				_normP.x = 1.0f - _normP.x;
			if (_oneMinusY)
				_normP.y = 1.0f - _normP.y;

			return Vector2.LerpUnclamped(Vector2.LerpUnclamped(_tl, _tr, _normP.x), Vector2.LerpUnclamped(_bl, _br, _normP.x), _normP.y );
		}

		public static Vector2 Lerp4P( Vector2[] _points, ref Vector2 _normP)
		{
			Debug.Assert(_points.Length == 4, "Lerp4P needs 4 points bl, tl, tr, br");
			return Lerp4P(_points[1], _points[2], _points[0], _points[3], ref _normP);
		}

		public static Vector3 Lerp4P( Vector3[] _points, ref Vector2 _normP)
		{
			Debug.Assert(_points.Length == 4, "Lerp4P needs 4 points bl, tl, tr, br");
			return Lerp4P(_points[1], _points[2], _points[0], _points[3], ref _normP);
		}

		public static Vector2 Bezier( Vector2 _p0, Vector2 _p1, Vector2 _p2, float _normP )
		{
			float oneMinusNormP = 1f - _normP;
			return
				oneMinusNormP * oneMinusNormP * _p0 +
				2f * oneMinusNormP * _normP * _p1 +
				_normP * _normP * _p2;
		}

		public static Vector2 Bezier (Vector2 _p0, Vector2 _p1, Vector2 _p2, Vector2 _p3, float _normP) {
			float oneMinusNormP = 1f - _normP;
			return
				oneMinusNormP * oneMinusNormP * oneMinusNormP * _p0 +
				3f * oneMinusNormP * oneMinusNormP * _normP * _p1 +
				3f * oneMinusNormP * _normP * _normP * _p2 +
				_normP * _normP * _normP * _p3;
		}

		private static float Binomial(int _order, int _point)
		{
			while( s_binomialLUT.Count <= _order)
			{
				int lutCount = s_binomialLUT.Count;

				if (lutCount == 0)
				{
					int[] startRow = {1};
					s_binomialLUT.Add(startRow);
					continue;
				}


				int[] nextRow = new int[lutCount + 1];

				nextRow[0] = 1;

				for( int i = 1, prev = lutCount-1; i < lutCount; i++ )
					nextRow[i] = s_binomialLUT[prev][i-1] + s_binomialLUT[prev][i];

				nextRow[lutCount] = 1;

				s_binomialLUT.Add(nextRow);
			}

			return s_binomialLUT[_order][_point];
		}

		public static Vector2 Bezier (Vector2[] _points, int _startIdx, int _numPoints, float _normP) {
			float oneMinusNormP = 1f - _normP;
			int npMinusOne = _numPoints - 1;

			Vector2 result = Vector2.zero;

			for (int i=0; i<_numPoints; i++)
			{
				float f = Mathf.Pow(oneMinusNormP, npMinusOne-i) * Mathf.Pow(_normP, i) * Binomial(npMinusOne, i);
				result += _points[_startIdx + i] * f;
			}

			return result;
		}

		public static Vector2 Bezier(Vector2[] _points, float _normP)
		{
			return Bezier(_points, 0, _points.Length, _normP);
		}

		public static Vector2 InterpPoint( Vector2[] _points, int _numX, int _numY, Vector2 _normP, bool _oneMinusX, bool _oneMinusY)
		{
			if (_oneMinusX)
				_normP.x = 1.0f - _normP.x;
			if (_oneMinusY)
				_normP.y = 1.0f - _normP.y;

			Vector2[] xPoints = Interpolate(_points, _numX, _numY, _normP.x);
			Vector2[] tResult = Interpolate(xPoints, _numY, 1, _normP.y);
			return tResult[0];
		}

		private static Vector2[] Interpolate(Vector2[] _points, int _numX, int _numY, float _norm)
		{
			Vector2[] result = new Vector2[_numY];

			for (int iY=0; iY<_numY; iY++)
			{
				int ip = iY * _numX;

				switch( _numX)
				{
					case 1:
						result[iY] = _points[ip];
						break;
					case 2:
						result[iY] = Vector2.Lerp(_points[ip], _points[ip+1], _norm);
						break;
					case 3:
						result[iY] = Bezier(_points[ip], _points[ip+1], _points[ip+2], _norm);
						break;
					case 4:
						result[iY] = Bezier(_points[ip], _points[ip+1], _points[ip+2], _points[ip+3], _norm);
						break;
					default:
						result[iY] = Bezier(_points, ip, _numX, _norm);
						break;
				}
			}

			return result;
		}

	}
}
