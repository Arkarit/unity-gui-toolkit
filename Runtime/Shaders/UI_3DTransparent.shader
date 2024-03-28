Shader "UIToolkit/Ui3DTransparent"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _ColorSelfIllumination ("Color Self illumination", Color) = (0,0,0,0)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
		[Toggle(FlipNormals)] _FlipNormals("Flip Normals", Float) = 0
 		[Toggle(USE_UI3D)] _UseUi3D ("Use UI3D", Float) = 1
		_Offset ("Offset", Vector) = (0,0,0,0)
		_Scale ("Scale", Vector) = (1,1,1,1)
   }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 200

		ZWrite Off
		ZTest LEqual

        CGPROGRAM

		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard vertex:vert alpha:blend

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		#pragma multi_compile __ FlipNormals
		#pragma multi_compile __ USE_UI3D
		#pragma multi_compile_instancing
		
		#include "UI_3D.cginc"

        ENDCG
    }
    FallBack "Diffuse"
}
