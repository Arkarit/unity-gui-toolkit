using UnityEngine;

namespace GuiToolkit
{
	/// <summary>
	/// A mesh-effect modifier (UiGradientBase) that applies a single flat color to the vertices of the Graphic or
	/// TextMeshPro text on the same GameObject, combined via multiply, replace or add.
	/// </summary>
	public class UiVertexColor : UiGradientBase
	{
		[SerializeField] protected Color m_color;
		protected override Color GetColor(Vector2 _) => m_color;
		protected override bool NeedsMeshBounds => false;
	}
}
