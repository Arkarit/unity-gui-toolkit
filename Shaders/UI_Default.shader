Shader "UIToolkit/UI_Default"
{
    Properties
    {
        _MainTex ("Base (RGB), Alpha (A)", 2D) = "white" {}

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
		[Toggle(Disabled)] _Disabled("Disabled", Float) = 0
		_DisabledAlpha("Disabled Alpha", Range(0,1)) = 0.7
		_DisabledDesaturateStrength("Disabled Desaturate Strength", Range(0,1)) = 0.8
		_DisabledBrightness("Disabled Brightness", Range(0,1)) = 0.7

    }
    SubShader
    {
        LOD 100

        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType"="Plane"
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

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #pragma multi_compile_local __ Disabled

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
                fixed4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                fixed4 color : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            fixed4 _TextureSampleAdd;

            bool _UseClipRect;
            float4 _ClipRect;

            bool _UseAlphaClip;
			float _DisabledAlpha;
			float _DisabledDesaturateStrength;
			float _DisabledBrightness;

            v2f vert (appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(o.worldPosition);

                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);

				#ifdef Disabled
					float3 luminance = (0.22 * v.color.r) + (0.72 * v.color.g) + (0.06 * v.color.b) ;
					o.color.rgb = lerp(v.color.rgb, luminance * _DisabledBrightness, _DisabledDesaturateStrength);
					o.color.a = v.color.a * _DisabledAlpha; 
				#else
					o.color = v.color;
				#endif

                return o;
            }

            fixed4 frag (v2f i) : COLOR
            {
                fixed4 color = (tex2D(_MainTex, i.texcoord) + _TextureSampleAdd) * i.color;

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip (color.a - 0.001);
                #endif

                return color;
            }
            ENDCG

        }
    }

	FallBack "UI/Default"
}
