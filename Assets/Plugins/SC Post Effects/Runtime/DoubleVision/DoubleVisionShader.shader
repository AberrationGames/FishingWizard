Shader "Hidden/SC Post Effects/Double Vision"
{
	HLSLINCLUDE

	#include "../../Shaders/Pipeline/Pipeline.hlsl"

	float _Amount;

	float4 Frag(Varyings input) : SV_Target
	{
		float4 screenColor = ScreenColor(SCREEN_COORDS);

		screenColor += SCREEN_COLOR(SCREEN_COORDS - float2(_Amount, 0));
		screenColor += SCREEN_COLOR(SCREEN_COORDS + float2(_Amount, 0));

		return screenColor / 3.0;
	}

	float4 FragEdges(Varyings input) : SV_Target
	{
		float2 coords = 2.0 * SCREEN_COORDS - 1.0;
		float2 end = SCREEN_COORDS - coords * dot(coords, coords) * _Amount;
		float2 delta = (end - SCREEN_COORDS) / 3;

		half4 texelA = SCREEN_COLOR(SCREEN_COORDS);
		half4 texelB = SCREEN_COLOR(delta + SCREEN_COORDS);
		half4 texelC = SCREEN_COLOR(delta * 2.0 + SCREEN_COORDS);

		return (texelA + texelB + texelC) / 3.0;
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
			#pragma fragment Frag

			ENDHLSL
		}

		Pass
		{
			HLSLPROGRAM
			#pragma multi_compile_vertex _ _USE_DRAW_PROCEDURAL
			#pragma exclude_renderers gles

			#pragma vertex Vert
			#pragma fragment FragEdges

			ENDHLSL
		}
	}
}