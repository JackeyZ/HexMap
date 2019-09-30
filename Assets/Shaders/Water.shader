﻿Shader "Custom/Water"
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
        Tags { "RenderType"="Opaque"  "Queue"="Transparent+1"} // +1 确保水在河流渲染之后再渲染
        LOD 200

        CGPROGRAM
		// 没有阴影不需要fullforwardshadows
        #pragma surface surf Standard alpha vertex:vert

        #pragma target 3.0
		
		#include "CgIncludes/Water.cginc"
		#include "CgIncludes/HexCellData.cginc"

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
			float visibility;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;

        UNITY_INSTANCING_BUFFER_START(Props)
        UNITY_INSTANCING_BUFFER_END(Props)

		void vert(inout appdata_full v, out Input data){
			UNITY_INITIALIZE_OUTPUT(Input, data);
			
			// 获得顶点临近三个格子的数据，其中x是格子可见性
			float4 cell0 = GetCellData(v, 0);
			float4 cell1 = GetCellData(v, 1);
			float4 cell2 = GetCellData(v, 2);
			
			// 根据临近格子的可见性和格子对顶点的占比，计算出本顶点的可见性
			data.visibility = cell0.x * v.color.x + cell1.x * v.color.y + cell2.x * v.color.z;		
			data.visibility = lerp(0.25, 1, data.visibility);
		}

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
			float waves = Waves(IN.worldPos.xz, _MainTex);

			fixed4 c = saturate(_Color + waves); // saturate挤压，不让rgba的数值超过1
			o.Albedo = c.rgb * IN.visibility;
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
