Shader "Custom/Road" {
    Properties {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }
    SubShader {
        Tags { "RenderType"="Opaque"  "Queue" = "Geometry+1"}  // "Queue" = "Geometry+1"把渲染顺序放在所有普通几何体之后
        LOD 200
		Offset -1, -1  // 深度测试偏移
         
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows decal:blend vertex:vert
        #pragma target 3.0
		// 导入获得格子数据的文件
		#include "CgIncludes/HexCellData.cginc" 
 
        sampler2D _MainTex;
 
        struct Input {
            float2 uv_MainTex;
			float3 worldPos;
			float visibility;
        };
 
        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
 
		void vert(inout appdata_full v, out Input data){
			UNITY_INITIALIZE_OUTPUT(Input, data);
 
			float4 cell0 = GetCellData(v, 0);
			float4 cell1 = GetCellData(v, 1);
 
			data.visibility = cell0.x * v.color.x + cell1.x * v.color.y;	// 每个格子用可见性*格子占比，再相加获得当前顶点的可见性
			data.visibility = lerp(0.25, 1, data.visibility);
		}

        void surf (Input IN, inout SurfaceOutputStandard o) {
			float4 noise = tex2D(_MainTex, IN.worldPos.xz * 0.025);
            fixed4 c = _Color * ((noise.y * 0.75 + 0.25) * IN.visibility);
			float blend = IN.uv_MainTex.x;
			blend *= noise.x + 0.5;
			blend = smoothstep(0.4, 0.7, blend); // 曲线 0~0.4透明  0.4~0.7为曲线平滑过渡 0.7~1为完全不透明

            o.Albedo = c.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = blend;//c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}