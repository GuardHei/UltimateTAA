Shader "Advanced Render Pipeline/Unlit" {

    Properties {
    
    }
    
    SubShader {
        Pass {
            HLSLPROGRAM

            #pragma multi_compile_instancing
            #pragma vertex UnlitVertex
            #pragma fragment UnlitFragment
            
            #include "../ShaderLibrary/ARPCommon.hlsl"
            
            ENDHLSL
        }
    }
}