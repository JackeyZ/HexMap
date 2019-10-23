Shader "Custom/WaterShore"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Specular ("Specular", Color) = (0.2, 0.2, 0.2)
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

		#include "CgIncludes/water.cginc"
		#include "CgIncludes/HexCellData.cginc"


        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
			float2 visibility;
        };

        half _Glossiness;
        half _Specular;
        fixed4 _Color;

        UNITY_INSTANCING_BUFFER_START(Props)
        UNITY_INSTANCING_BUFFER_END(Props)

		void vert(inout appdata_full v, out Input data){
			UNITY_INITIALIZE_OUTPUT(Input, data);
			
			float4 cell0 = GetCellData(v, 0);
			float4 cell1 = GetCellData(v, 1);
			float4 cell2 = GetCellData(v, 2);

			data.visibility.x = cell0.x * v.color.x + cell1.x * v.color.y + cell2.x * v.color.z;
			data.visibility.x = lerp(0.25, 1, data.visibility.x);
			data.visibility.y = cell0.y * v.color.x + cell1.y * v.color.y + cell2.y * v.color.z;
		}

        void surf (Input IN, inout SurfaceOutputStandardSpecular o)
        {
			float shore = IN.uv_MainTex.y;
			float foam = Foam(shore, IN.worldPos.xz, _MainTex); // 泡沫（靠岸波、离岸波）
			
			float waves = Waves(IN.worldPos.xz, _MainTex);		// 开阔水面的波浪
			waves *= 1 - shore;									// 相当于waves = waves * （1 - shore）,岸边shore为1，表示越靠岸边，波浪越弱，
 
			fixed4 c = saturate(_Color + max(foam, waves));

			float explored = IN.visibility.y;					// 探索状态

			o.Albedo = c.rgb * IN.visibility.x;
			o.Specular = _Specular * explored;
			o.Smoothness = _Glossiness;
			o.Occlusion = explored;
			o.Alpha = c.a * explored;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
