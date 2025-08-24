using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

namespace CustomEditorTools
{
    [FunctionCategory("场景","场景中模投影")]
    public class MergeHighToMidModelEditor : FunctionImplementation
    {
        private GameObject highModelInScene;
        private GameObject midModelPrefab;
        private GameObject midModelInScene;

        public override void DrawGUI()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("中模用于投影", EditorStyles.boldLabel);

            // 高模选择框
            EditorGUILayout.BeginHorizontal();
            highModelInScene = (GameObject)EditorGUILayout.ObjectField(
                new GUIContent("场景高模", "拖入场景中的高模对象"),
                highModelInScene, typeof(GameObject), true, GUILayout.Width(400));
            EditorGUILayout.EndHorizontal();

            // 中模 Prefab 选择框
            EditorGUILayout.BeginHorizontal();
            midModelPrefab = (GameObject)EditorGUILayout.ObjectField(
                new GUIContent("Asset 中模 Prefab", "拖入中模的 Prefab 资产（可选）"),
                midModelPrefab, typeof(GameObject), false, GUILayout.Width(400));
            EditorGUILayout.EndHorizontal();

            // 中模场景对象选择框
            EditorGUILayout.BeginHorizontal();
            midModelInScene = (GameObject)EditorGUILayout.ObjectField(
                new GUIContent("场景中模", "拖入场景中的中模对象（可选）"),
                midModelInScene, typeof(GameObject), true, GUILayout.Width(400));
            EditorGUILayout.EndHorizontal();

            // 合并按钮
            EditorGUILayout.Space();
            if (GUILayout.Button("合并并保存预制体", GUILayout.Height(30)))
            {
                if (highModelInScene != null && (midModelInScene != null || midModelPrefab != null))
                {
                    MergeAndSavePrefab();
                }
                else
                {
                    EditorGUILayout.HelpBox("请选择场景中的高模对象，并选择中模 Prefab 或场景中模对象！", MessageType.Warning);
                }
            }

            // 错误提示
            if (highModelInScene == null)
            {
                EditorGUILayout.HelpBox("请确保已选择场景中的高模对象。", MessageType.Warning);
            }
            if (midModelInScene == null && midModelPrefab == null)
            {
                EditorGUILayout.HelpBox("请至少选择一个中模来源（Prefab 或场景对象）。", MessageType.Warning);
            }

            EditorGUILayout.EndVertical();
        }

        private void MergeAndSavePrefab()
        {
            // 验证高模
            if (highModelInScene == null)
            {
                Debug.LogError("场景高模对象为空！");
                return;
            }

            // 创建中模副本
            GameObject midModelInstance = null;
            if (midModelInScene != null)
            {
                // 优先使用场景中模对象
                midModelInstance = Object.Instantiate(midModelInScene);
                if (midModelInstance == null)
                {
                    Debug.LogError("复制场景中模对象失败！");
                    return;
                }
                Debug.Log($"从场景中模 {midModelInScene.name} 创建副本: {midModelInstance.name}");
            }
            else if (midModelPrefab != null)
            {
                // 使用中模 Prefab
                midModelInstance = PrefabUtility.InstantiatePrefab(midModelPrefab) as GameObject;
                if (midModelInstance == null)
                {
                    Debug.LogError("实例化中模 Prefab 失败！");
                    return;
                }
                Debug.Log($"从中模 Prefab {midModelPrefab.name} 实例化: {midModelInstance.name}");
            }
            else
            {
                Debug.LogError("未选择中模 Prefab 或场景中模对象！");
                return;
            }

            // 设置中模为高模的子级
            midModelInstance.transform.SetParent(highModelInScene.transform, false);
            midModelInstance.name = $"{highModelInScene.name}_Shadow";

            // 缩小中模副本的 scale 到 0.85 倍
            midModelInstance.transform.localScale *= 0.85f;
            Debug.Log($"已将中模副本 {midModelInstance.name} 的 localScale 缩小到 0.85 倍: {midModelInstance.transform.localScale}");

            // 获取高模和中模的 Renderer
            Renderer[] highRenderers = highModelInScene.GetComponentsInChildren<Renderer>();
            Renderer[] midRenderers = midModelInstance.GetComponentsInChildren<Renderer>();

            // 检查 Renderer 是否存在
            if (highRenderers.Length == 0)
            {
                Debug.LogWarning($"高模 {highModelInScene.name} 不包含任何 Renderer！");
            }
            if (midRenderers.Length == 0)
            {
                Debug.LogWarning($"中模副本 {midModelInstance.name} 不包含任何 Renderer！");
            }

            // 设置高模 Renderer 的 ShadowCastingMode 为 Off
            foreach (var highRenderer in highRenderers)
            {
                highRenderer.shadowCastingMode = ShadowCastingMode.Off;
            }

            // 设置中模 Renderer 的 ShadowCastingMode 和 staticShadowCaster
            foreach (var midRenderer in midRenderers)
            {
                midRenderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                midRenderer.staticShadowCaster = true;
            }

            // 设置中模为静态
            midModelInstance.isStatic = true;

            // 保存更改
            EditorUtility.SetDirty(highModelInScene);
            EditorUtility.SetDirty(midModelInstance);

            // 应用到高模的 Prefab 实例
            if (PrefabUtility.IsPartOfPrefabInstance(highModelInScene))
            {
                string prefabPath = AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(highModelInScene));
                PrefabUtility.ApplyPrefabInstance(highModelInScene, InteractionMode.UserAction);
                Debug.Log($"已将更改应用到高模 Prefab 实例: {prefabPath}");
            }
            else
            {
                Debug.LogWarning($"高模 {highModelInScene.name} 不是 Prefab 实例，未应用 Prefab 更改。");
            }

            Debug.Log($"合并完成: 高模 {highModelInScene.name} 与中模副本 {midModelInstance.name}");
        }
    }
}