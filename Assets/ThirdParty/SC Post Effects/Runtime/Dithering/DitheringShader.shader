Shader "Hidden/SC Post Effects/Dithering"
{
	HLSLINCLUDE

	#include "../../Shaders/Pipeline/Pipeline.hlsl"

	TEXTURE2D(_LUT);
	SAMPLER(sampler_LUT);

	float4 _Dithering_Coords;
	//X: Size
	//Y: Tiling
	//Z: Luminance influence
	//W: Intensity

	float3 ApplyDithering(float3 color, float2 uv)
	{
		float luminance = Luminance(LinearToSRGB(color.rgb));

		float lut = SAMPLE_TEXTURE2D(_LUT, sampler_LinearRepeat, uv).r;

		float dither = step(lut, luminance / _Dithering_Coords.z);

		return lerp(color, color * saturate(dither), _Dithering_Coords.w);
	}

	float4 Frag(Varyings input) : SV_Target
	{
		float4 screenColor = SCREEN_COLOR(SCREEN_COORDS);

		float2 lutUV = float2(SCREEN_COORDS.x *= _ScreenParams.x / _ScreenParams.y, SCREEN_COORDS.y * _ScreenParams.w) *  _Dithering_Coords.y * 32;
		float3 ditheredColor = ApplyDithering(screenColor.rgb, lutUV);
		
		return float4(lerp(screenColor.rgb, ditheredColor.rgb, _Dithering_Coords.w), screenColor.a);
	}
	
	ENDHLSL

	SubShader
	{
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			Name "Dithering"
			HLSLPROGRAM
			#pragma multi_compile_vertex _ _USE_DRAW_PROCEDURAL
			#pragma exclude_renderers gles

			#pragma vertex Vert
			#pragma fragment Frag

			ENDHLSL
		}
	}
}