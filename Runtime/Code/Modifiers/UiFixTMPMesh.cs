using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	/// <summary>
	/// A topology-changing mesh-effect modifier (BaseMeshEffectTMP) that removes degenerate zero-area quads from the
	/// mesh of the (TextMeshPro) text on the same GameObject.
	/// </summary>
	[ExecuteAlways]
	public class UiFixTMPMesh : BaseMeshEffectTMP
	{
		protected override bool ChangesTopology { get {return true;} }

		public override void ModifyMesh( VertexHelper _vh )
		{
			if (!IsActive())
				return;

			UiMeshModifierUtility.RemoveZeroQuads(_vh);
		}
	}

}
