// An invisible ground that only shows the shadow cast on it, for 3D icons (UiIcon3DPreset.ShadowCatcher).
//
// The result is premultiplied: colour = shadow colour * alpha, alpha = how much of the main directional light is
// blocked. Blended with One / OneMinusSrcAlpha, which is also right for the alpha channel, so no second render is
// needed for it. Fades out towards the edge of the quad so the ground never shows a hard border.
//
// One SubShader per pipeline: Built-in (CG, forward base pass) and URP (HLSL, main light shadow map). Both receive only
// the shadow of the MAIN directional light; point and spot lights add nothing. There is none for HDRP yet.
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

	// Universal Render Pipeline. Listed first: the pipeline tag keeps Built-in (and HDRP) from ever compiling it, so
	// a project without URP does not choke on the package includes.
	SubShader
	{
		Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry+10" "RenderType" = "Opaque" "IgnoreProjector" = "True" }

		Pass
		{
			Name "ShadowCatcher"
			Tags { "LightMode" = "UniversalForward" }

			Blend One OneMinusSrcAlpha
			ZWrite On
			Cull Off

			HLSLPROGRAM
			#pragma target 3.5
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
			#pragma multi_compile_fragment _ _SHADOWS_SOFT

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

			CBUFFER_START(UnityPerMaterial)
				half _Strength;
				half4 _ShadowColor;
			CBUFFER_END

			struct Attributes
			{
				float4 positionOS : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float2 uv : TEXCOORD0;
				float3 positionWS : TEXCOORD1;
			};

			Varyings vert( Attributes _input )
			{
				Varyings output;
				VertexPositionInputs positions = GetVertexPositionInputs(_input.positionOS.xyz);
				output.positionCS = positions.positionCS;
				output.positionWS = positions.positionWS;
				output.uv = _input.uv;
				return output;
			}

			half4 frag( Varyings _input ) : SV_Target
			{
				float4 shadowCoord = TransformWorldToShadowCoord(_input.positionWS);
				Light mainLight = GetMainLight(shadowCoord);

				float edge = length(_input.uv * 2 - 1);
				half alpha = (1 - mainLight.shadowAttenuation) * _Strength * (1 - smoothstep(0.5, 1, edge));
				return half4(_ShadowColor.rgb * alpha, alpha);
			}
			ENDHLSL
		}
	}

	// Built-in render pipeline
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
