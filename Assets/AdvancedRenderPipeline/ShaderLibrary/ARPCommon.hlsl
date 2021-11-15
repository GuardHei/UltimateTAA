#ifndef ARP_COMMON_INCLUDED
#define ARP_COMMON_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonLighting.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Sampling/Hammersley.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Sampling/Sampling.hlsl"

#define KILL_MICRO_MOVEMENT
#define MICRO_MOVEMENT_THRESHOLD (.01f * _ScreenSize.zw)

#define UNITY_MATRIX_M unity_ObjectToWorld
#define UNITY_PREV_MATRIX_M prevObjectToWorld
#define UNITY_MATRIX_I_M unity_WorldToObject
#define UNITY_PREV_MATRIX_I_M prevWorldToObject
#define UNITY_MATRIX_V unity_MatrixV
#define UNITY_MATRIX_VP unity_MatrixVP
#define UNITY_MATRIX_UNJITTERED_VP unjitteredVP
#define UNITY_MATRIX_P glstate_matrix_projection

CBUFFER_START(UnityPerDraw)
    float4x4 unity_ObjectToWorld;
    float4x4 unity_WorldToObject;
    float4x4 prevObjectToWorld;
    float4x4 prevWorldToObject;
    float4 unity_LODFade;
    real4 unity_WorldTransformParams;
CBUFFER_END

CBUFFER_START(CameraData)
    float3 _CameraPosWS;
    float3 _CameraFwdWS;
    float4 _ScreenSize; // { w, h, 1 / w, 1 / h }
CBUFFER_END

float4 unity_MotionVectorsParams;
float4x4 unity_MatrixVP;
float4x4 unjitteredVP;
float4x4 unity_MatrixV;
float4x4 glstate_matrix_projection;

#include "ARPInstancing.hlsl"
// #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"

//////////////////////////////////////////
// Built-in Lighting & Shadow Variables //
//////////////////////////////////////////

CBUFFER_START(MainLightData)
    float4 _MainLightDir;
    float4 _MainLightColor;
CBUFFER_END

// CBUFFER_START(MainLightShadowData)
    
// CBUFFER_END

//////////////////////////////////////////
// Alpha Related                        //
//////////////////////////////////////////

static float _AlphaCutOff;

//////////////////////////////////////////
// Built-in Textures and Samplers       //
//////////////////////////////////////////

TEXTURE2D(_AlbedoMap);
SAMPLER(sampler_AlbedoMap);
TEXTURE2D(_NormalMap);
SAMPLER(sampler_NormalMap);
TEXTURE2D(_MetallicMap);
SAMPLER(sampler_MetallicMap);
TEXTURE2D(_SpecularMap);
SAMPLER(sampler_SpecularMap);
TEXTURE2D(_SmoothnessMap);
SAMPLER(sampler_SmoothnessMap);
TEXTURE2D(_MetallicSmoothnessMap);
SAMPLER(sampler_MetallicSmoothnessMap);
TEXTURE2D(_OcclusionMap);
SAMPLER(sampler_OcclusionMap);
TEXTURE2D(_EmissionMap);
SAMPLER(sampler_EmissionMap);

//////////////////////////////////////////
// Built-in Utility Functions           //
//////////////////////////////////////////

float4 TransformObjectToWorldTangent(float4 tangentOS) {
    return float4(TransformObjectToWorldDir(tangentOS.xyz), tangentOS.w);
}

float3 ApplyNormalMap(float3 data, float3 normalWS, float4 tangentWS) {
    float3x3 tangentToWorld = CreateTangentToWorld(normalWS, tangentWS.xyz, tangentWS.w);
    return TransformTangentToWorld(data, tangentToWorld);
}

float2 CalculateMotionVector(float4 posCS, float4 prevPosCS) {
    float2 posNDC = posCS.xy / posCS.w;
    float2 prevPosNDC = prevPosCS.xy / prevPosCS.w;
    float2 mv = posNDC - prevPosNDC;
    
    #ifdef KILL_MICRO_MOVEMENT
    mv.x = abs(mv.x) < MICRO_MOVEMENT_THRESHOLD.x ? 0 : mv.x;
    mv.y = abs(mv.y) < MICRO_MOVEMENT_THRESHOLD.y ? 0 : mv.y;
    mv = clamp(mv, -1.0 + MICRO_MOVEMENT_THRESHOLD, 1.0 - MICRO_MOVEMENT_THRESHOLD);
    #else
    mv = clamp(mv, -1.0, 1.0);
    #endif

    #if UNITY_UV_STARTS_AT_TOP
    mv.y = -mv.y;
    #endif

    return mv;
}

// Convert from Clip space (-1..1) to NDC 0..1 space.
// Note it doesn't mean we don't have negative value, we store negative or positive offset in NDC space.
// Note: ((positionCS * 0.5 + 0.5) - (previousPositionCS * 0.5 + 0.5)) = (motionVector * 0.5)
// PS: From Unity HDRP
float2 EncodeMotionVector(float2 mv) {
    return mv * .5;
}

float2 DecodeMotionVector(float2 encoded) {
    return encoded * 2.0;
}

//////////////////////////////////////////
// PBR Utility Functions                //
//////////////////////////////////////////

float pow5(float b) {
    float temp0 = b * b;
    float temp1 = temp0 * temp0;
    return temp1 * b;
}

float LinearSmoothToLinearRoughness(float ls) {
    return 1 - ls;
}

// Roughness = Alpha
float LinearRoughnessToRoughness(float lr) {
    return lr * lr;
}

// (Linear Roughness) ^ 4 = AlphaG2
float RoughnessToAlphaG2(float roughness) {
    return roughness * roughness;
}

float ClampMinLinearRoughness(float linearRoughness) {
    return max(linearRoughness, .04f); // Anti specular flickering
}

float3 GetF0(float3 albedo, float metallic) {
    float3 f0 = float3(.04, .04, .04);
    // return lerp(f0, albedo.rgb, metallic);
    return f0 * (1.0 - metallic) + albedo * metallic;
}

float3 GetF0(float3 reflectance) {
    return .16 * (reflectance * reflectance);
}

float3 F_Schlick(in float3 f0, in float f90, in float u) {
    return f0 + (f90 - f0) * pow5(1.0 - u);
}

float3 F_Schlick(in float3 f0, in float u) {
    return f0 + (float3(1.0, 1.0, 1.0) - f0) * pow5(1.0 - u);
}

float V_SmithGGX(float NdotL, float NdotV, float alphaG2) {
    const float lambdaV = NdotL * sqrt((-NdotV * alphaG2 + NdotV) * NdotV + alphaG2);
    const float lambdaL = NdotV * sqrt ((-NdotL * alphaG2 + NdotL) * NdotL + alphaG2);
    return .5 / (lambdaV + lambdaL);
}

float D_GGX(float NdotH, float alphaG2) {
    // Higher accuracy?
    const float f = (alphaG2 - 1) * NdotH * NdotH + 1;
    // const float f = (NdotH * alphaG2 - NdotH) * NdotH + 1;
    return alphaG2 / (f * f);
}

float DisneyDiffuseRenormalized(float NdotV, float NdotL, float LdotH, float linearRoughness) {
    float energyBias = lerp(0, .5, linearRoughness);
    float energyFactor = lerp(1.0, 1.0 / 1.51, linearRoughness);
    float fd90 = energyBias + 2.0 * LdotH * LdotH * linearRoughness;
    const float3 f0 = float3(1.0, 1.0, 1.0);
    float lightScatter = F_Schlick(f0, fd90, NdotL).r;
    float viewScatter = F_Schlick(f0, fd90, NdotV).r;

    return lightScatter * viewScatter * energyFactor;
}

float CalculateFd(float NdotV, float NdotL, float LdotH, float linearRoughness) {
    float d = DisneyDiffuseRenormalized(NdotV, NdotL, LdotH, linearRoughness);
    return d / PI;
}

float3 CalculateFr(float NdotV, float NdotL, float NdotH, float LdotH, float roughness, float f0) {
    float alphaG2 = RoughnessToAlphaG2(roughness);
    float3 F = F_Schlick(f0, LdotH);
    float V = V_SmithGGX(NdotV, NdotL, alphaG2);
    float D = D_GGX(NdotH, alphaG2);
    return D * V * F / PI;
}

//////////////////////////////////////////
// IBL Utility Functions                //
//////////////////////////////////////////

float3 ImportanceSampleGGX(float2 u, float3 V, float roughness) {
    float cosTheta = sqrt(SafeDiv(1.0 - u.x, 1.0 + (roughness * roughness - 1.0) * u.x));
    float phi = TWO_PI * u.y;
    float3 H = SphericalToCartesian(phi, cosTheta);
    return H;
}

float2 PrecomputeL_DFG(float NdotV, float NdotL, float roughness) {
    float alphaG2 = RoughnessToAlphaG2(roughness);
    float3 V = float3(sqrt(1.0f - NdotV * NdotV), .0f, NdotV);
    float2 r = .0f;
    float3 N = float3(.0f, .0f, 1.0f);
    const uint SAMPLE_COUNT = 2048u;
    for (uint i = 0; i < SAMPLE_COUNT; i++) {
        float2 Xi = Hammersley2dSeq(i, SAMPLE_COUNT);
        float3 H = ImportanceSampleGGX(Xi, N, roughness);
        float3 L = 2.0f * dot(V, H) * H - V;

        float VdotH = saturate(dot(V, H));
        float NdotL = saturate(L.z);
        float NdotH = saturate(H.z);

        if (NdotL > .0f) {
            float G = V_SmithGGX(NdotL, NdotV, alphaG2);
            float Gv = G * VdotH / NdotH;
            float Fc = pow5(1.0f - VdotH);
            r.x += Gv * (1.0f - Fc);
            r.y += Gv * Fc;
        }
    }

    return r * (1.0f / SAMPLE_COUNT);
}

#endif