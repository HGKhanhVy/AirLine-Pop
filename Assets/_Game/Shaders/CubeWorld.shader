// The CubeAnimals mobile PBR shader with its lighting inputs completed: the original never
// passes the world position to URP, so point and spot lights are measured from the world
// origin and shadows sample the wrong coordinates. Same properties, same look otherwise,
// so any CubeAnimals material can switch to it.
Shader "AirLine Pop/Cube World"
{
    Properties
    {
        [MainColor] _Tint("Main Color", Color) = (1,1,1,1)
        [MainTexture] _AlbedoMap("Albedo (RGB) Alpha(A)", 2D) = "white" {}

        _NormalInflow("Normal Map", 2D) = "bump" {}
        _NormalStr("Normal Strength", Range(0, 2)) = 1.0

        [Header(PBR Surface Settings)]
        _PBRComposite("PBR Mask (R:Met, G:AO, B:Rough)", 2D) = "white" {}
        _MetMult("Metallic Multiplier", Range(0, 1)) = 1.0
        _RoughMult("Roughness Multiplier", Range(0, 1)) = 1.0
        _OcclusionMult("AO Multiplier", Range(0, 1)) = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "StandardForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex VertexStage
            #pragma fragment FragmentStage

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct AppData
            {
                float4 vertex   : POSITION;
                float2 uv       : TEXCOORD0;
                float3 normal   : NORMAL;
                float4 tangent  : TANGENT;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 posCS    : SV_POSITION;
                float2 uv       : TEXCOORD0;
                float3 nWS      : TEXCOORD1;
                float3 posWS    : TEXCOORD2;
                float4 tWS      : TEXCOORD3;
                float3 vDirWS   : TEXCOORD4;
                DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 5);
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _AlbedoMap_ST;
                half4 _Tint;
                half _NormalStr;
                half _MetMult;
                half _RoughMult;
                half _OcclusionMult;
            CBUFFER_END

            TEXTURE2D(_AlbedoMap);    SAMPLER(sampler_AlbedoMap);
            TEXTURE2D(_NormalInflow); SAMPLER(sampler_NormalInflow);
            TEXTURE2D(_PBRComposite); SAMPLER(sampler_PBRComposite);

            v2f VertexStage(AppData v)
            {
                v2f o = (v2f)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                VertexPositionInputs posInput = GetVertexPositionInputs(v.vertex.xyz);
                o.posCS = posInput.positionCS;
                o.posWS = posInput.positionWS;
                o.uv = TRANSFORM_TEX(v.uv, _AlbedoMap);

                VertexNormalInputs normInput = GetVertexNormalInputs(v.normal, v.tangent);
                o.nWS = normInput.normalWS;
                o.tWS = float4(normInput.tangentWS, v.tangent.w);
                o.vDirWS = GetWorldSpaceViewDir(posInput.positionWS);
                OUTPUT_SH(o.nWS, o.vertexSH);
                return o;
            }

            half4 FragmentStage(v2f i) : SV_Target
            {
                half4 baseColor = SAMPLE_TEXTURE2D(_AlbedoMap, sampler_AlbedoMap, i.uv) * _Tint;
                half4 pbrMask = SAMPLE_TEXTURE2D(_PBRComposite, sampler_PBRComposite, i.uv);

                half3 rawNormal = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalInflow, sampler_NormalInflow, i.uv), _NormalStr);
                half3 bitangent = cross(i.nWS, i.tWS.xyz) * i.tWS.w;
                half3 worldNormal = normalize(mul(rawNormal, half3x3(i.tWS.xyz, bitangent, i.nWS)));

                SurfaceData sData = (SurfaceData)0;
                sData.albedo = baseColor.rgb;
                sData.metallic = pbrMask.r * _MetMult;
                sData.smoothness = saturate(1.0 - (pbrMask.b * _RoughMult));
                sData.occlusion = lerp(1.0, pbrMask.g, _OcclusionMult);
                sData.normalTS = rawNormal;
                sData.alpha = baseColor.a;

                InputData lightInput = (InputData)0;
                lightInput.positionWS = i.posWS;
                lightInput.positionCS = i.posCS;
                lightInput.normalWS = worldNormal;
                lightInput.viewDirectionWS = normalize(i.vDirWS);
                lightInput.bakedGI = SampleSH(worldNormal);
                lightInput.shadowCoord = TransformWorldToShadowCoord(i.posWS);
                lightInput.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.posCS);
                lightInput.shadowMask = half4(1, 1, 1, 1);

                return UniversalFragmentPBR(lightInput, sData);
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
}
