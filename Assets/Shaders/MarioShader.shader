Shader "Custom/MarioShader" {
    
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
        _IridescenceScale("Iridescence Scale", Range(0, 1)) = 1
        [NoScaleOffset]
        _IridescenceMap("Iridescence Map", 2D) = "black" { }
        [NoScaleOffset]
        _IridescenceMask("Iridescence Mask", 2D) = "white" { } 
        _SSSRange("SSS Range", Range(0, 9)) = 1.5
        _SSSScale("SSS Scale", Range(0, 4)) = 0
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

            #include "../AdvancedRenderPipeline/ShaderLibrary/ARPSurface.hlsl"

            TEXTURE2D(_IridescenceMap);
            SAMPLER(sampler_IridescenceMap);
            TEXTURE2D(_IridescenceMask);
            SAMPLER(sampler_IridescenceMask);

            UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
                ARP_SURF_PER_MATERIAL_DATA
                UNITY_DEFINE_INSTANCED_PROP(float, _IridescenceScale)
                UNITY_DEFINE_INSTANCED_PROP(float, _SSSScale)
                UNITY_DEFINE_INSTANCED_PROP(float, _SSSRange)
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

                float2 uvIridescence = normalize(mul(GetWorldToViewMatrix(), float4(matData.N, .0f))).xy * .5f + .5f;
                float3 iridescence = SAMPLE_TEXTURE2D(_IridescenceMap, sampler_IridescenceMap, uvIridescence).rgb;
                float iridescenceMask = SAMPLE_TEXTURE2D(_IridescenceMask, sampler_IridescenceMask, matData.uv).r;
                float iridescenceScale = iridescenceMask * UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _IridescenceScale);
                iridescence *= iridescenceScale;

                matData.diffuse += (1.0f - matData.pack0.r) * iridescence;
                matData.f0 += GetF0(iridescence, matData.pack0.r);
                matData.pack1.rgb *= matData.diffuse;

                ARPSurfLightInputData lightData;
                ARPSurfLightSetup(lightData, matData);

                ARPSurfLightingData lightingData;
                ARPSurfLighting(lightingData, matData, lightData);

                float sssRange = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _SSSRange);
                float sssScale = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _SSSScale);

                float sss = saturate(1.0f - dot(matData.V, matData.N));
                sss = max(pow(sss, sssRange) * 1.5f, .0f);
                sss *= sssScale;
                
                lightingData.emissive += sss;

                float3 finalLighting = lightingData.directDiffuse + lightingData.directSpecular + lightingData.indirectDiffuse + lightingData.emissive;

                output.forward = float4(finalLighting, 1.0f);
                // output.forward = float4(directDiffuse, 1.0f);
                // output.forward = sss;
                // output.forward = float4(matData.diffuse, 1.0f);
                // output.forward = float4(uvIridescence, .0f, 1.0f);
                // output.forward = iridescenceMask;
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
