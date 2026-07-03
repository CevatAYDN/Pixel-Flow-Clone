Shader "PixelFlow/BentoGlass"
{
    Properties
    {
        _MainTex ("Background Sample", 2D) = "white" {}
        _GlassColor ("Glass Tint", Color) = (0.08, 0.08, 0.12, 0.78)
        _BorderColor ("Border Color", Color) = (1, 1, 1, 0.12)
        _CornerRadius ("Corner Radius", Range(0, 0.1)) = 0.025
        _BlurStrength ("Blur Strength", Range(0, 1)) = 0.6
        _NoiseTex ("Noise", 2D) = "gray" {}
        _HighlightIntensity ("Highlight", Range(0, 1)) = 0.08
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
        LOD 200

        Pass
        {
            Name "BentoGlass"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _GlassColor;
                float4 _BorderColor;
                float _CornerRadius;
                float _BlurStrength;
                float _HighlightIntensity;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                OUT.screenPos = ComputeScreenPos(OUT.positionCS);
                return OUT;
            }

            float roundedBoxSDF(float2 uv, float2 size, float radius)
            {
                float2 q = abs(uv) - size + radius;
                return min(max(q.x, q.y), 0.0) + length(max(q, 0.0)) - radius;
            }

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 size = float2(0.5, 0.5) - _CornerRadius;
                float sdf = roundedBoxSDF(IN.uv - 0.5, size, _CornerRadius);

                if (sdf > 0.0) discard;

                float edgeFade = 1.0 - smoothstep(-0.005, 0.0, sdf);

                float2 noiseUV = IN.uv * 24.0;
                float n = hash21(floor(noiseUV));
                float blur = lerp(1.0, 0.92 + n * 0.08, _BlurStrength);

                float topGlow = smoothstep(0.0, 0.5, IN.uv.y) * _HighlightIntensity;
                float border = smoothstep(-0.003, 0.0, sdf) * _BorderColor.a;

                float3 baseTint = _GlassColor.rgb * blur;
                baseTint += float3(topGlow, topGlow, topGlow) * 0.5;

                half4 col;
                col.rgb = baseTint;
                col.a = _GlassColor.a * edgeFade + border * _BorderColor.a;
                return col;
            }
            ENDHLSL
        }
    }
    FallBack "Sprites/Default"
}
