using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Scarecrow
{
    /// <summary>
    /// 材质关键字管理器
    /// 统一处理所有的关键字设置逻辑，避免重复代码
    /// </summary>
    public static class KeywordManager
    {
        /// <summary>
        /// 设置 Toggle 关键字
        /// 属性名 + "_ON" 作为关键字名
        /// </summary>
        public static void SetToggleKeyword(MaterialProperty property, bool enable)
        {
            string keyword = GetKeywordFromPropertyName(property.name);
            SetKeyword(property, keyword, enable);
        }

        /// <summary>
        /// 设置 Enum 关键字
        /// 格式为：属性名_选项名（全大写）
        /// 只有选中的选项才启用其关键字
        /// </summary>
        public static void SetEnumKeyword(MaterialProperty property, string[] enumOptions, int selectedIndex)
        {
            if (selectedIndex < 0 || selectedIndex >= enumOptions.Length)
            {
                Debug.LogWarning($"[KeywordManager] Invalid enum index {selectedIndex} for {property.name}");
                return;
            }

            foreach (Material material in property.targets)
            {
                foreach (string option in enumOptions)
                {
                    string keyword = GetEnumKeyword(property.name, option);
                    if (option == enumOptions[selectedIndex])
                        material.EnableKeyword(keyword);
                    else
                        material.DisableKeyword(keyword);
                }
            }
        }

        /// <summary>
        /// 设置折叠页编辑权限关键字
        /// 格式为：属性名_ON
        /// </summary>
        public static void SetFoldoutEditorKeyword(MaterialProperty property, bool isEditable)
        {
            string keyword = GetKeywordFromPropertyName(property.name);
            SetKeyword(property, keyword, isEditable);
        }

        /// <summary>
        /// 统一的关键字设置方法
        /// </summary>
        private static void SetKeyword(MaterialProperty property, string keyword, bool enable)
        {
            foreach (Material material in property.targets)
            {
                if (enable)
                    material.EnableKeyword(keyword);
                else
                    material.DisableKeyword(keyword);
            }
        }

        /// <summary>根据属性名生成关键字名</summary>
        public static string GetKeywordFromPropertyName(string propertyName)
        {
            return propertyName.ToUpperInvariant() + "_ON";
        }

        /// <summary>根据属性名和选项生成 Enum 关键字</summary>
        public static string GetEnumKeyword(string propertyName, string optionName)
        {
            return (propertyName + "_" + optionName).ToUpperInvariant();
        }
    }
}
