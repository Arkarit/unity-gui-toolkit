using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	public static class UiTesselationUtil
	{
		private const float MIN_TESSELATE_SIZE = 15.0f;

		private static UIVertex s_vertex;

		public static bool Tesselate( VertexHelper _vertexHelper, float _sizeH, float _sizeV )
		{
			int numVerts = _vertexHelper.currentVertCount;
			List<UIVertex> oldVerts = new List<UIVertex>(numVerts);
			List<UIVertex> newVerts = new List<UIVertex>();
			List<int> newIndices = new List<int>();

			for (int i=0; i<numVerts; i++)
			{
				_vertexHelper.PopulateUIVertex(ref s_vertex, i);
				oldVerts.Add(s_vertex);
			}

			Tesselate(oldVerts, newVerts, newIndices, _sizeH, _sizeV);

			_vertexHelper.Clear();
			_vertexHelper.AddUIVertexStream(newVerts, newIndices);
			return true;
		}

		public static bool Tesselate( List<UIVertex> _inQuadVertices, List<UIVertex> _outQuadVertices, List<int> _outIndices, float _sizeH, float _sizeV )
		{
			int startingVertexCount = _inQuadVertices.Count;
			for (int i = 0; i < startingVertexCount; i += 4)
			{
				TessellateQuad(_inQuadVertices, _outQuadVertices, _outIndices, i, _sizeH, _sizeV);
			}
			return true;
		}

		public static void TessellateQuad( List<UIVertex> _inQuadVertices, List<UIVertex> _outVertices, List<int> _outIndices, int _startIdx, float _sizeH, float _sizeV )
		{
			// Read the existing quad vertices
			UIVertex bl = _inQuadVertices[_startIdx];
			UIVertex tl = _inQuadVertices[_startIdx + 1];
			UIVertex tr = _inQuadVertices[_startIdx + 2];
			UIVertex br = _inQuadVertices[_startIdx + 3];

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

			_outVertices.Capacity = numH * numV * 4;
			int currentOutIndex = 0;

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

					if (true || iY==0 )
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
		}

	}
}