

using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

// ViewModel层，负责驱动处理器链来构建UI结构。

namespace Rendering.Editor
{

    /// <summary>
    /// 分组数据结构，包含一个通用ViewModel列表。
    /// </summary>
    public class PropertyGroup
    {
        public MaterialProperty HeaderProperty { get; }
        public List<BasePropertyViewModel> Properties { get; }
        public bool IsHeaderlessGroup => HeaderProperty == null;
        public bool IsToggleGroup { get; }
        public string GroupToggleKeyword { get; }

        public PropertyGroup(MaterialProperty header)
        {
            HeaderProperty = header;
            Properties = new List<BasePropertyViewModel>();

            if (header != null)
            {
                var mat = header.targets.First() as Material;
                if (mat != null && mat.shader != null)
                {
                    int propertyIndex = mat.shader.FindPropertyIndex(header.name);
                    if (propertyIndex != -1)
                    {
                        var attrs = mat.shader.GetPropertyAttributes(propertyIndex);
                        var toggleAttr = attrs.FirstOrDefault(a => a.StartsWith("GroupToggle("));
                        if (toggleAttr != null)
                        {
                            IsToggleGroup = true;
                            Match match = Regex.Match(toggleAttr, @"\((.*)\)");
                            if (match.Success) GroupToggleKeyword = match.Groups[1].Value.Trim();
                        }
                    }
                }
            }
        }
    }


    /// <summary>
    /// 负责渲染状态的独立ViewModel模块。
    /// </summary>
    public class RenderStateViewModel
    {
        private readonly Material _material;
        public enum BlendMode { Opaque, Transparent, Additive, Cutout, Background, Decal, Overlay, AdditiveOverlay }
        public enum RenderFace { Front = 2, Back = 1, Both = 0 }

        public bool IsEnabled { get; set; }
        public BlendMode Blend { get; set; }
        public RenderFace Cull { get; set; }
        public int QueueOffset { get; set; }

        private static readonly HashSet<string> RenderStatePropNames = new HashSet<string>
        {
            ShaderPropertyIDs.S_EnableRenderStates,
            ShaderPropertyIDs.S_Blend,
            ShaderPropertyIDs.S_Cull,
            ShaderPropertyIDs.S_QueueOffset
         };
        public static bool IsRenderStateProperty(string name) => RenderStatePropNames.Contains(name);

        public RenderStateViewModel(Material material)
        {
            _material = material;
            LoadFromMaterial();
        }

        public void LoadFromMaterial()
        {
            IsEnabled = _material.HasProperty(ShaderPropertyIDs.EnableRenderStates) && _material.GetFloat(ShaderPropertyIDs.EnableRenderStates) > 0.5f;
            Blend = (BlendMode)(_material.HasProperty(ShaderPropertyIDs.Blend) ? _material.GetFloat(ShaderPropertyIDs.Blend) : 0);
            Cull = (RenderFace)(_material.HasProperty(ShaderPropertyIDs.Cull) ? _material.GetFloat(ShaderPropertyIDs.Cull) : 2);
            QueueOffset = _material.HasProperty(ShaderPropertyIDs.QueueOffset) ? _material.GetInt(ShaderPropertyIDs.QueueOffset) : 0;
        }

        public void ApplyToMaterial()
        {
            if (_material.HasProperty(ShaderPropertyIDs.EnableRenderStates))
                _material.SetFloat(ShaderPropertyIDs.EnableRenderStates, IsEnabled ? 1 : 0);

            if (_material.HasProperty(ShaderPropertyIDs.Blend))
                _material.SetFloat(ShaderPropertyIDs.Blend, (float)Blend);

            if (_material.HasProperty(ShaderPropertyIDs.Cull))
                _material.SetFloat(ShaderPropertyIDs.Cull, (float)Cull);

            if (_material.HasProperty(ShaderPropertyIDs.QueueOffset))
                _material.SetFloat(ShaderPropertyIDs.QueueOffset, QueueOffset);

            if (!IsEnabled && _material.HasProperty(ShaderPropertyIDs.EnableRenderStates)) return;

            switch (Blend)
            {
                case BlendMode.Opaque:
                    SetMaterialState(tag: "", srcBlend: UnityEngine.Rendering.BlendMode.One, dstBlend: UnityEngine.Rendering.BlendMode.Zero, zWrite: 1, zTest: CompareFunction.LessEqual, surface: 0, alphaTest: false, queue: RenderQueue.Geometry);
                    break;
                case BlendMode.Cutout:
                    SetMaterialState(tag: "TransparentCutout", srcBlend: UnityEngine.Rendering.BlendMode.One, dstBlend: UnityEngine.Rendering.BlendMode.Zero, zWrite: 1, zTest: CompareFunction.LessEqual, surface: 0, alphaTest: true, queue: RenderQueue.AlphaTest);
                    break;
                case BlendMode.Transparent:
                    SetMaterialState(tag: "Transparent", srcBlend: UnityEngine.Rendering.BlendMode.SrcAlpha, dstBlend: UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha, zWrite: 0, zTest: CompareFunction.LessEqual, surface: 1, alphaTest: false, queue: RenderQueue.Transparent);
                    break;
                case BlendMode.Overlay:
                    SetMaterialState(tag: "Transparent", srcBlend: UnityEngine.Rendering.BlendMode.SrcAlpha, dstBlend: UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha, zWrite: 0, zTest: CompareFunction.Always, surface: 1, alphaTest: false, queue: RenderQueue.Transparent);
                    break;
                case BlendMode.AdditiveOverlay:
                    SetMaterialState(tag: "Transparent", srcBlend: UnityEngine.Rendering.BlendMode.SrcAlpha, dstBlend: UnityEngine.Rendering.BlendMode.One, zWrite: 0, zTest: CompareFunction.Always, surface: 1, alphaTest: false, queue: RenderQueue.Transparent);
                    break;
                case BlendMode.Additive:
                    SetMaterialState(tag: "Transparent", srcBlend: UnityEngine.Rendering.BlendMode.SrcAlpha, dstBlend: UnityEngine.Rendering.BlendMode.One, zWrite: 0, zTest: CompareFunction.LessEqual, surface: 1, alphaTest: false, queue: RenderQueue.Transparent);
                    break;
                case BlendMode.Background:
                    SetMaterialState(tag: "", srcBlend: UnityEngine.Rendering.BlendMode.One, dstBlend: UnityEngine.Rendering.BlendMode.Zero, zWrite: 1, zTest: CompareFunction.LessEqual, surface: 0, alphaTest: false, queue: RenderQueue.Background);
                    break;
                case BlendMode.Decal:
                    SetMaterialState(tag: "", srcBlend: UnityEngine.Rendering.BlendMode.SrcAlpha, dstBlend: UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha, zWrite: 0, zTest: CompareFunction.LessEqual, surface: 1, alphaTest: true, queue: RenderQueue.Geometry, queueOffset: 1);
                    break;
            }
            _material.SetInt(ShaderPropertyIDs.Cull, (int)Cull);
        }

        private void SetMaterialState(string tag, UnityEngine.Rendering.BlendMode srcBlend, UnityEngine.Rendering.BlendMode dstBlend, int zWrite, CompareFunction zTest, int surface, bool alphaTest, RenderQueue queue, int queueOffset = 0)
        {
            _material.SetOverrideTag("RenderType", tag);
            _material.SetInt(ShaderPropertyIDs.S_BlendOp, (int)BlendOp.Add);
            _material.SetInt(ShaderPropertyIDs.S_SrcBlend, (int)srcBlend);
            _material.SetInt(ShaderPropertyIDs.S_DstBlend, (int)dstBlend);
            _material.SetInt(ShaderPropertyIDs.S_ZWrite, zWrite);
            _material.SetInt(ShaderPropertyIDs.S_ZTest, (int)zTest);
            _material.SetInt(ShaderPropertyIDs.S_Surface, surface);

            SetKeyword(ShaderPropertyIDs.AlphaTestKeyword, alphaTest);

            _material.renderQueue = (int)queue + QueueOffset + queueOffset;
        }

        private void SetKeyword(string keyword, bool enabled)
        {
            if (enabled) _material.EnableKeyword(keyword);
            else _material.DisableKeyword(keyword);
        }
    }

    /// <summary>
    /// 主ViewModel，负责驱动整个GUI的逻辑。
    /// </summary>
    public class ShaderGUIViewModel
    {
        private readonly Material _material;
        private readonly List<IPropertyProcessor> _processors;
        public RenderStateViewModel RenderStates { get; }
        public List<PropertyGroup> PropertyGroups { get; private set; }
        private readonly Dictionary<string, MaterialProperty> _toggleProperties = new Dictionary<string, MaterialProperty>();


        public ShaderGUIViewModel(Material material)
        {
            _material = material;
            RenderStates = new RenderStateViewModel(material);

            // 初始化并排序处理器链
            _processors = new List<IPropertyProcessor>
        {
            new GroupProcessor(),
            // new ToggleProcessor(),
            new IfProcessor(),
            new TexProcessor(),
            new DefaultProcessor()
        };
            _processors.Sort((a, b) => a.Order.CompareTo(b.Order));

            CollectAllToggleProperties();
            BuildPropertyGroups();
        }

        public bool IsKeywordEnabled(string keyword)
        {
            if (_toggleProperties.TryGetValue(keyword, out var prop))
            {
                return prop.floatValue > 0.5f;
            }
            return false;
        }


        /// <summary>
        /// 提取所有带有[Toggle]或[GroupToggle]特性的属性，并存储它们的关键字和对应的MaterialProperty。
        /// </summary>
        private void CollectAllToggleProperties()
        {
            _toggleProperties.Clear();

            // 获取当前材质的所有属性
            var properties = MaterialEditor.GetMaterialProperties(new[] { _material });
            for (int i = 0; i < properties.Length; i++)
            {
                var attrs = _material.shader.GetPropertyAttributes(i);
                var toggleAttr = attrs.FirstOrDefault(a => a.StartsWith("Toggle(") || a.StartsWith("GroupToggle("));
                if (toggleAttr != null)
                {
                    // 匹配括号()，并捕获括号内部的所有字符
                    Match match = Regex.Match(toggleAttr, @"\((.*)\)");
                    if (match.Success)
                    {
                        // 提取关键字
                        string keyword = match.Groups[1].Value.Trim();
                        if (!string.IsNullOrEmpty(keyword))
                        {
                            _toggleProperties[keyword] = properties[i];
                        }
                    }
                }
            }
        }

        public void ApplyToggleKeyword(string keyword, bool value)
        {
            if (string.IsNullOrEmpty(keyword)) return;
            if (value) _material.EnableKeyword(keyword);
            else _material.DisableKeyword(keyword);
        }

        private void BuildPropertyGroups()
        {
            var properties = MaterialEditor.GetMaterialProperties(new[] { _material });
            var context = new PropertyProcessContext(_material, this);

            for (int i = 0; i < properties.Length; i++)
            {
                var property = properties[i];
                context.Property = property;
                context.Attributes = _material.shader.GetPropertyAttributes(i);
                context.IsHandled = false;

                foreach (var processor in _processors)
                {
                    processor.Process(context);
                    if (context.IsHandled)
                        break;
                }
            }
            PropertyGroups = context.ResultGroups;
        }
    }
}

