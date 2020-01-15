using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	public static class UiModifierUtil
	{
		private const float MIN_TESSELATE_SIZE = 1.0f;

		private static readonly List<float> s_zeroOneList = new List<float>() {0.0f, 1.0f};
		private static readonly List<UIVertex> s_oldVerts = new List<UIVertex>();
		private static readonly List<UIVertex> s_newVerts = new List<UIVertex>();
		private static readonly List<int> s_newIndices = new List<int>();
		private static readonly List<float> s_splitsH = new List<float>();
		private static readonly List<float> s_splitsV = new List<float>();

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
			UIVertex bl = _inTriangleList[_startIdx];
			UIVertex tl = _inTriangleList[_startIdx + 1];
			UIVertex tr = _inTriangleList[_startIdx + 2];
			UIVertex br = _inTriangleList[_startIdx + 4];

			if (!IsQuadValid(ref bl, ref tl, ref tr, ref br))
				return false;

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

		// split lists left to right, bottom to top
		public static bool Subdivide( VertexHelper _vertexHelper, List<float> _normalizedSplitsH, List<float> _normalizedSplitsV )
		{
			s_oldVerts.Clear();
			s_newVerts.Clear();
			s_newIndices.Clear();

			bool result = true;
				
			if (_normalizedSplitsH != null && _normalizedSplitsH.Count > 0 || _normalizedSplitsV != null && _normalizedSplitsV.Count > 0)
			{
				_vertexHelper.GetUIVertexStream(s_oldVerts);

				if (_normalizedSplitsH == null || _normalizedSplitsH.Count < 2)
					_normalizedSplitsH = s_zeroOneList;
				if (_normalizedSplitsV == null || _normalizedSplitsV.Count < 2)
					_normalizedSplitsV = s_zeroOneList;

				result = Subdivide(s_oldVerts, s_newVerts, s_newIndices, _normalizedSplitsH, _normalizedSplitsV);

				_vertexHelper.Clear();
				_vertexHelper.AddUIVertexStream(s_newVerts, s_newIndices);
			}

			return result;
		}

		public static bool Subdivide( List<UIVertex> _inTriangleList, List<UIVertex> _outVertices, List<int> _outIndices, List<float> _normalizedSplitsH, List<float> _normalizedSplitsV )
		{
			Rect minMaxRect = GetMinMaxRect( _inTriangleList );

			int startingVertexCount = _inTriangleList.Count;
			for (int i = 0; i < startingVertexCount; i += 6)
			{
				if (!SubdivideQuad(_inTriangleList, _outVertices, _outIndices, i, minMaxRect, _normalizedSplitsH, _normalizedSplitsV))
					return false;
			}

			return true;
		}

		public static bool SubdivideQuad( List<UIVertex> _inTriangleList, List<UIVertex> _outVertices, List<int> _outIndices, int _startIdx, Rect _minMaxRect, List<float> _normalizedSplitsH, List<float> _normalizedSplitsV )
		{
			UIVertex bl = _inTriangleList[_startIdx];
			UIVertex tl = _inTriangleList[_startIdx + 1];
			UIVertex tr = _inTriangleList[_startIdx + 2];
			UIVertex br = _inTriangleList[_startIdx + 4];

			if (!IsQuadValid(ref bl, ref tl, ref tr, ref br))
				return false;

			Rect normalizedRect = GetNormalizedRect( _inTriangleList, _startIdx, _minMaxRect );

			s_splitsH.Clear();
			s_splitsV.Clear();
			GetNormalizedSplits(normalizedRect, _normalizedSplitsH, _normalizedSplitsV, s_splitsH, s_splitsV);

			int currentOutIndex = _outVertices.Count;

			int numH = s_splitsH.Count;
			int numV = s_splitsV.Count;

			int[] lastBl = new int[numH];
			int[] lastTl = new int[numH];
			int[] lastTr = new int[numH];
			int[] lastBr = new int[numH];
			int ibl;
			int itl;
			int itr;
			int ibr;

			for (int iY = 0; iY < numV-1; iY++)
			{
				float bottomSplit = s_splitsV[iY]; 
				float topSplit = s_splitsV[iY+1];

				for (int iX = 0; iX < numH-1; iX++)
				{
					if (currentOutIndex >= 64996)
						return false;

					float leftSplit = s_splitsH[iX]; 
					float rightSplit = s_splitsH[iX+1];

					Split( 
						iX, iY, 
						ref bl, ref tl, ref tr, ref br,
						leftSplit, rightSplit, bottomSplit, topSplit,
						ref lastBl, ref lastTl, ref lastTr, ref lastBr,
						ref currentOutIndex,
						ref _outVertices,
						ref _outIndices
					);
				}
			}

			return true;
		}

		public static Rect GetMinMaxRect( List<UIVertex> _inTriangleList )
		{
			float maxValue = float.MaxValue / 2.0f;
			float xMin = maxValue;
			float yMin = maxValue;
			float xMax = -maxValue;
			float yMax = -maxValue;

			int numVertices = _inTriangleList.Count;
			for (int i=0; i<numVertices; i+= 6)
			{
				UIVertex bl = _inTriangleList[i];
				UIVertex tr = _inTriangleList[i + 2];
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
			UIVertex bl = _inTriangleList[_startIdx];
			UIVertex tr = _inTriangleList[_startIdx + 2];
			Rect result = new Rect();
			result.xMin = bl.position.x;
			result.yMin = bl.position.y;
			result.xMax = tr.position.x;
			result.yMax = tr.position.y;
			return result;
		}

		public static Rect GetNormalizedRect( List<UIVertex> _inTriangleList, int _startIdx, Rect _minMaxRect )
		{
			UIVertex bl = _inTriangleList[_startIdx];
			UIVertex tr = _inTriangleList[_startIdx + 2];

			float xMin = (bl.position.x - _minMaxRect.xMin) / _minMaxRect.width;
			float yMin = (bl.position.y - _minMaxRect.yMin) / _minMaxRect.height;
			float xMax = (tr.position.x - _minMaxRect.xMin) / _minMaxRect.width;
			float yMax = (tr.position.y - _minMaxRect.yMin) / _minMaxRect.height;

			return new Rect(xMin, yMin, xMax-xMin, yMax-yMin);
		}

		public static Vector2 GetNormalizedPointInRect( this Vector2 _this, Rect _rt)
		{
			return new Vector2(_this.x / _rt.width, _this.y / _rt.height);
		}

		public static Vector2 GetNormalizedPointInRect( this Vector3 _this, Rect _rt)
		{
			return new Vector2((_this.x - _rt.x) / _rt.width, (_this.y-_rt.y) / _rt.height);
		}



		private static void GetNormalizedSplits( Rect _nrmRect, List<float> _inAllNrmSplitsH, List<float> _inAllNrmSplitsV, List<float> _outNrmSplitsH, List<float> _outNrmSplitsV )
		{
			_outNrmSplitsH.Add(0);
			if (_inAllNrmSplitsH != null)
			{
				int count = _inAllNrmSplitsH.Count;
				for (int i=0; i<count; i++)
				{
					float split = _inAllNrmSplitsH[i];
					if (split > _nrmRect.xMin && split < _nrmRect.xMax)
						_outNrmSplitsH.Add((split - _nrmRect.xMin) / _nrmRect.width);
				}
			}
			_outNrmSplitsH.Add(1);

			_outNrmSplitsV.Add(0);
			if (_inAllNrmSplitsV != null)
			{
				int count = _inAllNrmSplitsV.Count;
				for (int i=0; i<count; i++)
				{
					float split = _inAllNrmSplitsV[i];
					if (split > _nrmRect.yMin && split < _nrmRect.yMax)
					{
						split = 1.0f - (1.0f - split - (1.0f - _nrmRect.yMax)) / _nrmRect.height;
						_outNrmSplitsV.Add( split );
					}
				}
			}
			_outNrmSplitsV.Add(1);

		}

		private static void GetRectSplits( Rect _rect, List<float> _splitsH, List<float> _splitsV )
		{
			int numH = _splitsH.Count;
			for (int i=0; i<numH; i++)
				_splitsH[i] = _splitsH[i] * _rect.width + _rect.x;
			int numV = _splitsV.Count;
			for (int i=0; i<numV; i++)
				_splitsV[i] = _splitsV[i] * _rect.height + _rect.y;
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
					_outVertices.Add(UiMath.Bilerp(_bl, _tl, _tr, _br, _leftSplit, _bottomSplit));
					_outVertices.Add(UiMath.Bilerp(_bl, _tl, _tr, _br, _leftSplit, _topSplit));
					_outVertices.Add(UiMath.Bilerp(_bl, _tl, _tr, _br, _rightSplit, _topSplit));
					_outVertices.Add(UiMath.Bilerp(_bl, _tl, _tr, _br, _rightSplit, _bottomSplit));

					ibl = _currentOutIndex;
					itl = _currentOutIndex + 1;
					itr = _currentOutIndex + 2;
					ibr = _currentOutIndex + 3;

					_currentOutIndex += 4;
				}
				else // x>0,y=0
				{
					_outVertices.Add(UiMath.Bilerp(_bl, _tl, _tr, _br, _rightSplit, _topSplit));
					_outVertices.Add(UiMath.Bilerp(_bl, _tl, _tr, _br, _rightSplit, _bottomSplit));

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
					_outVertices.Add(UiMath.Bilerp(_bl, _tl, _tr, _br, _leftSplit, _topSplit));
					_outVertices.Add(UiMath.Bilerp(_bl, _tl, _tr, _br, _rightSplit, _topSplit));

					ibl = _lastTl[_iX];
					itl = _currentOutIndex;
					itr = _currentOutIndex + 1;
					ibr = _lastTr[_iX];

					_currentOutIndex += 2;
				}
				else // x>0,y>0
				{
					_outVertices.Add(UiMath.Bilerp(_bl, _tl, _tr, _br, _rightSplit, _topSplit));

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


	}
}