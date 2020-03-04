using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	[ExecuteAlways]
	public class UiFixTMPMesh : BaseMeshEffectTMP
	{
		protected override bool ChangesTopology { get {return true;} }

		public override void ModifyMesh( VertexHelper _vh )
		{
			if (!IsActive())
				return;

			UiModifierUtility.RemoveZeroQuads(_vh);
		}
	}

}