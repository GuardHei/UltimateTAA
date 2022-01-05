Shader "Hidden/ARPTonemapping" {
    
    Properties {
        _MainTex("Texture", 2D) = "white" { }
    }
    
    SubShader {
        
        Pass {
            
            Name "Tonemapping"
            
            Cull Off
            ZWrite Off
            ZTest Always
            
            HLSLPROGRAM

            #pragma vertex TonemapVert
            #pragma fragment TonemapFragment

            #include "ARPCommon.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

            struct VertexOutput {
                float4 posCS : SV_POSITION;
                float2 screenUV : VAR_SCREEN_UV;
            };

            int _TonemappingMode;
            float4 _ColorGradeParams;
            float4 _ColorFilter;

            float3 ColorGradePostExposure(float3 input) {
                return input * _ColorGradeParams.x;
            }

            float3 ColorGradeContrast(float3 input) {
                float3 output = LinearToLogC(input);
                output = (output - ACEScc_MIDGRAY) * _ColorGradeParams.y + ACEScc_MIDGRAY;
                output = LogCToLinear(output);
                return output;
            }

            float3 ColorGradeFilter(float3 input) {
                return input * _ColorFilter.rgb;
            }

            float3 ColorGradeHueShift(float3 input) {
                float3 output = RgbToHsv(input);
                float hue = output.x + _ColorGradeParams.z;
                output.x = RotateHue(hue, .0f, 1.0f);
                output = HsvToRgb(output);
                return output;
            }

            float3 ColorGradeSaturation(float3 input) {
                float luminance = Luminance(input);
                return (input - luminance) * _ColorGradeParams.w + luminance;
            }

            float3 ColorGrade(float3 input) {
                float3 output = input;

                output = ColorGradePostExposure(output);
                output = ColorGradeContrast(output);
                output = ColorGradeFilter(output);
                
                output = max(output, .0f);

                output = ColorGradeHueShift(output);
                output = ColorGradeSaturation(output);

                output = max(output, .0f);
                
                return output;
            }

            VertexOutput TonemapVert(uint vertexID : SV_VertexID) {
                VertexOutput output;
                output.posCS = VertexIDToPosCS(vertexID);
                output.screenUV = VertexIDToScreenUV(vertexID);
                return output;
            }

            float4 TonemapFragment(VertexOutput input) : SV_TARGET {
                float2 uv = input.screenUV;
                if (_ProjectionParams.x < 0.0) uv.y = 1 - uv.y;
                float4 output = SAMPLE_TEXTURE2D(_MainTex, sampler_linear_clamp, uv);

                output.rgb = ColorGrade(output.rgb);
                
                if (_TonemappingMode == 1) output.rgb = AcesTonemap(unity_to_ACES(output.rgb));
                else if (_TonemappingMode == 2) output.rgb = NeutralTonemap(output.rgb);
                else if (_TonemappingMode == 3) output.rgb = output.rgb / (output.rgb + 1.0f);
                return output;
            }
            
            ENDHLSL
        }
    }
}