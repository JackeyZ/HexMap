Shader "Custom/Terrain" {
	Properties{
		_Color("Color", Color) = (1,1,1,1)
		_MainTex("Terrain Texture Array", 2DArray) = "white" {}
		_Glossiness("Smoothness", Range(0,1)) = 0.5
		_Metallic("Metallic", Range(0,1)) = 0.0
	}
	SubShader
	{
		Tags{ "RenderType" = "Opaque" }
		LOD 200
		
		CGPROGRAM
			#pragma surface surf Standard fullforwardshadows vertex:vert
			#pragma target 3.5

			UNITY_DECLARE_TEX2DARRAY(_MainTex);											// 声明Texture数组Sampler变量。

			half _Glossiness;
			half _Metallic;
			fixed4 _Color;

			struct Input {
				float4 color : COLOR;
				float3 worldPos;
				float3 terrain;
			};
 
			void vert (inout appdata_full v, out Input data) {
				UNITY_INITIALIZE_OUTPUT(Input, data);									// 此宏用于将给定类型的名称变量初始化为零。
				data.terrain = v.texcoord2.xyz;											// 获取第三个uv数据（x是第一个地形类型，y是第二个地形类型，z是第三个地形类型）
			}

			float4 GetTerrainColor (Input IN, int index) {
				float3 uvw = float3(IN.worldPos.xz * 0.02, IN.terrain[index]);
				float4 c = UNITY_SAMPLE_TEX2DARRAY(_MainTex, uvw);						// 对纹理数组进行取样，参数1、2是UV，参数3是第几个纹理
				return c * IN.color[index];												// 利用顶点颜色来做混合（splat贴图）
			}
			
			// 表面着色器，规定参数1是Input结构，参数2是inout的SurfaceOutput结构。
			void surf (Input IN, inout SurfaceOutputStandard o) {
                fixed4 c = GetTerrainColor(IN, 0) + GetTerrainColor(IN, 1) + GetTerrainColor(IN, 2);
                o.Albedo = c.rgb * _Color;												// 纹理颜色与设置的颜色融合
                o.Metallic = _Metallic;													// 金属感
                o.Smoothness = _Glossiness;												// 平滑
                o.Alpha = c.a;
            }
		ENDCG
	}
	FallBack "Diffuse"
}