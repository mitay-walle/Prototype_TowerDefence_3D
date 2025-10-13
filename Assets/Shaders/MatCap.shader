Shader "URP/MatCap Unlit"
{
    Properties
    {
        _MatCapTex ("MatCap (spherical)", 2D) = "gray" {}
        [HDR]_MatCapTint ("MatCap Tint", Color) = (1,1,1,1)
        _MatCapStrength ("MatCap Strength", Range(0,5)) = 1
        _MatCapPow ("MatCap Power", Range(.001,15)) = 1
        [KeywordEnum(View,World)] _MATCAP_SPACE ("MatCap Space", Float) = 0

        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Int) = 5 // One
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Int) = 10 // OneMinusSrcAlpha
        [Enum(UnityEngine.Rendering.BlendOp)] _BlendOp ("Blend Op", Int) = 0 // Add

        _Opacity ("Opacity", Range(0,1)) = 1
        [Toggle(_ZWRITE_ON)] _ZWrite ("ZWrite On (for transparent)", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalRenderPipeline"
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
        }
        LOD 100

        Pass
        {
            Name "UniversalForward"
            Tags
            {
                "LightMode"="UniversalForward"
            }

            BlendOp [_BlendOp]
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            Cull Back

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            // Keywords
            #pragma multi_compile _ _MATCAP_SPACE_VIEW _MATCAP_SPACE_WORLD

            #pragma target 3.0
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_Position;
                float3 normalWS : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _MatCapTint;
                float  _MatCapStrength;
                float  _MatCapPow;
                float  _Opacity;
            CBUFFER_END

            TEXTURE2D(_MatCapTex);
            SAMPLER(sampler_MatCapTex);

            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);

                return o;
            }

            float3 GetMatcapNormal(Varyings i)
            {
                float3 nWS = normalize(i.normalWS);
                float3 nVS = mul((float3x3)UNITY_MATRIX_V, nWS);

                #if _MATCAP_SPACE_WORLD
                    // Rotate with object — just return as is (converted to view-space)
                    return nVS;
                #else
                // Classic: camera-locked
                return normalize(nVS);
                #endif
            }

            float4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                float3 nVS = pow(GetMatcapNormal(i), _MatCapPow);
                float2 uv = nVS.xy * 0.5f + 0.5f;

                float4 mc = SAMPLE_TEXTURE2D(_MatCapTex, sampler_MatCapTex, uv) * _MatCapTint;
                mc.rgb *= _MatCapStrength;
                mc.a *= _Opacity;

                return mc;
            }
            ENDHLSL
        }
    }

    Fallback Off
}