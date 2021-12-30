Shader "Hidden/ARPShadow" {
    
    Properties {
        _Cull("Cull", Float) = 0
        _AlbedoTint("Albedo Tint", Color) = (1,1,1,1)
        _AlbedoMap("Albedo", 2D) = "white" { }
    }
    
    SubShader {
        
        Pass {
            
            Name "OpaqueShadowCaster"
            
            Tags {
                "LightMode" = "ShadowCaster"
            }
            
            Cull [_Cull]
            
            HLSLPROGRAM

            #pragma multi_compile_instancing
            #pragma vertex ShadowVertex
            #pragma fragment ShadowFragment
            
            #include "ARPCommon.hlsl"

            struct BasicVertexInput {
                float3 posOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct BasicVertexOutput {
                float4 posCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            BasicVertexOutput ShadowVertex(BasicVertexInput input) {
                BasicVertexOutput output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                output.posCS = TransformObjectToHClip(input.posOS);
                #if UNITY_REVERSED_Z
                    output.posCS.z = min(output.posCS.z, output.posCS.w * UNITY_NEAR_CLIP_VALUE);
                #else
                    output.posCS.z = max(output.posCS.z, output.posCS.w * UNITY_NEAR_CLIP_VALUE);
                #endif
                return output;
            }

            void ShadowFragment(BasicVertexOutput input) {
                UNITY_SETUP_INSTANCE_ID(input);
            }
            
            ENDHLSL
        }
        
        Pass {
            
            Name "AlphaTestShadowCaster"
            
            Tags {
                "LightMode" = "ShadowCaster"
            }
            
            Cull [_Cull]
            
            HLSLPROGRAM

            #pragma multi_compile_instancing
            #pragma vertex ShadowVertex
            #pragma fragment ShadowFragment
            
            #include "ARPCommon.hlsl"

            struct AlphaTestShadowVertexInput {
                float3 posOS : POSITION;
                float2 baseUV : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct AlphaTestShadowVertexOutput {
                float4 posCS : SV_POSITION;
                float2 baseUV : VAR_BASE_UV;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
                UNITY_DEFINE_INSTANCED_PROP(float4, _AlbedoTint)
                UNITY_DEFINE_INSTANCED_PROP(float4, _AlbedoMap_ST)
            UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

            AlphaTestShadowVertexOutput ShadowVertex(AlphaTestShadowVertexInput input) {
                AlphaTestShadowVertexOutput output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                output.posCS = TransformObjectToHClip(input.posOS);

                #if UNITY_REVERSED_Z
                    output.posCS.z = min(output.posCS.z, output.posCS.w * UNITY_NEAR_CLIP_VALUE);
                #else
                    output.posCS.z = max(output.posCS.z, output.posCS.w * UNITY_NEAR_CLIP_VALUE);
                #endif

                float4 albedoST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _AlbedoMap_ST);
                output.baseUV = input.baseUV * albedoST.xy + albedoST.zw;
                
                return output;
            }

            void ShadowFragment(AlphaTestShadowVertexOutput input) {
                UNITY_SETUP_INSTANCE_ID(input);
                float alpha = SAMPLE_TEXTURE2D(_AlbedoMap, sampler_AlbedoMap, input.baseUV).a;
                alpha *= UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _AlbedoTint).a;
                clip(alpha - _AlphaCutOff);
            }
            
            ENDHLSL
        }
    }
}