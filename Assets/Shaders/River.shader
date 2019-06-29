﻿Shader "Custom/River"
{
    Properties
    {
        _Color ("Color", Color) = (0,0,0,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque"  "Queue"="Transparent"}
        LOD 200

        CGPROGRAM
		// 没有阴影不需要fullforwardshadows
        #pragma surface surf Standard alpha

        #pragma target 3.0
		
		#include "CgIncludes/Water.cginc"

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
			float4 color : COLOR0;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;

        UNITY_INSTANCING_BUFFER_START(Props)
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
			float riverNoise = River(IN.uv_MainTex, _MainTex);
			fixed4 c =  saturate(_Color + riverNoise);  // _Color四个分量都加上噪声 ,saturate挤压，不让rgba的数值超过1
			o.Albedo = c.rgb;
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
