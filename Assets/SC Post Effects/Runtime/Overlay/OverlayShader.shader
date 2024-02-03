Shader "Hidden/SC Post Effects/Overlay"
{
	HLSLINCLUDE

	#include "../../Shaders/Pipeline/Pipeline.hlsl"

	TEXTURE2D(_OverlayTex);
	float4 _Params;
	//X: Intensity
	//Y: Tiling
	//Z: Auto aspect (bool)
	//W: Blend mode
	float _LuminanceThreshold;

	float4 Frag(Varyings input) : SV_Target
	{
		UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

		float4 screenColor = ScreenColor(SCREEN_COORDS);

		if(_Params.z == 1) SCREEN_COORDS.x *= _ScreenParams.x / _ScreenParams.y;

		float4 overlay = SAMPLE_TEXTURE2D(_OverlayTex, sampler_LinearRepeat, SCREEN_COORDS * _Params.y);

		float luminance = smoothstep(-0.01,  _LuminanceThreshold, Luminance(screenColor));
		//return float4(luminance.xxx, 1);
		overlay.a *= luminance;
		
		float3 color = 0;

		if (_Params.w == 0) color = lerp(screenColor.rgb, overlay.rgb, overlay.a * _Params.x);
		if (_Params.w == 1) color = lerp(screenColor.rgb, BlendAdditive(overlay.rgb, screenColor.rgb), overlay.a * _Params.x);
		if (_Params.w == 2) color = lerp(screenColor.rgb, overlay.rgb * screenColor.rgb, overlay.a * _Params.x);
		if (_Params.w == 3) color = lerp(screenColor.rgb, BlendScreen(overlay.rgb, screenColor.rgb), overlay.a * _Params.x);

		return float4(color.rgb, screenColor.a);
	}


	ENDHLSL

	SubShader
	{
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			Name "Texture overlay"
			HLSLPROGRAM
			#pragma multi_compile_vertex _ _USE_DRAW_PROCEDURAL
			#pragma exclude_renderers gles

			#pragma vertex Vert
			#pragma fragment Frag

			ENDHLSL
		}
	}
}