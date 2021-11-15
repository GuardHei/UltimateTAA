Shader "Advanced Render Pipeline/ARPStandard" {
    
    Properties {
        [Enum(Dynamic, 1, Alpha, 2, Custom, 3)]
        _StencilRef("Stencil Ref", int) = 1
        _AlbedoTint("Albedo Tint", Color) = (1,1,1,1)
        _AlbedoMap("Albedo", 2D) = "white" { }
        _NormalScale("Normal Scale", Range(0, 1)) = 1
        [NoScaleOffset]
        _NormalMap("Normal", 2D) = "bump" { }
        _MetallicScale("Metallic Scale", Range(0, 1)) = 0
        _SmoothnessScale("Smoothness Scale", Range(0, 1)) = 1
        _MetallicSmoothnessMap("Metallic (RGB) Smoothness (A)", 2D) = "white" { }
        // _SpecularMap("Specular", 2D) = "black" { }
        // _SmoothnessMap("Smoothness", 2D) = "white" { }
        _OcclusionMap("Occlusion", 2D) = "white" { }
        [HDR]
        _EmissionTint("Emission Tint", Color) = (0, 0, 0, 1)
        _EmissionMap("Emission", 2D) = "black" { }
    }
    
    SubShader {
        
        UsePass "Hidden/ARPDepthStencilMV/DefaultDynamicDepthStencil"
        
        UsePass "Hidden/ARPShadow/OpaqueShadowCaster"
        
        Pass {
            
            Tags {
                "LightMode" = "Forward"
            }
            
            ZTest Equal
            ZWrite Off
			Cull Back
            
            HLSLPROGRAM

            #pragma multi_compile_instancing
            #pragma vertex StandardVertex
            #pragma fragment StandardFragment

            #include "../ShaderLibrary/ARPCommon.hlsl"

            struct VertexInput {
                float3 posOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 baseUV : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct VertexOutput {
                float4 posCS : SV_POSITION;
                float3 normalWS : VAR_NORMAL;
                float4 tangentWS : VAR_TANGENT;
                float3 viewDirWS : TEXCOORD1;
                float2 baseUV : VAR_BASE_UV;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct GBufferOutput {
                float3 forward : SV_TARGET0;
                float2 gbuffer1 : SV_TARGET1;
                float4 gbuffer2 : SV_TARGET2;
            };

            UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
                UNITY_DEFINE_INSTANCED_PROP(float, _NormalScale)
                UNITY_DEFINE_INSTANCED_PROP(float, _MetallicScale)
                UNITY_DEFINE_INSTANCED_PROP(float, _SmoothnessScale)
                UNITY_DEFINE_INSTANCED_PROP(float4, _AlbedoTint)
                UNITY_DEFINE_INSTANCED_PROP(float4, _EmissionTint)
                UNITY_DEFINE_INSTANCED_PROP(float4, _AlbedoMap_ST)
                // UNITY_DEFINE_INSTANCED_PROP(float4, _NormalMap_ST)
                // UNITY_DEFINE_INSTANCED_PROP(float4, _MetallicSmoothnessMap_ST)
                // UNITY_DEFINE_INSTANCED_PROP(float4, _OcclusionMap_ST)
                // UNITY_DEFINE_INSTANCED_PROP(float4, _EmissionMap_ST)
            UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

            VertexOutput StandardVertex(VertexInput input) {
                VertexOutput output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                
                output.posCS = TransformObjectToHClip(input.posOS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.tangentWS = TransformObjectToWorldTangent(input.tangentOS);

                output.viewDirWS = output.normalWS;
                
                float4 albedoST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _AlbedoMap_ST);
                output.baseUV = input.baseUV * albedoST.xy + albedoST.zw;
                return output;
            }

            GBufferOutput StandardFragment(VertexOutput input) {
                UNITY_SETUP_INSTANCE_ID(input);
                
                GBufferOutput output;

                float3 normalWS = normalize(input.normalWS);
                float normalScale = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _NormalScale);
                float3 normalData = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, input.baseUV), normalScale);
                
                float3 N = ApplyNormalMap(normalData, normalWS, input.tangentWS);
                float3 V = normalize(input.viewDirWS);
                float3 L = _MainLightDir;

                float NdotV;
                float NVCos = GetViewReflectedNormal(N, V, NdotV);
                float3 H = normalize(V + L);
                float LdotH = saturate(dot(L, H));
                float NdotH = saturate(dot(N, H));
                float NdotL = saturate(dot(N, L));

                float3 albedo = SAMPLE_TEXTURE2D(_AlbedoMap, sampler_AlbedoMap, input.baseUV).rgb;
                albedo *= UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _AlbedoTint).rgb;

                float occlusion = SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, input.baseUV).r;
                albedo *= occlusion;

                float4 metallicSmoothness = SAMPLE_TEXTURE2D(_MetallicSmoothnessMap, sampler_MetallicSmoothnessMap, input.baseUV);
                
                float linearSmoothness = metallicSmoothness.a;
                linearSmoothness *= UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _SmoothnessScale);
                float linearRoughness = LinearSmoothToLinearRoughness(linearSmoothness);
                linearRoughness = ClampMinLinearRoughness(linearRoughness);
                float roughness = LinearRoughnessToRoughness(linearRoughness);

                float metallic = metallicSmoothness.r;
                metallic *= UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _MetallicScale);

                float3 emission = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, input.baseUV).rgb;
                emission *= UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _EmissionTint);

                float3 diffuse = (1.0 - metallic) * albedo;
                float3 f0 = GetF0(albedo, metallic);

                float fd = CalculateFd(NdotV, NdotL, LdotH, linearRoughness);
                float3 fr = CalculateFr(NdotV, NdotL, NdotH, LdotH, roughness, f0);

                float3 mainLighting = NdotL * _MainLightColor.rgb;

                diffuse *= fd * mainLighting;
                diffuse += emission;
                
                output.forward = float4(diffuse.r, diffuse.g, diffuse.b, 1.0) + fr * mainLighting;
                // diffuse = fd * NdotL * _MainLightColor.rgb;
                // output.forward = float4(diffuse, 1.0);
                output.gbuffer1 = PackNormalOctQuadEncode(N);
                output.gbuffer2 = float4(fr, roughness);
                return output;
            }

            ENDHLSL
        }
    }
}
