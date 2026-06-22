using UnityEditor;

namespace GuiToolkit.Editor
{
	public class UiGlitterShaderEditor : ShaderEditorBase
	{
		protected override void OnGUI()
		{
			DisplayProperty("_MainTex", "Optional mask. The alpha channel modulates the glitter (white = fully visible). Note that this is overwritten by CanvasRenderer when used in UI context.");

			SmallSpace();
			DisplayProperty("_ColorMode", "Color picking strategy per sparkle. Off: all sparkles use Color 1. Rainbow: continuous hue cycle. TwoColor / ThreeColor: each sparkle is hashed to one of the palette colors. DualRender: outer star in Color 1, inner star in Color 2 (two-color gradient per sparkle).");
			DisplayProperty("_Color", "Primary tint. Used for all sparkles when Color Mode is Off; used as one of the palette colors otherwise.");
			var colorModeProp = FindProperty("_ColorMode", MaterialProperties, false);
			int colorMode = colorModeProp != null ? (int)colorModeProp.floatValue : 0;
			if (colorMode == 1) // Rainbow
			{
				DisplayProperty("_ColorVariation", "0 = uniform tint; 1 = full rainbow per sparkle (still multiplied with the vertex color).");
				DisplayProperty("_RainbowFloor", "Minimum value for each rainbow channel. 0 = dark hues possible (highly saturated, but blue/violet sparkles can look like dark holes); 0.4 = pastel rainbow with no dark hues; 1 = all hues collapse to white.");
			}
			else if (colorMode == 2) // TwoColor
			{
				DisplayProperty("_Color2", "Second palette color. Each sparkle is randomly assigned Color 1 or Color 2.");
			}
			else if (colorMode == 3) // ThreeColor
			{
				DisplayProperty("_Color2", "Second palette color.");
				DisplayProperty("_Color3", "Third palette color. Each sparkle is randomly assigned one of the three colors with equal probability.");
			}
			else if (colorMode == 4) // DualRender
			{
				DisplayProperty("_Color2", "Inner color. Layered on top of Color 1 at a smaller size, giving each sparkle a two-color gradient (Color 1 on the outside, Color 2 in the core).");
				DisplayProperty("_DualScale", "Size of the inner (Color 2) star relative to the outer. 0.5 = half size, 1 = same size (Color 2 wins everywhere, Color 1 invisible).");
			}

			SmallSpace();
			DisplayProperty("_Density", "Cells per UV unit. Higher = more (and smaller) sparkles. Try 4..32.");
			DisplayProperty("_Coverage", "Fraction of cells that actually contain a sparkle. 0 = none, 1 = every cell.");
			DisplayProperty("_Speed", "Twinkle speed in cycles per second. Each cell has its own randomized period multiplier on top of this.");
			DisplayProperty("_ScrollSpeedX", "Horizontal scroll speed of the sparkle field in UV units per second. 0 = no scrolling. The mask texture stays static.");
			DisplayProperty("_ScrollSpeedY", "Vertical scroll speed of the sparkle field in UV units per second.");
			DisplayProperty("_SizeMin", "Minimum sparkle size, relative to a cell. Each sparkle picks a random size between min and max.");
			DisplayProperty("_SizeMax", "Maximum sparkle size, relative to a cell. Set equal to min for uniform size.");
			DisplayProperty("_SpikeSharpness", "How thin the cross spikes are. 1 = round blob, 32 = needle-thin star.");
			DisplayProperty("_Brightness", "Output multiplier. With additive blending you can dial this above 1.");

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
