Shader "Custom/Terrain" {
	Properties{
		_Color("Color", Color) = (1,1,1,1)
		_MainTex("Terrain Texture Array", 2DArray) = "white" {}
		_NormalTex("Terrain Normal Array", 2DArray) = "Blue" {}
		_GridTex ("Grid Texture", 2D)= "white" {}										// 网格贴图
		_Glossiness("Smoothness", Range(0,1)) = 0.5
		//_Metallic("Metallic", Range(0,1)) = 0.0
		_Specular ("Specular", Color) = (0.2, 0.2, 0.2)
		_NormalMapDegree("法线贴图表现程度", Range(0, 1)) = 1
		_BackgroundColor ("Background Color", Color) = (0,0,0)							// 背景颜色
		[Toggle(SHOW_MAP_DATA)] _ShowMapData ("Show Map Data", Float) = 0				// 是否显示数据而不显示贴图
	}
	SubShader
	{
		Tags{ "RenderType" = "Opaque" }
		LOD 200
		
		CGPROGRAM
			#pragma surface surf StandardSpecular fullforwardshadows vertex:vert
			#pragma target 3.5

			// 定义一个全局关键字（产生两个shader变体）用于切换是否显示网格，C#中使用Material.EnableKeyword("GRID_ON");和Material.DisableKeyword("GRID_ON");切换
			#pragma multi_compile _ GRID_ON				
			// 定义一个全局关键字（产生两个shader变体）用于判断地图是否处于编辑模式
			#pragma multi_compile _ HEX_MAP_EDIT_MODE							
			// 是否显示数据而不显示贴图
			#pragma shader_feature SHOW_MAP_DATA

			// 导入获得格子数据的文件
			#include "CgIncludes/HexCellData.cginc" 	
			
			UNITY_DECLARE_TEX2DARRAY(_MainTex);											// 声明Texture数组Sampler变量。
			UNITY_DECLARE_TEX2DARRAY(_NormalTex);										// 声明Texture数组Sampler变量。
			sampler2D _GridTex;
			half _Glossiness;
			//half _Metallic;
			fixed3 _Specular;
			fixed4 _Color;
			float _NormalMapDegree;
			half3 _BackgroundColor;

			struct Input {
				float4 color : COLOR;
				float3 worldPos;				// 顶点世界坐标
				float3 terrain;					// 地形类型下标
				float4 visibility;				// 是否可见（战争迷雾）1表示可见，0表示不可见，xyz代表附近三个地形的可见性，w表示探索状态（是否被探索过）

				#if defined(SHOW_MAP_DATA)
					float mapData;
				#endif
			};
 
			void vert (inout appdata_full v, out Input data) {
				UNITY_INITIALIZE_OUTPUT(Input, data);									// 此宏用于将给定类型的名称变量初始化为零。

				// 获取顶点附近三个六边形格子的数据
				float4 cell0 = GetCellData(v, 0);
				float4 cell1 = GetCellData(v, 1);
				float4 cell2 = GetCellData(v, 2);
 
				// 获取地形数据（x是第一个邻居六边形的地形，y是第二个邻居六边形的地形，z是第三个邻居六边形的地形）
				data.terrain.x = cell0.w;
				data.terrain.y = cell1.w;
				data.terrain.z = cell2.w;		
				
				// 附近三个地形是否可见
				data.visibility.x = cell0.x;
				data.visibility.y = cell1.x;
				data.visibility.z = cell2.x;
				data.visibility.xyz = lerp(0.25, 1, data.visibility.xyz);
				data.visibility.w = cell0.y * v.color.x + cell1.y * v.color.y + cell2.y * v.color.z;	// 用附近三个格子探索状态乘以三个格子对顶点的权重，的到当前顶点的探索状态

				#if defined(SHOW_MAP_DATA)
					data.mapData = cell0.z * v.color.x + cell1.z * v.color.y + cell2.z * v.color.z;		// cell0.z储存了地图格子数据
				#endif
			}

			float4 GetTerrainColor (Input IN, int index) {
				float3 uvw = float3(IN.worldPos.xz * 0.02, IN.terrain[index]);
				float4 c = UNITY_SAMPLE_TEX2DARRAY(_MainTex, uvw);						// 对纹理数组进行取样，参数1、2是UV，参数3是第几个纹理
				c *= IN.color[index];													// 利用顶点颜色来做混合（splat贴图）（color.r是第一个地形颜色占比，color.g是第二个地形颜色占比，color.b是第三个地形颜色占比）
				c *= IN.visibility[index];												// 战争迷雾
				return c;
			}
			
			float4 GetTerrainNormal (Input IN, int index) {
				float3 uvw = float3(IN.worldPos.xz * 0.02, IN.terrain[index]);
				float4 c = UNITY_SAMPLE_TEX2DARRAY(_NormalTex, uvw);	
				float4 defaultNormal = float4(0.5, 0.5, 1, 1); //  * 2 - 1之后就是（0, 0, 1）
				return (c - defaultNormal) * IN.color[index] * _NormalMapDegree + defaultNormal * 0.33333;	
			}

			// 表面着色器，规定参数1是Input结构，参数2是inout的SurfaceOutput结构。
			void surf (Input IN, inout SurfaceOutputStandardSpecular o) {
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

				float explored = IN.visibility.w;														// 探索状态（是否被探索过）
                o.Albedo = c.rgb * _Color * grid * explored;											// 纹理颜色与设置的颜色融合
				#if defined(SHOW_MAP_DATA)
					o.Albedo = IN.mapData * grid;
				#endif
                o.Specular = _Specular * explored;														// 高光
                o.Smoothness = _Glossiness;																// 平滑
				o.Occlusion = explored;																	// 遮挡反射
				o.Emission = _BackgroundColor * (1 -  explored);										// 自发光
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