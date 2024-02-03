Shader "Hidden/SC Post Effects/Posterize"
{
	HLSLINCLUDE

	#include "../../Shaders/Pipeline/Pipeline.hlsl"

	float _Depth;
	float4 _Params;

	float PosterizeChannel(float val, float amount)
	{
		return floor(val * amount) / (amount -1.0);
	}

	float3 PosterizeRGB(float3 val, float amount)
	{
		return floor(val.rgb * amount) / (amount -1.0);
	}

	float4 FragRGB(Varyings input): SV_Target
	{
		UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
		float4 screenColor = SCREEN_COLOR(SCREEN_COORDS);

		#ifndef UNITY_COLORSPACE_GAMMA
		screenColor.rgb = LinearToSRGB(screenColor.rgb);
		#endif

		float3 rgb = PosterizeRGB(screenColor.rgb, _Params.w);

		#ifndef UNITY_COLORSPACE_GAMMA
		rgb = SRGBToLinear(rgb);
		#endif
		
		return float4(rgb, screenColor.a);
	}

	float4 FragHSV(Varyings input) : SV_Target
	{
		UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
		float4 screenColor = SCREEN_COLOR(SCREEN_COORDS);

		//Required
		#ifdef URP
		screenColor.rgb = saturate(screenColor.rgb);
		#endif
		
		#ifndef UNITY_COLORSPACE_GAMMA
		screenColor.rgb = LinearToSRGB(screenColor.rgb);
		#endif
		
		float3 hsv = RgbToHsv(screenColor.rgb);
		hsv.x = PosterizeChannel(hsv.x, _Params.x);
		hsv.y = PosterizeChannel(hsv.y, _Params.y);
		hsv.z = PosterizeChannel(hsv.z, _Params.z);

		float3 rgb = HsvToRgb(hsv);

		#ifndef UNITY_COLORSPACE_GAMMA
		rgb = SRGBToLinear(rgb);
		#endif
		
		return float4(rgb, screenColor.a);
	}

	ENDHLSL

	SubShader
	{
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			Name "Posterize RGB"
			HLSLPROGRAM
			#pragma multi_compile_vertex _ _USE_DRAW_PROCEDURAL
			#pragma exclude_renderers gles

			#pragma vertex Vert
			#pragma fragment FragRGB

			ENDHLSL
		}
		Pass
		{
			Name "Posterize HSV"
			HLSLPROGRAM
			#pragma multi_compile_vertex _ _USE_DRAW_PROCEDURAL
			#pragma exclude_renderers gles

			#pragma vertex Vert
			#pragma fragment FragHSV

			ENDHLSL
		}
	}
}