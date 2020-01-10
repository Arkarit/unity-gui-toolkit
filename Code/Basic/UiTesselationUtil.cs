using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	public static class UiTesselationUtil
	{
		private static UIVertex s_vertex;

		public static void Tesselate( VertexHelper _vertexHelper, float _tessellationSize )
		{
			int numVerts = _vertexHelper.currentVertCount;
			List<UIVertex> oldVerts = new List<UIVertex>(numVerts);
			List<UIVertex[]> newQuads = new List<UIVertex[]>();

			for (int i=0; i<numVerts; i++)
			{
				_vertexHelper.PopulateUIVertex(ref s_vertex, i);
				oldVerts.Add(s_vertex);
			}

			Tesselate(oldVerts, newQuads, _tessellationSize);

			_vertexHelper.Clear();

			int numQuads = newQuads.Count;
			for (int i=0; i<numQuads; i++)
			{
				_vertexHelper.AddUIVertexQuad(newQuads[i]);
			}
		}

		public static void Tesselate( List<UIVertex> _in, List<UIVertex[]> _quadsOut, float _tessellationSize )
		{
			int startingVertexCount = _in.Count;
			for (int i = 0; i < startingVertexCount; i += 4)
			{
				TessellateQuad(_in, _quadsOut, i, _tessellationSize);
			}
		}

		public static void TessellateQuad( List<UIVertex> _in, List<UIVertex[]> _quadsOut, int _startIdx, float _tessellationSize )
		{
			// Read the existing quad vertices
			UIVertex bl = _in[_startIdx];
			UIVertex tl = _in[_startIdx + 1];
			UIVertex tr = _in[_startIdx + 2];
			UIVertex br = _in[_startIdx + 3];

			// Position deltas, A and B are the local quad up and right axes
			Vector3 right = tr.position - tl.position;
			Vector3 up = tl.position - bl.position;

			// Determine how many tiles there should be
			float size = 1.0f / Mathf.Max(1.0f, _tessellationSize);
			int numH = Mathf.CeilToInt(right.magnitude * size);
			int numV = Mathf.CeilToInt(up.magnitude * size);

			// Build the sub quads
			float quadWidth = 1.0f / (float)numH;
			float quadHeight = 1.0f / (float)numV;
			float startBProp = 0.0f;

			for (int iY = 0; iY < numV; ++iY)
			{
				float endBProp = (float)(iY + 1) * quadHeight;
				float startAProp = 0.0f;
				for (int iX = 0; iX < numH; ++iX)
				{
					float endAProp = (float)(iX + 1) * quadWidth;

					UIVertex[] quad = new UIVertex[4];
					// Append new quad to list
					quad[0] = UiMath.Bilerp(bl, tl, tr, br, startAProp, startBProp);
					quad[1] = UiMath.Bilerp(bl, tl, tr, br, startAProp, endBProp);
					quad[2] = UiMath.Bilerp(bl, tl, tr, br, endAProp, endBProp);
					quad[3] = UiMath.Bilerp(bl, tl, tr, br, endAProp, startBProp);
					_quadsOut.Add(quad);

					startAProp = endAProp;
				}
				startBProp = endBProp;
			}
		}
/*
		private static void DumpQuad(UIVertex[] _quad, int _i)
		{
			string s = $"I:{_i} - " 
				+ DumpVertex(_quad[0], 0)
				+ DumpVertex(_quad[1], 1)
				+ DumpVertex(_quad[2], 2)
				+ DumpVertex(_quad[3], 3);
			Debug.Log(s);
		}

		private static string DumpVertex(UIVertex _vert, int _i)
		{
			return $"{_i} (x {_vert.position.x} y {_vert.position.y}), ";
		}
*/
	}
}