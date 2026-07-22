Shader "Hidden/PixelFlow/GlowPulse"
{
    Properties
    {
        _Color ("Glow Tint", Color) = (1,1,1,1)
        _PulseSpeed ("Pulse Speed", Float) = 6.5
        _PulseAmplitude ("Pulse Amplitude", Float) = 0.10
        _PulseOffset ("Pulse Offset", Float) = 0.65
    }

    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True"}
        LOD 100

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "GlowPulseUnlit"
            Tags {"LightMode"="UniversalForward"}

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _PulseSpeed;
                float _PulseAmplitude;
                float _PulseOffset;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                // Pass vertex color (from LineRenderer) multiplied by material tint
                OUT.color = IN.color * _Color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // GPU-side glow pulse: _Time.y ile alpha animasyonu — 0 CPU maliyeti
                // Eskiden CPU: glowPulse = 0.52f + sin(Time.time * 6.5f) * 0.05f (her 3 frame'de 1)
                // Şimdi GPU: _Time.y ile otomatik, her frame ücretsiz
                float pulse = _PulseOffset + sin(_Time.y * _PulseSpeed) * _PulseAmplitude;
                half4 c = IN.color;
                c.a *= pulse;
                return c;
            }
            ENDHLSL
        }
    }

    FallBack "Sprites/Default"
}
