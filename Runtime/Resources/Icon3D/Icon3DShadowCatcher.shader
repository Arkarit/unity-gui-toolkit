// An invisible ground that only shows the shadow cast on it, for 3D icons (UiIcon3DPreset.ShadowCatcher).
//
// The result is premultiplied: colour = shadow colour * alpha, alpha = how much of the main directional light is
// blocked. Blended with One / OneMinusSrcAlpha, which is also right for the alpha channel, so no second render is
// needed for it. Fades out towards the edge of the quad so the ground never shows a hard border.
//
// Forward base pass only: it receives the shadow of the main directional light. Point and spot lights add nothing.
//
// It lives in the OPAQUE queue and writes depth although it blends: directional shadows are collected in screen space
// from the camera depth texture, which only holds opaque geometry. A transparent ground has no depth there, and the
// shadow would fall on whatever is behind it - i.e. nothing.
Shader "Hidden/UIToolkit/Icon3DShadowCatcher"
{
	Properties
	{
		_Strength ("Strength", Range(0, 1)) = 0.6
		_ShadowColor ("Shadow Colour", Color) = (0, 0, 0, 1)
	}

	SubShader
	{
		Tags { "Queue" = "Geometry+10" "RenderType" = "Opaque" "IgnoreProjector" = "True" }

		Pass
		{
			Tags { "LightMode" = "ForwardBase" }

			Blend One OneMinusSrcAlpha
			ZWrite On
			Cull Off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fwdbase
			#include "UnityCG.cginc"
			#include "AutoLight.cginc"

			fixed _Strength;
			fixed4 _ShadowColor;

			struct v2f
			{
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
				SHADOW_COORDS(1)
			};

			v2f vert( appdata_base v )
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = v.texcoord.xy;
				TRANSFER_SHADOW(o)
				return o;
			}

			fixed4 frag( v2f i ) : SV_Target
			{
				fixed lit = SHADOW_ATTENUATION(i);
				float edge = length(i.uv * 2 - 1);
				fixed alpha = (1 - lit) * _Strength * (1 - smoothstep(0.5, 1, edge));
				return fixed4(_ShadowColor.rgb * alpha, alpha);
			}
			ENDCG
		}
	}

	// Provides the ShadowCaster pass the camera depth texture is rendered with
	Fallback "VertexLit"
}
