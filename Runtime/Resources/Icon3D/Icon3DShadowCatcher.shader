// An invisible ground that only shows the shadow cast on it, for 3D icons (UiIcon3DPreset.ShadowCatcher).
//
// The result is premultiplied: colour = shadow colour * alpha, alpha = how much of the main directional light is
// blocked. Blended with One / OneMinusSrcAlpha, which is also right for the alpha channel, so no second render is
// needed for it. Fades out towards the edge of the quad so the ground never shows a hard border.
//
// One SubShader per pipeline: Built-in (CG, forward base pass), URP (HLSL, main light shadow map) and HDRP (HLSL, HDRP's shadow loop). Both receive only
// the shadow of the MAIN directional light; point and spot lights add nothing. HDRP gets its own as well.
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

	// High Definition Render Pipeline. Same trick as the Shader Graph "Shadow Matte" of HDRP's unlit target: the shadow of
	// the main directional light from HDRP's own shadow loop, written as colour and alpha. Hand written because a Shader
	// Graph asset can not ship in a package that also serves projects without Shader Graph. The pipeline tag keeps it
	// from being compiled anywhere else.
	SubShader
	{
		Tags { "RenderPipeline" = "HDRenderPipeline" "Queue" = "Transparent" "RenderType" = "Transparent" }

		Pass
		{
			Name "ForwardOnly"
			Tags { "LightMode" = "ForwardOnly" }

			Blend One OneMinusSrcAlpha
			ZWrite Off
			ZTest LEqual
			Cull Off

			HLSLPROGRAM
			#pragma target 4.5
			#pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
			#pragma vertex Vert
			#pragma fragment Frag
			#pragma multi_compile_fragment _ SHADOWS_SHADOWMASK
			#pragma multi_compile_fragment SCREEN_SPACE_SHADOWS_OFF SCREEN_SPACE_SHADOWS_ON
			#pragma multi_compile_fragment SHADOW_LOW SHADOW_MEDIUM SHADOW_HIGH
			#pragma multi_compile_fragment AREA_SHADOW_MEDIUM AREA_SHADOW_HIGH

			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl"
			#define SHADERPASS SHADERPASS_FORWARD_UNLIT
			#define HAS_LIGHTLOOP

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonLighting.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariablesFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/HDShadow.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoopDef.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/PunctualLightCommon.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/HDShadowLoop.hlsl"

			CBUFFER_START(UnityPerMaterial)
				float _Strength;
				float4 _ShadowColor;
			CBUFFER_END

			struct Attributes
			{
				float3 positionOS : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float3 positionRWS : TEXCOORD0;
				float2 uv : TEXCOORD1;
			};

			Varyings Vert( Attributes _input )
			{
				Varyings output;
				output.positionRWS = TransformObjectToWorld(_input.positionOS);
				output.positionCS = TransformWorldToHClip(output.positionRWS);
				output.uv = _input.uv;
				return output;
			}

			float4 Frag( Varyings _input ) : SV_Target
			{
				PositionInputs posInput = GetPositionInput(_input.positionCS.xy, _ScreenSize.zw, _input.positionCS.z, _input.positionCS.w, _input.positionRWS);

				HDShadowContext shadowContext = InitShadowContext();
				float3 shadow3;
				ShadowLoopMin(shadowContext, posInput, float3(0, 1, 0), LIGHTFEATUREFLAGS_DIRECTIONAL, 0xFFFFFFFF, shadow3);   // every rendering layer: the function that reads the mesh's own is named differently in HDRP 14 and 17

				float lit = dot(shadow3, float3(1.0 / 3.0, 1.0 / 3.0, 1.0 / 3.0));
				float edge = length(_input.uv * 2 - 1);
				float alpha = (1 - lit) * _Strength * (1 - smoothstep(0.5, 1, edge));
				return float4(_ShadowColor.rgb * alpha, alpha);
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
