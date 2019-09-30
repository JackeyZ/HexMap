Shader "Custom/Estuaries" // 河口shader
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
        #pragma surface surf Standard alpha vertex:vert

        #pragma target 3.0

		#include "CgIncludes/water.cginc"
		#include "CgIncludes/HexCellData.cginc"

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
			float2 riverUV;
			float visibility;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;

        UNITY_INSTANCING_BUFFER_START(Props)
        UNITY_INSTANCING_BUFFER_END(Props)

		// 顶点函数
		// appdata_full：位置position，切线tangent，法线normal，四个纹理（UV）坐标（texcoord、texcoord1、texcoord2、texcoord3）和颜色。
		void vert (inout appdata_full v, out Input o) {
			UNITY_INITIALIZE_OUTPUT(Input, o);							// 此宏用于将给定类型的名称变量初始化为零
			o.riverUV = v.texcoord1.xy;
			
			float4 cell0 = GetCellData(v, 0);
			float4 cell1 = GetCellData(v, 1);

			o.visibility = cell0.x * v.color.x + cell1.x * v.color.y;
			o.visibility = lerp(0.25, 1, o.visibility);
		}

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
			float shore = IN.uv_MainTex.y;
			float foam = Foam(shore, IN.worldPos.xz, _MainTex);			// 泡沫（靠岸波、离岸波）
			
			float waves = Waves(IN.worldPos.xz, _MainTex);				// 开阔水面的波浪
			waves *= 1 - shore;											// 相当于waves = waves * （1 - shore）,岸边shore为1，表示越靠岸边，波浪越弱，

			float shoreWater = max(foam, waves);						// 泡沫加上水平面波纹

			float river = River(IN.riverUV, _MainTex);
			float water = lerp(shoreWater, river, IN.uv_MainTex.x);		// 线性插值，融合两种波纹（IN.uv_MainTex.x瀑布下方为1，扩散外围为0），
																		// 这里插值之后代表越靠近瀑布river波纹越明显，shoreWater波纹渐渐消失
 
			fixed4 c = saturate(_Color + water);
			o.Albedo = c.rgb * IN.visibility;
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
