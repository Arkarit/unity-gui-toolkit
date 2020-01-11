using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	public static class UiTesselationUtil
	{
		private const float MIN_TESSELATE_SIZE = 1.0f;

		public static bool Tesselate( VertexHelper _vertexHelper, float _sizeH, float _sizeV )
		{
			List<UIVertex> oldVerts = new List<UIVertex>();
			List<UIVertex> newVerts = new List<UIVertex>();
			List<int> newIndices = new List<int>();

			_vertexHelper.GetUIVertexStream(oldVerts);

			bool result = Tesselate(oldVerts, newVerts, newIndices, _sizeH, _sizeV);

			_vertexHelper.Clear();
			_vertexHelper.AddUIVertexStream(newVerts, newIndices);

			return result;
		}

		public static bool Tesselate( List<UIVertex> _inTriangleList, List<UIVertex> _outVertices, List<int> _outIndices, float _sizeH, float _sizeV )
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
			float startBProp = 0.0f;

			int currentOutIndex = _outVertices.Count;

			int[] lastBl = new int[numH];
			int[] lastTl = new int[numH];
			int[] lastTr = new int[numH];
			int[] lastBr = new int[numH];
			int ibl;
			int itl;
			int itr;
			int ibr;

			for (int iY = 0; iY < numV; ++iY)
			{
				float endBProp = (float)(iY + 1) * quadHeight;
				float startAProp = 0.0f;

				for (int iX = 0; iX < numH; ++iX)
				{
					float endAProp = (float)(iX + 1) * quadWidth;

					if (currentOutIndex >= 64996)
						return false;

					if (iY==0 )
					{
						if (iX == 0) // x=0,y=0
						{
							_outVertices.Add( UiMath.Bilerp(bl, tl, tr, br, startAProp, startBProp));
							_outVertices.Add( UiMath.Bilerp(bl, tl, tr, br, startAProp, endBProp));
							_outVertices.Add( UiMath.Bilerp(bl, tl, tr, br, endAProp, endBProp));
							_outVertices.Add( UiMath.Bilerp(bl, tl, tr, br, endAProp, startBProp));

							ibl = currentOutIndex;
							itl = currentOutIndex + 1;
							itr = currentOutIndex + 2;
							ibr = currentOutIndex + 3;

							currentOutIndex += 4;
						}
						else // x>0,y=0
						{
							_outVertices.Add( UiMath.Bilerp(bl, tl, tr, br, endAProp, endBProp));
							_outVertices.Add( UiMath.Bilerp(bl, tl, tr, br, endAProp, startBProp));

							int lastIX = iX-1;

							ibl = lastBr[lastIX];
							itl = lastTr[lastIX];
							itr = currentOutIndex;
							ibr = currentOutIndex + 1;

							currentOutIndex += 2;
						}
					}
					else
					{
						if (iX == 0) // x=0,y>0
						{
							_outVertices.Add( UiMath.Bilerp(bl, tl, tr, br, startAProp, endBProp));
							_outVertices.Add( UiMath.Bilerp(bl, tl, tr, br, endAProp, endBProp));

							ibl = lastTl[iX];
							itl = currentOutIndex;
							itr = currentOutIndex + 1;
							ibr = lastTr[iX];

							currentOutIndex += 2;
						}
						else // x>0,y>0
						{
							_outVertices.Add( UiMath.Bilerp(bl, tl, tr, br, endAProp, endBProp));

							int lastIX = iX-1;

							ibl = lastTl[iX];
							itl = lastTr[lastIX];
							itr = currentOutIndex;
							ibr = lastTr[iX];

							currentOutIndex += 1;
						}

					}

					_outIndices.Add(ibl);
					_outIndices.Add(itl);
					_outIndices.Add(itr);

					_outIndices.Add(itr);
					_outIndices.Add(ibr);
					_outIndices.Add(ibl);

					lastBl[iX] = ibl;
					lastTl[iX] = itl;
					lastTr[iX] = itr;
					lastBr[iX] = ibr;

					startAProp = endAProp;
				}
				startBProp = endBProp;
			}

			return true;
		}
	}
}