using UnityEditor;
using UnityEngine;

namespace Scarecrow
{
    /// <summary>
    /// 纹理属性绘制器
    /// 支持绘制额外属性（如颜色）并自动处理纹理缩放和偏移
    /// </summary>
    public class TexDrawer : PropertyDrawerBase
    {
        /// <summary>额外属性名称（如颜色）</summary>
        private string _additionalPropertyName;

        /// <summary>该控件会在以下选项中显示</summary>
        private string[] _showList = new string[0];

        /// <summary>是否总是显示</summary>
        private bool _isAlwaysShow = true;

        /// <summary>
        /// 纹理绘制器构造函数
        /// 参数说明：
        /// - additionalPropertyName: 在纹理后面绘制的额外属性（如 _Color）
        /// - showList: 条件显示列表，当任意选项被激活时显示该属性
        /// </summary>
        public TexDrawer() : this("") { }

        public TexDrawer(string additionalPropertyName, params string[] showList)
        {
            _additionalPropertyName = additionalPropertyName;
            _showList = showList ?? new string[0];
            _isAlwaysShow = _showList.Length == 0;
        }

        public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
        {
            CacheShaderGUI(editor);

            if (ShaderGUI == null)
                return base.GetPropertyHeight(prop, label, editor);

            // 检查属性是否要被绘制
            if (!(_isAlwaysShow || ShaderGUI.GetShowState(_showList)))
                return -2f;

            // 检查是否有额外属性
            MaterialProperty additionalProp = ShaderGUI.FindProperty(_additionalPropertyName);
            if (additionalProp != null)
                return -1.5f;
            else
                return base.GetPropertyHeight(prop, label, editor);
        }

        public override void OnGUI(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
        {
            CacheShaderGUI(editor);

            // 验证 ShaderGUI
            if (ShaderGUI == null)
            {
                ShowUsageError(prop, "Please use SimpleShaderGUI in your shader");
                return;
            }

            // 验证属性类型
            if (!ValidatePropertyType(prop, MaterialProperty.PropType.Texture, "Property must be of type texture"))
                return;

            // 检查属性是否要被绘制
            if (!(_isAlwaysShow || ShaderGUI.GetShowState(_showList)))
                return;

            // 获取额外属性并绘制
            MaterialProperty additionalProp = ShaderGUI.FindProperty(_additionalPropertyName);
            if (additionalProp != null)
            {
                float labelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = labelWidth + 2f;
                editor.TexturePropertySingleLine(label, prop, additionalProp);
                EditorGUIUtility.labelWidth = labelWidth;
            }
            else
            {
                editor.TexturePropertyMiniThumbnail(position, prop, label.text, "");
            }

            // 绘制纹理缩放和偏移
            if (prop.flags != MaterialProperty.PropFlags.NoScaleOffset)
            {
                EditorGUI.indentLevel++;
                SetMixedValueDisplay(HasMixedValue(prop));
                editor.TextureScaleOffsetProperty(prop);
                ClearMixedValueDisplay();
                EditorGUI.indentLevel--;
            }
        }
    }
}
