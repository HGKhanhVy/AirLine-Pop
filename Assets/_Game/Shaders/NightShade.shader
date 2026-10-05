// The dark of a night flight, drawn as one sheet over the board. Instead of cutting hard
// holes, it works out how much light reaches each pixel from a few soft lights and lets the
// board through by that much: a round pool that fades out from its middle, and a headlight
// beam that widens and fades away ahead of the plane. The dark warms towards the edge of the
// light and every light flickers a little, like a lamp rather than a cut-out.
//
// The lights are set from script each frame: _NightLight holds a light's position and the
// way it faces (xy, zw), _NightShape its pool radius, beam reach, beam width and strength.
Shader "AirLinePop/Night Shade"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _NightColor ("Night", Color) = (0.03, 0.04, 0.12, 1)
        _GlowColor ("Glow", Color) = (1, 0.9, 0.62, 1)
        _Darkness ("Darkness", Range(0, 1)) = 1
        _GlowStrength ("Glow Strength", Range(0, 1)) = 0.6
        _Flicker ("Flicker", Range(0, 0.5)) = 0.07
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #define NIGHT_LIGHTS 3

            CBUFFER_START(UnityPerMaterial)
                half4 _NightColor;
                half4 _GlowColor;
                half _Darkness;
                half _GlowStrength;
                half _Flicker;
            CBUFFER_END

            float4 _NightLight[NIGHT_LIGHTS];
            float4 _NightShape[NIGHT_LIGHTS];

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 positionWS : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.positionWS = positionWS.xy;
                return output;
            }

            // How much of one light reaches a point, from 0 to 1.
            float LightAt(float2 p, float4 light, float4 shape, int index)
            {
                float2 offset = p - light.xy;
                float2 forward = light.zw;
                float2 side = float2(forward.y, -forward.x);

                float pool = 1.0 - smoothstep(shape.x * 0.2, shape.x, length(offset));

                float beam = 0.0;

                if (shape.y > 0.0)
                {
                    float ahead = dot(offset, forward);
                    float across = abs(dot(offset, side));
                    float t = saturate(ahead / shape.y);
                    float halfWidth = lerp(shape.x * 0.5, shape.z * 0.5, t);
                    beam = (1.0 - smoothstep(halfWidth * 0.25, halfWidth, across))
                        * (1.0 - smoothstep(0.35, 1.0, t))
                        * smoothstep(-shape.x * 0.3, shape.x * 0.2, ahead);
                }

                // A slow, uneven waver, out of step from one light to the next.
                float time = _Time.y;
                float waver = 0.5 + 0.5 * sin(time * 9.0 + index * 2.3) * sin(time * 3.7 + index * 1.1);
                return max(pool, beam * 0.95) * shape.w * (1.0 - _Flicker * waver);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float light = 0.0;

                for (int i = 0; i < NIGHT_LIGHTS; i++)
                {
                    light = max(light, LightAt(input.positionWS, _NightLight[i], _NightShape[i], i));
                }

                light = saturate(light);
                float alpha = _Darkness * (1.0 - light);

                // The dark warms towards the light's edge, so the light seems to spill into it
                // rather than stop at a line; most where the light is half way out.
                float haze = _GlowStrength * light * (1.0 - light) * 4.0;
                half3 colour = lerp(_NightColor.rgb, _GlowColor.rgb, haze);
                return half4(colour, alpha);
            }
            ENDHLSL
        }
    }
}
