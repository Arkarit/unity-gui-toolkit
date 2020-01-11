using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	[ExecuteAlways]
	public class UiDistort : BaseMeshEffect
	{
		public Vector2 m_topLeft = Vector2.up;
		public Vector2 m_topRight = Vector2.one;
		public Vector2 m_bottomLeft = Vector2.zero;
		public Vector2 m_bottomRight = Vector2.right;

		public override void ModifyMesh( VertexHelper _vh )
		{
			if (!IsActive())
				return;

		}

#if UNITY_EDITOR
		protected override void OnValidate()
		{
			this.SetDirty();
		}
#endif
	}
}
