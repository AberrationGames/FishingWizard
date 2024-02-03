Shader "Hidden/SC Post Effects/Transition"
{
	HLSLINCLUDE

	#include "../../Shaders/Pipeline/Pipeline.hlsl"

	TEXTURE2D(_Gradient);
	SamplerState sampler_Gradient;

	float4 _Params;
	float4 _TransitionColor;

	#define PROGRESS _Params.x
	#define INVERT _Params.y

	float4 Frag(Varyings input) : SV_Target
	{
		UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

		float4 screenColor = SCREEN_COLOR(SCREEN_COORDS);

		float gradientTex = SAMPLE_TEXTURE2D(_Gradient, sampler_Gradient, SCREEN_COORDS).r;
		if(INVERT > 0) gradientTex = 1-gradientTex;
		
		float alpha = smoothstep(gradientTex, PROGRESS, 1.01) * _TransitionColor.a;

		return float4(lerp(screenColor.rgb, _TransitionColor.rgb, alpha), screenColor.a);
	}

	ENDHLSL

	SubShader
	{
	Cull Off ZWrite Off ZTest Always

		Pass
		{
			Name "Transition"
			HLSLPROGRAM
			#pragma multi_compile_vertex _ _USE_DRAW_PROCEDURAL
			#pragma exclude_renderers gles

			#pragma vertex Vert
			#pragma fragment Frag

			ENDHLSL
		}
	}
}