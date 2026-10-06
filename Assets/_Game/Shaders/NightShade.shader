// The dark of a night flight, drawn as one sheet over the board. Instead of cutting hard
// holes, it works out how much light reaches each pixel from a few soft lights and lets the
// board through by that much: a round pool that fades out from its middle, and a headlight
// beam opening out ahead of the plane like a torch's cone, out to the edge of the screen.
// The dark warms towards the edge of the light and every light flickers a little, like a
// lamp rather than a cut-out.
//
// The lights are set from script each frame: _NightLight holds a light's position and the
// way it faces (xy, zw), _NightShape its pool radius, beam reach, the tangent of the beam's
// half angle, and strength, and _NightBeam how far the beam is switched on.
//
// The weather is drawn here too, so it costs no more than the dark already does: fog that
// drifts through the dark and shows most where the light catches it, rain that glints in
// the beam, and lightning that lights the whole board for a moment. _WeatherScale is one
// board square in world units, so the weather keeps its size on any board.
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
        _FogColor ("Fog", Color) = (0.5, 0.56, 0.7, 1)
        _FlashColor ("Lightning", Color) = (0.26, 0.31, 0.5, 1)
        _Fog ("Fog Amount", Range(0, 1)) = 0
        _Rain ("Rain Amount", Range(0, 1)) = 0
        _Flash ("Lightning Amount", Range(0, 1)) = 0
        _WeatherScale ("Board Square", Float) = 1
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
                half4 _FogColor;
                half4 _FlashColor;
                half _Fog;
                half _Rain;
                half _Flash;
                float _WeatherScale;
            CBUFFER_END

            float4 _NightLight[NIGHT_LIGHTS];
            float4 _NightShape[NIGHT_LIGHTS];

            // x: how bright each light's beam is, from 0 (switched off) to 1.
            float4 _NightBeam[NIGHT_LIGHTS];

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
                    // A torch's cone: narrow at the nose, opening at a fixed angle (z holds
                    // the tangent of its half angle), dimming a little with distance.
                    float halfWidth = shape.x * 0.5 + max(ahead, 0.0) * shape.z;
                    beam = (1.0 - smoothstep(halfWidth * 0.35, halfWidth, across))
                        * (1.0 - smoothstep(0.35, 1.0, t))
                        * lerp(1.0, 0.7, saturate(ahead / (shape.y * 0.25)))
                        * smoothstep(-shape.x * 0.3, shape.x * 0.2, ahead);
                }

                // A slow, uneven waver, out of step from one light to the next.
                float time = _Time.y;
                float waver = 0.5 + 0.5 * sin(time * 9.0 + index * 2.3) * sin(time * 3.7 + index * 1.1);
                return max(pool, beam * 0.95 * _NightBeam[index].x) * shape.w * (1.0 - _Flicker * waver);
            }

            float Hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float Noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(Hash(i), Hash(i + float2(1, 0)), u.x),
                    lerp(Hash(i + float2(0, 1)), Hash(i + float2(1, 1)), u.x), u.y);
            }

            // Three layers of soft noise drifting at different speeds, each turned against the
            // last so the noise grid never lines up into blocks, and the fog rolls rather than slides.
            float FogAt(float2 p)
            {
                const float2x2 turn = float2x2(0.8, -0.6, 0.6, 0.8);
                float t = _Time.y;
                float2 q = p * 0.22 + float2(t * 0.06, t * 0.015);
                float n = Noise(q) * 0.55;
                q = mul(turn, q) * 2.1 - float2(t * 0.04, -t * 0.02);
                n += Noise(q) * 0.3;
                q = mul(turn, q) * 2.3 + float2(t * 0.03, t * 0.05);
                n += Noise(q) * 0.15;
                return smoothstep(0.35, 0.8, n);
            }

            // Thin slanting streaks falling fast: the plane is cut into narrow columns, each
            // with its own speed, and only some cells of each column hold a drop.
            float RainAt(float2 p)
            {
                p.x += p.y * 0.18;
                const float columnWidth = 0.22;
                const float dropSpacing = 1.6;
                float column = floor(p.x / columnWidth);
                float seed = Hash(float2(column, 7.1));
                float y = p.y / dropSpacing + _Time.y * (5.0 + seed * 2.0) + seed * 10.0;
                float cell = floor(y);
                float along = frac(y);
                float drop = Hash(float2(column, cell));
                float across = abs(frac(p.x / columnWidth) - (0.3 + 0.4 * drop));
                return (1.0 - smoothstep(0.0, 0.1, across))
                    * smoothstep(0.0, 0.08, along) * (1.0 - smoothstep(0.2, 0.35, along))
                    * step(0.45, drop);
            }

            // Lays a layer of colour over what is there so far, as alpha blending would.
            void Over(inout half3 colour, inout float alpha, half3 layer, float layerAlpha)
            {
                float result = layerAlpha + alpha * (1.0 - layerAlpha);
                colour = (layer * layerAlpha + colour * alpha * (1.0 - layerAlpha)) / max(result, 1e-4);
                alpha = result;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float light = 0.0;

                for (int i = 0; i < NIGHT_LIGHTS; i++)
                {
                    light = max(light, LightAt(input.positionWS, _NightLight[i], _NightShape[i], i));
                }

                float2 square = input.positionWS / _WeatherScale;
                float fog = _Fog > 0.0 ? _Fog * FogAt(square) : 0.0;

                // Fog swallows some of the light, and lightning lights everything for a moment.
                light = saturate(light) * (1.0 - fog * 0.3);
                // A flash shows the board only dimly, washed blue-white, so it reads as lightning and not daylight.
                light = max(light, _Flash * 0.45);

                // Worked out at full dark; the dark fades in and out by _Darkness below.
                float alpha = 1.0 - light;

                // The dark warms towards the light's edge, so the light seems to spill into it
                // rather than stop at a line; most where the light is half way out.
                float haze = _GlowStrength * light * (1.0 - light) * 4.0;
                half3 colour = lerp(_NightColor.rgb, _GlowColor.rgb, haze);
                colour = lerp(colour, _FlashColor.rgb, _Flash);

                // Fog shows faintly in the dark and thickly where the light catches it.
                colour = lerp(colour, _FogColor.rgb, fog * 0.12);
                Over(colour, alpha, _FogColor.rgb, saturate(fog * light * 0.45));
                alpha *= _Darkness;

                // The rain is laid over after the dark has faded by _Darkness, so it keeps
                // falling once the morning comes. In the dark it shows most in the light; by
                // day it shows everywhere, whiter against the bright sky.
                if (_Rain > 0.0)
                {
                    float shine = lerp(1.0, 0.2 + light * 0.8, _Darkness);
                    float rain = _Rain * RainAt(square) * shine;
                    half3 rainColour = lerp(half3(1, 1, 1), lerp(_FogColor.rgb, half3(1, 1, 1), 0.5), _Darkness);
                    Over(colour, alpha, rainColour, saturate(rain * 0.7));
                }

                return half4(colour, alpha);
            }
            ENDHLSL
        }
    }
}
