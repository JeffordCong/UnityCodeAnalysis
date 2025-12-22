using UnityEditor;
using UnityEngine;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace Scarecrow
{
    public static class ShaderPropertyIDs
    {
        public const string S_EnableRenderStates = "_EnableRenderStates";

        public const string S_BlendModeIndex = "_BlendModeIndex";
        public const string S_CullIndex = "_CullIndex";
        public const string S_QueueOffsetValue = "_QueueOffsetValue";
        public const string S_SrcBlend = "_SrcBlend";
        public const string S_DstBlend = "_DstBlend";
        public const string S_ZWrite = "_ZWrite";
        public const string S_ZTest = "_ZTest";
        public const string S_AlphaClip = "_AlphaClip";

        public const string S_Cull = "_Cull";


        public const string S_AlphaClipKeyword = "_ALPHATEST_ON";
    }


    /// <summary>
    /// 属性绘制工具类
    /// 提供折叠页检测、属性查询、批量操作等工具方法
    /// </summary>
    public static class PropertyDrawHelper
    {
        /// <summary>
        /// 折叠页标记
        /// 在折叠页属性的显示名字后面务必添加此标记，用于标识该属性为折叠页
        /// 其他属性务必不要添加
        /// </summary>
        public const string FoldoutSign = "_Foldout";

        /// <summary>缓存的折叠页 Regex，用于性能优化</summary>
        private static readonly Regex FoldoutRegex = new(FoldoutSign + @"\s*$");

        #region 折叠页检测

        /// <summary>
        /// 判断属性是否是折叠页
        /// 通过检查显示名称末尾是否包含 _Foldout 标记
        /// </summary>
        public static bool IsFoldout(MaterialProperty property)
        {
            if (property == null)
                return false;
            return FoldoutRegex.IsMatch(property.displayName);
        }

        /// <summary>
        /// 获取折叠页的显示名称（移除 _Foldout 标记）
        /// </summary>
        public static string GetFoldoutDisplayName(MaterialProperty property)
        {
            if (property == null)
                return string.Empty;
            return FoldoutRegex.Replace(property.displayName, "");
        }

        #endregion

        #region 批量操作

        /// <summary>
        /// 检查属性数组中是否存在任何折叠页
        /// </summary>
        public static bool HasAnyFoldout(MaterialProperty[] properties)
        {
            if (properties == null || properties.Length == 0)
                return false;

            foreach (var prop in properties)
            {
                if (IsFoldout(prop))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 获取所有折叠页属性
        /// </summary>
        public static List<MaterialProperty> GetFoldoutProperties(MaterialProperty[] properties)
        {
            var result = new List<MaterialProperty>();
            if (properties == null || properties.Length == 0)
                return result;

            foreach (var prop in properties)
            {
                if (IsFoldout(prop))
                    result.Add(prop);
            }
            return result;
        }

        /// <summary>
        /// 获取所有非折叠页属性
        /// </summary>
        public static List<MaterialProperty> GetNonFoldoutProperties(MaterialProperty[] properties)
        {
            var result = new List<MaterialProperty>();
            if (properties == null || properties.Length == 0)
                return result;

            foreach (var prop in properties)
            {
                if (!IsFoldout(prop))
                    result.Add(prop);
            }
            return result;
        }

        #endregion

        #region 属性查询

        /// <summary>
        /// 查找指定名称的属性
        /// </summary>
        public static MaterialProperty FindPropertyByName(string name, MaterialProperty[] properties)
        {
            if (string.IsNullOrEmpty(name) || properties == null || properties.Length == 0)
                return null;

            foreach (var prop in properties)
            {
                if (prop != null && prop.name == name)
                    return prop;
            }
            return null;
        }

        /// <summary>
        /// 查找指定类型的第一个属性
        /// </summary>
        public static MaterialProperty FindPropertyByType(MaterialProperty.PropType type,
                                                          MaterialProperty[] properties)
        {
            if (properties == null || properties.Length == 0)
                return null;

            foreach (var prop in properties)
            {
                if (prop.type == type)
                    return prop;
            }
            return null;
        }

        /// <summary>
        /// 获取指定类型的所有属性
        /// </summary>
        public static List<MaterialProperty> GetPropertiesByType(MaterialProperty.PropType type,
                                                                  MaterialProperty[] properties)
        {
            var result = new List<MaterialProperty>();
            if (properties == null || properties.Length == 0)
                return result;

            foreach (var prop in properties)
            {
                if (prop.type == type)
                    result.Add(prop);
            }
            return result;
        }

        #endregion

        #region 属性验证

        /// <summary>
        /// 验证属性是否隐藏在检查器中
        /// </summary>
        public static bool IsHiddenInInspector(MaterialProperty property)
        {
            if (property == null)
                return true;

            return (property.flags & MaterialProperty.PropFlags.HideInInspector) ==
                   MaterialProperty.PropFlags.HideInInspector;
        }

        /// <summary>
        /// 验证属性是否支持缩放和偏移
        /// </summary>
        public static bool SupportsScaleOffset(MaterialProperty property)
        {
            if (property == null)
                return false;

            return property.flags != MaterialProperty.PropFlags.NoScaleOffset;
        }

        /// <summary>
        /// 验证是否为有效的材质属性
        /// </summary>
        public static bool IsValidProperty(MaterialProperty property)
        {
            return property != null && !string.IsNullOrEmpty(property.name);
        }

        #endregion

        #region 属性操作

        /// <summary>
        /// 获取属性的显示名称（没有下划线前缀）
        /// </summary>
        public static string GetCleanDisplayName(MaterialProperty property)
        {
            if (property == null)
                return string.Empty;

            string displayName = property.displayName;

            // 移除折叠页标记
            if (IsFoldout(property))
                displayName = GetFoldoutDisplayName(property);

            return displayName;
        }


        public static void SetMaterialProp_Int(Material material, string propertyName, int value)
        {
            if (material != null && material.HasProperty(propertyName))
            {
                material.SetInt(propertyName, value);
            }
        }
        /// <summary>
        /// 在材质上设置属性的浮点值
        /// </summary>
        public static void SetMaterialProp_Float(Material material, string propertyName, float value)
        {
            if (material != null && material.HasProperty(propertyName))
            {
                material.SetFloat(propertyName, value);
            }
        }

        /// <summary>
        /// 在材质上设置属性的颜色值
        /// </summary>
        public static void SetMatetialProp_Color(Material material, string propertyName, Color value)
        {
            if (material != null && material.HasProperty(propertyName))
            {
                material.SetColor(propertyName, value);
            }
        }

        /// <summary>
        /// 在材质上设置属性的纹理值
        /// </summary>
        public static void SetMaterialProp_Texture(Material material, string propertyName, Texture value)
        {
            if (material != null && material.HasProperty(propertyName))
            {
                material.SetTexture(propertyName, value);
            }
        }

        #endregion

        #region 关键字管理

        /// <summary>
        /// 生成标准的 Shader 关键字名称（属性名_ON）
        /// </summary>
        public static string GenerateKeyword(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
                return string.Empty;

            return propertyName.ToUpperInvariant() + "_ON";
        }

        /// <summary>
        /// 生成 Enum 类型的关键字名称（属性名_选项名）
        /// </summary>
        public static string GenerateEnumKeyword(string propertyName, string optionName)
        {
            if (string.IsNullOrEmpty(propertyName) || string.IsNullOrEmpty(optionName))
                return string.Empty;

            return (propertyName + "_" + optionName).ToUpperInvariant();
        }

        #endregion
    }
}
