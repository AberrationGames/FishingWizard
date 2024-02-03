Shader "Hidden/SC Post Effects/Color Split"
{
	HLSLINCLUDE

	#include "../../Shaders/Pipeline/Pipeline.hlsl"

	float4 _Params;

	#define OFFSET _Params.x
	#define EDGEFADE _Params.y
	#define LUM_THRESHOLD _Params.z

	#define EDGE_SIZE 2.5
	#define EDGE_FALLOFF 2.0

	float Mask(float2 uv, float3 sourceColor, float minLuminance)
	{
		float mask = lerp(1.0, EdgeMask(uv, EDGE_SIZE, EDGE_FALLOFF), EDGEFADE);

		float luminance = smoothstep(0, minLuminance, Luminance(sourceColor.rgb)) * (minLuminance + 1);
		mask = (mask * luminance);

		/* Scale down by depth?
		float depth = LINEAR_EYE_DEPTH(SAMPLE_DEPTH(uv));
		mask /= (depth * 0.01);
		*/

		return saturate(mask);
	}

	float4 FragSingle(Varyings input) : SV_Target
	{
		UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

		float4 original = SCREEN_COLOR(SCREEN_COORDS);
		
		float mask = Mask(SCREEN_COORDS, original.rgb, LUM_THRESHOLD);

		float offset = OFFSET * mask;
		float red = ScreenColor(SCREEN_COORDS - float2(offset, 0)).r;
		float blue = ScreenColor(SCREEN_COORDS + float2(offset, 0)).b;

		float4 splitColors = float4(red, original.g, blue, original.a);
		
		//return mask;

		return float4(lerp(original.rgb, splitColors.rgb, mask), original.a);
	}

	float4 FragDouble(Varyings input) : SV_Target
	{
		UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

		float4 original = SCREEN_COLOR(SCREEN_COORDS);
		
		float mask = Mask(SCREEN_COORDS, original.rgb, LUM_THRESHOLD);

		float offset = OFFSET * mask;
		float redX = ScreenColor(SCREEN_COORDS - float2(offset, 0)).r;
		float redY = ScreenColor(SCREEN_COORDS - float2(0, offset)).r;
		
		float blueX = ScreenColor(SCREEN_COORDS + float2(offset, 0)).b;
		float blueY = ScreenColor(SCREEN_COORDS + float2(0, offset)).b;

		float4 splitColorsX = float4(redX, original.g, blueX, original.a);
		float4 splitColorsY = float4(redY, original.g, blueY, original.a);

		float4 blendedColors = (splitColorsX + splitColorsY) * 0.5;
		
		//return mask;

		return float4(lerp(original.rgb, blendedColors.rgb, mask), original.a);
	}

	ENDHLSL

	SubShader
	{
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			Name "Color Split: Horizontal"
			HLSLPROGRAM
			#pragma multi_compile_vertex _ _USE_DRAW_PROCEDURAL
			#pragma exclude_renderers gles

			#pragma vertex Vert
			#pragma fragment FragSingle

			ENDHLSL
		}
		Pass
		{
			Name "Color Split: Horizontal + Vertical"
			HLSLPROGRAM
			#pragma multi_compile_vertex _ _USE_DRAW_PROCEDURAL
			#pragma exclude_renderers gles

			#pragma vertex Vert
			#pragma fragment FragDouble

			ENDHLSL
		}
	}
}