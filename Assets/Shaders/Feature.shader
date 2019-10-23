Shader "Custom/Feature"			// 特征物体shader
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Specular ("Specular", Color) = (0.2, 0.2, 0.2)
		_BackgroundColor ("Background Color", Color) = (0,0,0)							// 背景颜色
		[NoScaleOffset] _GridCoordinates ("Grid Coordinates", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf StandardSpecular fullforwardshadows vertex:vert

        #pragma target 3.0
		
		// 定义一个全局关键字（产生两个shader变体）用于判断地图是否处于编辑模式
		#pragma multi_compile _ HEX_MAP_EDIT_MODE		

		#include "CgIncludes/HexCellData.cginc"

        sampler2D _MainTex, _GridCoordinates;

        struct Input
        {
            float2 uv_MainTex;
			float2 visibility;
        };

        half _Glossiness;
        half _Specular;
        fixed4 _Color;
        half3 _BackgroundColor;

        UNITY_INSTANCING_BUFFER_START(Props)
        UNITY_INSTANCING_BUFFER_END(Props)

		void vert(inout appdata_full v, out Input data){
			UNITY_INITIALIZE_OUTPUT(Input, data);
			
			float3 pos = mul(unity_ObjectToWorld, v.vertex);
			float4 gridUV = float4(pos.xz, 0, 0);
			gridUV.x *= 1 / (4 * 8.66025404);						// 8.66..是六边形内接圆半径
			gridUV.y *= 1 / (2 * 15.0);								// 外接圆半径是10
			float2 cellDataCoordinates = floor(gridUV.xy) + tex2Dlod(_GridCoordinates, gridUV).rg;	// 位于第几行第几列的纹理里 + 存储在纹理中的偏移量
			cellDataCoordinates *= 2;								// 因为_GridCoordinates 是 2X2的，所以这里要把算出来的坐标乘以2，得到顶点所在六边形位于地图的第几行第几列

			float4 cellData = GetCellData(cellDataCoordinates);		// 根据顶点所在的六边形坐标获取六边形的可见性
			data.visibility.x = cellData.x;							
			data.visibility.x = lerp(0.25, 1, data.visibility.x);
			data.visibility.y = cellData.y;							// 是否被探索
		}

        void surf (Input IN, inout SurfaceOutputStandardSpecular o)
        {
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
			float explored = IN.visibility.y;
            o.Albedo = c.rgb * IN.visibility.x * explored;
            o.Specular = _Specular;
            o.Smoothness = _Glossiness;
            o.Occlusion = explored;
            o.Emission = _BackgroundColor * (1 -  explored);
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
