Shader "Hidden/SC Post Effects/Edge Detection" {

	HLSLINCLUDE

	#define REQUIRE_DEPTH
	#define REQUIRE_DEPTH_NORMALS
	#include "../../Shaders/Pipeline/Pipeline.hlsl"
	//Camera depth textures

	//Parameters
	uniform half4 _Sensitivity;
	uniform half _BackgroundFade;
	uniform float _EdgeSize;
	uniform float4 _EdgeColor;
	uniform float _EdgeOpacity;
	uniform float _Exponent;
	uniform float _Threshold;
	float4 _FadeParams;
	//X: Start
	//Y: End
	//Z: Invert
	//W: Enabled

	uniform float4 _SobelParams;

	inline half IsSame(half2 centerNormal, float centerDepth, half4 theSample)
	{
		// difference in normals
		half2 diff = abs(centerNormal - theSample.xy) * _Sensitivity.y;
		half isSameNormal = (diff.x + diff.y) * _Sensitivity.y < 0.1;
		// difference in depth
		float sampleDepth = DecodeFloatRG(theSample.zw);
		float zdiff = abs(centerDepth - sampleDepth);
		// scale the required threshold by the distance
		half isSameDepth = zdiff * _Sensitivity.x < 0.09 * centerDepth;

		// return:
		// 1 - if normals and depth are similar enough
		// 0 - otherwise

		return isSameNormal * isSameDepth;
	}

	half4 fragDNormals(Varyings input) : SV_Target
	{
		UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

		half4 original = SCREEN_COLOR(SCREEN_COORDS);

		half4 center = SAMPLE_DEPTH_NORMALS(SCREEN_COORDS);
		//return center;
		half4 sample1 = SAMPLE_DEPTH_NORMALS(SCREEN_COORDS + float2(-_MainTex_TexelSize.x, -_MainTex_TexelSize.y) * _EdgeSize);
		half4 sample2 = SAMPLE_DEPTH_NORMALS(SCREEN_COORDS + float2(+_MainTex_TexelSize.x, -_MainTex_TexelSize.y) * _EdgeSize);

		// encoded normal
		half2 centerNormal = center.xy;
		// decoded depth
		float centerDepth = DecodeFloatRG(center.zw);

		half edge = 1;
		edge *= IsSame(centerNormal, centerDepth, sample1);
		edge *= IsSame(centerNormal, centerDepth, sample2);
		edge = 1 - edge;

		//Edges only
		original = lerp(original, float4(1, 1, 1, 1), _BackgroundFade);

		//Opacity
		float3 edgeColor = lerp(original.rgb, _EdgeColor.rgb, _EdgeOpacity * LinearDepthFade(centerDepth, _FadeParams.x, _FadeParams.y, _FadeParams.z, _FadeParams.w));
		edgeColor = saturate(edgeColor);

		return float4(lerp(original.rgb, edgeColor.rgb, edge).rgb, original.a);

	}

	half4 fragRobert(Varyings input) : SV_Target
	{
		UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

		half4 original = SCREEN_COLOR(SCREEN_COORDS);
		//return SAMPLE_DEPTH_NORMALS(SCREEN_COORDS);

		half4 sample1 = SAMPLE_DEPTH_NORMALS(SCREEN_COORDS + _MainTex_TexelSize.xy * half2(1, 1) * _EdgeSize);
		half4 sample2 = SAMPLE_DEPTH_NORMALS(SCREEN_COORDS + _MainTex_TexelSize.xy * half2(-1, -1) * _EdgeSize);
		half4 sample3 = SAMPLE_DEPTH_NORMALS(SCREEN_COORDS + _MainTex_TexelSize.xy * half2(-1, 1) * _EdgeSize);
		half4 sample4 = SAMPLE_DEPTH_NORMALS(SCREEN_COORDS + _MainTex_TexelSize.xy * half2(1, -1) * _EdgeSize);

		float centerDepth = DecodeFloatRG(sample1.zw);

		half edge = 1.0;

		edge *= IsSame(sample1.xy, DecodeFloatRG(sample1.zw), sample2);
		edge *= IsSame(sample3.xy, DecodeFloatRG(sample3.zw), sample4);

		edge = 1 - edge;

		//Edges only
		original = lerp(original, float4(1, 1, 1, 1), _BackgroundFade);

		//Opacity
		float3 edgeColor = lerp(original.rgb, _EdgeColor.rgb, _EdgeOpacity * LinearDepthFade(centerDepth, _FadeParams.x, _FadeParams.y, _FadeParams.z, _FadeParams.w));

		//return original;
		return float4(lerp(original.rgb, edgeColor.rgb, edge).rgb, original.a);
	}

	float4 fragSobel(Varyings input) : SV_Target
	{
		UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

		// inspired by borderlands implementation of popular "sobel filter"
		half4 original = SCREEN_COLOR(SCREEN_COORDS);

		float centerDepth = LINEAR_DEPTH(SAMPLE_DEPTH(SCREEN_COORDS));
		//return centerDepth;
		float4 depthsDiag;
		float4 depthsAxis;

		float2 texelSize = _EdgeSize * _MainTex_TexelSize.xy;

		depthsDiag.x = LINEAR_DEPTH(SAMPLE_DEPTH(SCREEN_COORDS + texelSize)); // TR
		depthsDiag.y = LINEAR_DEPTH(SAMPLE_DEPTH(SCREEN_COORDS + texelSize * half2(-1, 1))); // TL
		depthsDiag.z = LINEAR_DEPTH(SAMPLE_DEPTH(SCREEN_COORDS - texelSize * half2(-1, 1))); // BR
		depthsDiag.w = LINEAR_DEPTH(SAMPLE_DEPTH(SCREEN_COORDS - texelSize)); // BL

		depthsAxis.x = LINEAR_DEPTH(SAMPLE_DEPTH(SCREEN_COORDS + texelSize * half2(0, 1))); // T
		depthsAxis.y = LINEAR_DEPTH(SAMPLE_DEPTH(SCREEN_COORDS - texelSize * half2(1, 0))); // L
		depthsAxis.z = LINEAR_DEPTH(SAMPLE_DEPTH(SCREEN_COORDS + texelSize * half2(1, 0))); // R
		depthsAxis.w = LINEAR_DEPTH(SAMPLE_DEPTH(SCREEN_COORDS - texelSize * half2(0, 1))); // B	

		//Thin edges
		if (_SobelParams.x == 1) {
			depthsDiag = (depthsDiag > centerDepth.xxxx) ? depthsDiag : centerDepth.xxxx;
			depthsAxis = (depthsAxis > centerDepth.xxxx) ? depthsAxis : centerDepth.xxxx;
		}
		depthsDiag -= centerDepth;
		depthsAxis /= centerDepth;

		const float4 HorizDiagCoeff = float4(1,1,-1,-1);
		const float4 VertDiagCoeff = float4(-1,1,-1,1);
		const float4 HorizAxisCoeff = float4(1,0,0,-1);
		const float4 VertAxisCoeff = float4(0,1,-1,0);

		float4 SobelH = depthsDiag * HorizDiagCoeff + depthsAxis * HorizAxisCoeff;
		float4 SobelV = depthsDiag * VertDiagCoeff + depthsAxis * VertAxisCoeff;

		float SobelX = dot(SobelH, float4(1,1,1,1));
		float SobelY = dot(SobelV, float4(1,1,1,1));
		float Sobel = sqrt(SobelX * SobelX + SobelY * SobelY);

		Sobel = 1.0 - pow(saturate(Sobel), _Exponent);

		float edge = 1 - Sobel;

		//Orthographic camera: Still not correct, but value should be flipped
		if (unity_OrthoParams.w) edge = 1 - edge;

		//Edges only
		original = lerp(original, float4(1, 1, 1, 1), _BackgroundFade);

		//Opacity
		float3 edgeColor = lerp(original.rgb, _EdgeColor.rgb, _EdgeOpacity * LinearDepthFade(centerDepth, _FadeParams.x, _FadeParams.y, _FadeParams.z, _FadeParams.w));

		return float4(lerp(original.rgb, edgeColor.rgb, edge).rgb, original.a);
	}

	float4 fragLum(Varyings input) : SV_Target
	{
		UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

		float4 original = SCREEN_COLOR(SCREEN_COORDS);

		float centerDepth = LINEAR_DEPTH(SAMPLE_DEPTH(SCREEN_COORDS));

		half3 p1 = original.rgb;
		half3 p2 = ScreenColor(SCREEN_COORDS + float2(-_MainTex_TexelSize.x, -_MainTex_TexelSize.y) * _EdgeSize).rgb;
		half3 p3 = ScreenColor(SCREEN_COORDS + float2(+_MainTex_TexelSize.x, -_MainTex_TexelSize.y) * _EdgeSize).rgb;

		half3 diff = p1 * 2 - p2 - p3;
		half edge = dot(diff, diff);
		edge = step(edge, _Threshold);

		edge = 1 - edge;

		//Edges only
		original = lerp(original, float4(1, 1, 1, 1), _BackgroundFade);

		//Opacity
		float3 edgeColor = lerp(original.rgb, _EdgeColor.rgb, _EdgeOpacity * LinearDepthFade(centerDepth, _FadeParams.x, _FadeParams.y, _FadeParams.z, _FadeParams.w));
		edgeColor = saturate(edgeColor);

		//return original;
		return float4(lerp(original.rgb, edgeColor.rgb, edge).rgb, original.a);
	}

	ENDHLSL

	Subshader 
	{
		ZTest Always Cull Off ZWrite Off
		
		Pass
		{
			Name "Edge Detection: Depth Normals"
			
			HLSLPROGRAM
			#pragma multi_compile_local _ _RECONSTRUCT_NORMAL
			#pragma multi_compile_vertex _ _USE_DRAW_PROCEDURAL
			#pragma exclude_renderers gles
			
			#ifndef URP 
			#undef _RECONSTRUCT_NORMAL
			#endif
			
			#pragma vertex Vert
			#pragma fragment fragDNormals
			ENDHLSL
		}
		Pass
		{
			Name "Edge Detection: Cross Depth Normals"
			
			HLSLPROGRAM
			#pragma multi_compile_local _ _RECONSTRUCT_NORMAL
			#pragma multi_compile_vertex _ _USE_DRAW_PROCEDURAL
			#pragma exclude_renderers gles
			
			#ifndef URP 
			#undef _RECONSTRUCT_NORMAL
			#endif
			#pragma vertex Vert
			#pragma fragment fragRobert
			ENDHLSL
		}
		Pass
		{
			Name "Edge Detection: Sobel"
			
			HLSLPROGRAM
			#pragma multi_compile_vertex _ _USE_DRAW_PROCEDURAL
			#pragma exclude_renderers gles
			
			#pragma vertex Vert
			#pragma fragment fragSobel
			ENDHLSL
		}
		Pass
		{
			Name "Edge Detection: Luminance"
			
			HLSLPROGRAM
			#pragma multi_compile_vertex _ _USE_DRAW_PROCEDURAL
			#pragma exclude_renderers gles
			
			#pragma vertex Vert
			#pragma fragment fragLum
			ENDHLSL
		}
	}

	Fallback off

} // shader
