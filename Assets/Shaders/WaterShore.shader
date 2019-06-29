Shader "Custom/WaterShore"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
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

		#include "CgIncludes/water.cginc"

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;

        UNITY_INSTANCING_BUFFER_START(Props)
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
			float shore = IN.uv_MainTex.y;
			float foam = Foam(shore, IN.worldPos.xz, _MainTex); // 泡沫（靠岸波、离岸波）
			
			float waves = Waves(IN.worldPos.xz, _MainTex);		// 开阔水面的波浪
			waves *= 1 - shore;									// 相当于waves = waves * （1 - shore）,岸边shore为1，表示越靠岸边，波浪越弱，
 
			fixed4 c = saturate(_Color + max(foam, waves));

			o.Albedo = c.rgb;
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
