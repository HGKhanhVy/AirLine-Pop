// A cast shadow without a shadow map: the mesh is flattened onto a plane of constant
// depth along the board light, then shifted by the same oblique shear as the model, and
// filled with the board shadow colour.
//
// The stencil bit lets each pixel be darkened once however many triangles, or however
// many blocks, overlap there. Give each kind of shadow its own bit so one does not
// block another: the blocks' shadow on the ground and the airplane's on the blocks.
Shader "AirLinePop/Planar Shadow"
{
    Properties
    {
        _PlaneDepth ("Plane Depth", Float) = 0
        [IntRange] _StencilBit ("Stencil Bit", Range(1, 128)) = 128
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
            ZTest Always
            Cull Off

            Stencil
            {
                Ref [_StencilBit]
                ReadMask [_StencilBit]
                WriteMask [_StencilBit]
                Comp NotEqual
                Pass Replace
            }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "BoardLitShared.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _PlaneDepth;
                float _StencilBit;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 light = normalize(_AirLineLightDir.xyz);

                // Slide along the light until the plane is reached. The light always
                // travels into the board, so light.z is positive.
                float travel = (_PlaneDepth - positionWS.z) / max(light.z, 0.05);
                float3 onPlane = positionWS + light * travel;
                onPlane.z = _PlaneDepth;

                output.positionCS = TransformWorldToHClip(onPlane);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                return _AirLineShadowColor;
            }
            ENDHLSL
        }
    }
}
