#include "Pipeline/Pipeline.hlsl"

//Value is set up through script and represents texel radius. Once horizontally, once vertically, per blur pass
float4 _BlurOffsets;

#define BOX_RCP 0.25

static const uint NeumannKernelSize = 4;
static const int2 NeumannKernel[NeumannKernelSize] = { int2(-1,0), int2(1,0), int2(0,1), int2(0,-1) };

float4 BoxFilter4(TEXTURE2D_X_PARAM(textureName, samplerTex), float2 uv, float2 texelSize, float amount)
{
	float4 color = 0;

	UNITY_UNROLL
	for(uint k = 0; k < NeumannKernelSize; k++)
	{
		color += SAMPLE_TEXTURE2D_X(textureName, samplerTex, uv + (NeumannKernel[k] * texelSize.xy * amount));
	}

	return color * BOX_RCP;
}

// Standard box filtering
half4 UpsampleBox(TEXTURE2D_X_PARAM(tex, samplerTex), float2 uv, float2 texelSize, float4 sampleScale)
{
	float4 d = texelSize.xyxy * float4(-1.0, -1.0, 1.0, 1.0) * (sampleScale * 0.5);

	half4 s;
	s = (SAMPLE_TEXTURE2D_X(tex, samplerTex, (uv + d.xy)));
	s += (SAMPLE_TEXTURE2D_X(tex, samplerTex, (uv + d.zy)));
	s += (SAMPLE_TEXTURE2D_X(tex, samplerTex, (uv + d.xw)));
	s += (SAMPLE_TEXTURE2D_X(tex, samplerTex, (uv + d.zw)));

	return s * BOX_RCP;
}

float4 FragBlurBox(Varyings input) : SV_Target
{
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

	return BoxFilter4(TEXTURE2D_X_ARGS(_MainTex, sampler_MainTex), SCREEN_COORDS, _BlurOffsets.xy, 1.0).rgba;
}

static const uint GaussianKernelSize = 7;
static const float GaussianWeights[GaussianKernelSize] = { 0.40, 0.15, 0.15, 0.10, 0.10, 0.05, 0.055 };

static float2 GaussianOffsetKernel[GaussianKernelSize] =
{
	float2(0,0),
	+ _BlurOffsets.xy,
	- _BlurOffsets.xy,
	+ _BlurOffsets.xy * 2.0,
	- _BlurOffsets.xy * 2.0,
	+ _BlurOffsets.xy * 6.0,
	- _BlurOffsets.xy * 6.0,
};

float4 FragBlurGaussian(Varyings input) : SV_Target
{
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

	float4 color = 0;
	
	UNITY_UNROLL
 	for(uint k = 0; k < GaussianKernelSize; k++)
	{
		color += GaussianWeights[k] * SAMPLE_TEXTURE2D_X(_MainTex, sampler_MainTex, SCREEN_COORDS + GaussianOffsetKernel[k]);
	}

	return color;
}