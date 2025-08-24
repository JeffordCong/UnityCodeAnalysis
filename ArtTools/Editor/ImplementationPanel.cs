using UnityEditor;
using UnityEngine;

namespace CustomEditorTools
{
    /// <summary>
    /// 右侧实现面板，只负责展示和切换当前功能实现
    /// </summary>
    public class ImplementationPanel
    {
        private FunctionImplementation currentImplementation;

        public void DrawGUI()
        {
            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("实现区域", EditorStyles.boldLabel);
            if (currentImplementation == null)
            {
                GUILayout.Label("请选择一个功能。", EditorStyles.wordWrappedLabel);
            }
            else
            {
                currentImplementation.DrawGUI();
            }
            GUILayout.EndVertical();
        }

        public void SetSelectedFunction(ArtEditorWindow.FunctionInfo? functionInfo)
        {
            currentImplementation?.Dispose();
            currentImplementation = null;

            if (functionInfo.HasValue)
            {
                currentImplementation = (FunctionImplementation)System.Activator.CreateInstance(functionInfo.Value.Type);
                currentImplementation?.Initialize();
            }
        }

        public void Dispose()
        {
            currentImplementation?.Dispose();
            currentImplementation = null;
        }
    }

    /// <summary>
    /// 左侧功能按钮区，动态生成所有分组下功能按钮，支持自定义显示名
    /// </summary>
    public class DynamicFunctionPanel
    {
        private readonly ArtEditorWindow.FunctionInfo[] functions;
        private readonly ImplementationPanel implementationPanel;

        public DynamicFunctionPanel(ArtEditorWindow.FunctionInfo[] functions, ImplementationPanel panel)
        {
            this.functions = functions;
            this.implementationPanel = panel;
        }

        public void DrawGUI()
        {
            GUILayout.BeginVertical();
            GUILayout.Label("功能列表", EditorStyles.boldLabel);
            foreach (var func in functions)
            {
                string btnName = string.IsNullOrEmpty(func.DisplayName) ? func.Name : func.DisplayName;
                if (GUILayout.Button(btnName, GUILayout.Height(32)))
                {
                    implementationPanel.SetSelectedFunction(func);
                }
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
        }

        public void Dispose() { }
    }
}