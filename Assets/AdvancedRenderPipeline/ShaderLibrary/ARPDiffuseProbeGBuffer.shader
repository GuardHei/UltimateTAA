Shader "Hidden/ARPDiffuseProbeGBuffer" {
    
    Properties {
        [Enum(UnityEngine.Rendering.CullMode)]
        _Cull("Cull", Float) = 0
        _AlbedoTint("Albedo Tint", Color) = (1,1,1,1)
        _AlbedoMap("Albedo", 2D) = "white" { }
        _NormalScale("Normal Scale", Range(0, 1)) = 1
        [NoScaleOffset]
        _NormalMap("Normal", 2D) = "bump" { }
        _HeightScale("Height Scale", Range(0, .3)) = 0
        _HeightMap("Height", 2D) = "white" { }
        _MetallicScale("Metallic Scale", Range(0, 1)) = 0
        _SmoothnessScale("Smoothness Scale", Range(0, 1)) = 1
        _MetallicSmoothnessMap("Metallic (RGB) Smoothness (A)", 2D) = "white" { }
        _OcclusionMap("Occlusion", 2D) = "white" { }
        [HDR]
        _EmissiveTint("Emissive Tint", Color) = (0, 0, 0, 1)
        _EmissiveMap("Emissive", 2D) = "black" { }
    }
    
    SubShader {

        Pass {
            
            Name "DiffuseProbeGBuffer"
            
            Tags {
                "LightMode" = "DiffuseProbeGBuffer"
            }
            
            ZTest LEqual
            ZWrite On
			Cull [_Cull]
            
            HLSLPROGRAM

            #pragma shader_feature_local _PARALLAX_MAP
            #pragma multi_compile_instancing
            #pragma vertex Vertex
            #pragma fragment Fragment

            #include "ARPSurface.hlsl"
            // #include "ARPCommon.hlsl"

            struct GBufferOutput {
                float4 gbuffer0 : SV_TARGET0;
                float2 gbuffer1 : SV_TARGET1;
                float gbuffer2 : SV_TARGET2;
            };

            /*
            struct BasicVertexInput {
                float3 posOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct BasicVertexOutput {
                float4 posCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            */

            /*
            BasicVertexOutput Vertex(BasicVertexInput input) {
                BasicVertexOutput output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                output.posCS = TransformObjectToHClip(input.posOS);
                return output;
            }

            void Fragment() { }
            */

            UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
                ARP_SURF_PER_MATERIAL_DATA
            UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)
            ARPSurfVertexOutput Vertex(ARPSurfVertexInput input) {
                ARPSurfVertexOutput output;
                
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float4 albedoST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _AlbedoMap_ST);
                
                ARPSurfVertexSetup(output, input, albedoST);

                // float4 posCS = output.posCS;
                // posCS.x = -posCS.x;
                //output.posCS = posCS;

                return output;
            }

            GBufferOutput Fragment(ARPSurfVertexOutput input) {
                UNITY_SETUP_INSTANCE_ID(input);

                GBufferOutput output;

                ARPSurfMatInputData matInput;

                ARP_SURF_MATERIAL_INPUT_SETUP(matInput);

                ARPSurfMatOutputData matData = (ARPSurfMatOutputData) 0;
                ARPSurfMaterialSetup(matData, input, matInput);

                // ARPSurfLightInputData lightData;
                // ARPSurfLightSetup(lightData, matData);

                // ARPSurfLightingData lightingData = (ARPSurfLightingData) 0;
                // ARPSurfLighting(lightingData, matData, lightData);

                float3 albedo = SAMPLE_TEXTURE2D(_AlbedoMap, sampler_AlbedoMap, input.baseUV).rgb;
                albedo *= matInput.albedoTint;
                float radialDepth = length(input.posWS - _CameraPosWS.xyz);

                output.gbuffer0 = float4(albedo, .0f);
                output.gbuffer1 = EncodeNormalComplex(matData.N);
                output.gbuffer2 = radialDepth;

                return output;
            }
            
            ENDHLSL
        }
    }
}