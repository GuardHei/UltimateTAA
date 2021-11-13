#ifndef ARP_COMMON_INCLUDED
#define ARP_COMMON_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

#define UNITY_MATRIX_M unity_ObjectToWorld
#define UNITY_PREV_MATRIX_M prevObjectToWorld
#define UNITY_MATRIX_I_M unity_WorldToObject
#define UNITY_PREV_MATRIX_I_M prevWorldToObject
#define UNITY_MATRIX_V unity_MatrixV
#define UNITY_MATRIX_VP unity_MatrixVP
#define UNITY_MATRIX_UNJITTERED_VP nonJitteredVP
#define UNITY_MATRIX_P glstate_matrix_projection

CBUFFER_START(UnityPerDraw)
    float4x4 unity_ObjectToWorld;
    float4x4 unity_WorldToObject;
    float4x4 prevObjectToWorld;
    float4x4 prevWorldToObject;
    float4 unity_LODFade;
    real4 unity_WorldTransformParams;
CBUFFER_END

float4 unity_MotionVectorsParams;
float4x4 unity_MatrixVP;
float4x4 nonJitteredVP;
float4x4 unity_MatrixV;
float4x4 glstate_matrix_projection;

#include "ARPInstancing.hlsl"
// #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"

//////////////////////////////////////////
// Built-in Lighting & Shadow Variables //
//////////////////////////////////////////



//////////////////////////////////////////
// Alpha Related                        //
//////////////////////////////////////////

static float _AlphaCutOff;

//////////////////////////////////////////
// Built-in Textures and Samplers       //
//////////////////////////////////////////

TEXTURE2D(_AlbedoMap);
SAMPLER(sampler_AlbedoMap);

#endif