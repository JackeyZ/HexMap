Shader "Custom/Feature"			// 特征物体shader
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
		[NoScaleOffset] _GridCoordinates ("Grid Coordinates", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert

        #pragma target 3.0

		#include "CgIncludes/HexCellData.cginc"

        sampler2D _MainTex, _GridCoordinates;

        struct Input
        {
            float2 uv_MainTex;
			float visibility;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;

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

			data.visibility = GetCellData(cellDataCoordinates).x;	// 根据顶点所在的六边形坐标获取六边形的可见性
			data.visibility = lerp(0.25, 1, data.visibility);
		}

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb * IN.visibility;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
