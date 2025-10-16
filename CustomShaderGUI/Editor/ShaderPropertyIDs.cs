using UnityEngine;

namespace Rendering.Editor
{
    /// <summary>
    /// 集中管理Shader属性名称和ID，消除“魔法字符串”。
    /// 提供 const string 用于逻辑判断和初始化，提供 readonly int (PropertyToID) 用于高性能的材质读写。
    /// </summary>
    public static class ShaderPropertyIDs
    {
        // --- Render State Properties (在Shader中声明的属性) ---
        public const string S_EnableRenderStates = "_EnableRenderStates";
        public const string S_Blend = "_Blend";
        public const string S_Cull = "_Cull";
        public const string S_QueueOffset = "_QueueOffset";

        public static readonly int EnableRenderStates = Shader.PropertyToID(S_EnableRenderStates);
        public static readonly int Blend = Shader.PropertyToID(S_Blend);
        public static readonly int Cull = Shader.PropertyToID(S_Cull);
        public static readonly int QueueOffset = Shader.PropertyToID(S_QueueOffset);


        // --- Material State Properties (由RenderStateViewModel在后台设置的属性) ---
        public const string S_BlendOp = "_BlendOp";
        public const string S_SrcBlend = "_SrcBlend";
        public const string S_DstBlend = "_DstBlend";
        public const string S_ZWrite = "_ZWrite";
        public const string S_ZTest = "_ZTest";
        public const string S_Surface = "_Surface";

        public static readonly int BlendOp = Shader.PropertyToID(S_BlendOp);
        public static readonly int SrcBlend = Shader.PropertyToID(S_SrcBlend);
        public static readonly int DstBlend = Shader.PropertyToID(S_DstBlend);
        public static readonly int ZWrite = Shader.PropertyToID(S_ZWrite);
        public static readonly int ZTest = Shader.PropertyToID(S_ZTest);
        public static readonly int Surface = Shader.PropertyToID(S_Surface);


        // --- Keywords ---
        public const string AlphaTestKeyword = "_ALPHATEST_ON";
    }
}