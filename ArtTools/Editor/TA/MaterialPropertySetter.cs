using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace CustomEditorTools.TA
{
    [FunctionCategory("TA", "材质属性设置")]
    public class MaterialPropertySetter : FunctionImplementation
    {
        // --- 私有成员变量 ---
        private Shader _targetShader;
        private List<Material> _materials = new List<Material>();
        private Vector2 _scrollPosition;

        // **修改点**: 只保留需要设置的属性名
        private string _shaderProperty = "_YourProperty"; // 例如: _IsEnabled, _Switch, 等
        /*
                // --- 生命周期管理 ---

                // **修改点**: 更改了菜单路径和窗口标题，以反映新的功能
                [MenuItem("Tools/Professional/Material Property Setter")]
                public static void ShowWindow()
                {
                    GetWindow<MaterialPropertySetter>("Property Setter");
                }
        */

        public override void Initialize()
        {
            // 窗口启用时的初始化逻辑
        }

        public override void Dispose()
        {
            // 窗口关闭时的清理逻辑
            _materials.Clear();
        }

        // --- UI绘制 ---

        public override void DrawGUI()
        {
            EditorGUILayout.LabelField("材质属性设置器 (0 或 1)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("选择一个Shader，工具会列出所有使用该Shader的材质，并提供批量将指定属性设置为0或1的功能。", MessageType.Info);

            EditorGUI.BeginChangeCheck();
            _targetShader = (Shader)EditorGUILayout.ObjectField("目标Shader", _targetShader, typeof(Shader), false);
            if (EditorGUI.EndChangeCheck() && _targetShader != null)
            {
                FindAndFilterMaterials();
            }

            EditorGUILayout.Space();

            // **修改点**: UI简化，只暴露需要修改的属性名
            _shaderProperty = EditorGUILayout.TextField("目标属性名", _shaderProperty);

            EditorGUILayout.Space();
            DrawActionButtons();
            EditorGUILayout.Space();
            DrawMaterialList();
        }

        /// <summary>
        /// 绘制操作按钮
        /// </summary>
        private void DrawActionButtons()
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("刷新材质列表"))
            {
                if (_targetShader != null)
                {
                    FindAndFilterMaterials();
                }
                else
                {
                    EditorUtility.DisplayDialog("提示", "请先选择一个目标Shader。", "确定");
                }
            }
            GUILayout.EndHorizontal();

            // **修改点**: 按钮的文本和功能变得更直接
            GUI.enabled = _materials.Any();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("设置为 1"))
            {
                SetProperty(1.0f);
            }

            if (GUILayout.Button("设置为 0"))
            {
                SetProperty(0.0f);
            }
            GUILayout.EndHorizontal();
            GUI.enabled = true;
        }

        /// <summary>
        /// 绘制找到的材质列表
        /// </summary>
        private void DrawMaterialList()
        {
            EditorGUILayout.LabelField($"找到的材质 ({_materials.Count})", EditorStyles.boldLabel);
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(position.height - 180));
            if (_materials.Any())
            {
                foreach (var mat in _materials)
                {
                    EditorGUILayout.ObjectField(mat.name, mat, typeof(Material), false);
                }
            }
            else
            {
                EditorGUILayout.LabelField("没有找到使用该Shader的材质。");
            }
            EditorGUILayout.EndScrollView();
        }

        // --- 核心逻辑 ---

        /// <summary>
        /// 查找并筛选项目中使用目标Shader的所有材质。
        /// </summary>
        private void FindAndFilterMaterials()
        {
            _materials.Clear();
            string[] guids = AssetDatabase.FindAssets("t:Material");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

                if (mat != null && mat.shader == _targetShader)
                {
                    _materials.Add(mat);
                }
            }
            Repaint();
        }

        /// <summary>
        /// **修改点**: 方法重命名并简化，现在只负责设置属性值。
        /// </summary>
        /// <param name="value">要设置的目标值 (0.0f 或 1.0f)。</param>
        private void SetProperty(float value)
        {
            if (!_materials.Any() || string.IsNullOrEmpty(_shaderProperty))
            {
                Debug.LogWarning("材质列表为空或属性名称未设置。");
                return;
            }

            AssetDatabase.StartAssetEditing();
            try
            {
                // 支持撤销操作
                Undo.RecordObjects(_materials.ToArray(), $"Set {_shaderProperty} to {value}");

                for (int i = 0; i < _materials.Count; i++)
                {
                    Material mat = _materials[i];
                    EditorUtility.DisplayProgressBar("处理材质", $"设置属性: {mat.name}", (float)i / _materials.Count);

                    // **核心操作**: 直接设置浮点属性的值，不再有任何关键字操作。
                    mat.SetFloat(_shaderProperty, value);

                    EditorUtility.SetDirty(mat);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
            }

            Debug.Log($"操作完成: 为 {_materials.Count} 个材质的属性 '{_shaderProperty}' 设置了值 '{value}'。");
        }
    }
}

