

using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;


namespace Rendering.Editor
{

    /// <summary>
    /// 属性处理器接口。每个实现都负责一种特定的Shader特性。
    /// </summary>
    public interface IPropertyProcessor
    {
        int Order { get; } // 用于控制处理器的执行顺序
        void Process(PropertyProcessContext context);
    }


    public class GroupProcessor : IPropertyProcessor
    {
        public int Order => 0;

        public void Process(PropertyProcessContext context)
        {
            var groupToggleKeyword = context.Attributes.FirstOrDefault(a => a.StartsWith("GroupToggle("));
            var groupBaseName = context.Attributes.FirstOrDefault(a => a.StartsWith("GroupBase("));

            if (groupToggleKeyword == null && groupBaseName == null)
                return;

            context.CurrentGroup = new PropertyGroup(context.Property);
            context.ResultGroups.Add(context.CurrentGroup);
            context.IsHandled = true;
        }
    }


    public class ToggleProcessor : IPropertyProcessor
    {
        public int Order => 10;
        public void Process(PropertyProcessContext context)
        {
            if (context.Attributes.Any(a => a.StartsWith("Toggle(")))
            {
                context.IsHandled = true;
            }
        }
    }


    public class TexProcessor : IPropertyProcessor
    {
        public int Order => 20;

        public void Process(PropertyProcessContext context)
        {
            if (!context.Attributes.Any(a => a == "Tex"))
                return;

            EnsureGroupExists(context);
            var vm = new TexPropertyViewModel(context.Property);
            context.CurrentGroup.Properties.Add(vm);
            context.IsHandled = true;
        }

        private void EnsureGroupExists(PropertyProcessContext context)
        {
            if (context.CurrentGroup == null)
            {
                context.CurrentGroup = new PropertyGroup(null); // 创建一个无头分组
                context.ResultGroups.Add(context.CurrentGroup);
            }
        }
    }

    /// 统一处理 [If(KEYWORD)] 特性。
    /// 职责1: 根据关键字状态决定属性是否可见。
    /// 职责2: 判断属性是否属于当前激活的ToggleGroup，如果不属于则关闭该组。
    /// </summary>
    public class IfProcessor : IPropertyProcessor
    {
        public int Order => 15;

        public void Process(PropertyProcessContext context)
        {
            var ifAttribute = context.Attributes.FirstOrDefault(a => a.StartsWith("If("));
            string keyword = null;

            if (ifAttribute != null)
            {
                Match match = Regex.Match(ifAttribute, @"\((.*)\)");
                if (match.Success)
                {
                    keyword = match.Groups[1].Value.Trim();
                }
            }


            if (context.CurrentGroup != null && context.CurrentGroup.IsToggleGroup)
            {
                // 如果属性的[If]关键字与组的关键字匹配...
                if (keyword == context.CurrentGroup.GroupToggleKeyword)
                {
                    // ...则根据开关状态决定是否隐藏它
                    if (!context.ViewModel.IsKeywordEnabled(keyword))
                    {
                        context.IsHandled = true; // 隐藏属性
                    }
                    // 如果开关开启，则什么都不做，让它正常流向下一个处理器
                    return; // 无论隐藏还是显示，此处理器的任务都已完成
                }
                else
                {
                    // 如果关键字不匹配或属性没有[If]，则ToggleGroup结束
                    context.CurrentGroup = null;
                    // 注意：这里不设置IsHandled，让属性在新的（无头）分组中被后续处理器处理
                    return;
                }
            }

            // --- 逻辑分支 2: 不在ToggleGroup内，但有独立的[If]特性 ---
            if (keyword != null)
            {
                if (!context.ViewModel.IsKeywordEnabled(keyword))
                {
                    context.IsHandled = true; // 隐藏属性
                }
            }
        }
    }


    /// <summary>
    /// 处理器(Order 100): 最后的默认处理器，处理所有未被处理的普通属性。
    /// </summary>
    public class DefaultProcessor : IPropertyProcessor
    {
        public int Order => 100;

        public void Process(PropertyProcessContext context)
        {
            if (context.IsHandled) return;

            // 跳过被渲染状态模块管理的属性
            if (RenderStateViewModel.IsRenderStateProperty(context.Property.name)) return;

            // 跳过隐藏属性
            if ((context.Property.flags & MaterialProperty.PropFlags.HideInInspector) != 0) return;

            EnsureGroupExists(context);
            var vm = new DefaultPropertyViewModel(context.Property);
            context.CurrentGroup.Properties.Add(vm);
            context.IsHandled = true;
        }

        private void EnsureGroupExists(PropertyProcessContext context)
        {
            if (context.CurrentGroup == null)
            {
                context.CurrentGroup = new PropertyGroup(null); // 创建一个无头分组
                context.ResultGroups.Add(context.CurrentGroup);
            }
        }
    }
}

