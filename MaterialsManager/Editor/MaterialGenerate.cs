using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MyEditor.MaterialSystem
{

    [CreateAssetMenu(menuName = "材质管理器/创建")] 
    public class MaterialGenerate : ScriptableObject
    {

        public MaterialConfig config;
        public Material targetMaterial;

        [Tooltip("自动创建并绑定材质球（推荐自动模式）")]
        public bool autoCreateMaterial = true;

        [HideInInspector]
        public bool isAutoCreatedMaterial = false; // 用于区分材质球是否自动生成（影响命名同步、编辑保护等）

        // 自动收集所有MaterialConfig派生类型，用于下拉切换类型
        // 反射查找当前AppDomain内所有继承MaterialConfig的非抽象类型，支持自动扩展（新类型无需改这里）
        public static List<Type> AllConfigTypes =>
            AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => typeof(MaterialConfig).IsAssignableFrom(t) && !t.IsAbstract)
                .ToList();

        // 下拉列表用，优先显示Config类的DisplayName属性，没有就用类型名
        public static List<string> AllConfigNames =>
            AllConfigTypes.Select(t =>
            {
                var tmp = ScriptableObject.CreateInstance(t) as MaterialConfig;
                return tmp != null ? tmp.DisplayName : t.Name;
            }).ToList();


#if UNITY_EDITOR

        /// 自动新建并绑定Material（与Generate资源同名、同目录），并设置自动生成标志
        /// 仅在无材质球且autoCreateMaterial开启时自动调用
        public void AutoCreateAndAssignMaterial()
        {
            if (targetMaterial != null) return; // 已绑定材质球时不重复生成

            string genPath = AssetDatabase.GetAssetPath(this);
            if (string.IsNullOrEmpty(genPath)) return;

            string dir = System.IO.Path.GetDirectoryName(genPath);
            string baseName = System.IO.Path.GetFileNameWithoutExtension(genPath);
            string matPath = AssetDatabase.GenerateUniqueAssetPath($"{dir}/{baseName}.mat"); // 生成唯一材质路径（防止重名覆盖）

            Shader shader = config != null ? config.GetShader() : Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                Debug.LogError("未找到指定Shader，无法创建材质。");
                return;
            }
            var mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, matPath);

            targetMaterial = mat;
            isAutoCreatedMaterial = true;
            AssetDatabase.SaveAssets();
        }
#endif


        /// 一键批量应用当前Config的所有参数和贴图到目标材质球
        /// 可自动生成材质球、自动Apply贴图导入参数、同步所有参数
        public void Generate()
        {
#if UNITY_EDITOR
            if (targetMaterial == null)
            {
                if (autoCreateMaterial)
                    AutoCreateAndAssignMaterial();
                else
                    return; // 手动模式下没拖材质球啥都不干
            }

            if (config != null && targetMaterial != null)
            {
                config.ApplyAllImportSettings();          // 批量Apply贴图的导入设置（如重命名、压缩、平台格式等）
                config.ApplyToMaterial(targetMaterial);   // 批量Apply所有参数/贴图到目标材质球
            }
#endif
        }

#if UNITY_EDITOR
        /// <summary>
        /// 只针对自动生成的材质球执行同步命名为当前Generate资源名（防止误操作）
        /// 手动拖拽的材质球不做处理
        /// </summary>
        public void SyncMaterialNameWithGenerate()
        {
            if (targetMaterial == null)
            {
                Debug.LogWarning("未绑定材质球，无法同步命名！");
                return;
            }

            if (!isAutoCreatedMaterial)
            {
                Debug.LogWarning("当前材质球不是自动生成的，不做同步（避免误操作）！");
                return;
            }

            string genPath = AssetDatabase.GetAssetPath(this);
            string genName = System.IO.Path.GetFileNameWithoutExtension(genPath);

            string matPath = AssetDatabase.GetAssetPath(targetMaterial);
            string matDir = System.IO.Path.GetDirectoryName(matPath);

            string newMatPath = System.IO.Path.Combine(matDir, genName + ".mat");
            newMatPath = AssetDatabase.GenerateUniqueAssetPath(newMatPath);            // 防重名

            string result = AssetDatabase.RenameAsset(matPath, genName);               // 资源重命名（只改名字不动文件夹）
            if (string.IsNullOrEmpty(result))
                Debug.Log($"已将自动生成的材质球同步命名为: {genName}.mat");
            else
                Debug.LogError($"材质球自动重命名失败: {result}");
        }
#endif
    }

#if UNITY_EDITOR

    [CustomEditor(typeof(MaterialGenerate))]
    public class MaterialGenerateEditor : Editor
    {
        private Editor configEditor;
        private List<Type> _typeList;
        private List<string> _nameList;

        private void OnEnable()
        {
            _typeList = MaterialGenerate.AllConfigTypes;
            _nameList = MaterialGenerate.AllConfigNames;
        }


        public override void OnInspectorGUI()
        {
            var materialGenerator = (MaterialGenerate)target;
            serializedObject.Update();

            // -------- 1. Config类型下拉选择 --------
            int curIndex = materialGenerator.config != null ? _typeList.FindIndex(t => t == materialGenerator.config.GetType()) : -1;
            int newIndex = EditorGUILayout.Popup("材质类型", curIndex, _nameList.ToArray());
            // 当前选中的Config类型，如果切换类型则销毁旧的、创建新的（实现热切换）

            if (newIndex != curIndex)
            {
                // 销毁旧的 config
                if (materialGenerator.config != null)
                    DestroyImmediate(materialGenerator.config, true);

                // 创建新的 config，作为ScriptableObject子资源挂在本Generate对象下
                if (newIndex >= 0)
                {
                    var newConfig = ScriptableObject.CreateInstance(_typeList[newIndex]) as MaterialConfig;
                    newConfig.name = "config";
                    AssetDatabase.AddObjectToAsset(newConfig, materialGenerator);
                    materialGenerator.config = newConfig;
                    AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(materialGenerator));
                    AssetDatabase.SaveAssets();
                }

                // 清理Config的嵌套Inspector
                if (configEditor != null)
                {
                    DestroyImmediate(configEditor);
                    configEditor = null;
                }
            }

            // -------- 2. 自动/手动材质球绑定 --------
            materialGenerator.autoCreateMaterial = EditorGUILayout.Toggle("自动创建并绑定Material", materialGenerator.autoCreateMaterial);

            if (!materialGenerator.autoCreateMaterial)
            {
                // 手动绑定模式，可拖拽Material资源
                EditorGUILayout.PropertyField(serializedObject.FindProperty("targetMaterial"));
            }
            else
            {
                // 自动绑定模式，目标材质球只读显示
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.ObjectField("自动绑定的Material", materialGenerator.targetMaterial, typeof(Material), false);
                EditorGUI.EndDisabledGroup();
            }

            // -------- 3. Config参数嵌套Inspector --------
            if (materialGenerator.config != null)
            {
                // 若Config切换了，需要重建嵌套Inspector
                if (configEditor == null || configEditor.target != materialGenerator.config)
                {
                    if (configEditor != null)
                        DestroyImmediate(configEditor);
                    configEditor = CreateEditor(materialGenerator.config);
                }

                if (configEditor != null)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("材质配置参数（可直接编辑）", EditorStyles.boldLabel);
                    configEditor.OnInspectorGUI();   // 递归绘制Config参数
                }
            }
            else
            {
                if (configEditor != null)
                {
                    DestroyImmediate(configEditor);
                    configEditor = null;
                }
                EditorGUILayout.HelpBox("请先选择材质类型。", MessageType.Info); // 无Config时提示
            }

            EditorGUILayout.Space();
            DrawGenerateButton(materialGenerator);

            EditorGUILayout.Space();
            DrawSyncNameButton(materialGenerator);

            serializedObject.ApplyModifiedProperties(); // 提交所有参数编辑
        }

        /// <summary>
        /// 一键生成/更新材质按钮
        /// 自动检测目录规范，自动创建材质球并Apply所有参数和贴图
        /// </summary>
        private void DrawGenerateButton(MaterialGenerate gen)
        {
            if (GUILayout.Button("一键生成/更新材质（自动目录检查）"))
            {
                string error;
                // 检查MaterialGenerate资源、材质球、贴图目录是否符合规范
                if (!AssetDirectoryChecker.CheckMaterialGenerateDirectory(gen, out error))
                {
                    EditorUtility.DisplayDialog("目录规范检查未通过", error, "终止生成/更新");
                    return;
                }

                if (gen.targetMaterial == null)
                {
                    gen.AutoCreateAndAssignMaterial(); // 无材质球自动生成
                    EditorUtility.SetDirty(gen);
                    AssetDatabase.SaveAssets();
                }

                gen.Generate(); // 递归Apply所有配置、贴图导入参数、材质参数
                if (gen.targetMaterial)
                {
                    EditorUtility.SetDirty(gen.targetMaterial);
                    AssetDatabase.SaveAssets();
                }
            }
        }
        /// <summary>
        /// 同步材质球命名为本资源名按钮（只针对自动生成的材质球有效）
        /// </summary>
        private void DrawSyncNameButton(MaterialGenerate gen)
        {
            if (GUILayout.Button("同步材质球命名为本资源名（仅自动生成的材质）"))
            {
                gen.SyncMaterialNameWithGenerate(); // 执行命名同步
                EditorUtility.SetDirty(gen);
                AssetDatabase.SaveAssets();
            }
        }

        // 清理嵌套Inspector引用
        private void OnDisable()
        {
            if (configEditor != null)
            {
                DestroyImmediate(configEditor);
                configEditor = null;
            }
        }
    }
#endif

}
