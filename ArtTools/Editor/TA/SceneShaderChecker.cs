using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace CustomEditorTools.TA
{

    [FunctionCategory("TA", "场景对象Shader检查")]
    public class SceneShaderChecker : FunctionImplementation
    {
        private Dictionary<Shader, List<Renderer>> shaderToRenderers = new Dictionary<Shader, List<Renderer>>();
        private Dictionary<Shader, bool> foldoutStates = new Dictionary<Shader, bool>();
        private Vector2 scrollPos;

        /*
                [MenuItem("Tools/Shader Checker")]
                public static void OpenWindow()
                {
                    GetWindow<ShaderChecker>("Shader Checker").minSize = new Vector2(400, 300);
                }

        private void OnEnable()
        {
            Refresh();
        }

  */

        public override void Initialize()
        {
            Refresh();
        }

        public override void DrawGUI()
        {
            EditorGUILayout.HelpBox("检查当前打开的场景中所有使用的 Shader，并列出每个 Shader 的使用者。", MessageType.Info);

            GUI.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
            EditorGUILayout.BeginVertical("box");

            if (GUILayout.Button("Refresh", GUILayout.Height(30)))
            {
                Refresh();
            }

            scrollPos = GUILayout.BeginScrollView(scrollPos, "box");
            foreach (var kvp in shaderToRenderers)
            {
                if (!foldoutStates.ContainsKey(kvp.Key))
                    foldoutStates[kvp.Key] = false;

                EditorGUILayout.BeginHorizontal("box");
                GUI.backgroundColor = new Color(0.3f, 0.3f, 0.3f);
                foldoutStates[kvp.Key] = EditorGUILayout.Foldout(foldoutStates[kvp.Key], kvp.Key.name, true);
                if (GUILayout.Button("Select All Users", GUILayout.Height(20)))
                {
                    Selection.objects = kvp.Value.Select(r => r.gameObject).ToArray();
                }
                EditorGUILayout.EndHorizontal();

                if (foldoutStates[kvp.Key])
                {
                    EditorGUI.indentLevel++;
                    foreach (var renderer in kvp.Value)
                    {
                        GUI.backgroundColor = new Color(0.25f, 0.25f, 0.25f);
                        if (GUILayout.Button(renderer.gameObject.name, GUILayout.Height(20)))
                        {
                            Selection.activeObject = renderer.gameObject;
                            EditorGUIUtility.PingObject(renderer.gameObject);
                        }
                    }
                    EditorGUI.indentLevel--;
                }
            }
            GUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void Refresh()
        {
            shaderToRenderers.Clear();
            var renderers = UnityEngine.Object.FindObjectsOfType<Renderer>(true);
            foreach (var renderer in renderers)
            {
                foreach (var mat in renderer.sharedMaterials)
                {
                    if (mat != null && mat.shader != null)
                    {
                        var shader = mat.shader;
                        if (!shaderToRenderers.ContainsKey(shader))
                        {
                            shaderToRenderers[shader] = new List<Renderer>();
                        }
                        if (!shaderToRenderers[shader].Contains(renderer))
                        {
                            shaderToRenderers[shader].Add(renderer);
                        }
                    }
                }
            }
        }
    }
}
