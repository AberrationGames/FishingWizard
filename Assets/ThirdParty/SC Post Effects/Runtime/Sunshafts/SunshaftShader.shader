Shader "Hidden/SC Post Effects/Sun Shafts"
{
	HLSLINCLUDE

	#define REQUIRE_DEPTH
	#include "../../Shaders/Pipeline/Pipeline.hlsl"

	TEXTURE2D_X(_SunshaftBuffer);

	half _BlendMode;
	float4 _SunThreshold;
	float4 _SunColor;
	float _BlurRadius;

	float3 _SunPosition; //In world-space
	float4 _Params;

	#define INTENSITY _Params.x
	#define FALLOFF _Params.y

	#define LOOP_RCP 0.09090 // 1/11
	
	//Converts the world-space position to screen space
	float2 GetSunPosition(float2 uv)
	{
		//World to clip space
		float4 positionCS = mul(UNITY_MATRIX_VP, float4(_SunPosition, 1.0));

		//Clip to view space
		float4 positionVS = positionCS * 0.5f;
	    positionVS.xy = float2(positionVS.x, positionVS.y *_ProjectionParams.x) + positionVS.w;
		positionVS.xy /= positionCS.w;

		return positionVS.xy;
	}

	//Without this, the sun shafts will also be visible in the opposite direction
	half GetViewingFactor()
	{
		float3 sunDir = normalize(_WorldSpaceCameraPos - _SunPosition.xyz);

		//Check the degree to which the camera is facing the sun's origin
		float viewFactor = (dot(sunDir, UNITY_MATRIX_V[2].xyz));

		return saturate(viewFactor);
	}

	float4 FragSky(Varyings input) : SV_Target
	{
		UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

		float depth = LINEAR_DEPTH(SAMPLE_DEPTH(SCREEN_COORDS).r);
		float4 skyColor = SCREEN_COLOR(SCREEN_COORDS);

		half2 vec = GetSunPosition(SCREEN_COORDS) - SCREEN_COORDS;

		//Correct for aspect ratio so the gradient remains circular
		float aspect = _ScreenParams.x / _ScreenParams.y;
		vec.x *= aspect;

		half viewingFactor = GetViewingFactor();
		half dist = saturate(FALLOFF - length(vec.xy));

		float4 outColor = 0;
		//reject near depth pixels
		if (depth > 0.99) 
		{
			outColor = dot(max(skyColor.rgb - _SunThreshold.rgb, float3(0, 0, 0)), float3(1, 1, 1)) * dist;
		}

		return outColor * viewingFactor;
	}

	float4 FragRadialBlur(Varyings input) : SV_Target
	{
		UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

		half4 c = half4(0,0,0,0);

		float2 uv = SCREEN_COORDS;
		for (int s = 0; s < 12; s++)
		{
			c += SCREEN_COLOR(uv);

			uv.xy += (GetSunPosition(uv) - SCREEN_COORDS) * _BlurRadius;
		}
		
		return c * LOOP_RCP;
	}

	float4 FragBlend(Varyings input) : SV_Target
	{
		UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

		float4 screenColor = SCREEN_COLOR(SCREEN_COORDS);

		float3 sunshafts = SAMPLE_TEXTURE2D_X(_SunshaftBuffer, sampler_LinearClamp, SCREEN_COORDS).rgb * INTENSITY;
		sunshafts.rgb *= _SunColor.rgb;
		//return float4(sunshafts.rgb, screenColor.a);

		float3 blendedColor = screenColor.rgb;

		if (_BlendMode == 0) blendedColor = BlendAdditive(screenColor.rgb, sunshafts.rgb); //Additive blend
		if (_BlendMode == 1) blendedColor = BlendScreen(sunshafts.rgb, screenColor.rgb); //Screen blend

		return float4(blendedColor.rgb, screenColor.a);
	}

	ENDHLSL

	SubShader
	{
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			Name "Sunshafts sky mask"
			HLSLPROGRAM
			#pragma multi_compile_vertex _ _USE_DRAW_PROCEDURAL
			#pragma exclude_renderers gles

			#pragma vertex Vert
			#pragma fragment FragSky

			ENDHLSL
		}
		Pass
		{
			Name "Sunshafts blur"
			HLSLPROGRAM
			#pragma multi_compile_vertex _ _USE_DRAW_PROCEDURAL
			#pragma exclude_renderers gles

			#pragma vertex Vert
			#pragma fragment FragRadialBlur

			ENDHLSL
		}
		Pass
		{
			Name "Sunshafts composite"
			HLSLPROGRAM
			#pragma multi_compile_vertex _ _USE_DRAW_PROCEDURAL
			#pragma exclude_renderers gles

			#pragma vertex Vert
			#pragma fragment FragBlend

			ENDHLSL
		}
	}
}