using UnityEditor;
using UnityEngine;

namespace Scarecrow
{
    /// <summary>
    /// 所有 PropertyDrawer 的基类
    /// 提供统一的验证和初始化逻辑，减少代码重复
    /// </summary>
    public abstract class PropertyDrawerBase : MaterialPropertyDrawer
    {
        /// <summary>当前操作的 SimpleShaderGUI 实例</summary>
        protected ISimpleShaderGUI ShaderGUI { get; private set; }

        /// <summary>
        /// 验证并获取 SimpleShaderGUI
        /// 如果验证失败会显示错误信息
        /// </summary>
        protected bool TryGetShaderGUI(MaterialEditor editor, out ISimpleShaderGUI shaderGUI)
        {
            shaderGUI = editor.customShaderGUI as ISimpleShaderGUI;
            return shaderGUI != null;
        }

        /// <summary>
        /// 验证属性类型
        /// </summary>
        protected bool ValidatePropertyType(MaterialProperty property, MaterialProperty.PropType expectedType, string errorMessage)
        {
            if (property.type != expectedType)
            {
                GUILayout.Label($"{property.displayName} : {errorMessage}");
                return false;
            }
            return true;
        }

        /// <summary>
        /// 验证是否应该显示该属性
        /// 基于 showList 和 ShaderGUI 的 SwitchList
        /// </summary>
        protected bool ShouldShowProperty(string[] showList)
        {
            if (ShaderGUI == null)
                return true;

            // 如果没有指定 showList，始终显示
            if (showList == null || showList.Length == 0)
                return true;

            // 检查 showList 中是否有任何一个被激活
            return ShaderGUI.GetShowState(showList);
        }

        /// <summary>
        /// 显示使用错误信息
        /// </summary>
        protected void ShowUsageError(MaterialProperty property, string message)
        {
            GUILayout.Label($"{property.displayName} : {message}");
        }

        /// <summary>
        /// 缓存 ShaderGUI 引用，所有 OnGUI 方法开始时应调用此方法
        /// </summary>
        protected void CacheShaderGUI(MaterialEditor editor)
        {
            ShaderGUI = editor.customShaderGUI as ISimpleShaderGUI;
        }

        /// <summary>
        /// 检查属性是否有混合值（多选材质时不同值）
        /// </summary>
        protected bool HasMixedValue(MaterialProperty property)
        {
            return property.hasMixedValue;
        }

        /// <summary>
        /// 获取展示混合值的状态
        /// </summary>
        protected void SetMixedValueDisplay(bool show)
        {
            EditorGUI.showMixedValue = show;
        }

        /// <summary>
        /// 清除混合值显示
        /// </summary>
        protected void ClearMixedValueDisplay()
        {
            EditorGUI.showMixedValue = false;
        }
    }
}
