Shader "Hidden/SC Post Effects/Scanlines"
{
	HLSLINCLUDE

	#include "../../Shaders/Pipeline/Pipeline.hlsl"

	float4 _Params;
	//X: Amount
	//Y: Intensity
	//Z: Speed

	float4 Frag(Varyings input): SV_Target
	{
		UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

		float4 screenColor = SCREEN_COLOR(SCREEN_COORDS);

		half linesY = SCREEN_COORDS.y - sin(SCREEN_COORDS.y * _Params.x + (_Time.w * _Params.z)) * _Params.x;

		float3 color = lerp(screenColor, screenColor * linesY, _Params.y).rgb;

		return float4(color.rgb, screenColor.a);
	}

	ENDHLSL

	SubShader
	{
		Cull Off ZWrite Off ZTest Always Blend Off

		Pass
		{
			Name "Scanlines"
			
			HLSLPROGRAM
			#pragma multi_compile_vertex _ _USE_DRAW_PROCEDURAL
			#pragma exclude_renderers gles

			#pragma vertex Vert
			#pragma fragment Frag
			ENDHLSL
		}
	}

	Fallback Off
}
