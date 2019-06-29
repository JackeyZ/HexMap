Shader "Custom/VertexColors" {
	Properties{
		_Color("Color", Color) = (1,1,1,1)
		_MainTex("Albedo (RGB)", 2D) = "white" {}
		_Glossiness("Smoothness", Range(0,1)) = 0.5
		_Metallic("Metallic", Range(0,1)) = 0.0
	}
	SubShader
	{
		Tags{ "RenderType" = "Opaque" }
		LOD 200
		
		CGPROGRAM
			#pragma surface surf Standard fullforwardshadows
			#pragma target 3.0

			sampler2D _MainTex;

			// 表面着色器输入
			struct Input {
				float2 uv_MainTex;		// 纹理UV坐标，命名规则一定是uv + _ + 纹理（texture）名称, 或者uv2 + _ + 纹理2名称
				float4 color : COLOR;	// 顶点颜色
			};

			half _Glossiness;
			half _Metallic;
			fixed4 _Color;

			// 表面着色器，规定参数1是Input结构，参数2是inout的SurfaceOutput结构。
			void surf(Input IN, inout SurfaceOutputStandard o) {
				fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;			// 根据传入UV坐标提取纹理，再与材质_Color变量融合
				o.Albedo = c.rgb * IN.color;								// 纹理颜色与传入顶点颜色融合
				o.Metallic = _Metallic;										// 金属感
				o.Smoothness = _Glossiness;									// 平滑
				o.Alpha = c.a;
			}
		ENDCG
	}
	FallBack "Diffuse"
}