#ifndef ARP_COMMON_INCLUDED
#define ARP_COMMON_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

#define UNITY_MATRIX_M unity_ObjectToWorld
#define UNITY_PREV_MATRIX_M prevObjectToWorld
#define UNITY_MATRIX_I_M unity_WorldToObject
#define UNITY_PREV_MATRIX_I_M prevWorldToObject
#define UNITY_MATRIX_V unity_MatrixV
#define UNITY_MATRIX_VP unity_MatrixVP
#define UNITY_MATRIX_P glstate_matrix_projection

float4x4 unity_ObjectToWorld;
float4x4 unity_WorldToObject;
float4x4 prevObjectToWorld;
float4x4 prevWorldToObject;

float4x4 unity_MatrixVP;
float4x4 unity_MatrixV;
float4x4 glstate_matrix_projection;
real4 unity_WorldTransformParams;

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"

float4 UnlitVertex(float3 posOS : POSITION) : SV_POSITION {
    float3 posWS = TransformObjectToWorld(posOS);
    return TransformObjectToHClip(posWS);
}

float4 UnlitFragment() : SV_TARGET {
    return 0.0;
}

#endif