#ifndef ARP_COMMON_INCLUDED
#define ARP_COMMON_INCLUDED

#define KILL_MICRO_MOVEMENT
#define MICRO_MOVEMENT_THRESHOLD (.01f * _ScreenSize.zw)

#define SPEC_IBL_MAX_MIP 6u
#define DIFF_IBL_MAX_MIP 11u

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonLighting.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Sampling/Hammersley.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Sampling/Sampling.hlsl"

#define UNITY_MATRIX_M unity_ObjectToWorld
#define UNITY_PREV_MATRIX_M prevObjectToWorld
#define UNITY_MATRIX_I_M unity_WorldToObject
#define UNITY_PREV_MATRIX_I_M prevWorldToObject
#define UNITY_MATRIX_V unity_MatrixV
#define UNITY_MATRIX_VP unity_MatrixVP
#define UNITY_MATRIX_I_VP unity_InvMatrixVP
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

CBUFFER_START(CameraData)
    float4 _CameraPosWS;
    float4 _CameraFwdWS;
    float4 _ScreenSize; // { w, h, 1 / w, 1 / h }
    float4x4 _FrustumCornersWS; // row 0: topLeft, row 1: bottomLeft, row 2: topRight, row 3: float4 _ZBufferParams { (f - n) / n, 1, (f - n) / n * f, 1 / f }
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
float4x4 unity_InvMatrixVP;

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
// Image Based Lighting Variables       //
//////////////////////////////////////////

float _GlobalEnvMapExposure;
float _GlobalEnvMapRotation;
float _SkyboxMipLevel;

//////////////////////////////////////////
// Built-in Textures and Samplers       //
//////////////////////////////////////////

SAMPLER(sampler_point_clamp);
SAMPLER(sampler_linear_clamp);

TEXTURE2D(_RawColorTex);
TEXTURE2D(_ColorTex);
TEXTURE2D(_TAAColorTex);
TEXTURE2D(_HdrColorTex);
TEXTURE2D(_DisplayTex);
TEXTURE2D(_DepthTex);
TEXTURE2D(_StencilTex);
TEXTURE2D(_VelocityTex);
TEXTURE2D(_GBuffer1);
TEXTURE2D(_GBuffer2);
TEXTURE2D(_ScreenSpaceCubemap);
TEXTURE2D(_ScreenSpaceReflection);
TEXTURE2D(_IndirectSpecular);

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
TEXTURE2D(_EmissiveMap);
SAMPLER(sampler_EmissiveMap);

TEXTURE2D(_PreintegratedDGFLut);
SAMPLER(sampler_PreintegratedDGFLut);
TEXTURE2D(_PreintegratedDLut);
SAMPLER(sampler_PreintegratedDLut);
TEXTURE2D(_PreintegratedGFLut);
SAMPLER(sampler_PreintegratedGFLut);

TEXTURECUBE(_GlobalEnvMapSpecular);
SAMPLER(sampler_GlobalEnvMapSpecular);
TEXTURECUBE(_GlobalEnvMapDiffuse);
SAMPLER(sampler_GlobalEnvMapDiffuse);

//////////////////////////////////////////
// Built-in Utility Functions           //
//////////////////////////////////////////

// Convert from Clip space (-1..1) to NDC 0..1 space.
// Note it doesn't mean we don't have negative value, we store negative or positive offset in NDC space.
// Note: ((positionCS * 0.5 + 0.5) - (previousPositionCS * 0.5 + 0.5)) = (motionVector * 0.5)
// PS: From Unity HDRP
float2 EncodeMotionVector(float2 mv) {
    return mv * .5f;
}

float2 DecodeMotionVector(float2 encoded) {
    return encoded * 2.0f;
}

// Convert Normal from [-1, 1] to [0, 1]
float3 EncodeNormal(float3 N) {
    return N * .5f + .5f;
}

// Convert Normal from [0, 1] to [-1, 1]
float3 DecodeNormal(float3 packed) {
    return packed * 2.0f - 1.0f;
}

float4 VertexIDToPosCS(uint vertexID) {
    return float4(
        vertexID <= 1 ? -1.0f : 3.0f,
        vertexID == 1 ? 3.0f : -1.0f,
        .0f,
        1.0f);
}

float2 VertexIDToScreenUV(uint vertexID) {
    return float2(
        vertexID <= 1 ? .0f : 2.0f,
        vertexID == 1 ? 2.0f : .0f);
}

float4 VertexIDToFrustumCorners(uint vertexID) {
    return _FrustumCornersWS[vertexID];
}

float4 GetZBufferParams() {
    return _FrustumCornersWS[3];
}

float SampleDepth(float2 uv) {
    return SAMPLE_DEPTH_TEXTURE(_DepthTex, sampler_point_clamp, uv);
}

float SampleCorrectedDepth(float2 uv) {
    float depth = SampleDepth(uv);
    #if defined(UNITY_REVERSED_Z)
    depth = 1.0f - depth;
    #endif
    return depth;
}

float3 SampleNormalWS(float2 uv) {
    return DecodeNormal(SAMPLE_TEXTURE2D(_GBuffer1, sampler_point_clamp, uv).rgb);
}

float4 DepthToWorldPosFast(float depth, float3 ray) {
    float3 worldPos = _CameraPosWS.xyz + Linear01Depth(depth, GetZBufferParams()) * ray;
    return float4(worldPos, 0.0f);
}

float4 DepthToWorldPos(float depth, float2 uv) {
    float4 ndc = float4(uv.x * 2.0f - 1.0f, uv.y * 2.0f - 1.0f, depth, 1.0f);
    float4 worldPosAccurate = mul(UNITY_MATRIX_I_VP, ndc);
    worldPosAccurate /= worldPosAccurate.w;
    return worldPosAccurate;
}

float4 TransformObjectToWorldTangent(float4 tangentOS) {
    return float4(TransformObjectToWorldDir(tangentOS.xyz), tangentOS.w);
}

float3 RotateAroundYInDegrees (float3 vertex, float degrees) {
    float alpha = degrees * PI / 180.0f;
    float sina, cosa;
    sincos(alpha, sina, cosa);
    float2x2 m = float2x2(cosa, -sina, sina, cosa);
    return float3(mul(m, vertex.xz), vertex.y).xzy;
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

//////////////////////////////////////////
// PBR Utility Functions                //
//////////////////////////////////////////

float pow5(float b) {
    float temp0 = b * b;
    float temp1 = temp0 * temp0;
    return temp1 * b;
}

float LinearSmoothToLinearRoughness(float linearSmooth) {
    return 1.0f - linearSmooth;
}

// Roughness = Alpha
float LinearRoughnessToRoughness(float linearRoughness) {
    return linearRoughness * linearRoughness;
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
    // return max(linearRoughness, .04f);
    // return max(linearRoughness, 0.089f); // half precision float
    // return max(linearRoughness, REAL_EPS);
    return max(linearRoughness, .045f); // Anti specular flickering
}

float ClampMinRoughness(float roughness) {
    // return max(roughness, 0.089f); // half precision float
    // return max(roughness, REAL_EPS);
    return max(roughness, .045f); // Anti specular flickering
}

// maxMipLevel: start from 0
float LinearRoughnessToMipmapLevel(float linearRoughness, uint maxMipLevel) {
    // return linearRoughness * maxMipLevel;
    linearRoughness = linearRoughness * (1.7f - .7f * linearRoughness);
    return linearRoughness * maxMipLevel;
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

float3 F_SchlickRoughness(float3 f0, float u, float linearRoughness) {
    float r = 1.0f - linearRoughness;
    // r = 1.0f;
    return f0 + (max(float3(r, r, r), f0) - f0) * pow5(1.0f - u);
}

float V_SmithGGX(float NdotL, float NdotV, float alphaG2) {
    const float lambdaV = NdotL * sqrt((-NdotV * alphaG2 + NdotV) * NdotV + alphaG2);
    const float lambdaL = NdotV * sqrt ((-NdotL * alphaG2 + NdotL) * NdotL + alphaG2);
    return .5f / (lambdaV + lambdaL);
}

// Requires caller to "div PI"
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

float DisneyDiffuseMultiScatter(float NdotV, float NdotL, float NdotH, float LdotH, float alphaG2) {
    float g = saturate(.18455f * log(2.0f / alphaG2 - 1.0f));
    float f0 = LdotH + pow5(1.0f - LdotH);
    float f1 = (1.0f - .75f * pow5(1.0f - NdotL)) * (1.0f - .75f * pow5(1.0f - NdotV));
    float t = saturate(2.2f * g - .5f);
    float fd = f0 + (f1 - f0) * t;
    float fb = ((34.5f * g - 59.0f) * g + 24.5f) * LdotH * exp2(-max(73.2f * g - 21.2f, 8.9f) * sqrt(NdotH));
    return max(fd + fb, .0f);
}

float CalculateFd(float NdotV, float NdotL, float LdotH, float linearRoughness) {
    float d = DisneyDiffuseRenormalized(NdotV, NdotL, LdotH, linearRoughness);
    return d / PI;
}

float CalculateFdMultiScatter(float NdotV, float NdotL, float NdotH, float LdotH, float linearRoughness) {
    float d = DisneyDiffuseMultiScatter(NdotV, NdotL, NdotH, LdotH, linearRoughness);
    return d / PI;
}

float3 CalculateFr(float NdotV, float NdotL, float NdotH, float LdotH, float roughness, float3 f0) {
    float alphaG2 = RoughnessToAlphaG2(roughness);
    float3 F = F_Schlick(f0, LdotH);
    float V = V_SmithGGX(NdotV, NdotL, alphaG2);
    float D = D_GGX(NdotH, alphaG2);
    return D * V * F / PI;
}

float3 CalculateFrMultiScatter(float NdotV, float NdotL, float NdotH, float LdotH, float roughness, float3 f0, float3 energyCompensation) {
    return CalculateFr(NdotV, NdotL, NdotH, LdotH, roughness, f0) * energyCompensation;
}

//////////////////////////////////////////
// Offline IBL Utility Functions        //
//////////////////////////////////////////

float3 ImportanceSampleGGX(float2 u, float3 N, float alphaG2) {

    // float alphaG2 = roughness * roughness;
	
    float phi = 2.0f * PI * u.x;
    float cosTheta = sqrt((1.0f - u.y) / (1.0f + (alphaG2 * alphaG2 - 1.0f) * u.y));
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

float IBL_G_SmithGGX(float NdotV, float NdotL, float alphaG2) {
    // float alphaG2 = LinearRoughnessToAlphaG2(linearRoughness);
    const float lambdaV = NdotL * sqrt((-NdotV * alphaG2 + NdotV) * NdotV + alphaG2);
    const float lambdaL = NdotV * sqrt ((-NdotL * alphaG2 + NdotL) * NdotL + alphaG2);
    return (2 * NdotL) / (lambdaV + lambdaL);
    // return .5f / (lambdaV + lambdaL);
}

float IBL_Diffuse(float NdotV, float NdotL, float LdotH, float linearRoughness) {
    return DisneyDiffuseRenormalized(NdotV, NdotL, LdotH, linearRoughness);
    /*
    float f90 = lerp(.0f, .5f, linearRoughness) + (2.0f * LdotH * LdotH * linearRoughness);
    const float3 f0 = float3(1.0f, 1.0f, 1.0f);
    return F_Schlick(f0, f90, NdotL).r * F_Schlick(f0, f90, NdotV).r * lerp(1.0f, (1.0f / 1.51f), linearRoughness);
    */
}

float PrecomputeDiffuseL_DFG(float3 V, float NdotV, float linearRoughness) {
    // float3 V = float3(sqrt(1.0f - NdotV * NdotV), .0f, NdotV);
    float r = .0f;
    const uint SAMPLE_COUNT = 2048u;
    for (uint i = 0; i < SAMPLE_COUNT; i++) {
        float2 E = Hammersley2dSeq(i, SAMPLE_COUNT);
        float3 H = SampleHemisphereCosine(E.x, E.y);
        float3 L = 2.0f * dot(V, H) * H - V;

        float NdotL = saturate(L.z);
        float LdotH = saturate(dot(L, H));

        if (LdotH > .0f) {
            float diffuse = IBL_Diffuse(NdotV, NdotL, LdotH, linearRoughness);
            r += diffuse;
        }
    }
    return r / (float) SAMPLE_COUNT;
}

float2 PrecomputeSpecularL_DFG(float3 V, float NdotV, float linearRoughness) {
    float roughness = LinearRoughnessToRoughness(linearRoughness);
    float alphaG2 = RoughnessToAlphaG2(roughness);
    // float3 V = float3(sqrt(1.0f - NdotV * NdotV), .0f, NdotV);
    float2 r = .0f;
    float3 N = float3(.0f, .0f, 1.0f);
    const uint SAMPLE_COUNT = 2048u;
    for (uint i = 0; i < SAMPLE_COUNT; i++) {
        float2 Xi = Hammersley2dSeq(i, SAMPLE_COUNT);
        float3 H = ImportanceSampleGGX(Xi, N, alphaG2);
        float3 L = 2.0f * dot(V, H) * H - V;

        float VdotH = saturate(dot(V, H));
        float NdotL = saturate(L.z);
        float NdotH = saturate(H.z);

        if (NdotL > .0f) {
            float G = IBL_G_SmithGGX(NdotV, NdotL, alphaG2);
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

float4 PrefilterEnvMap(TextureCube envMap, float resolution, float roughness, float3 reflectionDir) {
    float alphaG2 = RoughnessToAlphaG2(roughness);
    float3 N, R, V;
    N = R = V = reflectionDir;
    float3 prefiltered = float3(.0f, .0f, .0f);
    float totalWeight = .0f;
    const uint SAMPLE_COUNT = 2048u;
    for (uint i = 0; i < SAMPLE_COUNT; i++) {
        float2 Xi = Hammersley2dSeq(i, SAMPLE_COUNT);
        float3 H = ImportanceSampleGGX(Xi, N, alphaG2);
        float3 L = 2.0f * dot(V, H) * H - V;

        float NdotL = saturate(dot(N, L));
        
        if (NdotL > .0f) {
            float VdotH = saturate(dot(V, H));
            float NdotH = VdotH;
            // float NdotH = saturate(dot(N, H));

            float D = D_GGX(NdotH, alphaG2) / PI;
            float pdf = D * NdotH / (4.0f * VdotH) + .0001f;
            
            float omegasS = 1.0f / (float(SAMPLE_COUNT) * pdf);
            float omegaP = 4.0f * PI / (6.0f * resolution * resolution);
            float mipLevel = roughness == .0f ? .0f : .5f * log2(omegasS / omegaP);

            totalWeight += NdotL;
            prefiltered += envMap.SampleLevel(sampler_linear_clamp, L, mipLevel).rgb * NdotL;
        }
    }

    return float4(prefiltered / totalWeight, 1.0f);
}

//////////////////////////////////////////
// Runtime IBL Utility Functions        //
//////////////////////////////////////////

float3 SampleGlobalEnvMapDiffuse(float3 dir) {
    dir = RotateAroundYInDegrees(dir, _GlobalEnvMapRotation);
    return _GlobalEnvMapDiffuse.SampleLevel(sampler_GlobalEnvMapDiffuse, dir, DIFF_IBL_MAX_MIP).rgb * _GlobalEnvMapExposure;
}

float3 SampleGlobalEnvMapSpecular(float3 dir, float mipLevel) {
    dir = RotateAroundYInDegrees(dir, _GlobalEnvMapRotation);
    return _GlobalEnvMapSpecular.SampleLevel(sampler_GlobalEnvMapSpecular, dir, mipLevel).rgb * _GlobalEnvMapExposure;
}

float ComputeHorizonSpecularOcclusion(float3 R, float3 vertexNormal, float horizonFade) {
    const float horizon = saturate(1.0f + horizonFade * dot(R, vertexNormal));
    return horizon * horizon;
}

float ComputeHorizonSpecularOcclusion(float3 R, float3 vertexNormal) {
    const float horizon = saturate(1.0f + dot(R, vertexNormal));
    return horizon * horizon;
}

float3 EvaluateDiffuseIBL(float3 kD, float3 N, float3 albedo, float d) {
    // float3 indirectDiffuse = _GlobalEnvMapDiffuse.SampleLevel(sampler_GlobalEnvMapDiffuse, N, DIFF_IBL_MAX_MIP).rgb;
    float3 indirectDiffuse = SampleGlobalEnvMapDiffuse(N);
    indirectDiffuse *= albedo * kD * d;
    return indirectDiffuse;
}

float3 EvaluateSpecularIBL(float3 kS, float3 R, float linearRoughness, float3 GF, float3 energyCompensation) {
    // GFD = 1.0f;
    // float3 indirectSpecular = _GlobalEnvMapSpecular.SampleLevel(sampler_GlobalEnvMapSpecular, R, LinearRoughnessToMipmapLevel(linearRoughness, SPEC_IBL_MAX_MIP)).rgb;
    float3 indirectSpecular = SampleGlobalEnvMapSpecular(R, LinearRoughnessToMipmapLevel(linearRoughness, SPEC_IBL_MAX_MIP));
    indirectSpecular *= GF * kS * energyCompensation;
    return indirectSpecular;
}

float3 EvaluateIBL(float3 N, float3 R, float NdotV, float linearRoughness, float3 albedo, float3 f0, float4 lut, float3 energyCompensation) {
    float3 kS = F_SchlickRoughness(f0, NdotV, linearRoughness);
    float3 kD = 1.0f - kS;
    
    float3 indirectDiffuse = EvaluateDiffuseIBL(kD, N, albedo, lut.a);
    float3 indirectSpecular = EvaluateSpecularIBL(kS, R, linearRoughness, lut.rgb, energyCompensation);
    
    return indirectDiffuse + indirectSpecular;
}

float3 CompensateDirectBRDF(float2 envGF, inout float3 energyCompensation, float3 specularColor) {
    float3 reflectionGF = lerp(saturate(50.0f * specularColor.g) * envGF.ggg, envGF.rrr, specularColor);
    energyCompensation = 1.0f + specularColor * (1.0f / envGF.r - 1.0f);
    
    return reflectionGF;
}

float4 CompensateDirectBRDF(float3 envGFD, inout float3 energyCompensation, float3 specularColor) {
    float3 GF = CompensateDirectBRDF(envGFD.rg, energyCompensation, specularColor);
    return float4(GF, envGFD.b);
}

float4 GetDGFFromLut(inout float3 energyCompensation, float3 specularColor, float roughness, float NdotV) {
    float3 envGFD = SAMPLE_TEXTURE2D_LOD(_PreintegratedDGFLut, sampler_PreintegratedDGFLut, float2(NdotV, roughness), 0).rgb;
    return CompensateDirectBRDF(envGFD, energyCompensation, specularColor);
}

float GetDFromLut(inout float3 energyCompensation, float3 specularColor, float roughness, float NdotV) {
    float2 envGD = SAMPLE_TEXTURE2D_LOD(_PreintegratedDLut, sampler_PreintegratedDLut, float2(NdotV, roughness), 0).rg;
    energyCompensation = 1.0f + specularColor * (1.0f / envGD.r - 1.0f);
    return envGD.g;
}

float3 GetGFFromLut(inout float3 energyCompensation, float3 specularColor, float roughness, float NdotV) {
    float2 envGF = SAMPLE_TEXTURE2D_LOD(_PreintegratedGFLut, sampler_PreintegratedGFLut, float2(NdotV, roughness), 0).rg;
    return CompensateDirectBRDF(envGF, energyCompensation, specularColor);
}

#endif