Shader "Hidden/SoftMask" {

SubShader {
	Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
	LOD 100
	
    Cull Off
	ZWrite Off
	Blend SrcAlpha One
//	ColorMask [_ColorMask]

	Pass {  
		Stencil
		{
		    Ref 0
		    Comp always
			Fail Keep
			ZFail Keep
			Pass Replace
		}

		CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag
			#pragma target 2.0
			
			#include "UnityCG.cginc"

			sampler2D _MainTex;
			float _Softness;
			float _Alpha;

			fixed4 frag (v2f_img i) : SV_Target
			{
return fixed4(i.uv.x, i.uv.y,0,1);
				return saturate(tex2D(_MainTex, i.uv).a / _Softness) * _Alpha;
			}
		ENDCG
	}
}

}
