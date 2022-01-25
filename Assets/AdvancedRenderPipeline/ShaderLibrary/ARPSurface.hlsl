#ifndef ARP_SURFACE_INCLUDED
#define ARP_SURFACE_INCLUDED

#include "ARPCommon.hlsl"

#define ARP_SURF_PER_MATERIAL_DATA                   \
UNITY_DEFINE_INSTANCED_PROP(float, _NormalScale)     \
UNITY_DEFINE_INSTANCED_PROP(float, _HeightScale)     \
UNITY_DEFINE_INSTANCED_PROP(float, _MetallicScale)   \
UNITY_DEFINE_INSTANCED_PROP(float, _SmoothnessScale) \
UNITY_DEFINE_INSTANCED_PROP(float4, _AlbedoTint)     \
UNITY_DEFINE_INSTANCED_PROP(float4, _EmissiveTint)   \
UNITY_DEFINE_INSTANCED_PROP(float4, _AlbedoMap_ST)   \

#define ARP_SURF_MATERIAL_INPUT_SETUP(matInput)                                                \
float3 albedoTint = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _AlbedoTint).rgb;            \
float3 emissiveTint = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _EmissiveTint).rgb;        \
float metallicScale = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _MetallicScale);           \
float linearSmoothnessScale = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _SmoothnessScale); \
float heightScale = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _HeightScale);               \
float normalScale = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _NormalScale);               \
matInput.albedoTint = albedoTint;                                                              \
matInput.pack0 = float4(metallicScale, linearSmoothnessScale, heightScale, normalScale);       \
matInput.pack1 = float4(emissiveTint, 1.0f);                                                   \

struct ARPSurfVertexInput {
    float3 posOS : POSITION;
    float3 normalOS : NORMAL;
    float4 tangentOS : TANGENT;
    float2 baseUV : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct ARPSurfVertexOutput {
    float4 posCS : SV_POSITION;
    float3 posWS : VAR_POSITION;
    float3 normalWS : VAR_NORMAL;
    float4 tangentWS : VAR_TANGENT;
    float3 viewDirWS : TEXCOORD1;
    #if defined(_PARALLAX_MAP)
    float3 viewDirTS : TEXCOORD2;
    #endif
    float2 baseUV : VAR_BASE_UV;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct ARPSurfGBufferOutput {
    float4 forward : SV_TARGET0;
    float2 gbuffer1 : SV_TARGET1;
    float4 gbuffer2 : SV_TARGET2;
    float gbuffer3 : SV_TARGET3;
};

struct ARPSurfMatInputData {
    float3 albedoTint;
    float4 pack0; // x: metallic scale, y: linear smoothness scale, z: height scale, w: normal scale
    float4 pack1; // rgb: emissive tint, a: material shadow strength
};

struct ARPSurfMatOutputData {
    float3 vertexN;
    float3 N;
    float3 V;
    float3 R;
    float2 uv;
    float3 diffuse;
    float3 f0;
    float4 pack0; // x: metallic, y: linear roughness, z: occlusion, w: NdotV
    float4 pack1; // rgb: emissive, a: material shadow
};

struct ARPSurfLightInputData {
    float3 color;
    float3 L;
    float3 H;
    float3 pack0; // x: LdotH, y: NdotH, z: NdotL
};

struct ARPSurfLightingData {
    float3 directDiffuse;
    float3 directSpecular;
    float3 indirectDiffuse;
    float3 emissive;
    float iblOcclusion;
};

// need to manually setup instance id
void ARPSurfVertexSetup(inout ARPSurfVertexOutput output, ARPSurfVertexInput input, float4 texST) {
    float3 posWS = TransformObjectToWorld(input.posOS);
    output.posWS = posWS;
    output.posCS = TransformWorldToHClip(posWS);
    output.normalWS = TransformObjectToWorldNormal(input.normalOS);
    output.tangentWS = TransformObjectToWorldTangent(input.tangentOS);

    output.viewDirWS = normalize(_CameraPosWS.xyz - posWS);

    #if defined(_PARALLAX_MAP)
    float3x3 objectToTangent = float3x3(
        input.tangentOS.xyz,
        cross(input.normalOS, input.tangentOS.xyz) * input.tangentOS.w,
        input.normalOS);

    float3 viewDirOS = mul(GetWorldToObjectMatrix(), float4(_CameraPosWS.xyz, 1.0f)).xyz - input.posOS.xyz;
    output.viewDirTS = mul(objectToTangent, viewDirOS);
    #endif

    output.baseUV = input.baseUV * texST.xy + texST.zw;
}

// need to manually setup instance id
void ARPSurfMaterialSetup(out ARPSurfMatOutputData output, ARPSurfVertexOutput input, ARPSurfMatInputData matInput) {
    float2 uv = input.baseUV;
    
    float matShadow = 1.0f;
    
    #if defined(_PARALLAX_MAP)
    float noise = InterleavedGradientNoise(input.posCS.xy, _FrameParams.z);
    uv = ApplyParallax(uv, input.viewDirTS, matInput.pack0.z, noise);
    #endif
    
    float3 normalWS = normalize(input.normalWS);
    float3 normalData = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv), matInput.pack0.w);
                
    float3 N = ApplyNormalMap(normalData, normalWS, input.tangentWS);

    float3 V = input.viewDirWS;
    float NdotV;
    N = GetViewReflectedNormal(N, V, NdotV);
    float3 R = reflect(-V, N);

    float3 albedo = SAMPLE_TEXTURE2D(_AlbedoMap, sampler_AlbedoMap, uv).rgb;
    albedo *= matInput.albedoTint;

    float occlusion = SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, uv).r;
    // albedo *= occlusion;
    
    float4 metallicSmoothness = SAMPLE_TEXTURE2D(_MetallicSmoothnessMap, sampler_MetallicSmoothnessMap, uv);
    float linearSmoothness = metallicSmoothness.a;
    linearSmoothness *= matInput.pack0.y;
    float linearRoughness = LinearSmoothToLinearRoughness(linearSmoothness);
    linearRoughness = ClampMinLinearRoughness(linearRoughness);

    float metallic = metallicSmoothness.r;
    metallic *= matInput.pack0.x;

    float3 emissive = SAMPLE_TEXTURE2D(_EmissiveMap, sampler_EmissiveMap, uv).rgb;
    emissive += matInput.pack1.rgb;
    
    float3 diffuse = (1.0f - metallic) * albedo;
    float3 f0 = GetF0(albedo, metallic);
    
    output.vertexN = normalWS;
    output.N = N;
    output.V = V;
    output.R = R;
    output.uv = uv;
    output.diffuse = diffuse;
    output.f0 = f0;
    output.pack0 = float4(metallic, linearRoughness, occlusion, NdotV);
    output.pack1 = float4(emissive, matShadow);
}

void ARPSurfLightSetup(out ARPSurfLightInputData output, ARPSurfMatOutputData input) {
    float3 L = _MainLight.direction.xyz;
    float3 H = normalize(input.V + L);
    float LdotH = saturate(dot(L, H));
    float NdotH = saturate(dot(input.N, H));
    float NdotL = saturate(dot(input.N, L));

    output.color = _MainLight.color.rgb;
    output.L = L;
    output.H = H;
    output.pack0 = float3(LdotH, NdotH, NdotL);
}

void ARPSurfLighting(out ARPSurfLightingData output, ARPSurfMatOutputData mat, ARPSurfLightInputData light) {
    float metallic = mat.pack0.x;
    float linearRoughness = mat.pack0.y;
    float roughness = LinearRoughnessToRoughness(linearRoughness);
    float alphaG2 = RoughnessToAlphaG2(roughness);
    float occlusion = mat.pack0.z;
    float NdotV = mat.pack0.w;
    float matShadow = mat.pack1.a;
    float LdotH = light.pack0.x;
    float NdotH = light.pack0.y;
    float NdotL = light.pack0.z;
    
    float3 energyCompensation;
    // float lut = GetDFromLut(energyCompensation, mat.f0, roughness, NdotV);
    float4 lut = GetDGFFromLut(energyCompensation, mat.f0, roughness, NdotV);
    float3 envGF = lut.rgb;
    float envD = lut.a;

    float fd_s = CalculateFd(NdotV, NdotL, LdotH, linearRoughness);
    float fd = CalculateFdMultiScatter(NdotV, NdotL, NdotH, LdotH, alphaG2);
    float3 fr = CalculateFrMultiScatter(NdotV, NdotL, NdotH, LdotH, alphaG2, mat.f0, energyCompensation);
    // float3 fr_s = CalculateFr(NdotV, NdotL, NdotH, LdotH, alphaG2, mat.f0);

    float3 mainLighting = NdotL * _MainLight.color.rgb * matShadow;

    float3 diffuseLighting = mat.diffuse * fd * mainLighting;

    float3 specularLighting = fr * mainLighting;

    float3 kS = F_SchlickRoughness(mat.f0, NdotV, linearRoughness);
    float3 kD = 1.0f - kS;
    kD *=  1.0f - metallic;

    float iblOcclusion = ComputeHorizonSpecularOcclusion(mat.R, mat.vertexN);

    float3 indirectDiffuse = EvaluateDiffuseIBL(kD, mat.N, mat.diffuse, envD) * occlusion;
    

    output.directDiffuse = diffuseLighting;
    output.directSpecular = specularLighting;
    output.indirectDiffuse = indirectDiffuse;
    output.emissive = mat.pack1.rgb;
    output.iblOcclusion = iblOcclusion;
}

#endif