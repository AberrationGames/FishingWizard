Shader "Hidden/SC Post Effects/Pixelize"
{
	HLSLINCLUDE

	#include "../../Shaders/Pipeline/Pipeline.hlsl"

	float4 _PixelizeParams;
	//XY: Pixel scale XY
	//Z: Scale
	//W: Center pixel (bool)

	#define OFFSET _PixelizeParams.w > 0 ? 0 : 0.5
	#define PIXEL_SCALE _PixelizeParams.xy
	#define SCALE _PixelizeParams.z
	
	float4 Frag(Varyings input) : SV_Target
	{
		UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

		const float offset = OFFSET;

		const float2 scale = (PIXEL_SCALE / _ScreenParams.xy) * SCALE;
		float x = round((SCREEN_COORDS.x / scale.x) + offset) * scale.x;
		float y = round((SCREEN_COORDS.y / scale.y) + offset) * scale.y;

		//Simply sample the input texture using the rounded SCREEN_COORDS coordinates
		return ScreenColor(float2(x,y));
	}

	ENDHLSL

	SubShader
	{
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			Name "Pixelize"
			HLSLPROGRAM
			#pragma multi_compile_vertex _ _USE_DRAW_PROCEDURAL
			#pragma exclude_renderers gles

			#pragma vertex Vert
			#pragma fragment Frag

			ENDHLSL
		}
	}
}