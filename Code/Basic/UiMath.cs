using UnityEngine;

namespace GuiToolkit
{
	public static class UiMath
	{
		// Note: In this implementation, only the absolute necessary values position, color and uv0 are lerped due to performance reasons,
		public static UIVertex Bilerp( UIVertex _bl, UIVertex _tl, UIVertex _tr, UIVertex _br, float _h, float _v )
		{
			UIVertex result = _bl;
			result.position = Bilerp(_bl.position, _tl.position, _tr.position, _br.position, _h, _v);
			result.color = Bilerp(_bl.color, _tl.color, _tr.color, _br.color, _h, _v);
			result.uv0 = Bilerp(_bl.uv0, _tl.uv0, _tr.uv0, _br.uv0, _h, _v);
			return result;
		}

		public static float Bilerp( float _bl, float _tl, float _tr, float _br, float _h, float _v )
		{
			float top = Mathf.Lerp(_tl, _tr, _h);
			float bottom = Mathf.Lerp(_bl, _br, _h);
			return Mathf.Lerp(bottom, top, _v);
		}

		public static Vector2 Bilerp( Vector2 _bl, Vector2 _tl, Vector2 _tr, Vector2 _br, float _h, float _v )
		{
			Vector2 top = Vector2.Lerp(_tl, _tr, _h);
			Vector2 bottom = Vector2.Lerp(_bl, _br, _h);
			return Vector2.Lerp(bottom, top, _v);
		}

		public static Vector3 Bilerp( Vector3 _bl, Vector3 _tl, Vector3 _tr, Vector3 _br, float _h, float _v )
		{
			Vector3 top = Vector3.Lerp(_tl, _tr, _h);
			Vector3 bottom = Vector3.Lerp(_bl, _br, _h);
			return Vector3.Lerp(bottom, top, _v);
		}

		public static Vector4 Bilerp( Vector4 _bl, Vector4 _tl, Vector4 _tr, Vector4 _br, float _h, float _v )
		{
			Vector4 top = Vector4.Lerp(_tl, _tr, _h);
			Vector4 bottom = Vector4.Lerp(_bl, _br, _h);
			return Vector4.Lerp(bottom, top, _v);
		}

		public static Color Bilerp( Color _bl, Color _tl, Color _tr, Color _br, float _h, float _v )
		{
			Color top = Color.Lerp(_tl, _tr, _h);
			Color bottom = Color.Lerp(_bl, _br, _h);
			return Color.Lerp(bottom, top, _v);
		}

		public static void Swap<T>(ref T _lhs, ref T _rhs)
		{
			T temp = _lhs;
			_lhs = _rhs;
			_rhs = temp;
		}

		public static Vector2 Lerp4P( Vector2 _tl, Vector2 _tr, Vector2 _bl, Vector2 _br, Vector2 _normP)
		{
			Debug.Assert(_normP.x >= 0 && _normP.x <= 1 && _normP.y >= 0 && _normP.y <= 1, "_normP needs to be normalized");
			return Vector2.Lerp(Vector2.Lerp(_tl, _tr, _normP.x), Vector2.Lerp(_bl, _br, _normP.x), _normP.y );
		}

		public static Vector2 Lerp4P( Vector2 _tl, Vector2 _tr, Vector2 _bl, Vector2 _br, Vector2 _normP, bool _oneMinusX, bool _oneMinusY)
		{
			Debug.Assert(_normP.x >= 0 && _normP.x <= 1 && _normP.y >= 0 && _normP.y <= 1, "_normP needs to be normalized");

			if (_oneMinusX)
				_normP.x = 1.0f - _normP.x;
			if (_oneMinusY)
				_normP.y = 1.0f - _normP.y;

			return Vector2.Lerp(Vector2.Lerp(_tl, _tr, _normP.x), Vector2.Lerp(_bl, _br, _normP.x), _normP.y );
		}

		public static Vector2 Lerp4P( Vector2[] _points, Vector2 _normP)
		{
			Debug.Assert(_normP.x >= 0 && _normP.x <= 1 && _normP.y >= 0 && _normP.y <= 1, "_normP needs to be normalized");
			Debug.Assert(_points.Length == 4, "Lerp4P needs 4 points bl, tl, tr, br");
			return Lerp4P(_points[1], _points[2], _points[0], _points[3], _normP);
		}

		public static Vector2 Bezier( Vector2 _p0, Vector2 _p1, Vector2 _p2, float _normP )
		{
			Debug.Assert(_normP >= 0 && _normP <= 1, "_normP needs to be normalized");
			float oneMinusNormP = 1f - _normP;
			return
				oneMinusNormP * oneMinusNormP * _p0 +
				2f * oneMinusNormP * _normP * _p1 +
				_normP * _normP * _p2;
		}

		public static Vector2 Bezier (Vector2 _p0, Vector2 _p1, Vector2 _p2, Vector2 _p3, float _normP) {
			Debug.Assert(_normP >= 0 && _normP <= 1, "_normP needs to be normalized");
			float OneMinusT = 1f - _normP;
			return
				OneMinusT * OneMinusT * OneMinusT * _p0 +
				3f * OneMinusT * OneMinusT * _normP * _p1 +
				3f * OneMinusT * _normP * _normP * _p2 +
				_normP * _normP * _normP * _p3;
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
				int xkOffset = GetKernelOffset(_numX, _norm);
				int ip = iY * _numX + xkOffset;

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
					default:
						result[iY] = Bezier(_points[ip], _points[ip+1], _points[ip+2], _points[ip+3], _norm);
						break;
				}
			}

			return result;
		}

		private static int GetKernelOffset(int _size, float _normValue)
		{
			return 0;
		}

	}
}
