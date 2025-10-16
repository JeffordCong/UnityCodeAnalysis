
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Rendering.Editor
{

    /// <summary>
    /// 数据封装和状态管理
    /// </summary>
    public class PropertyProcessContext
    {
        public Material Material { get; }
        public MaterialProperty Property { get; set; }
        public string[] Attributes { get; set; }
        public List<PropertyGroup> ResultGroups { get; }
        public PropertyGroup CurrentGroup { get; set; }
        public bool IsHandled { get; set; }
        public ShaderGUIViewModel ViewModel { get; }


        public PropertyProcessContext(Material material, ShaderGUIViewModel viewModel)
        {
            Material = material;
            ViewModel = viewModel;
            ResultGroups = new List<PropertyGroup>();
        }
    }
}

