using UnityEditor;

namespace GuiToolkit.Editor
{
	public class UiGlitterShaderEditor : ShaderEditorBase
	{
		protected override void OnGUI()
		{
			DisplayProperty("_MainTex", "Optional mask. The alpha channel modulates the glitter (white = fully visible). Note that this is overwritten by CanvasRenderer when used in UI context.");
			DisplayProperty("_Color", "Tint of the sparkles. Alpha scales overall opacity.");

			SmallSpace();
			DisplayProperty("_Density", "Cells per UV unit. Higher = more (and smaller) sparkles. Try 4..32.");
			DisplayProperty("_Coverage", "Fraction of cells that actually contain a sparkle. 0 = none, 1 = every cell.");
			DisplayProperty("_Speed", "Twinkle speed in cycles per second. Each cell has its own randomized period multiplier on top of this.");
			DisplayProperty("_Size", "Sparkle size relative to a cell. <1 = sparkle fits inside its cell; >1 = sparkle bleeds into neighbors (3x3 sampling handles this).");
			DisplayProperty("_SpikeSharpness", "How thin the cross spikes are. 1 = round blob, 32 = needle-thin star.");
			DisplayProperty("_Brightness", "Output multiplier. With additive blending you can dial this above 1.");

			SmallSpace();
			KeywordToggle("_UseColorVariation", () =>
			{
				DisplayProperty("_ColorVariation", "0 = uniform tint; 1 = full rainbow per sparkle (still multiplied with the vertex color).");
			});

			LargeSpace();
			Foldout("_FoldoutStencil", () =>
			{
				DisplayProperty("_StencilComp");
				DisplayProperty("_Stencil");
				DisplayProperty("_StencilOp");
				DisplayProperty("_StencilWriteMask");
				DisplayProperty("_StencilReadMask");
				DisplayProperty("_ColorMask");
				SmallSpace();
				DisplayProperty("_UseUIAlphaClip");
			});

			LargeSpace();
			Foldout("_FoldoutBlendMode", () =>
			{
				DisplayProperty("_SrcBlend");
				DisplayProperty("_DstBlend");
				DisplayBlendingHelper();
				SmallSpace();
				DisplayProperty("_Cull");
				DisplayProperty("_ZTest");
				DisplayProperty("_ZWrite");
				MaterialEditor.RenderQueueField();
			});
		}
	}
}
