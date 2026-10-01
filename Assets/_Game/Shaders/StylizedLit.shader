// Soft cartoon lighting for the board's 3D models, lit by the board light rather than
// by scene lights, so it looks the same in every scene and costs one light on mobile.
//
// Albedo is the vertex colour times a tint. The vertex alpha picks the tint: 0 takes
// _BaseColor, 1 takes _WallColor. A block's top carries alpha 0, so it shows the palette
// colour that arrives on a MaterialPropertyBlock, and its walls fade to alpha 1, the earth
// or rock under the grass. Models whose vertices are all alpha 1 simply take _WallColor,
// which defaults to white.
//
// Blocks, scenery and the airplane all write and test depth, so the tilted camera sees
// near walls cover far ones. The material still exposes ZWrite and ZTest for overlays.
Shader "AirLinePop/Stylized Lit"
{
    Properties
    {
        _BaseColor ("Color", Color) = (1, 1, 1, 1)
        _WallColor ("Wall Color", Color) = (1, 1, 1, 1)
        _Specular ("Specular", Range(0, 1)) = 0.25
        _Gloss ("Gloss", Range(2, 128)) = 28
        _Rim ("Rim", Range(0, 1)) = 0.12
        _DetailTex ("Detail", 2D) = "grey" {}
        _DetailScale ("Detail Scale", Float) = 1
        _DetailStrength ("Detail Strength", Range(0, 1)) = 0
        [Enum(Off, 0, On, 1)] _ZWrite ("ZWrite", Float) = 1
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("ZTest", Float) = 4
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
    }

    SubShader
    {
        // Opaque: the models draw first, and sprites and lines draw after them, depth
        // tested against them.
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "IgnoreProjector" = "True"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // Same layout as the forward pass, which the SRP batcher requires.
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _WallColor;
                half _Specular;
                half _Gloss;
                half _Rim;
                float _DetailScale;
                half _DetailStrength;
            CBUFFER_END

            float4 DepthVert(float4 positionOS : POSITION) : SV_POSITION
            {
                return TransformObjectToHClip(positionOS.xyz);
            }

            half DepthFrag() : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }
            ZWrite [_ZWrite]
            ZTest [_ZTest]
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "BoardLitShared.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _WallColor;
                half _Specular;
                half _Gloss;
                half _Rim;
                float _DetailScale;
                half _DetailStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewDirWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                half4 color : COLOR;
            };

            TEXTURE2D(_DetailTex);
            SAMPLER(sampler_DetailTex);

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.positionWS = positionWS;
                output.viewDirWS = GetWorldSpaceNormalizeViewDir(positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                // Vertex colours are authored in sRGB like every other colour picker value,
                // but unlike material colours Unity hands them over unconverted. Without
                // this a linear-space project washes every model out towards white.
                half4 vertexColor = input.color;
                // The usual cheap polynomial fit of the sRGB curve, exact to well under 1%.
                #if !defined(UNITY_COLORSPACE_GAMMA)
                half3 c = vertexColor.rgb;
                vertexColor.rgb = c * (c * (c * 0.305306011h + 0.682171111h) + 0.012522878h);
                #endif
                half3 tint = lerp(_BaseColor.rgb, _WallColor.rgb, input.color.a);
                output.color = half4(vertexColor.rgb * tint, 1.0h);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 normal = normalize(input.normalWS);
                float3 toLight = -normalize(_AirLineLightDir.xyz);
                float3 toCamera = normalize(input.viewDirWS);

                // Half-Lambert keeps the unlit side readable instead of black.
                half diffuse = saturate(dot(normal, toLight) * 0.5h + 0.5h);
                half facing = saturate(-normal.z * 0.5h + 0.5h);
                half3 ambient = lerp(_AirLineGroundAmbient.rgb, _AirLineSkyAmbient.rgb, facing);
                half specular = pow(saturate(dot(normal, normalize(toLight + toCamera))), _Gloss) * _Specular;
                half rim = pow(1.0h - saturate(dot(normal, toCamera)), 3.0h) * _Rim;

                // Grain laid over upward faces from above, in world space, so grass, sand and
                // water read as surfaces rather than flat paint. It averages to one, so it
                // shades without shifting the colour; walls and undersides stay clean.
                half up = saturate(-normal.z);
                half detail = SAMPLE_TEXTURE2D(_DetailTex, sampler_DetailTex, input.positionWS.xy * _DetailScale).r;
                half3 albedo = input.color.rgb * (1.0h + (detail - 0.5h) * 2.0h * _DetailStrength * up);
                half3 colour = albedo * (ambient + diffuse * _AirLineLightColor.rgb)
                    + (specular + rim) * _AirLineLightColor.rgb;
                return half4(colour, input.color.a);
            }
            ENDHLSL
        }
    }
}
