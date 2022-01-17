Shader "Advanced Render Pipeline/ARPStandard" {
    
    Properties {
        [Enum(Dynamic, 1, Alpha, 2, Custom, 3)]
        _StencilRef("Stencil Ref", int) = 1
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
        
        UsePass "Hidden/ARPDepth/MotionVectors"
        
        UsePass "Hidden/ARPDepth/DynamicDepth"
        
        UsePass "Hidden/ARPShadow/OpaqueShadowCaster"
        
        Pass {
            
            Name "StandardForward"
            
            Tags {
                "LightMode" = "OpaqueForward"
            }
            
            ZTest Equal
            ZWrite Off
			Cull [_Cull]
            
            HLSLPROGRAM

            #pragma shader_feature_local _PARALLAX_MAP
            #pragma multi_compile_instancing
            #pragma vertex StandardVertex
            #pragma fragment StandardFragment

            #include "../ShaderLibrary/ARPSurface.hlsl"

            UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
                UNITY_DEFINE_INSTANCED_PROP(float, _NormalScale)
                UNITY_DEFINE_INSTANCED_PROP(float, _HeightScale)
                UNITY_DEFINE_INSTANCED_PROP(float, _MetallicScale)
                UNITY_DEFINE_INSTANCED_PROP(float, _SmoothnessScale)
                UNITY_DEFINE_INSTANCED_PROP(float4, _AlbedoTint)
                UNITY_DEFINE_INSTANCED_PROP(float4, _EmissiveTint)
                UNITY_DEFINE_INSTANCED_PROP(float4, _AlbedoMap_ST)
            UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

            ARPSurfVertexOutput StandardVertex(ARPSurfVertexInput input) {
                ARPSurfVertexOutput output;
                
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float4 albedoST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _AlbedoMap_ST);
                
                ARPSurfVertexSetup(output, input, albedoST);

                return output;
            }

            ARPSurfGBufferOutput StandardFragment(ARPSurfVertexOutput input) {
                UNITY_SETUP_INSTANCE_ID(input);
                ARPSurfGBufferOutput output;

                ARPSurfMatInputData matInput;

                float3 albedoTint = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _AlbedoTint).rgb;
                float3 emissiveTint = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _EmissiveTint).rgb;
                float metallicScale = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _MetallicScale);
                float linearSmoothnessScale = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _SmoothnessScale);
                float heightScale = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _HeightScale);
                float normalScale = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _NormalScale);

                matInput.albedoTint = albedoTint;
                matInput.emissiveTint = emissiveTint;
                matInput.pack0 = float4(metallicScale, linearSmoothnessScale, heightScale, normalScale);

                ARPSurfMatOutputData matData;
                ARPSurfMaterialSetup(matData, input, matInput);

                ARPSurfLightInputData lightData;
                ARPSurfLightSetup(lightData, matData);

                ARPSurfLightingData lightingData;
                ARPSurfLighting(lightingData, matData, lightData);

                output.forward = lightingData.forwardLighting;
                output.gbuffer1 = EncodeNormalComplex(matData.N);
                output.gbuffer2 = float4(matData.f0, matData.pack0.y);
                output.gbuffer3 = lightingData.iblOcclusion;

                return output;
            }

            ENDHLSL
        }
    }
    
    CustomEditor "AdvancedRenderPipeline.Editor.ShaderGUIs.StandardShaderGUI"
}
