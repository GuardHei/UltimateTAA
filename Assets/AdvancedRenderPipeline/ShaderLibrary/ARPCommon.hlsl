#ifndef ARP_COMMON_INCLUDED
#define ARP_COMMON_INCLUDED

#define KILL_MICRO_MOVEMENT
#define MICRO_MOVEMENT_THRESHOLD (.01f * _ScreenSize.zw)

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonLighting.hlsl"
// #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Sampling/Hammersley.hlsl"
#include "ARPSequence.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Sampling/Sampling.hlsl"

#define UNITY_MATRIX_M unity_ObjectToWorld
#define UNITY_PREV_MATRIX_M prevObjectToWorld
#define UNITY_MATRIX_I_M unity_WorldToObject
#define UNITY_PREV_MATRIX_I_M prevWorldToObject
#define UNITY_MATRIX_V unity_MatrixV
#define UNITY_MATRIX_VP unity_MatrixVP
#define UNITY_MATRIX_UNJITTERED_VP unjitteredVP
#define UNITY_MATRIX_P glstate_matrix_projection

struct RTHandleProperties {
    int4 viewportSize; // xy: curr, zw: prev
    int4 rtSize; // xy: curr, zw: prev
    float4 rtHandleScale; // xy: curr, zw: prev
};

struct DirectionalLight {
    float4 direction;
    float4 color;
};

/*
struct CameraData {
    float3 _CameraPosWS;
    float3 _CameraFwdWS;
    float4 _ScreenSize; // { w, h, 1 / w, 1 / h }
    RTHandleProperties _RTHandleProps;
};
*/

CBUFFER_START(CameraData)
    float3 _CameraPosWS;
    float3 _CameraFwdWS;
    float4 _ScreenSize; // { w, h, 1 / w, 1 / h }
    RTHandleProperties _RTHandleProps;
CBUFFER_END

CBUFFER_START(UnityPerDraw)
    float4x4 unity_ObjectToWorld;
    float4x4 unity_WorldToObject;
    float4x4 prevWorldToObject;
    float4 unity_LODFade;
    real4 unity_WorldTransformParams;
CBUFFER_END

float4 _ProjectionParams;
float4 unity_MotionVectorsParams;
float4x4 unity_MatrixVP;
float4x4 unjitteredVP;
float4x4 unity_MatrixV;
float4x4 glstate_matrix_projection;

float4x4 prevObjectToWorld;

#include "ARPInstancing.hlsl"
// #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"

//////////////////////////////////////////
// Built-in Lighting & Shadow Variables //
//////////////////////////////////////////

CBUFFER_START(MainLightData)
    DirectionalLight _MainLight;
CBUFFER_END

//////////////////////////////////////////
// Alpha Related                        //
//////////////////////////////////////////

static float _AlphaCutOff;

//////////////////////////////////////////
// Built-in Textures and Samplers       //
//////////////////////////////////////////

SAMPLER(sampler_linear_clamp);

TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);
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

TEXTURE2D(_PreintegratedDGFLut);
SAMPLER(sampler_PreintegratedDGFLut);

//////////////////////////////////////////
// Built-in Utility Functions           //
//////////////////////////////////////////

float4 VertexIDToPosCS(uint vertexID) {
    return float4(
        vertexID <= 1 ? -1.0 : 3.0,
        vertexID == 1 ? 3.0 : -1.0,
        0.0,
        1.0);
}

float2 VertexIDToScreenUV(uint vertexID) {
    return float2(
        vertexID <= 1 ? 0.0 : 2.0,
        vertexID ==1 ? 2.0 : 0.0);
}

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

    if (_ProjectionParams.x < 0.0) mv.y = -mv.y;

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

float LinearRoughnessToAlphaG2(float linearRoughness) {
    float roughness = linearRoughness * linearRoughness;
    return roughness * roughness;
}

float ClampMinLinearRoughness(float linearRoughness) {
    // return max(linearRoughness, 0.089f); // half precision float
    return max(linearRoughness, .045f); // Anti specular flickering
}

float3 GetF0(float3 albedo, float metallic) {
    float3 f0 = float3(.04f, .04f, .04f);
    // return lerp(f0, albedo.rgb, metallic);
    return f0 * (1.0f - metallic) + albedo * metallic;
}

float3 GetF0(float3 reflectance) {
    return .16 * (reflectance * reflectance);
}

float3 F_Schlick(in float3 f0, in float f90, in float u) {
    return f0 + (f90 - f0) * pow5(1.0f - u);
}

float3 F_Schlick(in float3 f0, in float u) {
    return f0 + (float3(1.0f, 1.0f, 1.0f) - f0) * pow5(1.0f - u);
}

float V_SmithGGX(float NdotL, float NdotV, float alphaG2) {
    const float lambdaV = NdotL * sqrt((-NdotV * alphaG2 + NdotV) * NdotV + alphaG2);
    const float lambdaL = NdotV * sqrt ((-NdotL * alphaG2 + NdotL) * NdotL + alphaG2);
    return .5f / (lambdaV + lambdaL);
}

float D_GGX(float NdotH, float alphaG2) {
    // Higher accuracy?
    const float f = (alphaG2 - 1.0f) * NdotH * NdotH + 1.0f;
    // const float f = (NdotH * alphaG2 - NdotH) * NdotH + 1;
    return alphaG2 / (f * f);
}

float DisneyDiffuseRenormalized(float NdotV, float NdotL, float LdotH, float linearRoughness) {
    float energyBias = lerp(.0f, .5f, linearRoughness);
    float energyFactor = lerp(1.0f, 1.0f / 1.51f, linearRoughness);
    float f90 = energyBias + 2.0f * LdotH * LdotH * linearRoughness;
    const float3 f0 = float3(1.0f, 1.0f, 1.0f);
    float lightScatter = F_Schlick(f0, f90, NdotL).r;
    float viewScatter = F_Schlick(f0, f90, NdotV).r;

    return lightScatter * viewScatter * energyFactor;
}

float CalculateFd(float NdotV, float NdotL, float LdotH, float linearRoughness) {
    float d = DisneyDiffuseRenormalized(NdotV, NdotL, LdotH, linearRoughness);
    return d / PI;
}

float3 CalculateFr(float NdotV, float NdotL, float NdotH, float LdotH, float roughness, float3 f0) {
    float alphaG2 = RoughnessToAlphaG2(roughness);
    float3 F = F_Schlick(f0, LdotH);
    float V = V_SmithGGX(NdotV, NdotL, alphaG2);
    float D = D_GGX(NdotH, alphaG2);
    return D * V * F / PI;
}

//////////////////////////////////////////
// Offline IBL Utility Functions        //
//////////////////////////////////////////

float3 CosineSampleHemisphere(float2 u) {
    float u1 = u.x;
    float u2 = u.y;
    float r = sqrt(u1);
    float theta = 2.0f * PI * u2;
 
    float x = r * cos(theta);
    float y = r * sin(theta);
 
    return float3(x, y, sqrt(max(.0f, 1.0f - u1)));
}

float3 ImportanceSampleGGX(float2 u, float3 N, float roughness) {

    float a = roughness * roughness;
	
    float phi = 2.0f * PI * u.x;
    float cosTheta = sqrt((1.0f - u.y) / (1.0f + (a * a - 1.0f) * u.y));
    float sinTheta = sqrt(1.0f - cosTheta * cosTheta);
	
    float3 H;
    H.x = cos(phi) * sinTheta;
    H.y = sin(phi) * sinTheta;
    H.z = cosTheta;
	
    float3 up = abs(N.z) < 0.999f ? float3(.0f, .0f, 1.0f) : float3(1.0f, .0f, .0f);
    float3 tangent = normalize(cross(up, N));
    float3 bitangent = cross(N, tangent);
	
    return tangent * H.x + bitangent * H.y + N * H.z;
}



float IBL_G_SmithGGX(float NdotL, float NdotV, float linearRoughness) {
    float alphaG2 = LinearRoughnessToAlphaG2(linearRoughness);
    const float lambdaV = NdotL * sqrt((-NdotV * alphaG2 + NdotV) * NdotV + alphaG2);
    const float lambdaL = NdotV * sqrt ((-NdotL * alphaG2 + NdotL) * NdotL + alphaG2);
    return (2 * NdotL) / (lambdaV + lambdaL);
    
}


float IBL_Diffuse(float NdotV, float NdotL, float LdotH, float linearRoughness) {
    float f90 = lerp(.0f, .5f, linearRoughness) + (2.0f * LdotH * LdotH * linearRoughness);
    const float3 f0 = float3(1.0f, 1.0f, 1.0f);
    return F_Schlick(f0, f90, NdotL).r * F_Schlick(f0, f90, NdotV).r * lerp(1.0f, (1.0f / 1.51f), linearRoughness);
}

float PrecomputeDiffuseL_DFG(float3 V, float NdotV, float linearRoughness) {
    // float3 V = float3(sqrt(1.0f - NdotV * NdotV), .0f, NdotV);
    float r = .0f;
    const uint SAMPLE_COUNT = 2048u;
    for (uint i = 0; i < SAMPLE_COUNT; i++) {
        // float2 E = Hammersley2dSeq(i, SAMPLE_COUNT);
        float2 E = Hammersley(i, SAMPLE_COUNT);
        float3 H = CosineSampleHemisphere(E);
        float3 L = 2.0f * dot(V, H) * H - V;

        float NdotL = saturate(L.z);
        float LdotH = saturate(dot(L, H));

        if (LdotH > .0f) {
            float diffuse = IBL_Diffuse(NdotV, NdotL, LdotH, linearRoughness);
            // float diffuse = DisneyDiffuseRenormalized(NdotV, NdotL, LdotH, linearRoughness);
            r += diffuse;
        }
    }
    return r / (float) SAMPLE_COUNT;
}

float2 PrecomputeSpecularL_DFG(float3 V, float NdotV, float linearRoughness) {
    float roughness = LinearRoughnessToRoughness(linearRoughness);
    // float3 V = float3(sqrt(1.0f - NdotV * NdotV), .0f, NdotV);
    float2 r = .0f;
    float3 N = float3(.0f, .0f, 1.0f);
    const uint SAMPLE_COUNT = 2048u;
    for (uint i = 0; i < SAMPLE_COUNT; i++) {
        // float2 Xi = Hammersley2dSeq(i, SAMPLE_COUNT);
        float2 Xi = Hammersley(i, SAMPLE_COUNT);
        float3 H = ImportanceSampleGGX(Xi, N, roughness);
        float3 L = 2.0f * dot(V, H) * H - V;

        float VdotH = saturate(dot(V, H));
        float NdotL = saturate(L.z);
        float NdotH = saturate(H.z);

        if (NdotL > .0f) {
            float G = IBL_G_SmithGGX(NdotL, NdotV, roughness);
            float Gv = G * VdotH / NdotH;
            float Fc = pow5(1.0f - VdotH);
            // r.x += Gv * (1.0f - Fc);
            r.x += Gv;
            r.y += Gv * Fc;
        }
    }

    return r / (float) SAMPLE_COUNT;
} 

float4 PrecomputeL_DFG(float NdotV, float linearRoughness) {
    float3 V = float3(sqrt(1.0f - NdotV * NdotV), .0f, NdotV);
    float4 color;
    color.xy = PrecomputeSpecularL_DFG(V, NdotV, linearRoughness);
    color.z = PrecomputeDiffuseL_DFG(V, NdotV, linearRoughness);
    color.w = 1.0f;
    return color;
}

//////////////////////////////////////////
// Runtime IBL Utility Functions        //
//////////////////////////////////////////

float4 CompensateDirectBRDF(float3 envGFD, inout float3 energyCompensation, float3 specularColor) {
    float3 reflectionGF = lerp(saturate(50.0f * specularColor.g) * envGFD.ggg, envGFD.rrr, specularColor);
    energyCompensation = 1.0f + specularColor * (1.0f / envGFD.r - 1.0f);
    return float4(reflectionGF, envGFD.b);
}

float4 GetDGFFromLut(inout float3 energyCompensation, float3 specularColor, float roughness, float NdotV) {
    float3 envGFD = SAMPLE_TEXTURE2D_LOD(_PreintegratedDGFLut, sampler_PreintegratedDGFLut, float2(NdotV, roughness), 0).rgb;
    return CompensateDirectBRDF(envGFD, energyCompensation, specularColor);
}

#endif