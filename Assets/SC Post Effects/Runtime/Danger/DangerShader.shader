Shader "Hidden/SC Post Effects/Danger"
{
	HLSLINCLUDE

	#include "../../Shaders/Pipeline/Pipeline.hlsl"

	TEXTURE2D(_Overlay);
	float4 _Color;
	float4 _Params;
	//X: Intensity
	//Y: Size

	float Vignette(float2 uv)
	{
		float vignette = uv.x * uv.y * (1 - uv.x) * (1 - uv.y);
		return clamp(16.0 * vignette, 0, 1);
	}

	float4 Frag(Varyings input): SV_Target
	{
		UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

		float overlay = SAMPLE_TEXTURE2D(_Overlay, sampler_LinearClamp, SCREEN_COORDS).a;

		float vignette = Vignette(SCREEN_COORDS);
		overlay = (overlay * _Params.y) ;
		vignette = (vignette / overlay);
		vignette = 1-saturate(vignette);

		float4 screenColor = ScreenColor(SCREEN_COORDS);

		float alpha = vignette * _Color.a * _Params.x;

		return float4(lerp(screenColor.rgb, _Color.rgb, alpha), screenColor.a);
	}

	ENDHLSL

	SubShader
	{
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			Name "Danger"
			HLSLPROGRAM
			#pragma multi_compile_vertex _ _USE_DRAW_PROCEDURAL
			#pragma exclude_renderers gles

			#pragma vertex Vert
			#pragma fragment Frag

			ENDHLSL
		}
	}
}