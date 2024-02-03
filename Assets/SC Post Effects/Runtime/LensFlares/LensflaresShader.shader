Shader "Hidden/SC Post Effects/Lensflares"
{
	HLSLINCLUDE

	#include "../../Shaders/Pipeline/Pipeline.hlsl"
	#include "../../Shaders/Blurring.hlsl"

	TEXTURE2D(_BloomTex);
	SAMPLER(sampler_BloomTex);
	TEXTURE2D(_FlaresTex);
	SAMPLER(sampler_FlaresTex);
	TEXTURE2D(_ColorTex);
	TEXTURE2D(_MaskTex);

	float4 _FlaresTex_TexelSize;

	float _SampleDistance;
	float _Threshold;
	float _Distance;
	float _Falloff;
	float _Intensity;
	float _Ghosts;
	float _HaloSize;
	float _HaloWidth;
	float _ChromaticAbberation;

	float4 _GhostParams;
	//X = NumGhost
	//Y = GhostDinstance
	//Z = GhostFalloff
	float4 _HaloParams;

	float4 FragLuminanceDiff(Varyings input) : SV_Target
	{
		UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

		float4 screenColor = ScreenColor(SCREEN_COORDS);
		
		float3 luminance = LuminanceThreshold(screenColor.rgb, _Threshold);
		luminance *= _Intensity;

		return float4(luminance.rgb, screenColor.a);
	}

	float4 FragGhosting(Varyings input) : SV_Target
	{
		UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

		//Flip bloom buffer
		float2 texcoord = -SCREEN_COORDS + 1.0;
		float2 centerVec = float2(0.5, 0.5);

		//Radial mask
		float2 center = texcoord * 2 - 1;
		float falloff = 1-dot(center, center) * _Falloff;
		falloff = saturate(falloff);

		//return float4(falloff, falloff, falloff, 1);

		//Ghosting
		float2 ghostVec = (centerVec - texcoord) * _Distance;

		float3 result = float3(0,0,0);
		for (int i = 0; i < _Ghosts; ++i)
		{
			float2 offset = frac(texcoord + ghostVec * float(i));

			result += SAMPLE_TEXTURE2D(_BloomTex, sampler_BloomTex, offset).rgb * falloff;
		}

		//Add halo
		float2 haloVec = normalize(centerVec - texcoord) * _HaloSize;
		float haloFalloff = length(float2(centerVec - frac(texcoord + haloVec))) * _HaloWidth;
		haloFalloff = saturate(pow(1-haloFalloff, 5.0));

		float halo = SAMPLE_TEXTURE2D(_BloomTex, sampler_BloomTex, texcoord + haloVec).r * (haloFalloff);
		result.rgb += halo * 1;

		//Add color ramp
		float4 colorRamp = SAMPLE_TEXTURE2D(_ColorTex, sampler_LinearClamp, length(centerVec - texcoord) / 0.70).rgba;

		//Use color ramp alpha channel to blend
		result = lerp(result, result*colorRamp.rgb, colorRamp.a);

		//result *= (_Intensity * 2);

		return float4(result, 1);
	}

	float4 FragBlend(Varyings input) : SV_Target
	{
		UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
		
		float4 original = ScreenColor(SCREEN_COORDS);
		float3 flares = SAMPLE_TEXTURE2D(_FlaresTex, sampler_FlaresTex, SCREEN_COORDS).rgb;
		//return float4(flares.rgb, 1);
		float mask = SAMPLE_TEXTURE2D(_MaskTex, sampler_LinearClamp, SCREEN_COORDS).r;

		//CA
		if(_ChromaticAbberation > 0)
		{
			float2 direction = normalize((float2(0.5, 0.5) - SCREEN_COORDS));
			float3 distortion = float3(-_FlaresTex_TexelSize.x * _ChromaticAbberation, 0, _FlaresTex_TexelSize.x * _ChromaticAbberation);

			float red = SAMPLE_TEXTURE2D(_FlaresTex, sampler_LinearClamp, SCREEN_COORDS + direction * distortion.r).r;
			float green = SAMPLE_TEXTURE2D(_FlaresTex, sampler_LinearClamp, SCREEN_COORDS + direction * distortion.g).g;
			float blue = SAMPLE_TEXTURE2D(_FlaresTex, sampler_LinearClamp, SCREEN_COORDS + direction * distortion.b).b;

			flares = float3(red, green, blue);
		}

		flares *= mask;
		return float4(original.rgb + flares, original.a);
	}

	float4 FragDebug(Varyings input) : SV_Target
	{
		UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

		float4 original = SCREEN_COLOR(SCREEN_COORDS);
		float3 flares = SAMPLE_TEXTURE2D(_FlaresTex, sampler_LinearClamp, SCREEN_COORDS).rgb;
		float mask = SAMPLE_TEXTURE2D(_MaskTex, sampler_LinearClamp, SCREEN_COORDS).r;

		//CA
		if (_ChromaticAbberation > 0)
		{
			float2 direction = normalize((float2(0.5, 0.5) - SCREEN_COORDS));
			float3 distortion = float3(-_FlaresTex_TexelSize.x * _ChromaticAbberation, 0, _FlaresTex_TexelSize.x * _ChromaticAbberation);

			float red = SAMPLE_TEXTURE2D(_FlaresTex, sampler_LinearClamp, SCREEN_COORDS + direction * distortion.r).r;
			float green = SAMPLE_TEXTURE2D(_FlaresTex, sampler_LinearClamp, SCREEN_COORDS + direction * distortion.g).g;
			float blue = SAMPLE_TEXTURE2D(_FlaresTex, sampler_LinearClamp, SCREEN_COORDS + direction * distortion.b).b;

			flares = float3(red, green, blue);
		}

		flares *= mask;

		return float4(flares.rgb, original.a);
	}

	ENDHLSL

	SubShader
	{
		Cull Off ZWrite Off ZTest Always

		Pass //0
		{
			Name "Lens flares: Luminance mask"
			HLSLPROGRAM
			#pragma multi_compile_vertex _ _USE_DRAW_PROCEDURAL
			#pragma exclude_renderers gles

			#pragma vertex Vert
			#pragma fragment FragLuminanceDiff

			ENDHLSL
		}
		Pass //1
		{
			Name "Lens flares: Ghosting"
			HLSLPROGRAM
			#pragma multi_compile_vertex _ _USE_DRAW_PROCEDURAL
			#pragma exclude_renderers gles

			#pragma vertex Vert
			#pragma fragment FragGhosting

			ENDHLSL
		}
		Pass //2
		{
			Name "Lens flares: Blur"
			HLSLPROGRAM
			#pragma multi_compile_vertex _ _USE_DRAW_PROCEDURAL
			#pragma exclude_renderers gles

			#pragma vertex Vert
			#pragma fragment FragBlurGaussian

			ENDHLSL
		}
		Pass //3
		{
			Name "Lens flares: Composite"
			HLSLPROGRAM
			#pragma multi_compile_vertex _ _USE_DRAW_PROCEDURAL
			#pragma exclude_renderers gles

			#pragma vertex Vert
			#pragma fragment FragBlend

			ENDHLSL
		}
		Pass //4
		{
			Name "Lens flares: Debug"
			HLSLPROGRAM
			#pragma multi_compile_vertex _ _USE_DRAW_PROCEDURAL
			#pragma exclude_renderers gles

			#pragma vertex Vert
			#pragma fragment FragDebug

			ENDHLSL
		}
	}
}