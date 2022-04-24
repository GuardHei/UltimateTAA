Shader "Advanced Render Pipeline/ARPStandardDebug" {
    
    Properties {
        [Enum(AdvancedRenderPipeline.Runtime.DiffuseProbeDebugMode)]
        _DebugMode("Debug Mode", Int) = 2
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
                UNITY_DEFINE_INSTANCED_PROP(float, _DebugMode)
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

                float2 octUV = EncodeNormalComplex(matData.N);

                float3 debugInfo = lightingData.indirectDiffuse;

                if (IsDDGIEnabled()) {
                    if (IsInsideDDGIVolume(input.posWS, .5f)) {
                        int probeIndex = GetNearestProbeIndex1dFromPosWS(input.posWS);

                        float2 uvNoBorder = octUV;

                        int debugMode = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _DebugMode);

                        if (debugMode == 0) {
                            uvNoBorder = GetIrradianceMapUV(matData.N);
                            float4 info = SAMPLE_TEXTURE2D_ARRAY_LOD(_DiffuseProbeIrradianceArr, sampler_linear_clamp, uvNoBorder, probeIndex, 0);
                            debugInfo = info.rgb;
                        } else if (debugMode == 1) {
                            float3 energyCompensation;
                            float4 lut = GetDGFFromLut(energyCompensation, matData.f0, LinearRoughnessToRoughness(matData.linearRoughness), matData.NdotV);
                            float envD = lut.a;
                            float3 kS = F_SchlickRoughness(matData.f0, matData.NdotV, matData.linearRoughness);
                            float3 kD = 1.0f - kS;
                            kD *=  1.0f - matData.metallic;
                            uvNoBorder = GetIrradianceMapUV(matData.N);
                            float3 indirectDiffuse = SAMPLE_TEXTURE2D_ARRAY_LOD(_DiffuseProbeIrradianceArr, sampler_linear_clamp, uvNoBorder, probeIndex, 0).rgb;
                            debugInfo = indirectDiffuse * matData.diffuse * kD * (envD * matData.occlusion);
                        } else if (debugMode == 2) {
                            float4 info = SAMPLE_TEXTURE2D_ARRAY_LOD(_DiffuseProbeGBufferArr0, sampler_linear_clamp, uvNoBorder, probeIndex, 0);
                            debugInfo = info.rgb;
                        } else if (debugMode == 3) {
                            float4 info = SAMPLE_TEXTURE2D_ARRAY_LOD(_DiffuseProbeGBufferArr0, sampler_linear_clamp, uvNoBorder, probeIndex, 0);
                            debugInfo = info.a;
                        } else if (debugMode == 4) {
                            float2 info = SAMPLE_TEXTURE2D_ARRAY_LOD(_DiffuseProbeGBufferArr1, sampler_linear_clamp, uvNoBorder, probeIndex, 0).rg;
                            debugInfo.rg = info;
                            debugInfo.b = .0f;
                        } else if (debugMode == 5) {
                            float2 info = SAMPLE_TEXTURE2D_ARRAY_LOD(_DiffuseProbeGBufferArr1, sampler_linear_clamp, uvNoBorder, probeIndex, 0).rg;
                            debugInfo = DecodeNormalComplex(info);
                        } else if (debugMode == 6) {
                            float depth = SAMPLE_TEXTURE2D_ARRAY_LOD(_DiffuseProbeGBufferArr2, sampler_linear_clamp, uvNoBorder, probeIndex, 0).r;
                            debugInfo = float3(depth, depth, depth);
                        } else if (debugMode == 7) {
                            float depth = SAMPLE_TEXTURE2D_ARRAY_LOD(_DiffuseProbeGBufferArr2, sampler_linear_clamp, uvNoBorder, probeIndex, 0).r;
                            depth /= _DiffuseProbeParams0.w;
                            debugInfo = float3(depth, depth, depth);
                        } else if (debugMode == 8) {
                            uvNoBorder = GetVisibilityMapUV(matData.N);
                            float depth = SAMPLE_TEXTURE2D_ARRAY_LOD(_DiffuseProbeVBufferArr0, sampler_linear_clamp, uvNoBorder, probeIndex, 0).r;
                            debugInfo = float3(depth, depth, depth);
                        }  else if (debugMode == 9) {
                            uvNoBorder = GetVisibilityMapUV(matData.N);
                            float depth = SAMPLE_TEXTURE2D_ARRAY_LOD(_DiffuseProbeVBufferArr0, sampler_linear_clamp, uvNoBorder, probeIndex, 0).r;
                            depth /= GetMaxVisibilityDepth();
                            debugInfo = float3(depth, depth, depth);
                        } else if (debugMode == 10) {
                            uvNoBorder = GetVisibilityMapUV(matData.N);
                            float depth2 = SAMPLE_TEXTURE2D_ARRAY_LOD(_DiffuseProbeVBufferArr0, sampler_linear_clamp, uvNoBorder, probeIndex, 0).g;
                            debugInfo = float3(depth2, depth2, depth2);
                        }  else if (debugMode == 11) {
                            uvNoBorder = GetVisibilityMapUV(matData.N);
                            float depth2 = SAMPLE_TEXTURE2D_ARRAY_LOD(_DiffuseProbeVBufferArr0, sampler_linear_clamp, uvNoBorder, probeIndex, 0).g;
                            float maxDepth = GetMaxVisibilityDepth();
                            depth2 /= maxDepth * maxDepth;
                            debugInfo = float3(depth2, depth2, depth2);
                        } else if (debugMode == 12) {
                            float4 info = SAMPLE_TEXTURE2D_ARRAY_LOD(_DiffuseProbeRadianceArr, sampler_linear_clamp, uvNoBorder, probeIndex, 0);
                            debugInfo = info.rgb;
                        }
                    } else {
                        // set color to magenta to display error
                        debugInfo = float3(1.0f, .0f, 1.0f);
                    }
                }

                // output.forward = lightingData.forwardLighting;
                output.forward = float4(debugInfo, 1.0f);
                output.gbuffer1 = octUV;
                output.gbuffer2 = float4(float3(.0f, .0f, .0f), matData.linearRoughness);
                output.gbuffer3 = lightingData.iblOcclusion;

                return output;
            }

            ENDHLSL
        }
    }
    
    CustomEditor "AdvancedRenderPipeline.Editor.ShaderGUIs.StandardShaderGUI"
}
