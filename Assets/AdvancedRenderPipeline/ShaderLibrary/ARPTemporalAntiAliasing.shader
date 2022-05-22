Shader "Hidden/ARPTemporalAntiAliasing" {
    
    Properties {
        _MainTex("Texture", 2D) = "white" { }
    }
    
    SubShader {
        
        Pass {
            
            Name "TemporalAntiAliasing"
            
            Cull Off
            ZWrite Off
            ZTest Always
            
            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Fragment

            #include "ARPCommon.hlsl"
            #include "ARPAntiAliasing.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

            struct VertexOutput {
                float4 posCS : SV_POSITION;
                float2 screenUV : VAR_SCREEN_UV;
            };

            float _EnableReprojection;
            // { minHistoryWeight, maxHistoryWeight, minClipScale, maxClipScale }
            // { minVelocityRejection, velocityRejectionScale, minDepthRejection, resamplingSharpness }
            // { minSharpness, maxSharpness, motionSharpeningFactor, staticClipScale }
            // { minEdgeBlurriness, invalidHistoryThreshold }
            float4x4 _TaaParams;

            VertexOutput Vert(uint vertexID : SV_VertexID) {
                VertexOutput output;
                output.posCS = VertexIDToPosCS(vertexID);
                output.screenUV = VertexIDToScreenUV(vertexID);
                return output;
            }

            float4 Fragment(VertexOutput input) : SV_TARGET {
                float2 uv = input.screenUV;
                if (_ProjectionParams.x < .0f) uv.y = 1.0f - uv.y;

                float4 curr = SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_linear_clamp, uv, 0);
                curr = FastTonemap(curr);
                float4 output;

                if (_EnableReprojection < .0f) {
                    output = curr;
                    output.a = 1.0f;
                    return output;
                }

                int2 coord = int2(floor(uv.x * _ScreenSize.x), floor(uv.y * _ScreenSize.y));

                int2 offset1 = int2(1, 1); // top right
                int2 offset2 = int2(1, -1); // bottom right
                int2 offset3 = int2(-1, 1); // top left
                int2 offset4 = int2(-1, -1); // bottom left
                int2 offset5 = int2(0, 1); // top middle
                int2 offset6 = int2(1, 0); // middle right
                int2 offset7 = int2(0, -1); // bottom middle
                int2 offset8 = int2(-1, 0); // middle left

                int2 coord1 = coord + offset1;
                int2 coord2 = coord + offset2;
                int2 coord3 = coord + offset3;
                int2 coord4 = coord + offset4;
                
                float d0 = _DepthTex.SampleLevel(sampler_point_clamp, uv, 0).r;
                float d1 = _DepthTex.SampleLevel(sampler_point_clamp, uv, 0, offset1).r;
                float d2 = _DepthTex.SampleLevel(sampler_point_clamp, uv, 0, offset2).r;
                float d3 = _DepthTex.SampleLevel(sampler_point_clamp, uv, 0, offset3).r;
                float d4 = _DepthTex.SampleLevel(sampler_point_clamp, uv, 0, offset4).r;

                float linearDepth0 = DepthToLinearEyeSpace(d0);
                float linearDepth1 = DepthToLinearEyeSpace(d1);
                float linearDepth2 = DepthToLinearEyeSpace(d2);
                float linearDepth3 = DepthToLinearEyeSpace(d3);
                float linearDepth4 = DepthToLinearEyeSpace(d4);

                // velocity dilation
                float closetDepth = d0;
                float closetLinearDepth = linearDepth0;
                int2 closetOffset = int2(0, 0);
                if (CLOSER_DEPTH(closetDepth, d1)) {
                    closetDepth = d1;
                    closetLinearDepth = linearDepth1;
                    closetOffset = offset1;
                }
                if (CLOSER_DEPTH(closetDepth, d2)) {
                    closetDepth = d2;
                    closetLinearDepth = linearDepth2;
                    closetOffset = offset2;
                }
                if (CLOSER_DEPTH(closetDepth, d3)) {
                    closetDepth = d3;
                    closetLinearDepth = linearDepth3;
                    closetOffset = offset3;
                }
                if (CLOSER_DEPTH(closetDepth, d4)) {
                    closetDepth = d4;
                    closetLinearDepth = linearDepth4;
                    closetOffset = offset4;
                }
                
                float2 mv = LOAD_TEXTURE2D(_VelocityTex, coord + closetOffset).rg; // if velocity is outside the screen, we want to set it to 0 instead of clamping.
                float2 prevUV = uv - mv;

                float3 c1 = FastTonemap(_MainTex.SampleLevel(sampler_point_clamp, uv, 0, offset1)).rgb;
                float3 c2 = FastTonemap(_MainTex.SampleLevel(sampler_point_clamp, uv, 0, offset2)).rgb;
                float3 c3 = FastTonemap(_MainTex.SampleLevel(sampler_point_clamp, uv, 0, offset3)).rgb;
                float3 c4 = FastTonemap(_MainTex.SampleLevel(sampler_point_clamp, uv, 0, offset4)).rgb;
                float3 c5 = FastTonemap(_MainTex.SampleLevel(sampler_point_clamp, uv, 0, offset5)).rgb;
                float3 c6 = FastTonemap(_MainTex.SampleLevel(sampler_point_clamp, uv, 0, offset6)).rgb;
                float3 c7 = FastTonemap(_MainTex.SampleLevel(sampler_point_clamp, uv, 0, offset7)).rgb;
                float3 c8 = FastTonemap(_MainTex.SampleLevel(sampler_point_clamp, uv, 0, offset8)).rgb;

                float3 n0 = RGBToYCoCg(curr.rgb);
                float3 n1 = RGBToYCoCg(c1);
                float3 n2 = RGBToYCoCg(c2);
                float3 n3 = RGBToYCoCg(c3);
                float3 n4 = RGBToYCoCg(c4);
                float3 n5 = RGBToYCoCg(c5);
                float3 n6 = RGBToYCoCg(c6);
                float3 n7 = RGBToYCoCg(c7);
                float3 n8 = RGBToYCoCg(c8);

                float3 m1 = n0;
                float3 m2 = n0 * n0;

                m1 += n1;
                m1 += n2;
                m1 += n3;
                m1 += n4;
                m1 += n5;
                m1 += n6;
                m1 += n7;
                m1 += n8;
                m2 += n1 * n1;
                m2 += n2 * n2;
                m2 += n3 * n3;
                m2 += n4 * n4;
                m2 += n5 * n5;
                m2 += n6 * n6;
                m2 += n7 * n7;
                m2 += n8 * n8;

                float minHistoryWeight = _TaaParams[0].x;
                float maxHistoryWeight = _TaaParams[0].y;
                float minClipScale = _TaaParams[0].z;
                float maxClipScale = _TaaParams[0].w;
                float minVelocityRejection = _TaaParams[1].x;
                float velocityRejectionScale = _TaaParams[1].y;
                float minDepthRejection = _TaaParams[1].z;
                float resamplingSharpness = _TaaParams[1].w;
                float minSharpness = _TaaParams[2].x;
                float maxSharpness = _TaaParams[2].y;
                float motionSharpeningFactor = _TaaParams[2].z;
                float staticClipScale = _TaaParams[2].w;
                float minEdgeBlurriness = _TaaParams[3].x;
                float invalidHistoryThreshold = _TaaParams[3].y;

                int2 prevCoord = int2(floor(prevUV.x * _ScreenSize.x), floor(prevUV.y * _ScreenSize.y));

                float4 prev;

                // if sharpness == .0f, we use bilinear resampling, otherwise we perform the 1-sample bicubic resampling
                if (resamplingSharpness == .0f) prev = SAMPLE_TEXTURE2D_LOD(_PrevTaaColorTex, sampler_linear_clamp, prevUV, 0);
                else {
                    prev = SAMPLE_TEXTURE2D_LOD(_PrevTaaColorTex, sampler_point_clamp, prevUV, 0);
                    prev.rgb = OneTapBicubicFilter(curr, c5, c6, c7, c8, prev.rgb, prevUV, resamplingSharpness);
                }

                // velocity weighting
                float prevD0 = _PrevDepthTex.SampleLevel(sampler_point_clamp, prevUV, 0).r;
                float prevD1 = _PrevDepthTex.SampleLevel(sampler_point_clamp, prevUV, 0, offset1).r;
                float prevD2 = _PrevDepthTex.SampleLevel(sampler_point_clamp, prevUV, 0, offset2).r;
                float prevD3 = _PrevDepthTex.SampleLevel(sampler_point_clamp, prevUV, 0, offset3).r;
                float prevD4 = _PrevDepthTex.SampleLevel(sampler_point_clamp, prevUV, 0, offset4).r;

                float prevClosetDepth = prevD0;
                int2 prevClosetOffset = int2(0, 0);
                if (CLOSER_DEPTH(prevClosetDepth, prevD1)) {
                    prevClosetDepth = prevD1;
                    prevClosetOffset = offset1;
                }
                if (CLOSER_DEPTH(prevClosetDepth, prevD2)) {
                    prevClosetDepth = prevD2;
                    prevClosetOffset = offset2;
                }
                if (CLOSER_DEPTH(prevClosetDepth, prevD3)) {
                    prevClosetDepth = prevD3;
                    prevClosetOffset = offset3;
                }
                if (CLOSER_DEPTH(prevClosetDepth, prevD4)) {
                    prevClosetDepth = prevD4;
                    prevClosetOffset = offset4;
                }

                float2 prevMV = LOAD_TEXTURE2D(_PrevVelocityTex, prevCoord + prevClosetOffset).rg;
                float prevMVScale = length(prevMV);

                float mvScale = length(mv);

                float velocityWeight = saturate((length(prevMV - mv) - minVelocityRejection) * velocityRejectionScale);

                // return velocityWeight;

                // stencil test
                uint st0 = LOAD_TEXTURE2D(_StencilTex, coord).STENCIL_CHANNEL & 3;
                uint st1 = LOAD_TEXTURE2D(_StencilTex, coord1).STENCIL_CHANNEL & 3;
                uint st2 = LOAD_TEXTURE2D(_StencilTex, coord2).STENCIL_CHANNEL & 3;
                uint st3 = LOAD_TEXTURE2D(_StencilTex, coord3).STENCIL_CHANNEL & 3;
                uint st4 = LOAD_TEXTURE2D(_StencilTex, coord4).STENCIL_CHANNEL & 3;
                uint prevSt = LOAD_TEXTURE2D(_PrevStencilTex, prevCoord).STENCIL_CHANNEL & 3;

                bool mismatch = !(st0 == prevSt || st1 == prevSt || st2 == prevSt || st3 == prevSt || st4 == prevSt);

                /*
                bool atEdge = st0 != st1;
                atEdge = atEdge || st0 != st2;
                atEdge = atEdge || st0 != st3;
                atEdge = atEdge || st0 != st4;
                atEdge = atEdge || abs(linearDepth0 - linearDepth1) > minDepthRejection;
                atEdge = atEdge || abs(linearDepth0 - linearDepth2) > minDepthRejection;
                atEdge = atEdge || abs(linearDepth0 - linearDepth3) > minDepthRejection;
                atEdge = atEdge || abs(linearDepth0 - linearDepth4) > minDepthRejection;
                */

                // return atEdge ? 1.0f : .0f;

                // depth test
                float linearPrevDepth = PrevDepthToLinearEyeSpace(prevClosetDepth);

                float depthM1 = linearDepth0;
                float depthM2 = linearDepth0 * linearDepth0;
                depthM1 += linearDepth1;
                depthM2 += linearDepth1 * linearDepth1;
                depthM1 += linearDepth2;
                depthM2 += linearDepth2 * linearDepth2;
                depthM1 += linearDepth3;
                depthM2 += linearDepth3 * linearDepth3;
                depthM1 += linearDepth4;
                depthM2 += linearDepth4 * linearDepth4;

                // depth weighting
                // we use neighboring depths to compute the variance and calculate the chebyshev weight
                // this prevents aliasing on geometry edges due to a tight clip scale
                float depthWeight = (linearDepth0 - linearPrevDepth < minDepthRejection) ? 1.0f : DepthVariance(depthM1, depthM2, 5, linearDepth0, linearPrevDepth);
                
                float clipScale = lerp(maxClipScale, minClipScale, velocityWeight * depthWeight);
                
                clipScale = mismatch ? minClipScale : clipScale;

                // count for imprecision
                bool staticPixel = mvScale < 2.0f * FLT_EPS && prevMVScale < 2.0f * FLT_EPS;
                // increase clip box scale when it is a static pixel and we detect a large different in depth/stencil either historically or spatially
                // we also need to make sure it is not covered by any transparent effect, in which case the MV computation is broken
                bool antiFlicker = staticPixel && !mismatch && prev.a < .5f && curr.a > .99f;
                clipScale = antiFlicker ? staticClipScale : clipScale;

                // return antiFlicker ? 1.0f : .0f;
                // return mismatch ? 1.0f : .0f;
                // return (mismatch && !antiFlicker) ? 1.0f : .0f;

                prev.rgb = YCoCgToRGB(ClipVariance(m1, m2, 9.0f, clipScale, RGBToYCoCg(prev.rgb)));
                
                /*
                float3 minColor = n0;
                float3 maxColor = minColor;

                minColor = min(minColor, n1);
                minColor = min(minColor, n2);
                minColor = min(minColor, n3);
                minColor = min(minColor, n4);
                minColor = min(minColor, n5);
                minColor = min(minColor, n6);
                minColor = min(minColor, n7);
                minColor = min(minColor, n8);

                maxColor = max(maxColor, n1);
                maxColor = max(maxColor, n2);
                maxColor = max(maxColor, n3);
                maxColor = max(maxColor, n4);
                maxColor = max(maxColor, n5);
                maxColor = max(maxColor, n6);
                maxColor = max(maxColor, n7);
                maxColor = max(maxColor, n8);

                prev.rgb = YCoCgToRGB(clamp(RGBToYCoCg(prev.rgb), minColor, maxColor));

                // prev.rgb = YCoCgToRGB(ClipAABB(minColor, maxColor, RGBToYCoCg(prev.rgb)));
                */

                // return depthWeight;

                bool edgeBlur = !antiFlicker && (mismatch || depthWeight < invalidHistoryThreshold);
                float3 cross = c5 + c6 + c7 + c8;
                if (edgeBlur) {
                    // adaptive gaussian blur if pixel is at edge and doesn't have a valid history sample
                    float centerWeight = minEdgeBlurriness;
                    curr.rgb = AdaptiveGaussianBlur(curr.rgb, cross, c1 + c2 + c3 + c4, centerWeight);
                } else {
                    // cross pattern sharpening
                    float sharpnessFactor = lerp(maxSharpness, minSharpness, mvScale * motionSharpeningFactor);
                    curr.rgb = AdaptiveSharpening(curr.rgb, cross, sharpnessFactor);
                }

                // decrease history weight when transparent pixel has a large opacity (motion vector is less reliable here)
                float historyWeight = curr.a > .999f ? maxHistoryWeight : lerp(maxHistoryWeight, minHistoryWeight, curr.a);
                float3 resolved = lerp(curr.rgb, prev.rgb, historyWeight);

                output.rgb = resolved;
                // record antiFlicker history
                output.a = antiFlicker ? .0f : 1.0f;
                return output;
            }
            
            ENDHLSL
        }
    }
}