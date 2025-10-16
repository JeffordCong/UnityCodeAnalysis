using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;


namespace Rendering.Editor
{
    /// <summary>
    /// ViewModel 基类。View将根据这个类型来选择绘制器。
    /// </summary>
    public abstract class BasePropertyViewModel
    {
        public MaterialProperty Property { get; }
        protected BasePropertyViewModel(MaterialProperty property)
        {
            Property = property;
        }
    }

    /*
        DefaultPropertyViewModel和TexPropertyViewModel这样的子类，其核心目的并非添加新功能，
        而是用它们自身的类型（Type）来告诉View（CustomShaderGUI）应该使用哪一种方式来绘制对应的Shader属性。
    */

    /// <summary>
    /// 默认属性的ViewModel。
    /// </summary>
    public class DefaultPropertyViewModel : BasePropertyViewModel
    {
        public DefaultPropertyViewModel(MaterialProperty property) : base(property) { }
    }

    /// <summary>
    /// [Tex]单行纹理属性。
    /// </summary>
    public class TexPropertyViewModel : BasePropertyViewModel
    {
        public TexPropertyViewModel(MaterialProperty property) : base(property) { }
    }


}