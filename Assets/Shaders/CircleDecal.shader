Shader "Custom/CircleDualTextureDecal"
{
    Properties
    {
        _FillTex("Fill Texture", 2D) = "white" {}
        _FillTiling("Fill Tiling", Vector) = (1,1,0,0)

        _EdgeTex("Edge Texture", 2D) = "black" {}
        _EdgeTiling("Edge Tiling", Vector) = (1,1,0,0)

        _Thickness("Edge Thickness", Float) = 0.05

        [Enum(Add,0, AlphaBlend,1, Multiply,2)] _BlendMode("Blend Mode", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "DecalProjector"="True"
        }

        Pass
        {
            Name "ForwardLit"
            Tags
            {
                "LightMode"="UniversalForward"
            }
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
            };

            sampler2D _FillTex;
            float4    _FillTex_ST;

            sampler2D _EdgeTex;
            float4    _EdgeTex_ST;

            float _Thickness;
            float _BlendMode;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS);
                OUT.worldPos = TransformObjectToWorld(IN.positionOS);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Центр в UV (0.5, 0.5)
                float2 uvCenter = IN.uv - 0.5;

                // Радиус от центра (0..0.707)
                float dist = length(uvCenter);

                // Маска круга и края
                float edgeMask = smoothstep(1, 1 - _Thickness, dist);
                float fillMask = saturate(-dist);

                // Координаты для текстур с учётом Tiling/Offset
                float2 fillUV = TRANSFORM_TEX(IN.uv, _FillTex);
                float2 edgeUV = TRANSFORM_TEX(float2(atan2(uvCenter.y, uvCenter.x) / (2*3.14f) + 0.5, dist), _EdgeTex);

                half4 fillCol = tex2D(_FillTex, fillUV);
                half4 edgeCol = tex2D(_EdgeTex, edgeUV);

                // Альфа-смешивание по маскам
                half4 col = fillCol * fillMask + edgeCol * edgeMask;

                col.rgb = lerp(fillCol.rgb, edgeCol.rgb, edgeMask);

                return col;
            }
            ENDHLSL
        }
    }
}