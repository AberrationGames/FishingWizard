Shader "Hidden/SC Post Effects/Light Streaks"
{
	HLSLINCLUDE

	#include "../../Shaders/Pipeline/Pipeline.hlsl"
	#include "../../Shaders/Blurring.hlsl"

	TEXTURE2D(_BloomTex);

	float4 _Params;
	//X: Luminance threshold
	//Y: Intensity
	//Z: ...
	//W: ...

	float4 FragLuminanceDiff(Varyings input) : SV_Target
	{
		UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

		float4 screenColor = ScreenColor(SCREEN_COORDS);
		
		float3 luminance = LuminanceThreshold(screenColor.rgb, _Params.x);
		luminance *= _Params.y;

		return float4(luminance.rgb, screenColor.a);
	}

	float4 FragBlend(Varyings input) : SV_Target
	{
		UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

		float4 original = ScreenColor(SCREEN_COORDS);
		float3 bloom = SAMPLE_TEXTURE2D_X(_BloomTex, sampler_LinearClamp, SCREEN_COORDS).rgb;

		return float4(original.rgb + bloom, original.a);
	}

	float4 FragDebug(Varyings input) : SV_Target
	{
		UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

		return SAMPLE_TEXTURE2D_X(_BloomTex, sampler_LinearClamp, SCREEN_COORDS);
	}

	ENDHLSL

	SubShader
	{
		Cull Off ZWrite Off ZTest Always

		Pass //0
		{
			Name "Light Streaks: Luminance filter"
			HLSLPROGRAM
			#pragma multi_compile_vertex _ _USE_DRAW_PROCEDURAL
			#pragma exclude_renderers gles

			#pragma vertex Vert
			#pragma fragment FragLuminanceDiff

			ENDHLSL
		}
		Pass //1
		{
			Name "Light Streaks: Streak (Performance mode)"
			HLSLPROGRAM
			#pragma multi_compile_vertex _ _USE_DRAW_PROCEDURAL
			#pragma exclude_renderers gles

			#pragma vertex Vert
			#pragma fragment FragBlurBox

			ENDHLSL
		}
		Pass //2
		{
			Name "Light Streaks: Streak (Appearance mode)"
			HLSLPROGRAM
			#pragma multi_compile_vertex _ _USE_DRAW_PROCEDURAL
			#pragma exclude_renderers gles

			#pragma vertex Vert
			#pragma fragment FragBlurGaussian

			ENDHLSL
		}
		Pass //3
		{
			Name "Light Streaks: Composite"
			HLSLPROGRAM
			#pragma multi_compile_vertex _ _USE_DRAW_PROCEDURAL
			#pragma exclude_renderers gles

			#pragma vertex Vert
			#pragma fragment FragBlend

			ENDHLSL
		}
		Pass //4
		{
			Name "Light Streaks: Debug"
			HLSLPROGRAM
			#pragma multi_compile_vertex _ _USE_DRAW_PROCEDURAL
			#pragma exclude_renderers gles

			#pragma vertex Vert
			#pragma fragment FragDebug

			ENDHLSL
		}
	}
}