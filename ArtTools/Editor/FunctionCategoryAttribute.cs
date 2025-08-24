using UnityEditor;
using System;

namespace CustomEditorTools
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class FunctionCategoryAttribute : Attribute
    {
        public string Category { get; }
        public string DisplayName { get; }
        public int Order { get; }
        public bool HasOrder { get; }

        // 分类+显示名
        public FunctionCategoryAttribute(string category, string displayName)
        {
            Category = category;
            DisplayName = displayName;
            Order = 0;
            HasOrder = false;
        }

        // 分类+显示名+顺序
        public FunctionCategoryAttribute(string category, string displayName, int order)
        {
            Category = category;
            DisplayName = displayName;
            Order = order;
            HasOrder = true;
        }

    }
    public abstract class FunctionImplementation :EditorWindow
    {
        public abstract void DrawGUI();
        public virtual void Initialize() { }
        public virtual void Dispose() { }
    }
}