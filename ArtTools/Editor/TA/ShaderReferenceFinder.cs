using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CustomEditorTools.TA
{
    public class MaterialInfo
    {
        public string Path { get; set; }
        public string Name { get; set; }
        public string ShaderName { get; set; }
    }

    [FunctionCategory("TA","shader引用查找替换")]
    public class ShaderReferenceFinder : FunctionImplementation
    {
        private Shader selectedShader;
        private string shaderName = "";
        private DefaultAsset selectedFolder;
        private string searchFolder = "Assets";
        private Shader replaceShader;
        private Vector2 scrollPosition;
        private List<MaterialInfo> foundMaterials = new List<MaterialInfo>();

        public override void DrawGUI()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Find Materials Referencing Shader", EditorStyles.boldLabel);

            // 文件夹选择字段
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Folder", GUILayout.Width(100));
            DefaultAsset newFolder =
                (DefaultAsset)EditorGUILayout.ObjectField(selectedFolder, typeof(DefaultAsset), false);
            if (newFolder != selectedFolder)
            {
                selectedFolder = newFolder;
                if (selectedFolder != null)
                {
                    string path = AssetDatabase.GetAssetPath(selectedFolder);
                    if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
                    {
                        searchFolder = path;
                    }
                    else
                    {
                        Debug.LogWarning($"Invalid folder selected: {path}. Defaulting to Assets.");
                        searchFolder = "Assets";
                        selectedFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>("Assets");
                    }
                }
                else
                {
                    searchFolder = "Assets";
                    selectedFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>("Assets");
                }
            }

            EditorGUILayout.EndHorizontal();

            // Shader 选择字段
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Shader", GUILayout.Width(100));
            selectedShader = (Shader)EditorGUILayout.ObjectField(selectedShader, typeof(Shader), false);
            EditorGUILayout.EndHorizontal();

            // Shader 名称输入框
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Shader Name", GUILayout.Width(100));
            shaderName = EditorGUILayout.TextField(shaderName);
            EditorGUILayout.EndHorizontal();

            // 新 Shader 选择字段
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("New Shader", GUILayout.Width(100));
            replaceShader = (Shader)EditorGUILayout.ObjectField(replaceShader, typeof(Shader), false);
            EditorGUILayout.EndHorizontal();

            // 查找和替换按钮
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Find Materials", GUILayout.Height(30)))
            {
                FindMaterials();
            }

            if (GUILayout.Button("Replace Shaders", GUILayout.Height(30)))
            {
                ReplaceShaders();
            }

            EditorGUILayout.EndHorizontal();

            // 材质表格
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Found Materials", EditorStyles.boldLabel);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            // 表头
            EditorGUILayout.BeginHorizontal("box");
            GUILayout.Label("Path", EditorStyles.boldLabel, GUILayout.Width(300));
            GUILayout.Label("Name", EditorStyles.boldLabel, GUILayout.Width(150));
            GUILayout.Label("Shader", EditorStyles.boldLabel, GUILayout.Width(150));
            EditorGUILayout.EndHorizontal();

            // 表格内容
            if (foundMaterials.Count == 0)
            {
                EditorGUILayout.LabelField("No materials found.", EditorStyles.helpBox);
            }
            else
            {
                foreach (var materialInfo in foundMaterials)
                {
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button(materialInfo.Path, GUILayout.Width(300)))
                    {
                        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialInfo.Path);
                        if (material != null)
                        {
                            EditorGUIUtility.PingObject(material);
                        }
                    }

                    GUILayout.Label(materialInfo.Name, GUILayout.Width(150));
                    GUILayout.Label(materialInfo.ShaderName, GUILayout.Width(150));
                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void FindMaterials()
        {
            if (selectedShader == null && string.IsNullOrEmpty(shaderName))
            {
                Debug.LogWarning("Please select a Shader or enter a Shader name.");
                return;
            }

            string targetShaderName = selectedShader != null ? selectedShader.name : shaderName;
            Debug.Log($"Searching for materials referencing Shader: {targetShaderName} in folder: {searchFolder}");

            // 清空之前的查找结果
            foundMaterials.Clear();

            // 验证搜索路径
            if (string.IsNullOrEmpty(searchFolder) || !Directory.Exists(searchFolder))
            {
                Debug.LogWarning($"Search folder is invalid: {searchFolder}. Defaulting to Assets.");
                searchFolder = "Assets";
            }

            // 查找指定文件夹下的材质
            string[] guids = AssetDatabase.FindAssets("t:Material", new[] { searchFolder });
            int materialCount = 0;

            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(assetPath))
                {
                    Debug.LogWarning($"Invalid asset path for GUID: {guid}");
                    continue;
                }

                Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
                if (material == null || material.shader == null)
                {
                    Debug.LogWarning($"Failed to load material or shader at: {assetPath}");
                    continue;
                }

                // 检查 Shader 是否匹配
                if (material.shader.name.Contains(targetShaderName, StringComparison.OrdinalIgnoreCase))
                {
                    // 添加到表格
                    foundMaterials.Add(new MaterialInfo
                    {
                        Path = assetPath,
                        Name = material.name,
                        ShaderName = material.shader.name
                    });

                    // 输出 Console 错误日志
                    //    Debug.LogError($"Material referencing '{material.shader.name}': {assetPath}", material);
                    materialCount++;
                }
            }

            if (materialCount == 0)
            {
                Debug.Log($"No materials found referencing Shader: {targetShaderName} in folder: {searchFolder}");
            }
            else
            {
                Debug.Log(
                    $"Found {materialCount} materials referencing Shader: {targetShaderName} in folder: {searchFolder}");
            }
        }

        private void ReplaceShaders()
        {
            if (replaceShader == null)
            {
                Debug.LogWarning("Please select a New Shader to replace with.");
                return;
            }

            if (foundMaterials.Count == 0)
            {
                Debug.LogWarning("No materials to replace. Please find materials first.");
                return;
            }

            Debug.Log($"Replacing shaders with: {replaceShader.name} for {foundMaterials.Count} materials");

            foreach (var materialInfo in foundMaterials)
            {
                Material material = AssetDatabase.LoadAssetAtPath<Material>(materialInfo.Path);
                if (material != null)
                {
                    material.shader = replaceShader;
                    EditorUtility.SetDirty(material);
                    materialInfo.ShaderName = replaceShader.name; // 更新表格显示
                    Debug.Log($"Replaced shader for material: {materialInfo.Path} to {replaceShader.name}");
                }
                else
                {
                    Debug.LogWarning($"Failed to load material at: {materialInfo.Path}");
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Shader replacement completed and assets saved.");
        }
    }
}