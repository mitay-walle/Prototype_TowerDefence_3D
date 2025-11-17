Shader "URP/MatCapVoxel"
{
    Properties
    {
        _MatCapTex ("MatCap (spherical)", 2D) = "gray" {}
        [HDR]_MatCapTint ("MatCap Tint", Color) = (1,1,1,1)
        _MatCapStrength ("MatCap Strength", Range(0,5)) = 1
        _MatCapPow ("MatCap Power", Range(.01,15)) = 1
        [KeywordEnum(View,World)] _MATCAP_SPACE ("MatCap Space", Float) = 0

        [KeywordEnum(Classic,Face,Triplanar)] _MATCAP_MODE ("MatCap Mode", Float) = 0

        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Int) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Int) = 10
        [Enum(UnityEngine.Rendering.BlendOp)] _BlendOp ("Blend Op", Int) = 0

        _Opacity ("Opacity", Range(0,1)) = 1
        [Toggle(_ZWRITE_ON)] _ZWrite ("ZWrite On", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
        }
        LOD 120

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode"="UniversalForward" }

            BlendOp [_BlendOp]
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            Cull Back

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            // Keywords
            #pragma multi_compile _ _MATCAP_SPACE_VIEW _MATCAP_SPACE_WORLD
            #pragma multi_compile _ _MATCAP_MODE_CLASSIC _MATCAP_MODE_FACE _MATCAP_MODE_TRIPLANAR

            #pragma target 3.0
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float3 positionWS : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_Position;
                float3 normalWS    : TEXCOORD0;
                float3 positionWS  : TEXCOORD1;

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
                o.normalWS    = TransformObjectToWorldNormal(v.normalOS);
                o.positionWS  = TransformObjectToWorld(v.positionOS.xyz);

                return o;
            }

            float3 GetNormalVS(float3 normalWS)
            {
                float3 nVS = mul((float3x3)UNITY_MATRIX_V, normalWS);

                #if _MATCAP_SPACE_VIEW
                    return normalize(nVS);
                #else
                    return normalize(nVS);
                #endif
            }

            //------------------------------------------
            // MODE 1: Classic Matcap
            //------------------------------------------
            float4 SampleClassicMatcap(float3 normalWS)
            {
                float3 nVS = GetNormalVS(normalWS);
                float3 nP  = normalize(pow(abs(nVS), _MatCapPow)) * sign(nVS);

                float2 uv = nP.xy * 0.5 + 0.5;

                return SAMPLE_TEXTURE2D(_MatCapTex, sampler_MatCapTex, uv);
            }

            //------------------------------------------
            // MODE 2: Face-Based Matcap
            //------------------------------------------
            float4 SampleFaceMatcap(float3 normalWS)
            {
                float3 n = normalize(normalWS);
                float3 an = abs(n);
                float2 uv;

                if (an.x > an.y && an.x > an.z)
                {
                    uv = (n.x > 0) ? float2(0.75, 0.5) : float2(0.25, 0.5);
                }
                else if (an.y > an.x && an.y > an.z)
                {
                    uv = (n.y > 0) ? float2(0.5, 0.75) : float2(0.5, 0.25);
                }
                else
                {
                    uv = (n.z > 0) ? float2(0.5, 0.5) : float2(0.0, 0.5);
                }

                return SAMPLE_TEXTURE2D(_MatCapTex, sampler_MatCapTex, uv);
            }

            //------------------------------------------
            // MODE 3: Triplanar Matcap
            //------------------------------------------
            float4 SampleTriplanarMatcap(float3 normalWS)
            {
                float3 n = normalize(normalWS);
                float3 an = abs(n);
                float3 w = an / (an.x + an.y + an.z + 1e-5);

                float2 uvX = (n.yz) * 0.5 + 0.5;
                float2 uvY = (n.xz) * 0.5 + 0.5;
                float2 uvZ = (n.xy) * 0.5 + 0.5;

                float4 x = SAMPLE_TEXTURE2D(_MatCapTex, sampler_MatCapTex, uvX);
                float4 y = SAMPLE_TEXTURE2D(_MatCapTex, sampler_MatCapTex, uvY);
                float4 z = SAMPLE_TEXTURE2D(_MatCapTex, sampler_MatCapTex, uvZ);

                return x * w.x + y * w.y + z * w.z;
            }

            //------------------------------------------
            // FRAGMENT
            //------------------------------------------
            float4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                float4 col;

                #if _MATCAP_MODE_CLASSIC
                    col = SampleClassicMatcap(i.normalWS);

                #elif _MATCAP_MODE_FACE
                    col = SampleFaceMatcap(i.normalWS);

                #elif _MATCAP_MODE_TRIPLANAR
                    col = SampleTriplanarMatcap(i.normalWS);

                #else
                    col = float4(1,0,1,1); // fallback (не должен быть виден)
                #endif

                col.rgb *= _MatCapTint.rgb * _MatCapStrength;
                col.a   *= _Opacity;

                return col;
            }

            ENDHLSL
        }
    }

    Fallback Off
}
