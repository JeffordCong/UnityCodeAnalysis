using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace Scarecrow
{
    public class SimpleShaderGUI : ShaderGUI, ISimpleShaderGUI
    {
        /// <summary>折叠页缩进等级</summary>
        public const int FoldoutIndent = 1;

        public ShaderGUIContext Context { get; } = new ShaderGUIContext();

        /// <summary>当前折叠页等级（描述属性绘制在哪级折叠页中）</summary>
        public int FoldoutLevel => Context.FoldoutState.Level;

        /// <summary>折叠页编辑等级</summary>
        public int FoldoutLevel_Editor => Context.FoldoutState.EditorLevel;

        /// <summary>折叠页状态：true=展开，false=折叠</summary>
        public bool FoldoutOpen => Context.FoldoutState.IsOpen;

        /// <summary>折叠页中的属性是否可编辑</summary>
        public bool FoldoutEditor => Context.FoldoutState.IsEditable;

        /// <summary>面板切换列表（Toggle/Enum 激活状态）</summary>
        public List<string> SwitchList => Context.SwitchList;

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            Context.Initialize(materialEditor, properties);

            DrawRenderStateGUI(Context.Material);

            for (int i = 0; i < properties.Length; i++)
            {
                if (!PropertyDrawHelper.IsFoldout(properties[i]))
                    EditorGUI.BeginDisabledGroup(!Context.FoldoutState.IsEditable);

                if (Context.FoldoutState.IsOpen || PropertyDrawHelper.IsFoldout(properties[i]))
                {
                    if ((properties[i].flags & MaterialProperty.PropFlags.HideInInspector) != MaterialProperty.PropFlags.HideInInspector)
                        materialEditor.ShaderProperty(properties[i], properties[i].displayName);
                }

                if (!PropertyDrawHelper.IsFoldout(properties[i]))
                    EditorGUI.EndDisabledGroup();
            }

            EditorGUILayout.Space(20);
            EditorGUILayout.LabelField("Additional Settings", EditorStyles.boldLabel);
            materialEditor.DoubleSidedGIField();
            materialEditor.RenderQueueField();
            materialEditor.EnableInstancingField();
        }

        /// <summary>
        /// 设置折叠页状态
        /// </summary>
        public void SetFoldout(int level, int editorLevel, bool isOpen, bool isEditable = true)
        {
            // 更新编辑器的缩进等级
            int indentDiff = level - Context.FoldoutState.Level;
            EditorGUI.indentLevel += indentDiff * FoldoutIndent;

            // 更新折叠页状态
            Context.FoldoutState.Set(level, editorLevel, isOpen, isEditable);
        }

        /// <summary>
        /// 判断属性是否应该显示
        /// 只有包含在 SwitchList 中的选项才会显示
        /// </summary>
        public bool GetShowState(string[] showList)
        {
            if (showList == null || showList.Length == 0)
                return true;

            foreach (string option in showList)
            {
                if (SwitchList.Contains(option))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 查找一个材质属性
        /// </summary>
        public MaterialProperty FindProperty(string name)
        {
            return Context.FindProperty(name);
        }


        private void DrawRenderStateGUI(Material material)
        {
            var enableRenderStatesProp = FindProperty(ShaderPropertyIDs.S_EnableRenderStates);
            if (enableRenderStatesProp == null)
                return;

            // 读取缓存值
            var blendModeIndexProp = FindProperty(ShaderPropertyIDs.S_BlendModeIndex);
            var cullIndexProp = FindProperty(ShaderPropertyIDs.S_CullIndex);
            var queueOffsetProp = FindProperty(ShaderPropertyIDs.S_QueueOffsetValue);
            var alphaClipProp = FindProperty("_AlphaClip");

            bool isEnabled = Context.RenderStateManager.DrawUI(Context.MaterialEditor, enableRenderStatesProp, blendModeIndexProp, cullIndexProp, queueOffsetProp, alphaClipProp);

            if (isEnabled)
            {
                Context.RenderStateManager.ApplyToMaterial(material);
            }
        }

    }
}
