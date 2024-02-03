#ifndef UNITY_GRAPHFUNCTIONS_LW_INCLUDED //Shader Graph
#endif
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

#if defined(SHADERGRAPH_PREVIEW)
#define TEXTURE2D_X TEXTURE2D
#define SAMPLE_TEXTURE2D_X SAMPLE_TEXTURE2D
#endif

#include "../../Shaders/SCPE.hlsl"

TEXTURE2D_X(_MainTex);
SAMPLER(sampler_MainTex);

void GetSourceImage_float(float2 uv, out float4 color)
{
    color = SAMPLE_TEXTURE2D_X(_MainTex, sampler_MainTex, uv);
}

void WorldSpacePosition_float(float4 uv, float rawDepth, out float3 positionWS)
{
    positionWS = GetWorldPosition(uv.xy, rawDepth);
}

void GetScreenLuminance_float(float4 color, out float luminance)
{
    luminance = Luminance(color.rgb);
}

void DistanceFactor_float(float3 positionWS, float start, float end, out float factor)
{
    float pixelDist = length(_WorldSpaceCameraPos.xyz - positionWS.xyz);

    //Distance based scalar
    factor = saturate((end - pixelDist ) / (end-start));
}

void LinearDepthFade_float(float linearDepth, float start, float end, out float fadeFactor)
{
    fadeFactor = LinearDepthFade(linearDepth, start, end, 0, 1);
}