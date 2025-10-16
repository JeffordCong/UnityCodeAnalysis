Shader "Test/GUI Demo"
{
    Properties
    {
        [Header(Render States)]
        [Toggle] _EnableRenderStates ("Show Advanced Render States", Float) = 0
        
        [GroupBase(Advanced Settings)] _AdvancedGroup ("Advanced Settings", Float) = 0
        _Tiling ("Tiling", Vector) = (1, 1, 0, 0)
        _Offset ("Offset", Vector) = (0, 0, 0, 0)
        
        [GroupToggle(PBR_ON)] _UsePBR ("Enable PBR Metallic Workflow", Float) = 0
        [If(PBR_ON)] _Metallic ("Metallic", Range(0, 1)) = 0
        [If(PBR_ON)] [Tex]   _MetallicGlossMap ("Metallic Map", 2D) = "white" { }
        [If(PBR_ON)] _OcclusionStrength ("Occlusion", Range(0, 1)) = 1
        [If(PBR_ON)] [Tex] _OcclusionMap ("Occlusion Map", 2D) = "white" { }



        [HideInInspector] _Blend ("Blend Mode", Float) = 0 // 0:Opaque, 1:Transparent, 2:Additive, 3:Cutout
        [HideInInspector] _Cull ("Cull Mode", Float) = 2 // 0:Off, 1:Front, 2:Back
        [HideInInspector] _QueueOffset ("Queue Offset", Float) = 0 // 渲染队列偏移量


        [HideInInspector] _SrcBlend ("Src Blend", Float) = 1
        [HideInInspector] _DstBlend ("Dst Blend", Float) = 0
        [HideInInspector] _ZWrite ("Z Write", Float) = 1
        [HideInInspector] _Cull ("Cull", Float) = 2
        [HideInInspector] _ZTest ("ZTest", Float) = 4

        [HideInInspector] _Surface ("Surface", Float) = 1

        [HideInInspector] _StencilRef ("Stencil Ref", Float) = 0
        [HideInInspector] _StencilComp ("Stencil Comp", Float) = 8
        [HideInInspector] _StencilPass ("Stencil Pass", Float) = 2
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "IgnoreProjector" = "True"
            "UniversalMaterialType" = "Unlit" "RenderPipeline" = "UniversalPipeline" }
        LOD 100

        // -------------------------------------
        // Render State Commands
        Blend [_SrcBlend][_DstBlend], [_SrcBlendAlpha][_DstBlendAlpha]
        ZWrite [_ZWrite]
        Cull [_Cull]

        Pass
        {
            Name "Unlit"

            // -------------------------------------
            // Render State Commands
            AlphaToMask[_AlphaToMask]

            Stencil
            {
                Ref [_StencilRef]
                Comp [_StencilComp]
                Pass [_StencilPass]
                Fail [_StencilFail]
                ZFail [_StencilZFail]
            }


            
            HLSLPROGRAM
            #pragma target 2.0

            // -------------------------------------
            // Shader Stages
            #pragma vertex UnlitPassVertex
            #pragma fragment UnlitPassFragment

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local_fragment _SURFACE_TYPE_TRANSPARENT
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ALPHAMODULATE_ON

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile_fog
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
            #pragma multi_compile _ DEBUG_DISPLAY
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            // -------------------------------------
            // Includes
            #include "Packages/com.unity.render-pipelines.universal/Shaders/UnlitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/UnlitForwardPass.hlsl"
            ENDHLSL
        }
    }


    CustomEditor "Rendering.Editor.CustomShaderGUI"
}