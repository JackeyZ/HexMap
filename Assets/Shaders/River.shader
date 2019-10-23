Shader "Custom/River"
{
    Properties
    {
        _Color ("Color", Color) = (0,0,0,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Specular ("Specular", Range(0,1)) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque"  "Queue"="Transparent"}
        LOD 200

        CGPROGRAM
		// 没有阴影不需要fullforwardshadows
        #pragma surface surf StandardSpecular alpha vertex:vert

        #pragma target 3.0
		
		// 定义一个全局关键字（产生两个shader变体）用于判断地图是否处于编辑模式
		#pragma multi_compile _ HEX_MAP_EDIT_MODE		

		#include "CgIncludes/Water.cginc"
		#include "CgIncludes/HexCellData.cginc"

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
			float4 color : COLOR0;
			float2 visibility;
        };

        half _Glossiness;
        half _Specular;
        fixed4 _Color;

        UNITY_INSTANCING_BUFFER_START(Props)
        UNITY_INSTANCING_BUFFER_END(Props)

		void vert (inout appdata_full v, out Input data){
			UNITY_INITIALIZE_OUTPUT(Input, data);
			
			float4 cell0 = GetCellData(v, 0);
			float4 cell1 = GetCellData(v, 1);

			data.visibility.x = cell0.x * v.color.x + cell1.x * v.color.y;		// 是否可见
			data.visibility.x = lerp(0.25, 1, data.visibility);
			data.visibility.y = cell0.y * v.color.x + cell1.y * v.color.y;		// 探索状态（是否探索过）
		}

        void surf (Input IN, inout SurfaceOutputStandardSpecular o)
        {
			float riverNoise = River(IN.uv_MainTex, _MainTex);
			float explored = IN.visibility.y;
			fixed4 c =  saturate(_Color + riverNoise);  // _Color四个分量都加上噪声 ,saturate挤压，不让rgba的数值超过1
			o.Albedo = c.rgb * IN.visibility.x;
			o.Specular = _Specular * explored;
			o.Occlusion = explored;
			o.Smoothness = _Glossiness;
			o.Alpha = c.a * explored;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
