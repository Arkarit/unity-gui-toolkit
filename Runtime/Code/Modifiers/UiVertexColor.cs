using UnityEngine;

namespace GuiToolkit
{
	public class UiVertexColor : UiGradientBase
	{
		[SerializeField] protected Color m_color;
		protected override Color GetColor(Vector2 _) => m_color;
		protected override bool NeedsMeshBounds => false;
	}
}
