using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace Scarecrow
{
    /// <summary>
    /// 渲染状态管理器
    /// 封装所有渲染状态相关的配置、UI绘制和应用逻辑
    /// </summary>
    public class RenderStateManager
    {
        /// <summary>混合模式配置信息</summary>
        private struct RenderStatePreset
        {
            public string RenderTag;
            public UnityEngine.Rendering.BlendMode SrcBlend;
            public UnityEngine.Rendering.BlendMode DstBlend;
            public int ZWrite;
            public CompareFunction ZTest;
            public int Surface;
            public bool AlphaTest;
            public RenderQueue Queue;
            public int QueueOffset;

            public RenderStatePreset(string tag, UnityEngine.Rendering.BlendMode srcBlend, UnityEngine.Rendering.BlendMode dstBlend, int zWrite, CompareFunction zTest,
                                     int surface, bool alphaTest, RenderQueue queue, int queueOffset = 0)
            {
                RenderTag = tag;
                SrcBlend = srcBlend;
                DstBlend = dstBlend;
                ZWrite = zWrite;
                ZTest = zTest;
                Surface = surface;
                AlphaTest = alphaTest;
                Queue = queue;
                QueueOffset = queueOffset;
            }
        }

        /// <summary>渲染混合模式枚举</summary>
        public enum BlendMode { Opaque, Transparent, Additive, Cutout, Background, Decal, Overlay, AdditiveOverlay }

        /// <summary>渲染面枚举</summary>
        public enum RenderFace { Front = 0, Both = 1, Back = 2 }

        /// <summary>当前混合模式</summary>
        public BlendMode Blend { get; set; }

        /// <summary>当前渲染面</summary>
        public RenderFace Cull { get; set; }

        /// <summary>渲染队列偏移</summary>
        public int QueueOffset { get; set; }

        /// <summary>混合模式预设配置表</summary>
        private static readonly Dictionary<BlendMode, RenderStatePreset> BlendModePresets = new()
        {
            { BlendMode.Opaque, new("", UnityEngine.Rendering.BlendMode.One, UnityEngine.Rendering.BlendMode.Zero, 1, CompareFunction.LessEqual, 0, false, RenderQueue.Geometry) },
            { BlendMode.Cutout, new("TransparentCutout", UnityEngine.Rendering.BlendMode.One, UnityEngine.Rendering.BlendMode.Zero, 1, CompareFunction.LessEqual, 0, true, RenderQueue.AlphaTest) },
            { BlendMode.Transparent, new("Transparent", UnityEngine.Rendering.BlendMode.SrcAlpha, UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha, 0, CompareFunction.LessEqual, 1, false, RenderQueue.Transparent) },
            { BlendMode.Overlay, new("Transparent", UnityEngine.Rendering.BlendMode.SrcAlpha, UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha, 0, CompareFunction.Always, 1, false, RenderQueue.Transparent) },
            { BlendMode.AdditiveOverlay, new("Transparent", UnityEngine.Rendering.BlendMode.SrcAlpha, UnityEngine.Rendering.BlendMode.One, 0, CompareFunction.Always, 1, false, RenderQueue.Transparent) },
            { BlendMode.Additive, new("Transparent", UnityEngine.Rendering.BlendMode.SrcAlpha, UnityEngine.Rendering.BlendMode.One, 0, CompareFunction.LessEqual, 1, false, RenderQueue.Transparent) },
            { BlendMode.Background, new("", UnityEngine.Rendering.BlendMode.One, UnityEngine.Rendering.BlendMode.Zero, 1, CompareFunction.LessEqual, 0, false, RenderQueue.Background) },
            { BlendMode.Decal, new("", UnityEngine.Rendering.BlendMode.SrcAlpha, UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha, 0, CompareFunction.LessEqual, 1, true, RenderQueue.Geometry, 1) },
        };

        public bool DrawUI(MaterialEditor editor, MaterialProperty enableProp, MaterialProperty blendModeIndexProp,
                          MaterialProperty cullIndexProp, MaterialProperty queueOffsetProp, MaterialProperty alphaClipProp = null)
        {
            if (enableProp == null)
                return false;

            bool isEnabled = enableProp.floatValue == 1;
            string buttonText = isEnabled ? "Disable Render States" : "Enable Render States";

            if (GUILayout.Button(buttonText, GUILayout.Height(25)))
            {
                enableProp.floatValue = isEnabled ? 0 : 1;
                isEnabled = !isEnabled;
            }

            if (isEnabled)
            {
                EditorGUI.indentLevel++;

                // 混合模式：仅当属性存在时显示UI和保存
                if (blendModeIndexProp != null)
                {
                    Blend = (BlendMode)(int)blendModeIndexProp.floatValue;

                    var blendModeNames = System.Enum.GetNames(typeof(BlendMode));
                    int currentBlendIndex = (int)Blend;
                    currentBlendIndex = EditorGUILayout.Popup("Blend Mode", currentBlendIndex, blendModeNames);
                    if (currentBlendIndex >= 0 && currentBlendIndex < blendModeNames.Length)
                    {
                        Blend = (BlendMode)currentBlendIndex;
                        blendModeIndexProp.floatValue = currentBlendIndex;
                    }

                    // 如果是 Cutout 模式且有 AlphaClip 属性，则绘制
                    if (Blend == BlendMode.Cutout && alphaClipProp != null)
                    {
                        editor.ShaderProperty(alphaClipProp, alphaClipProp.displayName);
                    }
                }
                else
                {
                    // 属性不存在，使用默认值，不显示UI
                    Blend = BlendMode.Opaque;
                }

                // 渲染面：仅当属性存在时显示UI和保存
                if (cullIndexProp != null)
                {
                    Cull = (RenderFace)(int)cullIndexProp.floatValue;

                    var renderFaceNames = new[] { "Front", "Both", "Back" };
                    int currentFaceIndex = (int)Cull;
                    currentFaceIndex = EditorGUILayout.Popup("Render Face", currentFaceIndex, renderFaceNames);
                    if (currentFaceIndex >= 0 && currentFaceIndex < renderFaceNames.Length)
                    {
                        Cull = (RenderFace)currentFaceIndex;
                        cullIndexProp.floatValue = currentFaceIndex;
                    }
                }
                else
                {
                    // 属性不存在，使用默认值，不显示UI
                    Cull = RenderFace.Front;
                }

                // 队列偏移：仅当属性存在时显示UI和保存
                if (queueOffsetProp != null)
                {
                    QueueOffset = (int)queueOffsetProp.floatValue;
                    QueueOffset = EditorGUILayout.IntSlider("Queue Offset", QueueOffset, -50, 50);
                    queueOffsetProp.floatValue = QueueOffset;
                }
                else
                {
                    // 属性不存在，使用默认值，不显示UI
                    QueueOffset = 0;
                }

                EditorGUI.indentLevel--;
            }

            return isEnabled;
        }

        /// <summary>
        /// 应用渲染状态到材质
        /// </summary>
        public void ApplyToMaterial(Material material)
        {
            if (!BlendModePresets.TryGetValue(Blend, out RenderStatePreset preset))
                return;

            ApplyRenderState
            (
            material,
            tag: preset.RenderTag,
            srcBlend: preset.SrcBlend,
            dstBlend: preset.DstBlend,
            zWrite: preset.ZWrite,
            zTest: preset.ZTest,
            alphaTest: preset.AlphaTest,
            queue: preset.Queue,
            queueOffset: preset.QueueOffset
             );
        }

        /// <summary>
        /// 应用具体的渲染状态到材质
        /// </summary>
        private void ApplyRenderState(Material material, string tag, UnityEngine.Rendering.BlendMode srcBlend, UnityEngine.Rendering.BlendMode dstBlend, int zWrite, CompareFunction zTest, bool alphaTest, RenderQueue queue, int queueOffset = 0)
        {
            material.SetOverrideTag("RenderType", tag);
            PropertyDrawHelper.SetMaterialProp_Int(material, ShaderPropertyIDs.S_SrcBlend, (int)srcBlend);
            PropertyDrawHelper.SetMaterialProp_Int(material, ShaderPropertyIDs.S_DstBlend, (int)dstBlend);
            PropertyDrawHelper.SetMaterialProp_Int(material, ShaderPropertyIDs.S_ZWrite, zWrite);
            PropertyDrawHelper.SetMaterialProp_Int(material, ShaderPropertyIDs.S_ZTest, (int)zTest);

            // 应用 AlphaTest 关键字
            if (alphaTest)
                material.EnableKeyword(ShaderPropertyIDs.S_AlphaClipKeyword);
            else
                material.DisableKeyword(ShaderPropertyIDs.S_AlphaClipKeyword);

            // 应用渲染面设置
            PropertyDrawHelper.SetMaterialProp_Float(material, ShaderPropertyIDs.S_Cull, (float)Cull);

            // 应用渲染队列和偏移
            material.renderQueue = (int)queue + QueueOffset;
        }
    }
}
