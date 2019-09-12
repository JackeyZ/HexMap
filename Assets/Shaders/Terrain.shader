Shader "Custom/Terrain" {
	Properties{
		_Color("Color", Color) = (1,1,1,1)
		_MainTex("Terrain Texture Array", 2DArray) = "white" {}
		_NormalTex("Terrain Normal Array", 2DArray) = "Blue" {}
		_GridTex ("Grid Texture", 2D)= "white" {}										// 网格贴图
		_Glossiness("Smoothness", Range(0,1)) = 0.5
		_Metallic("Metallic", Range(0,1)) = 0.0
		_NormalMapDegree("法线贴图表现程度", Range(0, 1)) = 1
	}
	SubShader
	{
		Tags{ "RenderType" = "Opaque" }
		LOD 200
		
		CGPROGRAM
			#pragma surface surf Standard fullforwardshadows vertex:vert
			#pragma target 3.5
			
			// 定义一个全局关键字（产生两个shader变体）用于切换是否显示网格，C#中使用Material.EnableKeyword("GRID_ON");和Material.DisableKeyword("GRID_ON");切换
			#pragma multi_compile _ GRID_ON												
			
			UNITY_DECLARE_TEX2DARRAY(_MainTex);											// 声明Texture数组Sampler变量。
			UNITY_DECLARE_TEX2DARRAY(_NormalTex);										// 声明Texture数组Sampler变量。
			sampler2D _GridTex;
			half _Glossiness;
			half _Metallic;
			fixed4 _Color;
			float _NormalMapDegree;

			struct Input {
				float4 color : COLOR;
				float3 worldPos;				// 顶点世界坐标
				float3 terrain;
			};
 
			void vert (inout appdata_full v, out Input data) {
				UNITY_INITIALIZE_OUTPUT(Input, data);									// 此宏用于将给定类型的名称变量初始化为零。
				data.terrain = v.texcoord2.xyz;											// 获取第三个uv数据（x是第一个地形类型，y是第二个地形类型，z是第三个地形类型）
			}

			float4 GetTerrainColor (Input IN, int index) {
				float3 uvw = float3(IN.worldPos.xz * 0.02, IN.terrain[index]);
				float4 c = UNITY_SAMPLE_TEX2DARRAY(_MainTex, uvw);						// 对纹理数组进行取样，参数1、2是UV，参数3是第几个纹理
				return c * IN.color[index];												// 利用顶点颜色来做混合（splat贴图）（color.r是第一个地形颜色占比，color.g是第二个地形颜色占比，color.b是第三个地形颜色占比）
			}
			
			float4 GetTerrainNormal (Input IN, int index) {
				float3 uvw = float3(IN.worldPos.xz * 0.02, IN.terrain[index]);
				float4 c = UNITY_SAMPLE_TEX2DARRAY(_NormalTex, uvw);	
				float4 defaultNormal = float4(0.5, 0.5, 1, 1); //  * 2 - 1之后就是（0, 0, 1）
				return (c - defaultNormal) * IN.color[index] * _NormalMapDegree + defaultNormal * 0.33333;	
			}

			// 表面着色器，规定参数1是Input结构，参数2是inout的SurfaceOutput结构。
			void surf (Input IN, inout SurfaceOutputStandard o) {
                fixed4 c = GetTerrainColor(IN, 0) + GetTerrainColor(IN, 1) + GetTerrainColor(IN, 2);		// 读取三个地形的颜色然后叠加
				fixed4 normal;
				if(_NormalMapDegree != 0)
				{
					normal = GetTerrainNormal(IN, 0) + GetTerrainNormal(IN, 1) + GetTerrainNormal(IN, 2);
				}

				fixed4 grid = 1;
				#if defined(GRID_ON)
					float2 gridUV = IN.worldPos.xz;
					gridUV.x *= 1 / (4 * 8.66025404);													// 贴图u的0~1有两个六边形，所以除以4倍内接圆半径
					gridUV.y *= 1 / (2 * 15.0);
					grid = tex2D(_GridTex, gridUV);														// 网格线
				#endif

                o.Albedo = c.rgb * _Color * grid;														// 纹理颜色与设置的颜色融合
                o.Metallic = _Metallic;																	// 金属感
                o.Smoothness = _Glossiness;																// 平滑
                o.Alpha = c.a;				
				if(_NormalMapDegree != 0)
				{
					o.Normal = UnpackNormal(normal);
				}
            }
		ENDCG
	}
	FallBack "Diffuse"
}