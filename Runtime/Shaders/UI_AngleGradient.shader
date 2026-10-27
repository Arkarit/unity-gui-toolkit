// An angle gradient around the centre of the graphic: the colour goes from Base to Highlight and back as the angle
// turns, Count times per full turn, and the whole pattern can rotate over time.
//
// Meant for a frame: a UiRoundedImage with a Frame Size has UVs from 0 to 1 over its whole rect, so the angle is
// measured from the rect's centre. With Count 4 the highlights sit in the middle of the four sides and the corners
// are darkest, for any aspect ratio (the angle is taken in UV space, where the corners are always on the diagonals).
// It works on any graphic whose UVs span the rect, e.g. a plain Image without a sprite.
//
// The highlight is 0.5 + 0.5 * cos(Count * (angle - rotation)), raised to Sharpness: 1 is a soft wave, higher
// values make the highlights narrower and leave more of the base colour. The graphic's colour tints the result,
// and its texture (if any) is multiplied in.
Shader "UIToolkit/UI_AngleGradient"
{
	Properties
	{
		[PerRendererData] _MainTex ("Texture (RGB,A)", 2D) = "white" {}
		_ColorBase ("Base Colour", Color) = (0,0,0,1)
		_ColorHighlight ("Highlight Colour", Color) = (1,1,1,1)
		_Count ("Highlights per Turn", Float) = 4
		_Sharpness ("Sharpness", Range(0.1, 32)) = 1
		_Rotation ("Rotation (deg)", Float) = 0
		_Speed ("Rotation Speed (deg/s)", Float) = 30

		_StencilComp ("Stencil Comparison", Float) = 8
		_Stencil ("Stencil ID", Float) = 0
		_StencilOp ("Stencil Operation", Float) = 0
		_StencilWriteMask ("Stencil Write Mask", Float) = 255
		_StencilReadMask ("Stencil Read Mask", Float) = 255
		_ColorMask ("Color Mask", Float) = 15
		[Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
	}

	SubShader
	{
		Tags
		{
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
			"PreviewType" = "Plane"
			"CanUseSpriteAtlas" = "True"
		}

		Stencil
		{
			Ref [_Stencil]
			Comp [_StencilComp]
			Pass [_StencilOp]
			ReadMask [_StencilReadMask]
			WriteMask [_StencilWriteMask]
		}

		Cull Off
		Lighting Off
		ZWrite Off
		ZTest [unity_GUIZTestMode]
		Blend SrcAlpha OneMinusSrcAlpha
		ColorMask [_ColorMask]

		Pass
		{
		CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"
			#include "UnityUI.cginc"

			#pragma multi_compile_local __ UNITY_UI_CLIP_RECT
			#pragma multi_compile_local __ UNITY_UI_ALPHACLIP

			struct appdata_t
			{
				float4 vertex   : POSITION;
				float2 texcoord : TEXCOORD0;
				half4  color    : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float4 vertex        : SV_POSITION;
				float2 texcoord      : TEXCOORD0;
				float4 worldPosition : TEXCOORD1;
				half4  color         : COLOR;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			sampler2D _MainTex;
			float4    _MainTex_ST;
			half4     _TextureSampleAdd;
			half4     _ColorBase;
			half4     _ColorHighlight;
			float     _Count;
			float     _Sharpness;
			float     _Rotation;
			float     _Speed;
			float4    _ClipRect;

			v2f vert(appdata_t v)
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				o.worldPosition = v.vertex;
				o.vertex = UnityObjectToClipPos(o.worldPosition);
				o.texcoord = v.texcoord;
				o.color = v.color;
				return o;
			}

			half4 frag(v2f i) : SV_Target
			{
				// The angle around the rect's centre, in UV space.
				float2 fromCentre = i.texcoord - 0.5;
				float angle = atan2(fromCentre.y, fromCentre.x);

				float rotation = radians(_Rotation + _Speed * _Time.y);
				float wave = 0.5 + 0.5 * cos(_Count * (angle - rotation));
				float highlight = pow(saturate(wave), _Sharpness);

				half4 color = lerp(_ColorBase, _ColorHighlight, highlight);
				color *= tex2D(_MainTex, TRANSFORM_TEX(i.texcoord, _MainTex)) + _TextureSampleAdd;
				color *= i.color;

				#ifdef UNITY_UI_CLIP_RECT
					color.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
				#endif

				#ifdef UNITY_UI_ALPHACLIP
					clip(color.a - 0.001);
				#endif

				return color;
			}
		ENDCG
		}
	}

	FallBack "UI/Default"
}
