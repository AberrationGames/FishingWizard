Shader "Hidden/SC Post Effects/Blur"
{
	HLSLINCLUDE
	#define MULTI_PASS
	#define REQUIRE_DEPTH

	#include "../../Shaders/Pipeline/Pipeline.hlsl"
	#include "../../Shaders/Blurring.hlsl"

	TEXTURE2D_X(_BlurredTex);
	SAMPLER(sampler_BlurredTex);
	float4 _FadeParams;
	
	//Separate pass, because this shouldn't be looped
	float4 FragBlend(Varyings input) : SV_Target
	{
		UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

		return SAMPLE_TEXTURE2D_X(_BlurredTex, sampler_BlurredTex, SCREEN_COORDS);
	}

	float4 FragDepthFade(Varyings input) : SV_Target
	{
		UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

		float4 screenColor = ScreenColor(SCREEN_COORDS);
		float3 blurredColor = SAMPLE_TEXTURE2D_X(_BlurredTex, sampler_BlurredTex, SCREEN_COORDS).rgb;

		float depth = SAMPLE_DEPTH(SCREEN_COORDS);

		float fadeDist = LinearDepthFade(LINEAR_DEPTH(depth), _FadeParams.x, _FadeParams.y, 0.0, 1.0);

		return float4(lerp(blurredColor.rgb, screenColor.rgb, fadeDist), screenColor.a);
	}

	ENDHLSL

	SubShader
	{
		Cull Off ZWrite Off ZTest Always

		Pass //0
		{
			Name "Blur Blend"
			HLSLPROGRAM
			#pragma multi_compile_vertex _ _USE_DRAW_PROCEDURAL
			#pragma exclude_renderers gles
			
			#pragma vertex Vert
			#pragma fragment FragBlend

			ENDHLSL
		}
		Pass //1
		{
			Name "Blur Depth Fade"
			HLSLPROGRAM
			#pragma multi_compile_vertex _ _USE_DRAW_PROCEDURAL
			#pragma exclude_renderers gles

			#pragma vertex Vert
			#pragma fragment FragDepthFade

			ENDHLSL
		}
		Pass //2
		{
			Name "Gaussian Blur"
			HLSLPROGRAM
			#pragma multi_compile_vertex _ _USE_DRAW_PROCEDURAL
			#pragma exclude_renderers gles

			#pragma vertex Vert
			#pragma fragment FragBlurGaussian

			ENDHLSL
		}
		Pass //3
		{
			Name "Box Blur"
			HLSLPROGRAM
			#pragma multi_compile_vertex _ _USE_DRAW_PROCEDURAL
			#pragma exclude_renderers gles

			#pragma vertex Vert
			#pragma fragment FragBlurBox

			ENDHLSL
		}

	}
}