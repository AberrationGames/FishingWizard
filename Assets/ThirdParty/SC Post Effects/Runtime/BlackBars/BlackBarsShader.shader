Shader "Hidden/SC Post Effects/Black Bars"
{
	HLSLINCLUDE

	#include "../../Shaders/Pipeline/Pipeline.hlsl"

	float2 _Size;

	float4 FragHorizontal(Varyings input): SV_Target
	{
		UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

		float2 uv = SCREEN_COORDS;
		float4 screenColor = ScreenColor(SCREEN_COORDS);

		half bars = min(uv.y, (1-uv.y));
		bars = step(_Size.x * _Size.y, bars);

		return float4(screenColor.rgb * bars, screenColor.a);
	}

	float4 FragVertical(Varyings input) : SV_Target
	{
		UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

		float2 uv = SCREEN_COORDS;
		float4 screenColor = ScreenColor(SCREEN_COORDS);

		half bars = (uv.x * (1-uv.x));
		bars = step(_Size.x * (_Size.y /2), bars);

		return float4(screenColor.rgb * bars, screenColor.a);
	}

	ENDHLSL

	SubShader
	{
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			HLSLPROGRAM
			#pragma multi_compile_vertex _ _USE_DRAW_PROCEDURAL
			#pragma exclude_renderers gles

			#pragma vertex Vert
			#pragma fragment FragHorizontal

			ENDHLSL
		}
		Pass
		{
			HLSLPROGRAM
			#pragma multi_compile_vertex _ _USE_DRAW_PROCEDURAL
			#pragma exclude_renderers gles

			#pragma vertex Vert
			#pragma fragment FragVertical

			ENDHLSL
		}
	}
}