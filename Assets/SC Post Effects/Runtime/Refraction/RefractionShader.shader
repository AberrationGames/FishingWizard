Shader "Hidden/SC Post Effects/Refraction"
{
	HLSLINCLUDE

	#include "../../Shaders/Pipeline/Pipeline.hlsl"

	TEXTURE2D(_RefractionNormal);
	float4 _Params;
	float4 _Tint;

	#define AMOUNT _Params.x
	
	float4 FragNormalMap(Varyings input) : SV_Target
	{
		UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

		float4 normal = SAMPLE_TEXTURE2D(_RefractionNormal, sampler_LinearClamp, SCREEN_COORDS).rgba;

		//Assuming DXT5nm compression!
		normal.x *= normal.w;

		//Remap to -1,-1
		normal.xy = normal.xy * 2.0 - 1.0;

		half diffuse = saturate(dot(normal, float3(0, 0, 1)) * 2.0 - 1.0);
		//return diffuse;
		float scale = length(normal.xy);
		//return diffuse;

		normal.xy += _ScreenParams.zw-1;
		float2 refraction = lerp(SCREEN_COORDS, (SCREEN_COORDS) - normal.rg, AMOUNT * scale);

		float4 original = ScreenColor(refraction);
		float3 tinted = lerp(original.rgb, _Tint.rgb, diffuse * AMOUNT * _Tint.a);
		
		return float4(tinted.rgb, original.a);
	}

	ENDHLSL

	SubShader
	{
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			Name "Refraction by normal map"
			HLSLPROGRAM
			#pragma multi_compile_vertex _ _USE_DRAW_PROCEDURAL
			#pragma exclude_renderers gles

			#pragma vertex Vert
			#pragma fragment FragNormalMap

			ENDHLSL
		}
	}
}