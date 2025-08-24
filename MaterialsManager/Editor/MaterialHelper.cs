using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace MyEditor.MaterialSystem
{
    /// <summary>
    /// 材质辅助工具类，提供关键字、属性、Pass等批量操作方法
    /// </summary>
    internal static class MaterialHelper
    {
        /// <summary>
        /// 获取Shader所有可用关键字（通过反射调用Unity内部API）
        /// </summary>
        private static string[] GetShaderKeywords(Shader shader)
        {
            List<string> selectedKeywords = new List<string>();
            string[] keywordLists = null, remainingKeywords = null;
            int[] filteredVariantTypes = null;
            var svc = new ShaderVariantCollection();
            MethodInfo getShaderVariantEntries = typeof(ShaderUtil).GetMethod(
                "GetShaderVariantEntriesFiltered",
                BindingFlags.NonPublic | BindingFlags.Static
            );
            object[] args = new object[] {
                shader,
                256,
                selectedKeywords.ToArray(),
                svc,
                filteredVariantTypes,
                keywordLists,
                remainingKeywords
            };
            getShaderVariantEntries.Invoke(null, args);

            // 解析返回参数
            // int[] passTypes = args[4] as int[];
            // string[] keywordArry = args[5] as string[];
            string[] remainingKeywordsArry = args[6] as string[];

            return remainingKeywordsArry;
        }

        /// <summary>
        /// 判断材质是否包含指定关键字
        /// </summary>
        private static bool HasKeyword(Material mat, string keyword)
        {
            var shaderKeywords = GetShaderKeywords(mat.shader);
            foreach (var shaderKeyword in shaderKeywords)
            {
                if (shaderKeyword == keyword)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 判断材质是否包含指定Pass
        /// </summary>
        private static bool HasPass(Material mat, string passName)
        {
            var passCount = mat.passCount;
            for (int i = 0; i < passCount; i++)
            {
                if (mat.GetPassName(i) == passName)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 判断材质是否包含指定属性
        /// </summary>
        private static bool HasProperty(Material mat, string property)
        {
            var count = ShaderUtil.GetPropertyCount(mat.shader);
            for (int i = 0; i < count; i++)
            {
                if (ShaderUtil.GetPropertyName(mat.shader, i) == property)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 启用/禁用Shader关键字
        /// </summary>
        private static void EnableKeyword(Material mat, string keyword, bool enable)
        {
            if (!HasKeyword(mat, keyword))
                return;

            var old = mat.IsKeywordEnabled(keyword);
            if (old == enable)
                return;

            if (enable)
                mat.EnableKeyword(keyword);
            else
                mat.DisableKeyword(keyword);

            EditorUtility.SetDirty(mat);
        }

        /// <summary>
        /// 启用/禁用Shader Pass
        /// </summary>
        private static void SetShaderPassEnabled(Material mat, string passName, bool enabled)
        {
            if (!HasPass(mat, passName))
                return;

            var old = mat.GetShaderPassEnabled(passName);
            if (old == enabled)
                return;

            mat.SetShaderPassEnabled(passName, enabled);
            EditorUtility.SetDirty(mat);
        }

        /// <summary>
        /// 设置int属性并切换关键字组（枚举型关键字）
        /// </summary>
        internal static void ToggleKeyWordEnum(Material mat, int keywordIndex, string[] keywords, string property)
        {
            SetInt(mat, property, keywordIndex);
            for (int i = 0; i < keywords.Length; i++)
            {
                var enable = keywordIndex == i;
                var keyword = keywords[i];
                EnableKeyword(mat, keyword, enable);
            }
        }

        /// Kyword名和属性名相同的情况
        /// 通过是否存在贴图来设置关键字 
        internal static void SetPropKeywordByTex(Material mat, string propKeyword, Texture2D tex)
        {
            bool enable = tex == null ? false : true;
            int value = enable ? 1 : 0;

            SetInt(mat, propKeyword, value);
            EnableKeyword(mat, propKeyword, enable);
        }

        internal static void ToggleKeyWord(Material mat, bool enable, string keyword, string property)
        {
            SetInt(mat, property, enable ? 1 : 0);
            EnableKeyword(mat, keyword, enable);
        }

        internal static void ToggleKeyWordOff(Material mat, bool enable, string keyword, string property)
        {
            SetInt(mat, property, enable ? 1 : 0);
            EnableKeyword(mat, keyword, !enable);
        }

        /// <summary>
        /// 设置int属性并切换Shader Pass
        /// </summary>
        internal static void TogglePass(Material mat, bool enable, string property, string pass)
        {
            SetInt(mat, property, enable ? 1 : 0);
            SetShaderPassEnabled(mat, pass, enable);
        }

        /// <summary>
        /// 设置材质的int属性
        /// </summary>
        public static void SetInt(Material mat, string property, int value)
        {
            if (!HasProperty(mat, property))
                return;

            var old = mat.GetInt(property);
            if (old == value)
                return;

            mat.SetInt(property, value);
            EditorUtility.SetDirty(mat);
        }

        /// <summary>
        /// 设置材质的float属性
        /// </summary>
        public static void SetFloat(Material mat, string property, float value)
        {
            if (!HasProperty(mat, property))
                return;

            var old = mat.GetFloat(property);
            if (old == value)
                return;

            mat.SetFloat(property, value);
            EditorUtility.SetDirty(mat);
        }

        /// <summary>
        /// 设置材质的贴图属性
        /// </summary>
        internal static void SetTexture(Material mat, string property, Texture tex, bool keepWhenNull = false)
        {
            if (!mat.HasProperty(property))
                return;

            if (mat.GetTexture(property) == tex)
                return;

            if (keepWhenNull && tex == null)
                return;

            mat.SetTexture(property, tex);
            EditorUtility.SetDirty(mat);
        }

        /// <summary>
        /// 设置材质的贴图缩放
        /// </summary>
        internal static void SetTextureScale(Material mat, string property, Vector2 scale)
        {
            if (!mat.HasProperty(property))
                return;

            var old = mat.GetTextureScale(property);
            if (old == scale)
                return;

            mat.SetTextureScale(property, scale);
            EditorUtility.SetDirty(mat);
        }
    }
}