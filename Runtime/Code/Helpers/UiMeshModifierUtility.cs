using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	public static class UiMeshModifierUtility
	{
		private const float MIN_TESSELATE_SIZE = 1.0f;

		public const int QUAD_BL_IDX_OFFSET = 0;
		public const int QUAD_TL_IDX_OFFSET = 1;
		public const int QUAD_TR_IDX_OFFSET = 2;
		public const int QUAD_BR_IDX_OFFSET = 4;
		public const int CORNERS_BL_IDX = 0;
		public const int CORNERS_TL_IDX = 1;
		public const int CORNERS_TR_IDX = 2;
		public const int CORNERS_BR_IDX = 3;

		private static readonly List<UIVertex> s_oldVerts = new List<UIVertex>();
		private static readonly List<UIVertex> s_newVerts = new List<UIVertex>();
		private static readonly List<int> s_newIndices = new List<int>();
		private static List<UIVertex> s_clipWorkA = new List<UIVertex>();
		private static List<UIVertex> s_clipWorkB = new List<UIVertex>();
		private static readonly List<UIVertex> s_clipPoly = new List<UIVertex>(8);

		public static void RemoveZeroQuads( VertexHelper _vertexHelper )
		{
			s_newVerts.Clear();
			s_newIndices.Clear();

			_vertexHelper.GetUIVertexStream(s_oldVerts);
			int count = s_oldVerts.Count;

			bool hasZeroQuad = false;
			for (int i=0; i<count; i += 6)
			{
				UIVertex bl = s_oldVerts[i + QUAD_BL_IDX_OFFSET];
				UIVertex tl = s_oldVerts[i + QUAD_TL_IDX_OFFSET];
				UIVertex tr = s_oldVerts[i + QUAD_TR_IDX_OFFSET];
				UIVertex br = s_oldVerts[i + QUAD_BR_IDX_OFFSET];
				if (IsZeroQuad(ref bl, ref tl, ref tr, ref br))
				{
					hasZeroQuad = true;
					break;
				}
			}

			if (!hasZeroQuad)
				return;

			for (int i=0; i<count; i += 6)
			{
				UIVertex bl = s_oldVerts[i + QUAD_BL_IDX_OFFSET];
				UIVertex tl = s_oldVerts[i + QUAD_TL_IDX_OFFSET];
				UIVertex tr = s_oldVerts[i + QUAD_TR_IDX_OFFSET];
				UIVertex br = s_oldVerts[i + QUAD_BR_IDX_OFFSET];

				if (!IsZeroQuad(ref bl, ref tl, ref tr, ref br))
				{
					int ibl = s_newVerts.Count;
					s_newVerts.Add(bl);
					int itl = s_newVerts.Count;
					s_newVerts.Add(tl);
					int itr = s_newVerts.Count;
					s_newVerts.Add(tr);
					int ibr = s_newVerts.Count;
					s_newVerts.Add(br);

					s_newIndices.Add(ibl);
					s_newIndices.Add(itl);
					s_newIndices.Add(itr);
					s_newIndices.Add(itr);
					s_newIndices.Add(ibr);
					s_newIndices.Add(ibl);
				}
			}

			_vertexHelper.Clear();
			_vertexHelper.AddUIVertexStream(s_newVerts, s_newIndices);
		}

		// Tessellate() assumes the input is a regular rectangle (axis-aligned BL/TL/TR/BR quads).
		// Subdivide() works on arbitrary triangle topology via per-triangle iso-line clipping.

		public static bool Tessellate( VertexHelper _vertexHelper, float _sizeH, float _sizeV )
		{
			s_oldVerts.Clear();
			s_newVerts.Clear();
			s_newIndices.Clear();

			_vertexHelper.GetUIVertexStream(s_oldVerts);

			bool result = Tessellate(s_oldVerts, s_newVerts, s_newIndices, _sizeH, _sizeV);

			_vertexHelper.Clear();
			_vertexHelper.AddUIVertexStream(s_newVerts, s_newIndices);

			return result;
		}

		public static bool Tessellate( List<UIVertex> _inTriangleList, List<UIVertex> _outVertices, List<int> _outIndices, float _sizeH, float _sizeV )
		{
			int startingVertexCount = _inTriangleList.Count;
			for (int i = 0; i < startingVertexCount; i += 6)
			{
				if (!TessellateQuad(_inTriangleList, _outVertices, _outIndices, i, _sizeH, _sizeV))
					return false;
			}
			return true;
		}

		public static bool TessellateQuad( List<UIVertex> _inTriangleList, List<UIVertex> _outVertices, List<int> _outIndices, int _startIdx, float _sizeH, float _sizeV )
		{
			UIVertex bl = _inTriangleList[_startIdx + QUAD_BL_IDX_OFFSET];
			UIVertex tl = _inTriangleList[_startIdx + QUAD_TL_IDX_OFFSET];
			UIVertex tr = _inTriangleList[_startIdx + QUAD_TR_IDX_OFFSET];
			UIVertex br = _inTriangleList[_startIdx + QUAD_BR_IDX_OFFSET];

			if (!IsQuadValid(ref bl, ref tl, ref tr, ref br))
			{
				if (!IsZeroQuad(ref bl, ref tl, ref tr, ref br))
					return false;

				// Workaround: Text mesh pro sometimes has zero (every position 0,0,0) quads, which change the bounding box.
				// As a consistent bounding box is much more important than a perfectly correct bounding box, we simply also add a zero quad.
				int ibl = _outVertices.Count;
				_outVertices.Add(bl);
				int itl = _outVertices.Count;
				_outVertices.Add(tl);
				int itr = _outVertices.Count;
				_outVertices.Add(tr);
				int ibr = _outVertices.Count;
				_outVertices.Add(br);

				_outIndices.Add(ibl);
				_outIndices.Add(itl);
				_outIndices.Add(itr);

				_outIndices.Add(itr);
				_outIndices.Add(ibr);
				_outIndices.Add(ibl);

				return true;
			}

			// Position deltas, A and B are the local quad up and right axes
			Vector3 right = tr.position - tl.position;
			Vector3 up = tl.position - bl.position;

			// Determine how many tiles there should be
			float sizeH = 1.0f / Mathf.Max(MIN_TESSELATE_SIZE, _sizeH);
			float sizeV = 1.0f / Mathf.Max(MIN_TESSELATE_SIZE, _sizeV);
			int numH = Mathf.CeilToInt(right.magnitude * sizeH);
			int numV = Mathf.CeilToInt(up.magnitude * sizeV);

			float quadWidth = 1.0f / (float)numH;
			float quadHeight = 1.0f / (float)numV;
			float bottomSplit = 0.0f;

			int currentOutIndex = _outVertices.Count;

			int[] lastBl = new int[numH];
			int[] lastTl = new int[numH];
			int[] lastTr = new int[numH];
			int[] lastBr = new int[numH];

			for (int iY = 0; iY < numV; ++iY)
			{
				float topSplit = (float)(iY + 1) * quadHeight;
				float leftSplit = 0.0f;

				for (int iX = 0; iX < numH; ++iX)
				{
					float rightSplit = (float)(iX + 1) * quadWidth;

					if (currentOutIndex >= 64996)
						return false;

					Split( 
						iX, iY, 
						ref bl, ref tl, ref tr, ref br,
						leftSplit, rightSplit, bottomSplit, topSplit,
						ref lastBl, ref lastTl, ref lastTr, ref lastBr,
						ref currentOutIndex,
						ref _outVertices,
						ref _outIndices
					);

					leftSplit = rightSplit;
				}
				bottomSplit = topSplit;
			}

			return true;
		}

		/// <summary>
		/// Splits the mesh in the given <see cref="VertexHelper"/> along axis-aligned iso-lines
		/// expressed as normalized positions (0..1) within the mesh's overall axis-aligned bounding box.
		///
		/// <paramref name="_normalizedSplitsH"/> defines vertical iso-lines at x = t * width  + xMin.
		/// <paramref name="_normalizedSplitsV"/> defines horizontal iso-lines at y = t * height + yMin.
		///
		/// The algorithm clips every input triangle independently against each iso-line using
		/// Sutherland-Hodgman half-plane clipping, fan-triangulating the resulting polygons.
		/// It is topology-agnostic: works for axis-aligned single quads, 9-sliced images,
		/// tessellated grids, radial-filled images, TMP glyphs, sheared/rotated quads, and arbitrary
		/// triangle soups. Splits at t&lt;=0 or t&gt;=1 are ignored.
		///
		/// Triangles in the output do not share vertices; each triangle owns its three vertices.
		/// </summary>
		public static void Subdivide( VertexHelper _vertexHelper, List<float> _normalizedSplitsH, List<float> _normalizedSplitsV )
		{
			bool hasH = _normalizedSplitsH != null && _normalizedSplitsH.Count > 0;
			bool hasV = _normalizedSplitsV != null && _normalizedSplitsV.Count > 0;
			if (!hasH && !hasV)
				return;

			s_oldVerts.Clear();
			_vertexHelper.GetUIVertexStream(s_oldVerts);

			if (s_oldVerts.Count < 3)
				return;

			Rect bounds = GetBounds(s_oldVerts);
			if (bounds.width <= 0f || bounds.height <= 0f)
				return;

			s_clipWorkA.Clear();
			s_clipWorkB.Clear();
			s_clipWorkA.AddRange(s_oldVerts);

			bool changed = false;

			if (hasH)
			{
				int n = _normalizedSplitsH.Count;
				for (int i = 0; i < n; i++)
				{
					float t = _normalizedSplitsH[i];
					if (t <= 0f || t >= 1f)
						continue;
					float line = t * bounds.width + bounds.xMin;
					s_clipWorkB.Clear();
					SplitTrianglesAlongAxis(s_clipWorkA, s_clipWorkB, line, 0);
					var tmp = s_clipWorkA;
					s_clipWorkA = s_clipWorkB;
					s_clipWorkB = tmp;
					changed = true;
				}
			}

			if (hasV)
			{
				int n = _normalizedSplitsV.Count;
				for (int i = 0; i < n; i++)
				{
					float t = _normalizedSplitsV[i];
					if (t <= 0f || t >= 1f)
						continue;
					float line = t * bounds.height + bounds.yMin;
					s_clipWorkB.Clear();
					SplitTrianglesAlongAxis(s_clipWorkA, s_clipWorkB, line, 1);
					var tmp = s_clipWorkA;
					s_clipWorkA = s_clipWorkB;
					s_clipWorkB = tmp;
					changed = true;
				}
			}

			if (!changed)
			{
				s_clipWorkA.Clear();
				s_clipWorkB.Clear();
				return;
			}

			s_newIndices.Clear();
			int outCount = s_clipWorkA.Count;
			if (outCount >= 65000)
			{
				Debug.LogWarning($"[UiMeshModifierUtility.Subdivide] Output exceeds Unity's 65k-vertex limit ({outCount} verts); mesh will be truncated.");
				outCount = 65000 - (65000 % 3);
			}
			for (int i = 0; i < outCount; i++)
				s_newIndices.Add(i);

			_vertexHelper.Clear();
			if (outCount == s_clipWorkA.Count)
			{
				_vertexHelper.AddUIVertexStream(s_clipWorkA, s_newIndices);
			}
			else
			{
				s_newVerts.Clear();
				for (int i = 0; i < outCount; i++)
					s_newVerts.Add(s_clipWorkA[i]);
				_vertexHelper.AddUIVertexStream(s_newVerts, s_newIndices);
				s_newVerts.Clear();
			}

			s_clipWorkA.Clear();
			s_clipWorkB.Clear();
		}

		// Splits each triangle in _in against the iso-line "axis-coord = _lineConstant",
		// emitting the resulting sub-triangles to _out.
		// _axis: 0 = X (vertical iso-line), 1 = Y (horizontal iso-line).
		private static void SplitTrianglesAlongAxis(List<UIVertex> _in, List<UIVertex> _out, float _lineConstant, int _axis)
		{
			int n = _in.Count;
			for (int i = 0; i + 2 < n; i += 3)
			{
				UIVertex v0 = _in[i];
				UIVertex v1 = _in[i + 1];
				UIVertex v2 = _in[i + 2];
				ClipTriangleToHalfPlane(ref v0, ref v1, ref v2, _lineConstant, _axis, _out, true);
				ClipTriangleToHalfPlane(ref v0, ref v1, ref v2, _lineConstant, _axis, _out, false);
			}
		}

		// Sutherland-Hodgman: clips triangle (_v0,_v1,_v2) against a half-plane.
		// _keepPositive=true keeps the (coord >= _lineConstant) side including the line itself;
		// _keepPositive=false keeps the (coord < _lineConstant) side strictly, so vertices exactly
		// on the line are not duplicated between the two output sides.
		private static void ClipTriangleToHalfPlane(ref UIVertex _v0, ref UIVertex _v1, ref UIVertex _v2, float _lineConstant, int _axis, List<UIVertex> _out, bool _keepPositive)
		{
			s_clipPoly.Clear();
			ClipEdge(ref _v0, ref _v1, _lineConstant, _axis, _keepPositive);
			ClipEdge(ref _v1, ref _v2, _lineConstant, _axis, _keepPositive);
			ClipEdge(ref _v2, ref _v0, _lineConstant, _axis, _keepPositive);

			int polyCount = s_clipPoly.Count;
			if (polyCount < 3)
				return;

			UIVertex anchor = s_clipPoly[0];
			for (int i = 1; i < polyCount - 1; i++)
			{
				_out.Add(anchor);
				_out.Add(s_clipPoly[i]);
				_out.Add(s_clipPoly[i + 1]);
			}
		}

		private static void ClipEdge(ref UIVertex _curr, ref UIVertex _next, float _lineConstant, int _axis, bool _keepPositive)
		{
			float dCurr = (_axis == 0 ? _curr.position.x : _curr.position.y) - _lineConstant;
			float dNext = (_axis == 0 ? _next.position.x : _next.position.y) - _lineConstant;

			bool currIn = _keepPositive ? (dCurr >= 0f) : (dCurr < 0f);
			bool nextIn = _keepPositive ? (dNext >= 0f) : (dNext < 0f);

			if (currIn)
			{
				if (nextIn)
				{
					s_clipPoly.Add(_next);
				}
				else
				{
					float t = dCurr / (dCurr - dNext);
					s_clipPoly.Add(LerpVertex(ref _curr, ref _next, t));
				}
			}
			else if (nextIn)
			{
				float t = dCurr / (dCurr - dNext);
				s_clipPoly.Add(LerpVertex(ref _curr, ref _next, t));
				s_clipPoly.Add(_next);
			}
		}

		// Mirrors UiMathUtility.Bilerp: only position, color and uv0 are interpolated.
		// uv1-3, normal and tangent come from _a unchanged - matches existing behavior of the
		// quad-based subdivision path and the UI gradient/distortion modifiers that consume it.
		private static UIVertex LerpVertex(ref UIVertex _a, ref UIVertex _b, float _t)
		{
			UIVertex r = _a;
			r.position = Vector3.Lerp(_a.position, _b.position, _t);
			r.color = Color.Lerp(_a.color, _b.color, _t);
			r.uv0 = Vector2.Lerp(_a.uv0, _b.uv0, _t);
			return r;
		}

		public static Rect GetBounds( List<UIVertex> _inTriangleList )
		{
			float maxValue = float.MaxValue / 2.0f;
			float xMin = maxValue;
			float yMin = maxValue;
			float xMax = -maxValue;
			float yMax = -maxValue;

			int numVertices = _inTriangleList.Count;
			for (int i=0; i<numVertices; i+= 6)
			{
				UIVertex bl = _inTriangleList[i + QUAD_BL_IDX_OFFSET];
				UIVertex tr = _inTriangleList[i + QUAD_TR_IDX_OFFSET];
				if (bl.position.x < xMin)
					xMin = bl.position.x;
				if (bl.position.y < yMin)
					yMin = bl.position.y;
				if (tr.position.x > xMax)
					xMax = tr.position.x;
				if (tr.position.y > yMax)
					yMax = tr.position.y;
			}

			Rect result = new Rect();
			result.xMin = xMin;
			result.yMin = yMin;
			result.xMax = xMax;
			result.yMax = yMax;
			return result;
		}

		public static Rect GetRect( List<UIVertex> _inTriangleList, int _startIdx )
		{
			UIVertex bl = _inTriangleList[_startIdx + QUAD_BL_IDX_OFFSET];
			UIVertex tr = _inTriangleList[_startIdx + QUAD_TR_IDX_OFFSET];
			Rect result = new Rect();
			result.xMin = bl.position.x;
			result.yMin = bl.position.y;
			result.xMax = tr.position.x;
			result.yMax = tr.position.y;
			return result;
		}

		public static Vector2 GetNormalizedPointInRect( this Vector2 _this, Rect _rt)
		{
			return new Vector2(_this.x / _rt.width, _this.y / _rt.height);
		}

		public static Vector2 GetNormalizedPointInRect( this Vector3 _this, Rect _rt)
		{
			return new Vector2((_this.x - _rt.x) / _rt.width, (_this.y-_rt.y) / _rt.height);
		}

		public static Vector2 GetNormalizedPointInRect( this Vector2 _this, Rect _rt, bool _oneMinusX, bool _oneMinusY)
		{
			Vector2 result = new Vector2(_this.x / _rt.width, _this.y / _rt.height);
			if (_oneMinusX)
				result.x = 1.0f - result.x;
			if (_oneMinusY)
				result.y = 1.0f - result.y;
			return result;
		}

		public static Vector2 GetNormalizedPointInRect( this Vector3 _this, Rect _rt, bool _oneMinusX, bool _oneMinusY)
		{
			Vector2 result = new Vector2((_this.x - _rt.x) / _rt.width, (_this.y-_rt.y) / _rt.height);
			if (_oneMinusX)
				result.x = 1.0f - result.x;
			if (_oneMinusY)
				result.y = 1.0f - result.y;
			return result;
		}


		private static void Split
		( 
			  int _iX
			, int _iY
			, ref UIVertex _bl
			, ref UIVertex _tl
			, ref UIVertex _tr
			, ref UIVertex _br
			, float _leftSplit
			, float _rightSplit
			, float _bottomSplit
			, float _topSplit
			, ref int[] _lastBl
			, ref int[] _lastTl
			, ref int[] _lastTr
			, ref int[] _lastBr
			, ref int _currentOutIndex
			, ref List<UIVertex> _outVertices
			, ref List<int> _outIndices )
		{
			int ibl;
			int itl;
			int itr;
			int ibr;

			if (_iY == 0)
			{
				if (_iX == 0) // x=0,y=0
				{
					_outVertices.Add(UiMathUtility.Bilerp(ref _bl, ref _tl, ref _tr, ref _br, _leftSplit, _bottomSplit));
					_outVertices.Add(UiMathUtility.Bilerp(ref _bl, ref _tl, ref _tr, ref _br, _leftSplit, _topSplit));
					_outVertices.Add(UiMathUtility.Bilerp(ref _bl, ref _tl, ref _tr, ref _br, _rightSplit, _topSplit));
					_outVertices.Add(UiMathUtility.Bilerp(ref _bl, ref _tl, ref _tr, ref _br, _rightSplit, _bottomSplit));

					ibl = _currentOutIndex;
					itl = _currentOutIndex + 1;
					itr = _currentOutIndex + 2;
					ibr = _currentOutIndex + 3;

					_currentOutIndex += 4;
				}
				else // x>0,y=0
				{
					_outVertices.Add(UiMathUtility.Bilerp(ref _bl, ref _tl, ref _tr, ref _br, _rightSplit, _topSplit));
					_outVertices.Add(UiMathUtility.Bilerp(ref _bl, ref _tl, ref _tr, ref _br, _rightSplit, _bottomSplit));

					int lastIX = _iX - 1;

					ibl = _lastBr[lastIX];
					itl = _lastTr[lastIX];
					itr = _currentOutIndex;
					ibr = _currentOutIndex + 1;

					_currentOutIndex += 2;
				}
			}
			else
			{
				if (_iX == 0) // x=0,y>0
				{
					_outVertices.Add(UiMathUtility.Bilerp(ref _bl, ref _tl, ref _tr, ref _br, _leftSplit, _topSplit));
					_outVertices.Add(UiMathUtility.Bilerp(ref _bl, ref _tl, ref _tr, ref _br, _rightSplit, _topSplit));

					ibl = _lastTl[_iX];
					itl = _currentOutIndex;
					itr = _currentOutIndex + 1;
					ibr = _lastTr[_iX];

					_currentOutIndex += 2;
				}
				else // x>0,y>0
				{
					_outVertices.Add(UiMathUtility.Bilerp(ref _bl, ref _tl, ref _tr, ref _br, _rightSplit, _topSplit));

					int lastIX = _iX - 1;

					ibl = _lastTl[_iX];
					itl = _lastTr[lastIX];
					itr = _currentOutIndex;
					ibr = _lastTr[_iX];

					_currentOutIndex += 1;
				}

			}

			_outIndices.Add(ibl);
			_outIndices.Add(itl);
			_outIndices.Add(itr);

			_outIndices.Add(itr);
			_outIndices.Add(ibr);
			_outIndices.Add(ibl);

			_lastBl[_iX] = ibl;
			_lastTl[_iX] = itl;
			_lastTr[_iX] = itr;
			_lastBr[_iX] = ibr;
		}

		private static bool IsQuadValid(ref UIVertex _bl, ref UIVertex _tl, ref UIVertex _tr, ref UIVertex _br)
		{
			return 
				   _bl.position.x < _br.position.x 
				&& _tl.position.x < _tr.position.x
				&& _bl.position.y < _tl.position.y
				&& _br.position.y < _tr.position.y;
		}

		private static bool IsZeroQuad( ref UIVertex _bl, ref UIVertex _tl, ref UIVertex _tr, ref UIVertex _br )
		{
			return
				_bl.position == Vector3.zero
				&& _tl.position == Vector3.zero
				&& _bl.position == Vector3.zero
				&& _br.position == Vector3.zero;
		}


	}
}
