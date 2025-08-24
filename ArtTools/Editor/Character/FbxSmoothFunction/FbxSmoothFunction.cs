using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

/*需要安装依赖：com.autodesk.fbx*/
namespace CustomEditorTools
{
    [FunctionCategory("角色", "FBX法线平滑工具", 2)]
    public class FbxSmoothFunction : FunctionImplementation
    {
        private static readonly string[] CHANNELS =
        {
            "UV 2",
            "UV 3",
            "UV 4",
            "UV 5",
            "UV 6",
            "UV 7",
            "UV 8",
        };

        private List<UnityEngine.Object> fbxObjects = new List<UnityEngine.Object>();
        private int selectedChannel = 0;
        private GUIStyle labelStyle;

        public override void DrawGUI()
        {
            if (labelStyle == null)
            {
                labelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 18,
                    alignment = TextAnchor.MiddleCenter,
                    clipping = TextClipping.Overflow
                };
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("FBX 法线平滑工具", labelStyle);
            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("1. 添加或移除要处理的 FBX 文件：", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            if (fbxObjects.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "当前未添加 FBX 文件，点击“添加 FBX”按钮以插入新的条目，或直接拖拽 FBX 资源到列表区域。",
                    MessageType.Info
                );
            }

            for (int i = 0; i < fbxObjects.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                fbxObjects[i] = EditorGUILayout.ObjectField(
                    fbxObjects[i],
                    typeof(UnityEngine.Object),
                    allowSceneObjects: false
                ) as UnityEngine.Object;

                if (GUILayout.Button("–", GUILayout.Width(24), GUILayout.Height(18)))
                {
                    fbxObjects.RemoveAt(i);
                    i--;
                }
                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button("添加 FBX", GUILayout.Height(24)))
            {
                fbxObjects.Add(null);
            }

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("2. 选择保存平滑法线的 UV 通道：", EditorStyles.boldLabel);
            selectedChannel = EditorGUILayout.Popup("UV 通道", selectedChannel, CHANNELS);
            EditorGUILayout.Space(20);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("确定", GUILayout.Width(100), GUILayout.Height(30)))
            {
                List<UnityEngine.Object> validList = new List<UnityEngine.Object>();
                foreach (var obj in fbxObjects)
                {
                    if (obj == null) continue;
                    string path = AssetDatabase.GetAssetPath(obj);
                    if (string.IsNullOrEmpty(path)) continue;
                    if (Path.GetExtension(path).ToLower() != ".fbx") continue;
                    validList.Add(obj);
                }

                if (validList.Count == 0)
                {
                    EditorUtility.DisplayDialog("提示", "请至少添加一个有效的 FBX 文件！", "确定");
                }
                else
                {
                    int uvChannelIndex = selectedChannel + 1;
                    // 这里调用你原有的FBX处理API
                    FbxMeshNormalProcessor.FbxModelNormalSmoothTool(validList.ToArray(), uvChannelIndex);
                    // 这里不能直接Close()窗口了，可以清空状态或提示操作完成
                    fbxObjects.Clear();
                    EditorUtility.DisplayDialog("提示", "法线处理完成！", "确定");
                }
            }

            GUILayout.Space(20);

            if (GUILayout.Button("取消", GUILayout.Width(100), GUILayout.Height(30)))
            {
                fbxObjects.Clear();
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
        }

        public override void Initialize()
        {
            // 保持之前的清空
            if (fbxObjects == null)
                fbxObjects = new List<UnityEngine.Object>();
            selectedChannel = 0;
        }

        public override void Dispose()
        {
            fbxObjects = null;
        }
    }
}
