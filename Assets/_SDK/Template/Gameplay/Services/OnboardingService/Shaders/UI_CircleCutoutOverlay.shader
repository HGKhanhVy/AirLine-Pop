Shader "UI/Circle Cutout Overlay"
{
    Properties
    {
        [PerRendererData]_MainTex("Sprite Texture", 2D) = "white" {}
        _Color("Tint", Color) = (1,1,1,1)
        _OverlayColor("Overlay Color", Color) = (0,0,0,0.72)
        _HoleCenter("Hole Center UV", Vector) = (0.5,0.5,0,0)
        _HoleRadius("Hole Radius (legacy)", Float) = 0.1
        _HoleAspect("Hole Aspect (legacy)", Float) = 1
        _CanvasSize("Canvas Size XY", Vector) = (1080,1920,0,0)
        _HoleRadiusPx("Hole Radius Px", Float) = 120
        _HoleSizePx("Hole Half Size Px XY", Vector) = (120,120,0,0)
        _ShapeType("Shape Type (0 Circle, 1 Rectangle)", Float) = 0
        _MaskScaleXY("Mask Scale XY", Vector) = (1,1,0,0)
        _MaskScaleX("Mask Scale X", Float) = 1
        _MaskScaleY("Mask Scale Y", Float) = 1
        _HoleBlur("Hole Blur", Range(0.0001,0.25)) = 0.02
        _BorderWidth("Border Width", Range(0,0.15)) = 0.02
        _BorderBlur("Border Blur", Range(0.0001,0.25)) = 0.01
        _BorderColor("Border Color", Color) = (1,1,1,1)
        _GlowColor("Glow Color", Color) = (1,1,1,1)
        _GlowIntensity("Glow Intensity", Range(0,4)) = 0.35
        _GlowWaveSpeed("Glow Wave Speed", Range(0.1,6)) = 1.1
        _GlowWaveWidth("Glow Wave Width", Range(0.0001,0.25)) = 0.012
        _GlowWaveTrail("Glow Wave Trail", Range(0.0001,0.5)) = 0.08
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "CanUseSpriteAtlas"="True"
        }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed4 _OverlayColor;
            float4 _HoleCenter;
            float _HoleRadius;
            float _HoleAspect;
            float4 _CanvasSize;
            float _HoleRadiusPx;
            float4 _HoleSizePx;
            float _ShapeType;
            float4 _MaskScaleXY;
            float _MaskScaleX;
            float _MaskScaleY;
            float _HoleBlur;
            float _BorderWidth;
            float _BorderBlur;
            fixed4 _BorderColor;
            fixed4 _GlowColor;
            float _GlowIntensity;
            float _GlowWaveSpeed;
            float _GlowWaveWidth;
            float _GlowWaveTrail;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 spriteCol = tex2D(_MainTex, i.uv) * i.color;

                float2 canvasSize = max(_CanvasSize.xy, float2(1.0, 1.0));
                float2 deltaUv = i.uv - _HoleCenter.xy;
                float2 deltaPx = deltaUv * canvasSize;
                float2 maskScale = max(_MaskScaleXY.xy, float2(1e-5, 1e-5));
                maskScale *= max(float2(_MaskScaleX, _MaskScaleY), float2(1e-5, 1e-5));
                deltaPx *= maskScale;
                float radiusPx = max(_HoleRadiusPx, 1e-5);
                float2 halfSizePx = max(_HoleSizePx.xy, float2(1e-5, 1e-5));
                float holeBlurPx = max(_HoleBlur * canvasSize.y, 1e-5);
                float borderWidthPx = max(_BorderWidth * canvasSize.y, 0.0);
                float borderBlurPx = max(_BorderBlur * canvasSize.y, 1e-5);

                float circleDist = length(deltaPx) - radiusPx;
                float2 rectQ = abs(deltaPx) - halfSizePx;
                float rectDist = length(max(rectQ, 0.0)) + min(max(rectQ.x, rectQ.y), 0.0);
                float useRect = step(0.5, _ShapeType);
                float edgeDistPx = lerp(circleDist, rectDist, useRect);

                float holeMask = smoothstep(-holeBlurPx, holeBlurPx, edgeDistPx);
                float borderOuter = smoothstep(borderWidthPx - borderBlurPx, borderWidthPx + borderBlurPx, edgeDistPx);
                float borderInner = smoothstep(-borderBlurPx, borderBlurPx, edgeDistPx);
                float borderMask = saturate(borderInner - borderOuter);

                fixed4 overlayCol = _OverlayColor;
                overlayCol.rgb = lerp(overlayCol.rgb, _BorderColor.rgb, borderMask * _BorderColor.a);

                // Animated outward glow: two waves half a cycle apart (continuous from inside out).
                float edgeUv = edgeDistPx / max(canvasSize.y, 1.0);
                float trail = max(_GlowWaveTrail, 1e-4);
                float glowWidthUv = max(_GlowWaveWidth, 1e-4);
                float spd = max(_GlowWaveSpeed, 0.1);
                float phase = _Time.y * spd;
                float c1 = frac(phase) * trail;
                float c2 = frac(phase + 0.5) * trail;
                // Sóng mờ dần khi tâm chạy ra xa mép lỗ (c → trail).
                float waveTravelFade1 = 1.0 - smoothstep(0.0, trail, c1);
                float waveTravelFade2 = 1.0 - smoothstep(0.0, trail, c2);
                float wave1 = (1.0 - smoothstep(0.0, glowWidthUv, abs(edgeUv - c1))) * step(0.0, edgeUv) * waveTravelFade1;
                float wave2 = (1.0 - smoothstep(0.0, glowWidthUv, abs(edgeUv - c2))) * step(0.0, edgeUv) * waveTravelFade2;
                // Vùng càng xa mép lỗ (edgeUv lớn) càng mờ.
                float outwardSpatialFade = 1.0 - smoothstep(0.0, trail * 1.35, edgeUv);
                float glowWave = saturate(wave1 + wave2) * 0.65 * outwardSpatialFade;
                float visibleShapeSizePx = lerp(radiusPx, min(halfSizePx.x, halfSizePx.y), useRect);
                float glowEnabled = step(1.0, visibleShapeSizePx);
                float glowMask = glowWave * _GlowColor.a * max(_GlowIntensity, 0.0) * glowEnabled;
                overlayCol.rgb += _GlowColor.rgb * glowMask;

                fixed4 outCol;
                outCol.rgb = overlayCol.rgb * spriteCol.a;
                outCol.a = overlayCol.a * holeMask * spriteCol.a;
                return outCol;
            }
            ENDCG
        }
    }
}
