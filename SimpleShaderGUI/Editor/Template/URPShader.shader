Shader "Hidden/ShaderGUI"
{
    Properties
    {
        [HideInInspector] _EnableRenderStates ("Enable Render States", Float) = 0

        // ========== 渲染状态属性 ==========
        [HideInInspector] _BlendModeIndex ("Blend Mode Index", Float) = 0
        [HideInInspector] _CullIndex ("Cull Index", Float) = 0
        [HideInInspector] _QueueOffsetValue ("Queue Offset", Float) = 0


        [HideInInspector] _SrcBlend ("Src Blend", Float) = 1
        [HideInInspector] _DstBlend ("Dst Blend", Float) = 0
        [HideInInspector] _ZWrite ("ZWrite", Float) = 1
        [HideInInspector] _ZTest ("ZTest", Float) = 4
        [HideInInspector] _Cull ("Cull", Float) = 2

        [HideInInspector] _AlphaClip("AlphaClip",Range(0,1)) = 1

        [Foldout(1, 2, 1, 1)]_MainFoldout ("Main Maps_Foldout", float) = 1
        [Tex(_MainColor)]_MainTex ("Main Tex", 2D) = "white" { }
        [HideInInspector]_MainColor ("Main Color", Color) = (1, 1, 1, 1)

        [Foldout(1, 2, 1, 1)]_NormalFoldout ("Normal Maps_Foldout", float) = 1
        [Tex(_NormalScale)] [NoScaleOffset]_NormalTex ("Normal Map", 2D) = "bump" { }
        [HideInInspector] _NormalScale ("Normal Scale", Range(0, 1)) = 1.0

        [Foldout(1, 1, 0, 0)]_EnableOutlinePass (" Outline Pass_Foldout", float) = 0
        [RangeVector4] _TestVector ("Test Vector", Vector) = (5, 10, 1, 50)


        // ========== 一级折叠页：表面效果 ==========
        [Foldout(1, 2, 1, 1)]_SurfaceFoldout ("Surface Properties_Foldout", float) = 1
        [Enum(Metallic, 0, Specular, 1)]_MetallicMode ("Metallic Mode", float) = 0



        // ========== Pass 切换控制（使用 Foldout 样式）==========
        // 属性名格式：_Enable{PassName}Pass，Pass 名称为 {PassName}
        // 例如：_EnableOutlinePass → Outline（Pass名称）



        // // ========== 一级折叠页：主要贴图 ==========
        // [Foldout(1, 1, 1, 1)]_MainFoldout ("Main Maps_Foldout", float) = 1
        // [Tex(_MainColor)]_MainTex ("Main Tex", 2D) = "white" { }
        // [HideInInspector]_MainColor ("Main Color", Color) = (1, 1, 1, 1)

        // // ========== 二级折叠页：法线和细节 ==========
        // [Foldout(2, 1, 1, 1)]_NormalFoldout ("Normal Maps_Foldout", float) = 1
        // [Tex()]_NormalTex ("Normal Map", 2D) = "bump" { }
        // [Range(0, 1)]_NormalScale ("Normal Scale", float) = 1.0


        // // ========== Toggle 控制显示/隐藏（二级属性） ==========
        // [Toggle_Switch]_UseDetailMap ("Use Detail Map", float) = 0
        // [Tex(, UseDetailMap)]_DetailTex ("Detail Map", 2D) = "white" { }
        // [Range(0, 1)]_DetailIntensity ("Detail Intensity", float) = 0.5

        // // ========== 跳出至一级 ==========
        // [Foldout_Out(1)]_SurfaceOut ("", float) = 0

        // // ========== 一级折叠页：表面效果 ==========
        // [Foldout(1, 1, 1, 1)]_SurfaceFoldout ("Surface Properties_Foldout", float) = 1
        // [Enum(Metallic, 0, Specular, 1)]_MetallicMode ("Metallic Mode", float) = 0
        // [Range(0, 1)]_Metallic ("Metallic", float) = 0.0
        // [Range(0, 1)]_Smoothness ("Smoothness", float) = 0.5

        // // ========== 二级折叠页：高级效果 ==========
        // [Foldout(2, 1, 1, 1)]_AdvancedFoldout ("Advanced Effects_Foldout", float) = 1
        // [Toggle_Switch]_UseParallax ("Use Parallax Mapping", float) = 0
        // [Range(0, 0.1)]_ParallaxHeight ("Parallax Height", float) = 0.02
        // [Toggle_Switch]_UseSSS ("Use Subsurface Scattering", float) = 0
        // [Range(0, 1)]_SSSIntensity ("SSS Intensity", float) = 0.5

        // // ========== 跳出至一级 ==========
        // [Foldout_Out(1)]_BlendOut ("", float) = 0

        // // // ========== 一级折叠页：混合和渲染 ==========
        // // [Foldout(1, 1, 0, 1)]_BlendFoldout ("Blend Settings_Foldout", float) = 1
        // // [Enum(UnityEngine.Rendering.BlendMode)]_SrcBlend ("Src Blend", float) = 1
        // // [Enum(UnityEngine.Rendering.BlendMode)]_DstBlend ("Dst Blend", float) = 0
        // // [Toggle_Switch]_UseAlphaMask ("Use Alpha Mask", float) = 0
        // // [Range(0, 1)]_AlphaCutoff ("Alpha Cutoff", float) = 0.5

        // // ========== 二级折叠页：阴影设置 ==========
        // [Foldout(2, 1, 1, 1)]_ShadowFoldout ("Shadow Settings_Foldout", float) = 1
        // [Toggle_Switch]_CastShadows ("Cast Shadows", float) = 1
        // [Toggle_Switch]_ReceiveShadows ("Receive Shadows", float) = 1
        // [Range(0, 1)]_ShadowBias ("Shadow Bias", float) = 0.0

        // // ========== 跳出至一级 ==========
        // [Foldout_Out(1)]_DebugOut ("", float) = 0

        // // ========== 一级折叠页：调试选项 ==========
        // [Foldout(1, 2, 1, 1)]_DebugFoldout ("Debug Options_Foldout", float) = 0
        // [Enum(None, 0, Normal, 1, Metallic, 2, Smoothness, 3)]_DebugMode ("Debug Mode", float) = 0
        // [Range(0, 1)]_DebugVisualize ("Visualize Amount", float) = 1.0

    }


    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry"
        }
        Cull [_Cull]
        Blend [_SrcBlend] [_DstBlend]
        ZWrite [_ZWrite]
        ZTest [_ZTest]


        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            half4 _MainColor;
        CBUFFER_END

        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);

        struct appdata
        {
            float4 vertex : POSITION;
            float2 uv : TEXCOORD0;
        };

        struct v2f
        {
            float4 vertex : SV_POSITION;
            float2 uv : TEXCOORD0;
        };


        v2f vert(appdata v)
        {
            v2f o;
            VertexPositionInputs vertexPos = GetVertexPositionInputs(v.vertex.xyz);
            o.vertex                       = vertexPos.positionCS;
            o.uv                           = TRANSFORM_TEX(v.uv, _MainTex);
            return o;
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags
            {
                "LightMode" = "UniversalForward"
            }



            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag


            half4 frag(v2f i) : SV_Target
            {
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * _MainColor;
                return col;
            }
            ENDHLSL
        }


        // ========== Outline Pass（通过 [Foldout] Pass 切换控制）==========
        Pass
        {
            Name "Outline"
            Tags
            {
                "LightMode" = "Outline"
            }



            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag


            half4 frag(v2f i) : SV_Target
            {
                return half4(1, 0, 0, 1); // 黑色外轮廓
            }
            ENDHLSL
        }
    }
    CustomEditor "Scarecrow.SimpleShaderGUI"
}