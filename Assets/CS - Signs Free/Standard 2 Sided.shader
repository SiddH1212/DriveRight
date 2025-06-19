Shader "URP/Standard2SidedURP"
{
    Properties
    {
        // Albedo
        _BaseMap("Albedo", 2D) = "white" {}
        _BaseColor("Base Color", Color) = (1,1,1,1)
        [Enum(Cutout, Transparent)] _SurfaceType("Surface Type", Float) = 0
        [Toggle] _AlphaClip("Alpha Clipping", Float) = 0
        [HideInInspector]_Cutoff("Alpha Cutoff", Range(0,1)) = 0.5

		// Metallic / Smoothness 
		_Metallic("Metallic", Range(0,1)) = 0.0
		_MetallicGlossMap("Metallic Gloss Map", 2D) = "white" {}
		_Smoothness("Smoothness", Range(0,1)) = 0.5
		_GlossMapScale("Gloss Map Scale", Range(0,1)) = 1.0
		[Enum(Metallic Alpha,0, Albedo Alpha,1)] _SmoothnessTextureChannel("Smoothness Source", Float) = 0

        // Normal Map
        _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Normal Scale", Float) = 1.0

        // Height (Parallax)
        [HideInInspector]_Parallax("Height Scale", Range(0.005,0.08)) = 0.02
        [HideInInspector]_ParallaxMap("Height Map", 2D) = "black" {}

        // Occlusion
        [HideInInspector]_OcclusionMap("Occlusion Map", 2D) = "white" {}
        [HideInInspector]_OcclusionStrength("Occlusion Strength", Range(0,1)) = 1.0

        // Emission
        _EmissionMap("Emission Map", 2D) = "white" {}
        _EmissionColor("Emission Color", Color) = (0,0,0,1)

        // Detail
        [HideInInspector]_DetailMask("Detail Mask", 2D) = "white" {}
        [HideInInspector]_DetailAlbedoMap("Detail Albedo x2", 2D) = "grey" {}
        [HideInInspector]_DetailNormalMap("Detail Normal Map", 2D) = "bump" {}
        [HideInInspector]_DetailNormalMapScale("Detail Normal Scale", Float) = 1.0

        // Two‑sided
        [HideInInspector]_CullMode("Cull Mode", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "AlphaTest"
        }
        LOD 300
        Cull Off

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _ALPHATEST_ON
            #pragma multi_compile _ _EMISSION
            #pragma multi_compile_fragment _ MAIN_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

			CBUFFER_START(UnityPerMaterial)
				float4 _BaseColor;
				float _Cutoff;
				float _Surface;
				float _Smoothness;
				float _GlossMapScale;
				float _Metallic;
				float _SmoothnessTextureChannel;
			CBUFFER_END
		

            // Textures
            TEXTURE2D(_BaseMap);       SAMPLER(sampler_BaseMap);
            TEXTURE2D(_MetallicGlossMap); SAMPLER(sampler_MetallicGlossMap);
            TEXTURE2D(_BumpMap);       SAMPLER(sampler_BumpMap);
            TEXTURE2D(_EmissionMap);   SAMPLER(sampler_EmissionMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float3 tangentWS   : TEXCOORD2;
                float3 bitangentWS : TEXCOORD3;
                float3 viewDirWS   : TEXCOORD4;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                // Object → World
                float3 posWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionHCS = TransformWorldToHClip(posWS);

                // UV
                OUT.uv = IN.uv;

                // TBN
                OUT.normalWS  = TransformObjectToWorldNormal(IN.normalOS);
                OUT.tangentWS = TransformObjectToWorldDir(IN.tangentOS.xyz);
                OUT.bitangentWS = cross(OUT.normalWS, OUT.tangentWS) * IN.tangentOS.w;

                // View dir
                OUT.viewDirWS = GetCameraPositionWS() - posWS;

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Albedo & alpha‑clip
                float4 baseCol = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;
                #ifdef _ALPHATEST_ON
                    clip(baseCol.a - _Cutoff);
                #endif

                // Metallic & smoothness from map
                float4 mGloss = SAMPLE_TEXTURE2D(_MetallicGlossMap, sampler_MetallicGlossMap, IN.uv);
                float metallic  = _Metallic * mGloss.r;
                float smoothness = _Smoothness * mGloss.a * _GlossMapScale;

                // Normal mapping
                float3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, IN.uv)) * _BumpScale;
                float3x3 TBN = float3x3(IN.tangentWS, IN.bitangentWS, IN.normalWS);
                float3 normalWS = normalize(mul(normalTS, TBN));

                // Emission
                float3 emission = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, IN.uv).rgb * _EmissionColor.rgb;

                // PBR surface inputs
                InputData inputData = (InputData)0;
                inputData.positionWS = 0; // unused by URP PBR
                inputData.normalWS   = normalWS;
                inputData.viewDirectionWS = normalize(IN.viewDirWS);

                SurfaceData surface = (SurfaceData)0;
                surface.albedo     = baseCol.rgb;
                surface.metallic   = metallic;
                surface.smoothness = smoothness;
                surface.normalWS   = normalWS;
                surface.emission   = emission;
                surface.alpha      = baseCol.a;

                return UniversalFragmentPBR(inputData, surface);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
    CustomEditor "UnityEditor.Rendering.Universal.ShaderGUI.LitShader"
}
