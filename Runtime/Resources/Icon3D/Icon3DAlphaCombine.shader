// Combines the two renders of an "exact alpha" 3D icon into one premultiplied-alpha image.
//
// Transparent object materials (glass, particles, anything with ordinary alpha blending) write a wrong alpha
// channel into a render target: SrcAlpha / OneMinusSrcAlpha applies to alpha as well, so a 50% layer over an empty
// target leaves alpha 0.25. The colour channels, on the other hand, are right.
//
// The same scene is therefore rendered twice: over transparent BLACK (_MainTex) and over transparent WHITE
// (_WhiteTex). What the object covers is exactly what the white background no longer shines through:
//
//     white - black = (1 - coverage) per channel   =>   alpha = 1 - mean(white - black)
//
// The colour of the black render is already premultiplied. This does not depend on the blend mode of any material
// or on any pipeline's alpha handling, which is why it is also the plan for URP and HDRP.
Shader "Hidden/UIToolkit/Icon3DAlphaCombine"
{
	Properties
	{
		_MainTex ("Over black", 2D) = "black" {}
		_WhiteTex ("Over white", 2D) = "white" {}
	}

	SubShader
	{
		Cull Off
		ZWrite Off
		ZTest Always
		Blend Off

		Pass
		{
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag
			#include "UnityCG.cginc"

			sampler2D _MainTex;
			sampler2D _WhiteTex;

			fixed4 frag( v2f_img i ) : SV_Target
			{
				half4 black = tex2D(_MainTex, i.uv);
				half4 white = tex2D(_WhiteTex, i.uv);

				half shine = (white.r - black.r + white.g - black.g + white.b - black.b) / 3;
				half alpha = saturate(1 - shine);

				// Premultiplied colour can not exceed its alpha
				return half4(min(black.rgb, alpha.xxx), alpha);
			}
			ENDCG
		}
	}
}
