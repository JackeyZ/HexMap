#ifndef WATER_CG_INCLUDE
#define WATER_CG_INCLUDE

	// 泡沫（靠岸波、离岸波）
	float Foam (float shore, float2 worldXZ, sampler2D noiseTex) {
	//  float shore = IN.uv_MainTex.y;
		shore = sqrt(shore) * 0.9;												// 开根号，sqrt(shore)函数shore越接近1，曲线越平滑 。* 0.9用于减弱颜色
 
		float2 noiseUV = worldXZ + _Time.y * 0.25;
		float4 noise = tex2D(noiseTex, noiseUV * 0.015);
 
		// 靠岸波
		float distortion1 = noise.x * (1 - shore);							// 越靠近岸边，变形越弱
		float foam1 = sin((shore + distortion1) * 10 - _Time.y);
		foam1 *= foam1;
 
		// 离岸波
		float distortion2 = noise.y * (1 - shore);							// 越靠近岸边，变形越弱
		float foam2 = sin((shore + distortion2) * 10 + _Time.y + 2);		// +2来偏移相位, 让靠岸波和离岸波错开
		foam2 *= foam2 * 0.7;												// *0.7降低振幅
 
		return max(foam1, foam2) * shore;									// *shore是为了越接近1，sin(x) * sin(x)曲线振幅越大，导致颜色越深
	}
	
	// 海波纹
	float Waves (float2 worldXZ, sampler2D noiseTex) {
		float2 uv1 = worldXZ;
		uv1.y += _Time.y;
		float4 noise1 = tex2D(noiseTex, uv1 * 0.025);
 
		float2 uv2 = worldXZ;
		uv2.x += _Time.y;
		float4 noise2 = tex2D(noiseTex, uv2 * 0.025);
 
		float blendWave = sin((worldXZ.x + worldXZ.y) * 0.1 + (noise1.y + noise2.z) + _Time.y);
		blendWave *= blendWave;
 
		float waves = lerp(noise1.z, noise1.w, blendWave) + lerp(noise2.x, noise2.y, blendWave);
		noise1.z + noise2.x;
		return smoothstep(0.75, 2, waves);
	}

	// 河流波纹
	float River(float2 riverUV, sampler2D noiseText){
		float2 uv = riverUV;
		uv.x = uv.x * 0.0625 + _Time.y * 0.005;
		uv.y -= _Time.y * 0.25;
		float4 noise = tex2D(noiseText, uv);
 
		float2 uv2 = riverUV;
		uv2.x = uv2.x * 0.0625 - _Time.y * 0.0052;
		uv2.y -= _Time.y * 0.23;
		float4 noise2 = tex2D(noiseText, uv2);

		return noise.r * noise2.a;
	}
#endif