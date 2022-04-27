#ifndef ARP_SURFACE_INCLUDED
#define ARP_SURFACE_INCLUDED

#include "ARPCommon.hlsl"

#define ARP_SURF_PER_MATERIAL_DATA                            \
UNITY_DEFINE_INSTANCED_PROP(float, _NormalScale)              \
UNITY_DEFINE_INSTANCED_PROP(float, _HeightScale)              \
UNITY_DEFINE_INSTANCED_PROP(float, _MetallicScale)            \
UNITY_DEFINE_INSTANCED_PROP(float, _SmoothnessScale)          \
UNITY_DEFINE_INSTANCED_PROP(float4, _AlbedoTint)              \
UNITY_DEFINE_INSTANCED_PROP(float4, _EmissiveTint)            \
UNITY_DEFINE_INSTANCED_PROP(float4, _AlbedoMap_ST)            \

#define ARP_CLEAR_COAT_PER_MATERIAL_DATA                      \
UNITY_DEFINE_INSTANCED_PROP(float, _ClearCoatScale)           \
UNITY_DEFINE_INSTANCED_PROP(float, _ClearCoatSmoothnessScale) \

#define ARP_ANISOTROPY_PER_MATERIAL_DATA                      \
UNITY_DEFINE_INSTANCED_PROP(float, _AnisotropyScale)          \
UNITY_DEFINE_INSTANCED_PROP(float, _AnisotropyLevel)          \

#define ARP_FABRIC_PER_MATERIAL_DATA                           \
UNITY_DEFINE_INSTANCED_PROP(float3, _SheenTint)               \
UNITY_DEFINE_INSTANCED_PROP(float3, _SubsurfaceTint)          \

#define ARP_SURF_MATERIAL_INPUT_SETUP(matInput)                                          \
float3 albedoTint = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _AlbedoTint).rgb;      \
float3 emissiveTint = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _EmissiveTint).rgb;  \
float metallicScale = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _MetallicScale);     \
float smoothnessScale = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _SmoothnessScale); \
float normalScale = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _NormalScale);         \
float heightScale = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _HeightScale);         \
matInput.albedoTint = albedoTint;                                                        \
matInput.emissiveTint = emissiveTint;                                                    \
matInput.metallicScale = metallicScale;                                                  \
matInput.linearSmoothnessScale = smoothnessScale;                                        \
matInput.normalScale = normalScale;                                                      \
matInput.heightScale = heightScale;                                                      \

#define ARP_CLEAR_COAT_MATERIAL_INPUT_SETUP(matInput)                                                      \
float clearCoatScale = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _ClearCoatScale);                     \
float clearCoatSmoothnessScale = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _ClearCoatSmoothnessScale); \
matInput.clearCoatScale = clearCoatScale;                                                                  \
matInput.linearClearCoatSmoothnessScale = clearCoatSmoothnessScale;                                        \

#define ARP_ANISOTROPY_MATERIAL_INPUT_SETUP(matInput)                                    \
float anisotropyScale = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _AnisotropyScale); \
float anisotropyLevel = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _AnisotropyLevel); \
matInput.anisotropyScale = anisotropyScale;                                              \
matInput.anisotropyLevel = anisotropyLevel;                                              \

#define ARP_FABRIC_MATERIAL_INPUT_SETUP(matInput)                                        \
float3 sheenTint = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _SheenTint);      \
float3 subsurfaceTint = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _SubsurfaceTint); \
matInput.sheenTint = sheenTint;                                                        \
matInput.subsurfaceTint = subsurfaceTint;                                              \

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
    float2 baseUV : VAR_BASE_UV;
    float3 viewDirWS : TEXCOORD1;
    #if defined(_PARALLAX_MAP)
    float3 viewDirTS : TEXCOORD2;
    #endif
    #if defined(_ANISOTROPY)
    float3 bitangentWS : TEXCOORD3;
    #endif
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
    float3 emissiveTint;
    float metallicScale;
    float linearSmoothnessScale;
    float normalScale;
    float materialShadowStrength;
    float heightScale;
    #if defined(_CLEAR_COAT)
    float clearCoatScale;
    float linearClearCoatSmoothnessScale;
    #endif
    #if defined(_ANISOTROPY)
    float anisotropyScale;
    float anisotropyLevel;
    #endif
    #if defined(_FABRIC)
    float3 sheenTint;
    #endif
    #if defined(_HAS_SUBSURFACE_COLOR)
    float3 subsurfaceTint;
    #endif
};

struct ARPSurfMatOutputData {
    float3 vertexN;
    float3 N;
    float3 V;
    float3 R;
    float3 posWS;
    float NdotV;
    float2 uv;
    float3 diffuse;
    float3 f0;
    float3 emissive;
    float metallic;
    float linearRoughness;
    float occlusion;
    float materialShadow;
    #if defined(_CLEAR_COAT)
    float clearCoat;
    float linearClearCoatRoughness;
    #endif
    #if defined(_ANISOTROPY)
    float anisotropyScale;
    float3 anisotropicT;
    float3 anisotropicB;
    #endif
    #if defined(_FABRIC)
    float3 sheen;
    #endif
    #if defined(_HAS_SUBSURFACE_COLOR)
    float3 subsurfaceColor;
    #endif
};

struct ARPSurfLightInputData {
    float3 color;
    float3 shadowedColor;
    float3 lighting;
    float3 L;
    float3 H;
    float3 pack0; // x: LdotH, y: NdotH, z: NdotL
};

struct ARPSurfLightingData {
    float3 directDiffuseLobe;
    float3 directSpecularLobe;
    float3 indirectDiffuse;
    float3 emissive;
    float4 forwardLighting;
    float iblOcclusion;
};

// need to manually setup instance id
void ARPSurfVertexSetup(inout ARPSurfVertexOutput output, ARPSurfVertexInput input, float4 texST) {
    float3 posWS = TransformObjectToWorld(input.posOS);
    output.posWS = posWS;
    output.posCS = TransformWorldToHClip(posWS);
    output.normalWS = TransformObjectToWorldNormal(input.normalOS);
    output.tangentWS = TransformObjectToWorldTangent(input.tangentOS);

    #if defined(_ANISOTROPY)
    output.bitangentWS = cross(output.normalWS, output.tangentWS.xyz) * input.tangentOS.w;
    #endif

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
void ARPSurfMaterialSetup(inout ARPSurfMatOutputData output, ARPSurfVertexOutput input, ARPSurfMatInputData matInput) {
    float2 uv = input.baseUV;
    
    float matShadow = 1.0f;
    
    #if defined(_PARALLAX_MAP)
    float noise = InterleavedGradientNoise(input.posCS.xy - float2(.5, .5), _FrameParams.z);
    uv = ApplyParallax(uv, input.viewDirTS, matInput.heightScale, noise);
    #endif
    
    float3 normalWS = normalize(input.normalWS);
    float3 normalData = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv), matInput.normalScale);

    float4 T = float4(normalize(input.tangentWS.xyz), input.tangentWS.w);
    float3x3 tangentToWorld;
    float3 N = ApplyNormalMap(normalData, normalWS, T, tangentToWorld);

    float3 V = input.viewDirWS;

    float3 albedo = SAMPLE_TEXTURE2D(_AlbedoMap, sampler_AlbedoMap, uv).rgb;
    albedo *= matInput.albedoTint;

    float occlusion = SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, uv).r;
    // albedo *= occlusion;
    
    float4 metallicSmoothness = SAMPLE_TEXTURE2D(_MetallicSmoothnessMap, sampler_MetallicSmoothnessMap, uv);
    float linearSmoothness = metallicSmoothness.a;
    linearSmoothness *= matInput.linearSmoothnessScale;
    float linearRoughness = LinearSmoothnessToLinearRoughness(linearSmoothness);
    // linearRoughness = ClampMinLinearRoughness(linearRoughness); // Move down

    float metallic = metallicSmoothness.r;
    metallic *= matInput.metallicScale;

    float3 emissive = SAMPLE_TEXTURE2D(_EmissiveMap, sampler_EmissiveMap, uv).rgb;
    emissive += matInput.emissiveTint;

    float3 diffuse = (1.0f - metallic) * albedo;
    float3 f0 = GetF0(albedo, metallic);

    float NdotV;
    float3 R;

    #if defined(_CLEAR_COAT)
    float clearCoat = matInput.clearCoatScale;
    float2 clearCoatParams = SAMPLE_TEXTURE2D(_ClearCoatMap, sampler_ClearCoatMap, uv).rg;
    clearCoat *= clearCoatParams.r;
    float linearClearCoatRoughness = LinearSmoothnessToLinearRoughness(clearCoatParams.g * matInput.linearClearCoatSmoothnessScale);
    linearClearCoatRoughness = ClampMinLinearRoughness(linearClearCoatRoughness);
    linearRoughness = lerp(linearRoughness, max(linearRoughness, linearClearCoatRoughness), clearCoat);
    f0 = lerp(f0, F0ClearCoatToSurface(f0), clearCoat);

    output.clearCoat = clearCoat;
    output.linearClearCoatRoughness = linearClearCoatRoughness;
    #endif

    #if defined(_FABRIC)
    float3 sheen = SAMPLE_TEXTURE2D(_SheenMap, sampler_SheenMap, uv).rgb;
    sheen *= matInput.sheenTint;
    output.sheen = sheen;
    #endif

    #if defined(_HAS_SUBSURFACE_COLOR)
    float3 subsurfaceColor = SAMPLE_TEXTURE2D(_SubsurfaceMap, sampler_SubsurfaceMap, uv).rgb;
    subsurfaceColor *= matInput.subsurfaceTint;
    output.subsurfaceColor = subsurfaceColor;
    #endif

    #if defined(_ANISOTROPY)
    float anisotropy = matInput.anisotropyScale;
    float4 anisotropyParams = SAMPLE_TEXTURE2D(_AnisotropyMap, sampler_AnisotropyMap, uv);
    anisotropy *= anisotropyParams.a;
    // anisotropy *= anisotropyParams.a * 2.0f - 1.0f;

    float3 B = normalize(input.bitangentWS);

    float3 tangentData = UnpackNormalScale(SAMPLE_TEXTURE2D(_TangentMap, sampler_TangentMap, uv), matInput.normalScale);
    // float3 anisotropyRotation = anisotropyParams.r;
    T.xyz = normalize(tangentData.x * T.xyz + tangentData.y * B * anisotropy + tangentData.z * N);

    B = cross(N, T.xyz);
    
    float3 anisotropyDirection = (anisotropy >= .0f) ? B : T.xyz;
    float3 anisotropicT = cross(anisotropyDirection, V); 
    float3 anisotropicN = cross(anisotropicT, anisotropyDirection);
    // float3 anisotropicN = cross(T.xyz, B);
    float anisotropyFactor = abs(anisotropy) * saturate(matInput.anisotropyLevel * linearRoughness);
    anisotropicN = normalize(lerp(N, anisotropicN, anisotropyFactor));
    N = anisotropicN;

    N = GetViewReflectedNormal(anisotropicN, V, NdotV);
    R = reflect(-V, N);

    output.anisotropyScale = anisotropy;
    output.anisotropicT = T.xyz;
    output.anisotropicB = B;
    #else
    N = GetViewReflectedNormal(N, V, NdotV);
    R = reflect(-V, N);
    #endif

    linearRoughness = ClampMinLinearRoughness(linearRoughness);
    
    output.vertexN = normalWS;
    output.N = N;
    output.V = V;
    output.R = R;
    output.posWS = input.posWS;
    output.NdotV = NdotV;
    output.uv = uv;
    output.diffuse = diffuse;
    output.f0 = f0;
    output.emissive = emissive;
    output.metallic = metallic;
    output.linearRoughness = linearRoughness;
    output.occlusion = occlusion;
    output.materialShadow = matShadow;
}

void ARPSurfLightSetup(inout ARPSurfLightInputData output, ARPSurfMatOutputData input) {
    const float3 L = _MainLightDirection.xyz;
    const float3 H = normalize(input.V + L);
    const float LdotH = max(saturate(dot(L, H)), .0001f);
    const float NdotH = max(saturate(dot(input.N, H)), .0001f);
    const float NdotL = max(saturate(dot(input.N, L)), .0001f);
    const float3 shadowedColor = _MainLightColor.rgb * input.materialShadow;
    const float3 lighting = NdotL * shadowedColor;

    output.color = _MainLightColor.rgb;
    output.shadowedColor = shadowedColor;
    output.lighting = lighting;
    output.L = L;
    output.H = H;
    output.pack0 = float3(LdotH, NdotH, NdotL);
}

void ARPSurfLighting(inout ARPSurfLightingData output, inout ARPSurfMatOutputData mat, ARPSurfLightInputData light) {
    float metallic = mat.metallic;
    float linearRoughness = mat.linearRoughness;
    float roughness = LinearRoughnessToRoughness(linearRoughness);
    float alphaG2 = RoughnessToAlphaG2(roughness);
    float occlusion = mat.occlusion;
    float3 emissive = mat.emissive;
    float NdotV = mat.NdotV;
    float LdotH = light.pack0.x;
    float NdotH = light.pack0.y;
    float NdotL = light.pack0.z;
    
    float3 energyCompensation;
    float4 lut = GetDGFFromLut(energyCompensation, mat.f0, roughness, NdotV);
    float3 envGF = lut.rgb;
    float envD = lut.a;

    float3 fd;
    float3 fr;

    #if defined(_FABRIC)
    // fd = CalculateFdMultiScatter(NdotV, NdotL, NdotH, LdotH, alphaG2, mat.diffuse);
    fd = CalculateFdFabric(roughness, mat.diffuse);
    // fr = CalculateFrMultiScatter(NdotV, NdotL, NdotH, LdotH, alphaG2, mat.f0, energyCompensation);
    fr = CalculateFrFabric(NdotV, NdotL, NdotH, LdotH, roughness, mat.sheen);
    #else
    fd = CalculateFdMultiScatter(NdotV, NdotL, NdotH, LdotH, alphaG2, mat.diffuse);
    fr = CalculateFrMultiScatter(NdotV, NdotL, NdotH, LdotH, alphaG2, mat.f0, energyCompensation);
    #endif

    /*
    #if defined(_ANISOTROPY)
    
    float anisotropy = mat.anisotropyScale;
    float3 anisotropicT = mat.anisotropicT;
    float3 anisotropicB = mat.anisotropicB;

    float2 atb;
    GetAnisotropyTB(anisotropy, roughness, atb);
    float TdotV = max(saturate(dot(anisotropicT, mat.V)), .0001);
    float BdotV = max(saturate(dot(anisotropicB, mat.V)), .0001);
    float TdotL = max(saturate(dot(anisotropicT, light.L)), .0001);
    float BdotL = max(saturate(dot(anisotropicB, light.L)), .0001);
    float TdotH = max(saturate(dot(anisotropicT, light.H)), .0001);
    float BdotH = max(saturate(dot(anisotropicB, light.H)), .0001);

    fr = CalculateFrAnisotropicMultiscatter(NdotV, NdotL, NdotH, LdotH, TdotH, BdotH, atb, TdotV, BdotV, TdotL, BdotL, mat.f0, energyCompensation);
    #else
    fr = CalculateFrMultiScatter(NdotV, NdotL, NdotH, LdotH, alphaG2, mat.f0, energyCompensation);
    #endif
    */
    
    float iblOcclusion = ComputeHorizonSpecularOcclusion(mat.R, mat.vertexN);
    
    float3 kS = F_SchlickRoughness(mat.f0, NdotV, linearRoughness);
    float3 kD = 1.0f - kS;
    kD *=  1.0f - metallic;
    // float negE = 1.0f - envGF;
    
    float3 indirectDiffuse;

    if (_DiffuseProbeParams5.w == .0f) {
        indirectDiffuse = float3(.0f, .0f, .0f);
    } else if (_DiffuseProbeParams5.w == 1.0f) {
        indirectDiffuse = EvaluateDiffuseIBL(kD, mat.N, mat.diffuse, envD) * occlusion;
    } else {
        // indirectDiffuse = EvaluateDiffuseIBL(kD, mat.N, mat.diffuse, envD) * occlusion;
        indirectDiffuse = SampleIndirectDiffuseGI(mat.posWS, mat.N, mat.diffuse, kD, envD) * occlusion;
    }
    
    float4 forwardLighting = float4(.0f, .0f, .0f, 1.0f);
    
    // forwardLighting.rgb = (fd + fr) * light.lighting;
    // forwardLighting.rgb = fd + fr;

    #if defined(_CLEAR_COAT)

    float clearCoat = mat.clearCoat;
    float linearClearCoatRoughness = mat.linearClearCoatRoughness;
    float clearCoatRoughness = LinearRoughnessToRoughness(linearClearCoatRoughness);
    float clearCoatAlphaG2 = RoughnessToAlphaG2(clearCoatRoughness);

    float clearCoatNdotV = ClampNdotV(dot(mat.vertexN, mat.V));
    float clearCoatNdotH = saturate(dot(mat.vertexN, light.H));
    float clearCoatNdotL = saturate(dot(mat.vertexN, light.L));

    float3 clearCoatR = reflect(-mat.V, mat.vertexN);
    
    float fc;
    float frc = CalculateFrClearCoat(clearCoatNdotH, LdotH, clearCoatAlphaG2, clearCoat, fc);
    float baseLayerLoss = 1.0f - fc;

    forwardLighting.rgb = fd + fr;
    
    forwardLighting.rgb *= baseLayerLoss;
    forwardLighting.rgb *= light.lighting;
    forwardLighting.rgb += frc * clearCoatNdotL * light.color;

    float fc_i = F_Schlick(.04f, clearCoatNdotV).r * clearCoat;
    float baseLayerLoss_i = 1.0f - fc_i;

    indirectDiffuse *= baseLayerLoss_i;
    mat.f0 *= baseLayerLoss_i;

    float3 clearCoatSpecularIBL = SampleGlobalEnvMapSpecular(clearCoatR, LinearRoughnessToMipmapLevel(linearClearCoatRoughness, SPEC_IBL_MAX_MIP));

    forwardLighting.rgb += clearCoatSpecularIBL * fc_i;

    // indirectDiffuse = .0f;
    // forwardLighting.rgb = frc;

    #elif defined(_HAS_SUBSURFACE_COLOR)

    forwardLighting.rgb = fd * saturate((NdotL + .5f) / 2.25f) * saturate(mat.subsurfaceColor + float3(NdotL, NdotL, NdotL)) * light.shadowedColor;
    forwardLighting.rgb += fr * light.lighting;
    
    #else // Normal direct lighting calculation

    forwardLighting.rgb = (fd + fr) * light.lighting;
    
    #endif

    forwardLighting.rgb += emissive + indirectDiffuse;
    
    output.directDiffuseLobe = fd;
    output.directSpecularLobe = fr;
    output.indirectDiffuse = indirectDiffuse;
    output.emissive = emissive;
    output.forwardLighting = forwardLighting;
    output.iblOcclusion = iblOcclusion;
}

#endif