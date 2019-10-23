Shader "Custom/Road" {
    Properties {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Specular ("Specular", Color) = (0.2, 0.2, 0.2)
    }
    SubShader {
        Tags { "RenderType"="Opaque"  "Queue" = "Geometry+1"}  // "Queue" = "Geometry+1"把渲染顺序放在所有普通几何体之后
        LOD 200
		Offset -1, -1  // 深度测试偏移
         
        CGPROGRAM
        #pragma surface surf StandardSpecular fullforwardshadows decal:blend vertex:vert
        #pragma target 3.0
		
		// 定义一个全局关键字（产生两个shader变体）用于判断地图是否处于编辑模式
		#pragma multi_compile _ HEX_MAP_EDIT_MODE		

		// 导入获得格子数据的文件
		#include "CgIncludes/HexCellData.cginc" 
 
        sampler2D _MainTex;
 
        struct Input {
            float2 uv_MainTex;
			float3 worldPos;
			float2 visibility;
        };
 
        half _Glossiness;
        half _Specular;
        fixed4 _Color;
 
		void vert(inout appdata_full v, out Input data){
			UNITY_INITIALIZE_OUTPUT(Input, data);
 
			float4 cell0 = GetCellData(v, 0);
			float4 cell1 = GetCellData(v, 1);
 
			data.visibility.x = cell0.x * v.color.x + cell1.x * v.color.y;	// 每个格子用可见性*格子占比，再相加获得当前顶点的可见性
			data.visibility.x = lerp(0.25, 1, data.visibility.x);
			
			data.visibility.y = cell0.y * v.color.x + cell1.y * v.color.y;	// 探索状态
		}

        void surf (Input IN, inout SurfaceOutputStandardSpecular o) {
			float4 noise = tex2D(_MainTex, IN.worldPos.xz * 0.025);
            fixed4 c = _Color * ((noise.y * 0.75 + 0.25) * IN.visibility.x);
			float blend = IN.uv_MainTex.x;
			blend *= noise.x + 0.5;
			blend = smoothstep(0.4, 0.7, blend); // 曲线 0~0.4透明  0.4~0.7为曲线平滑过渡 0.7~1为完全不透明

			float explored = IN.visibility.y;
            o.Albedo = c.rgb;
            o.Specular = _Specular * explored;
            o.Smoothness = _Glossiness;
			o.Occlusion = explored;
            o.Alpha = blend * explored;
        }
        ENDCG
    }
    FallBack "Diffuse"
}