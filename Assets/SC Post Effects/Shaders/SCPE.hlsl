// SC Post Effects
// Staggart Creations
// http://staggart.xyz

//-------------
// Generic functions
//-------------

float4 DebugEyeIndex()
{
	return float4(lerp(float3(1,0,0), float3(0,1,0), unity_StereoEyeIndex), 1.0);
}

#define ANGLE2RAD_RCP 0.01745329251 //=> Pi / 180
#define EPSILON         1.0e-4

#ifdef URP
//ZW components of depth-normals texture are passed in
inline float DecodeFloatRG( float2 enc )
{
	#if !_RECONSTRUCT_NORMAL
	float2 kDecodeDot = float2(1.0, 1/255.0);
	return dot( enc, kDecodeDot );
	#else
	return enc.y; //W-component contains linear depth
	#endif
}

// Decode normals stored in _CameraDepthNormalsTexture
float3 DecodeViewNormalStereo(float4 enc4)
{
	float kScale = 1.7777;
	float3 nn = enc4.xyz * float3(2.0 * kScale, 2.0 * kScale, 0) + float3(-kScale, -kScale, 1);
	float g = 2.0 / dot(nn.xyz, nn.xyz);
	float3 n;
	n.xy = g * nn.xy;
	n.z = g - 1.0;
	return n;
}

inline void DecodeDepthNormal( float4 enc, out float depth, out float3 normal )
{
	depth = DecodeFloatRG (enc.zw);
	normal = DecodeViewNormalStereo (enc);
}
#endif

float rand(float n) { return frac(sin(n) * 13758.5453123 * 0.01); }

float rand(float2 n) { return frac(sin(dot(n, float2(12.9898, 4.1414))) * 43758.5453); }

//v2.2.0, function now takes an angle in degrees
float2 RotateUV(float2 uv, float2 pivot, float angle)
{
	const float radians = ANGLE2RAD_RCP * angle;
	
	float cosine = cos(radians);
	float sine = sin(radians);
	
	const float2 rotator = (mul(uv - pivot, float2x2(cosine, -sine, sine, cosine)) + pivot);
	
	return saturate(rotator);
}

half LuminanceThreshold(half val, half threshold)
{
	half contrib = max(0.0, val - threshold);

	contrib /= max(val, 0.001);

	return val * contrib;
}

float max3(float3 color)
{
	return max(color.r, max(color.g, color.b));
}

half3 LuminanceThreshold(half3 color, half threshold)
{
	half br = max3(color);

	half contrib = max(0, br - threshold);

	contrib /= max(br, 0.001);

	return color * contrib;
}

float min3(float3 color)
{
	return min(color.r, min(color.g, color.b));
}

#define HALF_MAX        65504.0 // (2 - 2^-10) * 2^15

// Clamp HDR value within a safe range
half3 SafeHDR(half3 c)
{
	return min(c, HALF_MAX);
}

half4 SafeHDR(half4 c)
{
	return min(c, HALF_MAX);
}

// Quadratic color thresholding
// curve = (threshold - knee, knee * 2, 0.25 / knee)
half4 QuadraticThreshold(half4 color, half threshold, half3 curve)
{
	// Pixel brightness
	half br = max3(color.rgb);

	// Under-threshold part: quadratic curve
	half rq = clamp(br - curve.x, 0.0, curve.y);
	rq = curve.z * rq * rq;

	// Combine and apply the brightness response curve.
	color *= max(rq, br - threshold) / max(br, EPSILON);

	return color;
}

//HLSL rcp function only supports SM 5.0+ (and not PS4) and throws a warning about vector truncation
float3 rcp3(float3 input)
{
	return 1.0 / input.xyz;
}

float EdgeMask(float2 uv, float size, float smoothness)
{
	half2 d = abs(uv - float2(0.5, 0.5)) * size;
	d = pow(saturate(d), 2); // Roundness
	return pow(saturate(dot(d, d)), smoothness);
}

#if !defined(UNITY_COLOR_INCLUDED) && !SHADER_API_METAL && !SHADER_API_VULKAN //Already defined
float Luminance(float3 color)
{
	return (color.r * 0.3 + color.g * 0.59 + color.b * 0.11);
}
#endif

// (returns 1.0 when orthographic)
float CheckPerspective(float x)
{
	return lerp(x, 1.0, unity_OrthoParams.w);
}

#define NEAR_PLANE _ProjectionParams.y
#define FAR_PLANE _ProjectionParams.z

float LinearDepthFade(float linearDepth, float start, float end, float invert, float enable)
{
	if(enable == 0.0) return 1.0;
	
	float rawDepth = (linearDepth * FAR_PLANE) - NEAR_PLANE;
	float eyeDepth = FAR_PLANE - ((_ZBufferParams.z * (1.0 - rawDepth) + _ZBufferParams.w) * _ProjectionParams.w);

	float perspDist = rawDepth;
	float orthoDist = eyeDepth;

	//Non-linear depth values
	float dist = lerp(perspDist, orthoDist, unity_OrthoParams.w);
	
	float fadeFactor = saturate((end - dist) / (end-start));

	//OpenGL + Vulkan
	#if !defined(UNITY_REVERSED_Z)
	fadeFactor = 1-fadeFactor;
	#endif
	
	if (invert == 1.0) fadeFactor = 1-fadeFactor;

	return fadeFactor;
}

//Having a world position, fading can be radial
float LinearDistanceFade(float3 worldPos, float start, float end, float invert, float enable)
{
	if(enable == 0.0) return 1.0;
	
	float pixelDist = length(_WorldSpaceCameraPos.xyz - worldPos.xyz);

	//Distance based scalar
	float fadeFactor = saturate((end - pixelDist ) / (end-start));

	if (invert == 1.0) fadeFactor = 1-fadeFactor;

	return fadeFactor;
}

#if !defined(URP) && !defined(UNITY_COMMON_INCLUDED) //Backported to Built-in RP

//https://github.com/Unity-Technologies/Graphics/blob/9cba459544c8a16f0336173ce081f2e3e240c4cd/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl#L1097
float4 ComputeClipSpacePosition(float2 positionNDC, float deviceDepth)
{
	float4 positionCS = float4(positionNDC * 2.0 - 1.0, deviceDepth, 1.0);

	#if UNITY_UV_STARTS_AT_TOP
	positionCS.y = -positionCS.y;
	#endif

	return positionCS;
}
#endif

float3 GetWorldPosition(float2 screenPos, float deviceDepth )
{
	//This unrolls to an array using [unity_StereoEyeIndex] when VR is enabled
	float4x4 invViewProjMatrix = unity_CameraInvProjection;
	
	#if UNITY_REVERSED_Z //Anything other than OpenGL
	deviceDepth = (1.0 - deviceDepth) * 2.0 - 1.0;
	
	//https://issuetracker.unity3d.com/issues/shadergraph-inverse-view-projection-transformation-matrix-is-not-the-inverse-of-view-projection-transformation-matrix
	invViewProjMatrix._12_22_32_42 = -invViewProjMatrix._12_22_32_42;
	
	float rawDepth = deviceDepth;
	#else
	//Adjust z to match NDC for OpenGL
	float rawDepth = lerp(UNITY_NEAR_CLIP_VALUE, 1, deviceDepth);
	#endif

	//Unrolled from ComputeWorldSpacePosition and ComputeViewSpacePosition functions. Since this is bugged between different URP versions
	float4 positionCS  = ComputeClipSpacePosition(screenPos.xy, rawDepth);
	float4 hpositionWS = mul(invViewProjMatrix, positionCS);

	#ifndef USING_STEREO_MATRICES
	//The view space uses a right-handed coordinate system.
	hpositionWS.z = -hpositionWS.z;
	#endif
	
	float3 positionVS = hpositionWS.xyz / max(0, hpositionWS.w);
	float3 positionWS = mul(unity_CameraToWorld, float4(positionVS, 1.0)).xyz;

	return positionWS;
}
