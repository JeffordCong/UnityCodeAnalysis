using System;
using UnityEditor;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

namespace CustomEditorTools
{
    public class ArtEditorWindow : EditorWindow
    {
        private EditorViewModel viewModel;
        private Vector2 implementationScrollPos;

        [MenuItem("Tools/美术编辑器")]
        public static void ShowWindow()
        {
            var window = GetWindow<ArtEditorWindow>("美术编辑器");
            window.minSize = new Vector2(981, 564);
            window.maxSize = new Vector2(981, 564);
        }

        private void OnEnable()
        {
            viewModel = new EditorViewModel();
        }

        private void OnGUI()
        {
            // 顶部类目栏
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            string[] categories = viewModel.GetCategories();
            for (int i = 0; i < categories.Length; i++)
            {
                bool isSelected = (i == viewModel.SelectedCategory);
                if (GUILayout.Toggle(isSelected, categories[i], EditorStyles.toolbarButton) && !isSelected)
                {
                    viewModel.SetSelectedCategory(i);
                }
            }
            GUILayout.EndHorizontal();

            // 主布局
            GUILayout.BeginHorizontal(GUILayout.ExpandHeight(true));

            // 左侧功能栏
            GUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(300), GUILayout.ExpandHeight(true));
            viewModel.FunctionPanel?.DrawGUI();
            GUILayout.EndVertical();

            // 右侧实现栏（支持滚动）
            GUILayout.BeginVertical(GUILayout.ExpandHeight(true));
            implementationScrollPos = GUILayout.BeginScrollView(implementationScrollPos);
            viewModel.ImplementationPanel?.DrawGUI();
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }

        private void OnDisable()
        {
            viewModel?.Dispose();
        }

        // ViewModel
        private class EditorViewModel
        {
            private readonly string[] categories = { "TA", "场景", "角色" };
            public int SelectedCategory { get; private set; } = 0;
            public DynamicFunctionPanel FunctionPanel { get; private set; }
            public ImplementationPanel ImplementationPanel { get; private set; }

            public EditorViewModel()
            {
                FunctionManager.Initialize();
                ImplementationPanel = new ImplementationPanel();
                UpdateFunctionPanel();
            }

            public void SetSelectedCategory(int index)
            {
                if (index != SelectedCategory)
                {
                    SelectedCategory = index;
                    UpdateFunctionPanel();
                }
            }

            public string[] GetCategories() => categories;

            private void UpdateFunctionPanel()
            {
                FunctionPanel?.Dispose();
                ImplementationPanel.SetSelectedFunction(null); // 清空实现区域
                string category = categories[SelectedCategory];
                FunctionInfo[] functions = FunctionManager.GetFunctions(category);
                FunctionPanel = new DynamicFunctionPanel(functions, ImplementationPanel);
            }

            public void Dispose()
            {
                FunctionPanel?.Dispose();
                ImplementationPanel?.Dispose();
            }
        }

        // 功能管理
        private static class FunctionManager
        {
            // 存放所有分类的功能列表。Key是分类名，Value是该分类下的所有功能列表。
            private static Dictionary<string, List<FunctionInfo>> functionMap = new Dictionary<string, List<FunctionInfo>>();

            // 初始化，查找并收集所有功能类
            public static void Initialize()
            {
                functionMap.Clear();
                var types = typeof(FunctionImplementation).Assembly.GetTypes()
                    .Where(t => t.IsSubclassOf(typeof(FunctionImplementation)) && !t.IsAbstract);

                foreach (var type in types)
                {
                    var attribute = (FunctionCategoryAttribute)Attribute.GetCustomAttribute(type, typeof(FunctionCategoryAttribute));
                    if (attribute != null)
                    {
                        string category = attribute.Category;
                        if (!functionMap.ContainsKey(category))
                            functionMap[category] = new List<FunctionInfo>();

                        functionMap[category].Add(new FunctionInfo
                        {
                            Name = type.Name.Replace("Implementation", ""),
                            DisplayName = attribute.DisplayName,
                            Type = type,
                            Order = attribute.Order,
                            HasOrder = attribute.HasOrder
                        });
                    }
                }
                // 排序：Order优先，然后按DisplayName
                var categories = functionMap.Keys.ToList();
                foreach (var category in categories)
                {
                    functionMap[category] = functionMap[category]
                        .OrderBy(f => f.HasOrder ? 0 : 1)
                        .ThenBy(f => f.HasOrder ? f.Order : int.MaxValue)
                        .ThenBy(f => f.DisplayName ?? f.Name)
                        .ToList();
                }
            }
            public static FunctionInfo[] GetFunctions(string category)
            {
                return functionMap.TryGetValue(category, out List<FunctionInfo> functions) ? functions.ToArray() : new FunctionInfo[0];
            }
        }

        public struct FunctionInfo
        {
            public string Name;
            public Type Type;
            public int Order;
            public bool HasOrder;
            public string DisplayName; // 新增
        }
    }
}