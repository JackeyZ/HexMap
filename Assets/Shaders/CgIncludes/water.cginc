#ifndef WATER_CG_INCLUDE
#define WATER_CG_INCLUDE

	// ��ĭ�����������밶����
	float Foam (float shore, float2 worldXZ, sampler2D noiseTex) {
	//  float shore = IN.uv_MainTex.y;
		shore = sqrt(shore) * 0.9;												// �����ţ�sqrt(shore)����shoreԽ�ӽ�1������Խƽ�� ��* 0.9���ڼ�����ɫ
 
		float2 noiseUV = worldXZ + _Time.y * 0.25;
		float4 noise = tex2D(noiseTex, noiseUV * 0.015);
 
		// ������
		float distortion1 = noise.x * (1 - shore);							// Խ�������ߣ�����Խ��
		float foam1 = sin((shore + distortion1) * 10 - _Time.y);
		foam1 *= foam1;
 
		// �밶��
		float distortion2 = noise.y * (1 - shore);							// Խ�������ߣ�����Խ��
		float foam2 = sin((shore + distortion2) * 10 + _Time.y + 2);		// +2��ƫ����λ, �ÿ��������밶����
		foam2 *= foam2 * 0.7;												// *0.7�������
 
		return max(foam1, foam2) * shore;									// *shore��Ϊ��Խ�ӽ�1��sin(x) * sin(x)�������Խ�󣬵�����ɫԽ��
	}
	
	// ������
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

	// ��������
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