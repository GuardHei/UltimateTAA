Shader "Advanced Render Pipeline/ARPStandardDebug" {
    
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
        [NoScaleOffset]
        _HeightMap("Height", 2D) = "white" { }
        _MetallicScale("Metallic Scale", Range(0, 1)) = 0
        _SmoothnessScale("Smoothness Scale", Range(0, 1)) = 1
        [NoScaleOffset]
        _MetallicSmoothnessMap("Metallic (RGB) Smoothness (A)", 2D) = "white" { }
        [NoScaleOffset]
        _OcclusionMap("Occlusion", 2D) = "white" { }
        [HDR]
        _EmissiveTint("Emissive Tint", Color) = (0, 0, 0, 1)
        [NoScaleOffset]
        _EmissiveMap("Emissive", 2D) = "black" { }
    }
    
    SubShader {
        
        UsePass "Hidden/ARPDepth/StaticDepth"
        
        Pass {
            
            Name "StandardForward"
            
            Tags {
                "LightMode" = "OpaqueForward"
            }
            
            ZTest LEqual
            ZWrite On
			Cull [_Cull]
            
            HLSLPROGRAM

            #pragma shader_feature_local _PARALLAX_MAP
            #pragma multi_compile_instancing
            #pragma vertex StandardVertex
            #pragma fragment StandardFragment

            #include "../ShaderLibrary/ARPSurface.hlsl"

            UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
                ARP_SURF_PER_MATERIAL_DATA
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

                ARP_SURF_MATERIAL_INPUT_SETUP(matInput);

                ARPSurfMatOutputData matData = (ARPSurfMatOutputData) 0;
                ARPSurfMaterialSetup(matData, input, matInput);

                ARPSurfLightInputData lightData;
                ARPSurfLightSetup(lightData, matData);

                ARPSurfLightingData lightingData = (ARPSurfLightingData) 0;
                ARPSurfLighting(lightingData, matData, lightData);

                // output.forward = lightingData.forwardLighting;
                output.forward = float4(lightingData.indirectDiffuse, 1.0f);
                output.gbuffer1 = EncodeNormalComplex(matData.N);
                output.gbuffer2 = float4(float3(.0f, .0f, .0f), matData.linearRoughness);
                output.gbuffer3 = lightingData.iblOcclusion;

                return output;
            }

            ENDHLSL
        }
    }
    
    CustomEditor "AdvancedRenderPipeline.Editor.ShaderGUIs.StandardShaderGUI"
}
