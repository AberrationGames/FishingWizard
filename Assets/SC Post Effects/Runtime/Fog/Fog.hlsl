#if !defined(SHADERGRAPH_PREVIEW)
//#define REQUIRE_DEPTH

#if !defined(PIPELINE_INCLUDED) && !defined(UNIVERSAL_PIPELINE_CORE_INCLUDED)
#include "../../Shaders/Pipeline/Pipeline.hlsl"
#else

#endif

TEXTURE2D(_NoiseTex);
TEXTURE2D(_ColorGradient);
TEXTURE2D_X(_SkyboxTex);

#if !defined(SAMPLERS_DEFINED) && !defined(UNITY_CORE_SAMPLERS_INCLUDED) && !defined(UNITY_CORE_BLIT_INCLUDED)
#define SAMPLERS_DEFINED
SamplerState sampler_LinearClamp;
SamplerState sampler_LinearRepeat;
#endif
#endif

int4 _SceneFogMode;
float4 _HeightParams;
float4 _DistanceParams;
float4 _SceneFogParams;
float4 _DensityParams;
float4 _NoiseParams;
float4 _FogColor;
float4 _SkyboxParams;
//X: Influence
//Y: Horizon Min
//Z: Horizon Max
float4 _DirLightParams;
//XYZ: Direction
//W: Exponent
float4 _DirLightColor; //(a=free)
//Alpha: Intensity
uniform half _FarClippingPlane;	//Used for gradient distance

bool _SCPEFogEnabled;

half ComputeFactor(float coord)
{
    float fogFac = 0.0;
    if (_SceneFogMode.x == 1) // linear
        {
        // factor = (end-z)/(end-start) = z * (-1/(end-start)) + (end/(end-start))
        fogFac = coord * _SceneFogParams.z + _SceneFogParams.w;
        }
    if (_SceneFogMode.x == 2) // exp
        {
        // factor = exp(-density*z)
        fogFac = _SceneFogParams.y * coord; fogFac = exp2(-fogFac);
        }
    if (_SceneFogMode.x == 3) // exp2
        {
        // factor = exp(-(density*z)^2)
        fogFac = _SceneFogParams.x * coord; fogFac = exp2(-fogFac * fogFac);
        }
    return saturate(fogFac);
}

float ComputeDistance(float3 wpos, float depth)
{
    float3 wsDir = _WorldSpaceCameraPos.xyz - wpos;
    float dist;
    //Radial distance
    if (_SceneFogMode.y == 1)
        dist = length(wsDir);
    else
        dist = depth * _ProjectionParams.z;
    //Start distance
    dist -= _ProjectionParams.y;
    return dist;
}

//Use unique name, may clash with other fog assets
float ComputeHeightFogSCPE(float3 wpos)
{
    float3 wsDir = _WorldSpaceCameraPos.xyz - wpos;
    float FH = _HeightParams.x;
    float3 C = _WorldSpaceCameraPos;
    float3 V = wsDir;
    float3 P = wpos;
    float3 aV = _HeightParams.w * V;
    float FdotC = _HeightParams.y;
    float k = _HeightParams.z;
    float FdotP = P.y - FH;
    float FdotV = wsDir.y;
    float c1 = k * (FdotP + FdotC);
    float c2 = (1 - 2 * k) * FdotP;
    float g = min(c2, 0.0);
    g = -length(aV) * (c1 - g * g / abs(FdotV + 1.0e-5f));
    return g;
}

float GetFogDistance(float3 worldPos, float linearDepth)
{
    //Distance fog
    float distanceFog = 0;
    float distanceWeight = 0;
    if (_DistanceParams.z == 1) {
        distanceFog = ComputeDistance(worldPos, linearDepth);

        //Density (separated so it doesn't affect the UV of a gradient texture)
        distanceWeight = distanceFog * _DensityParams.x;
    }

    return distanceWeight;
}

float4 GetFogColor(float2 screenPos, float3 worldPos, float distanceFactor)
{
    #if !defined(SHADERGRAPH_PREVIEW)
    float4 fogColor = _FogColor.rgba;
#ifndef DISABLE_FOG_GRADIENT
    if (_SceneFogMode.z == 1) //Gradient
    {
        fogColor = SAMPLE_TEXTURE2D(_ColorGradient, sampler_LinearClamp, float2(distanceFactor / _FarClippingPlane, 0));
    }
#endif
#ifndef DISABLE_FOG_SKYBOXCOLOR
    if (_SceneFogMode.z == 2) //Skybox
    {
        fogColor = SAMPLE_TEXTURE2D_X_LOD(_SkyboxTex, sampler_LinearClamp, screenPos, 0).rgba; //Mip not used
    }
#endif

#if !defined(DISABLE_FOG_DIRECTIONAL_COLOR)
    if(_DirLightColor.a > 0)
    {
        float3 viewDir = normalize(_WorldSpaceCameraPos.xyz - worldPos);
        float NdotL = saturate(pow(saturate(dot(-viewDir, _DirLightParams.xyz)), _DirLightParams.a));
        fogColor.rgb = lerp(fogColor.rgb, _DirLightColor.rgb * _DirLightColor.a, NdotL);
    }
#endif

    return fogColor;
    #else
    return 1;
    #endif
}

float GetFogHeight(float3 worldPos, float skyboxMask)
{
    #if !defined(SHADERGRAPH_PREVIEW)
    //Height fog
    float heightFog = 0;
    float heightWeight = 0;
    if (_DistanceParams.w == 1) { //Heightfog enabled
        float noise = 1;
#ifndef DISABLE_FOG_HEIGHTNOISE
        if (_SceneFogMode.w == 1) //Noise enabled
        {
            float noise1 = SAMPLE_TEXTURE2D(_NoiseTex, sampler_LinearRepeat, worldPos.xz * _NoiseParams.x + (_Time.y * _NoiseParams.y * float2(0, 1))).r;
            float noise2 = SAMPLE_TEXTURE2D(_NoiseTex, sampler_LinearRepeat, worldPos.xz * _NoiseParams.x * 0.8 + (_Time.y * _NoiseParams.y * 0.2 * float2(0, 1))).r;
            
            noise = lerp(1, max(noise1, noise2), _DensityParams.y * skyboxMask);
        }
#endif
        heightFog = ComputeHeightFogSCPE(worldPos);
        heightWeight += heightFog * noise;
    }

    return heightWeight;
    #else
    return 1.0;
    #endif
}

//Note: clipZ should be linear depth when used in screen-space
float GetFogDensity(float3 worldPos, float linearDepth, float skyboxMask)
{
    #if !defined(SHADERGRAPH_PREVIEW)
    //Fog start distance
    float g = _DistanceParams.x;

    g += GetFogDistance(worldPos, linearDepth);
    g += GetFogHeight(worldPos, skyboxMask);

    //Fog density (Linear/Exp/ExpSqr)
    half fogFac = ComputeFactor(max(0.0, g));

    return fogFac;
    #else
    return 1.0;
    #endif
}

//Main function!
float4 ComputeFog(float3 worldPos, float2 screenPos, float linearDepth)
{
    if(_SCPEFogEnabled == false) return float4(0, 0, 0, 1.0);

    #if !defined(SHADERGRAPH_PREVIEW)

    float skyboxMask = 1;
    if (linearDepth > 0.99) skyboxMask = 0;

    //Same as GetFogDensity but distance/fog calculated separately
    float g = _DistanceParams.x;
    float distanceFog = GetFogDistance(worldPos, linearDepth);
    g += distanceFog;
    float heightFog = GetFogHeight(worldPos, skyboxMask);
    g += heightFog;
    float fogFactor = ComputeFactor(max(0.0, g));

    //Skybox influence
    if (linearDepth > 0.99) fogFactor = lerp(1.0, fogFactor, _SkyboxParams.x);

    float3 viewDir = normalize(worldPos - _WorldSpaceCameraPos);
    half horizon = saturate(viewDir.y);

    #define HORIZON_MIN _SkyboxParams.y
    #define HORIZON_MAX _SkyboxParams.z

    horizon = smoothstep(HORIZON_MIN, HORIZON_MAX, horizon);

    //fogFactor = mul(fogFactor, horizon);
    fogFactor += horizon;
    
    //Color
    float4 fogColor = GetFogColor(screenPos, worldPos, distanceFog);

    fogColor.a = saturate(fogFactor);

    return fogColor;
    #else
    return 1.0;
    #endif
}

//Override without the depth input. This is used in transparent shaders
float4 ComputeTransparentFog(float3 worldPos, float2 screenPos)
{
    return ComputeFog(worldPos, screenPos, 0.0);
}
//All in one function, for all shaders
float4 ApplyFog(float3 worldPos, float2 screenPos, float depth, inout float3 color)
{
    float4 fogColor = ComputeFog(worldPos, screenPos, depth);

    color = lerp(fogColor.rgb, color, fogColor.a);

    return fogColor;
}

//All in one function, for transparent shaders
float4 ApplyTransparencyFog(float3 worldPos, float2 screenPos, inout float3 color)
{
    float4 fogColor = ApplyFog(worldPos, screenPos, 0.0, color);

    return fogColor;
}

//Shader Graph subgraph functions
void ApplyFog_float(in float3 worldPos, in float2 screenPos, in float3 color, in float3 emission, out float3 outColor, out float3 outEmission, out float fogDensity)
{
    const float4 fogColor = ComputeFog(worldPos, screenPos, 0.0);

    fogDensity = fogColor.a;
    
    outEmission = lerp(fogColor.rgb, emission, fogColor.a);
    
    //Blend to black color so lighting gets nullified
    outColor.rgb = lerp(0, color.rgb, fogColor.a);
}

//Deprecated!
void ApplyFog_float(in float3 worldPos, in float3 worldNormal, in float4 screenPos, in float time, in float skyMask, in float3 color, out float4 outColor, out float3 foggedColor)
{
    const float4 fogColor = ComputeFog(worldPos, screenPos.xy, 0.0);

    outColor.rgb = lerp(fogColor.rgb, color.rgb, fogColor.a);
    outColor.a = 0;

    foggedColor = fogColor.rgb;
}

//Shader graph, multiply result with final alpha to blend transparent materials into the fog
void GetFogAlpha_float(in float3 worldPos, out float factor)
{
    factor = saturate(GetFogDensity(worldPos, 0.0, 0.0));
}
