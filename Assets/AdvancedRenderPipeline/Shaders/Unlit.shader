Shader "Advanced Render Pipeline/Unlit" {

    Properties {
        _BaseColor("Color", Color) = (1.0, 1.0, 1.0, 1.0)
    }
    
    SubShader {
        
        UsePass "Hidden/ARPDepthStencilMV/DefaultDynamicDepthStencil"
        
        UsePass "Hidden/ARPShadow/OpaqueShadowCaster"
        
        Pass {
            
            ZTest Equal
            ZWrite On
			Cull Back
            
            HLSLPROGRAM

            #pragma multi_compile_instancing
            #pragma vertex UnlitVertex
            #pragma fragment UnlitFragment
            
            #include "../ShaderLibrary/ARPCommon.hlsl"

            struct BasicVertexInput {
                float3 posOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct BasicVertexOutput {
                float4 posCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
                UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
            UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

            BasicVertexOutput UnlitVertex(BasicVertexInput input) {
                BasicVertexOutput output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                float3 posWS = TransformObjectToWorld(input.posOS);
                output.posCS = TransformObjectToHClip(posWS);
                return output;
            }

            float4 UnlitFragment(BasicVertexOutput input) : SV_TARGET {
                UNITY_SETUP_INSTANCE_ID(input);
                float4 color = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseColor);
                return color;
            }
            
            ENDHLSL
        }
    }
}