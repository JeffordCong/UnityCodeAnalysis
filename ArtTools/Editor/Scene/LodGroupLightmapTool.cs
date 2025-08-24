using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace CustomEditorTools
{
    [FunctionCategory("场景","LOD LightMap同步")]
    public class LodGroupLightmapTool : FunctionImplementation
    {
        // [MenuItem("工具/同步LOD Lightmap与静态标志")]
        // public static void ShowWindow()
        // {
        //     GetWindow<LodGroupLightmapTool>("LOD Lightmap同步");
        // }

        public override void DrawGUI()
        {
            GUILayout.Space(10);
            EditorGUILayout.LabelField("LOD Lightmap同步工具", EditorStyles.boldLabel);
            GUILayout.Space(8);
            EditorGUILayout.HelpBox(
                "点击下方按钮，将当前场景所有LODGroup对象的LOD0的Lightmap参数同步到其它LOD，并且取消所有非LOD0对象的静态标志。",
                MessageType.Info);

            GUILayout.Space(10);

            if (GUILayout.Button("一键同步所有LODGroup"))
            {
                SyncLodGroupLightmap();
            }
        }

        void SyncLodGroupLightmap()
        {
            int processedObjects = 0;

            // 获取所有LODGroup
            var allLodGroups = GameObject.FindObjectsOfType<LODGroup>();
            foreach (var lodGroup in allLodGroups)
            {
                var lods = lodGroup.GetLODs();
                if (lods.Length < 2) continue; // 跳过只有1级LOD的

                // 1. 获取LOD0所有Renderer的Lightmapping信息
                List<Renderer> lod0Renderers = new List<Renderer>();
                foreach (var r in lods[0].renderers)
                {
                    if (r != null) lod0Renderers.Add(r);
                }

                if (lod0Renderers.Count == 0) continue;

                // 2. 把LOD0的lightmapIndex/ScaleOffset传给后面所有LOD的Renderer
                for (int i = 1; i < lods.Length; i++)
                {
                    foreach (var renderer in lods[i].renderers)
                    {
                        if (renderer == null) continue;

                        // 非LOD0全部去掉静态标志
                        GameObjectUtility.SetStaticEditorFlags(renderer.gameObject, 0); // 全部非静态

                        // 只复制LOD0第一个Renderer的参数（如需按顺序可自定义改）
                        var refRenderer = lod0Renderers[0];
                        renderer.lightmapIndex = refRenderer.lightmapIndex;
                        renderer.lightmapScaleOffset = refRenderer.lightmapScaleOffset;
                    }
                }

                processedObjects++;
            }

            EditorUtility.DisplayDialog("LOD工具", $"已处理 {processedObjects} 个 LODGroup", "OK");
        }
    }

}