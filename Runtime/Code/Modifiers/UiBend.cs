using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	[ExecuteAlways]
	public class UiBend : BaseMeshEffectTMP
	{
		[SerializeField] private float m_angle = 90;

		public Rect Bounding {get; protected set;}

		protected static readonly List<UIVertex> s_verts = new();
		protected static UIVertex s_vertex;

		public override void ModifyMesh(VertexHelper _vertexHelper)
		{
			if (!IsActive())
				return;

			Bounding = UiMeshModifierUtility.GetBounds(s_verts);
			_vertexHelper.GetUIVertexStream(s_verts);
			float s = Mathf.Sin(Mathf.Deg2Rad * m_angle);
			float c = Mathf.Cos(Mathf.Deg2Rad * m_angle);
			
			for (int i = 0; i < _vertexHelper.currentVertCount; ++i)
			{
				_vertexHelper.PopulateUIVertex(ref s_vertex, i);

				var pos = s_vertex.position;
				Vector3 pointNormalized = pos.GetNormalizedPointInRect(Bounding);

				pointNormalized.x -= .5f;

				var pointTransformed = new Vector2
				(
					pointNormalized.x * c - pointNormalized.y * s,
					pointNormalized.x * s + pointNormalized.y * c
				);

				var pointLerped = Vector2.Lerp(pointNormalized, pointTransformed, pointNormalized.y);

				s_vertex.position = new Vector3
				(
					pointLerped.x * Bounding.width,
					pointLerped.y * Bounding.height,
					pos.z
				);

//				pointNormalized.x = Mathf.Sin(pointNormalized.x * Mathf.PI * Mathf.Deg2Rad * m_angle);
//				pointNormalized.y = Mathf.Cos(pointNormalized.y * Mathf.PI * Mathf.Deg2Rad * m_angle);

//				s_vertex.position = new Vector3
//				(
//					pointNormalized.x * Bounding.width,
//					pointNormalized.y * Bounding.height,
//					pos.z
//				);


				_vertexHelper.SetUIVertex(s_vertex, i);
			}
		}
	}
}
