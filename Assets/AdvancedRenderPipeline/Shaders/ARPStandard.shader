Shader "Advanced Render Pipeline/ARPStandard" {
    
    Properties {
        [Enum(Dynamic, 1, Alpha, 2, Custom, 3)]
        _StencilRef("Stencil Ref", int) = 1
        _AlbedoTint("Albedo Tint", Color) = (1,1,1,1)
        _AlbedoMap("Albedo", 2D) = "white" { }
        _NormalScale("Normal Scale", float) = 1
        _NormalMap("Normal", 2D) = "bump" { }
        _SpecularMap("Specular", 2D) = "black" { }
        _SmoothnessScale("Smoothness Scale", float) = 1
        _SmoothnessMap("Smoothness", 2D) = "white" { }
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
                float2 baseUV : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct VertexOutput {
                float4 posCS : SV_POSITION;
                float3 normalWS : VAR_NORMAL;
                float3 viewDirWS : TEXCOORD1;
                float2 baseUV : VAR_BASE_UV;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct GBufferOutput {
                float4 forward : SV_TARGET0;
                float2 gbuffer1 : SV_TARGET1;
                float4 gbuffer2 : SV_TARGET2;
            };

            UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
                UNITY_DEFINE_INSTANCED_PROP(float4, _AlbedoTint)
                UNITY_DEFINE_INSTANCED_PROP(float4, _AlbedoMap_ST)
            UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

            VertexOutput StandardVertex(VertexInput input) {
                VertexOutput output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                
                output.posCS = TransformObjectToHClip(input.posOS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);

                output.viewDirWS = output.normalWS;
                
                float4 albedoST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _AlbedoMap_ST);
                output.baseUV = input.baseUV * albedoST.xy + albedoST.zw;
                return output;
            }

            GBufferOutput StandardFragment(VertexOutput input) {
                UNITY_SETUP_INSTANCE_ID(input);
                
                GBufferOutput output;

                float3 N = normalize(input.normalWS);
                float3 V = normalize(input.viewDirWS);
                float3 L = _MainLightDir;

                float NdotV;
                float NVCos = GetViewReflectedNormal(N, V, NdotV);
                float3 H = normalize(V + L);
                float LdotH = saturate(dot(L, H));
                float NdotH = saturate(dot(N, H));
                float NdotL = saturate(dot(N, L));

                float4 albedo = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _AlbedoTint);
                albedo *= SAMPLE_TEXTURE2D(_AlbedoMap, sampler_AlbedoMap, input.baseUV).rgba;
                
                output.forward = albedo;
                output.gbuffer1 = PackNormalOctQuadEncode(N);
                // output.gbuffer1 = normalWS;
                output.gbuffer2 = 0;
                return output;
            }

            ENDHLSL
        }
    }
}
