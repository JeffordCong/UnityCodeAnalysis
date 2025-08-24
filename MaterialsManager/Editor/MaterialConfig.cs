using UnityEngine;

namespace MyEditor.MaterialSystem
{
    public abstract class MaterialConfig : ScriptableObject
    {
        /// <summary>
        /// 用于下拉菜单显示
        /// </summary>
        public abstract string DisplayName { get; }

        /// <summary>
        /// 返回本Config对应的Shader
        /// </summary>
        public abstract Shader GetShader();

        /// <summary>
        /// 应用所有贴图、参数到目标材质，并自动收集实际用到的变体
        /// </summary>
        public virtual void ApplyToMaterial(Material mat)
        {
            if (mat == null) return;
            mat.shader = GetShader();


            // 自动收集所有实际启用的Shader Keyword,不需要在子类实现
            if (mat.shaderKeywords != null)
            {
                foreach (string kw in mat.shaderKeywords)
                {
                    if (!string.IsNullOrEmpty(kw))
                        ShaderVariantCollector.AddKeyword(kw);
                }
            }
        }

        /// <summary>
        /// 子类应实现，批量设置所有贴图的导入参数
        /// </summary>
        public virtual void ApplyAllImportSettings()
        {
        }
    }
}