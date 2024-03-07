Shader "Hidden/SC Post Effects/Ambient Occlusion 2D"
{
	HLSLINCLUDE

	#include "../../Shaders/Pipeline/Pipeline.hlsl"
	#include "../../Shaders/Blurring.hlsl"

	TEXTURE2D_X(_AO);
	float _SampleDistance;
	float _Threshold;
	float _Blur;
	float _Intensity;

	float4 FragLuminanceDiff(Varyings input) : SV_Target
	{
		float4 original = SCREEN_COLOR(SCREEN_COORDS);

		half3 p1 = original.rgb;
		half3 p2 = ScreenColor(SCREEN_COORDS + float2(-_MainTex_TexelSize.x, -_MainTex_TexelSize.y) * _SampleDistance).rgb;
		half3 p3 = ScreenColor(SCREEN_COORDS + float2(+_MainTex_TexelSize.x, -_MainTex_TexelSize.y) * _SampleDistance).rgb;

		half3 diff = p1 * 2 - p2 - p3;
		half edge = dot(diff, diff);
		edge = step(edge, _Threshold);

		//Edges only
		original.rgb = lerp(1, edge, _Intensity);

		return original;
	}

	float4 FragBlend(Varyings input) : SV_Target
	{
		float4 screenColor = SCREEN_COLOR(SCREEN_COORDS);
		float ao = SAMPLE_TEXTURE2D_X(_AO, sampler_LinearClamp, SCREEN_COORDS).r;

		return float4(screenColor.rgb * ao, screenColor.a);
	}

	float4 FragDebug(Varyings input) : SV_Target
	{
		 return SAMPLE_TEXTURE2D_X(_AO, sampler_LinearClamp, SCREEN_COORDS);
	}

	ENDHLSL

SubShader
	{
		Cull Off ZWrite Off ZTest Always

		Pass //0
		{
			Name "Luminance filtering"
			HLSLPROGRAM
			#pragma multi_compile_vertex _ _USE_DRAW_PROCEDURAL
			#pragma exclude_renderers gles

			#pragma vertex Vert
			#pragma fragment FragLuminanceDiff
			ENDHLSL
		}
		Pass //1
		{	
			Name "Blurring"
			HLSLPROGRAM
			#pragma multi_compile_vertex _ _USE_DRAW_PROCEDURAL
			#pragma exclude_renderers gles

			#pragma vertex Vert
			#pragma fragment FragBlurGaussian
			ENDHLSL
		}
		Pass //2
		{
			Name "Composite"
			HLSLPROGRAM
			#pragma multi_compile_vertex _ _USE_DRAW_PROCEDURAL
			#pragma exclude_renderers gles

			#pragma vertex Vert
			#pragma fragment FragBlend
			ENDHLSL
		}
		Pass //3
		{
			Name "Debug"
			HLSLPROGRAM
			#pragma multi_compile_vertex _ _USE_DRAW_PROCEDURAL
			#pragma exclude_renderers gles

			#pragma vertex Vert
			#pragma fragment FragDebug
			ENDHLSL
		}
	}
}