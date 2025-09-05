using UnityEngine;
using System.Collections;
using System.IO;
using System;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using DigitalOpus.MB.Core;
using UnityEditor;
using Spine.Unity;
using Spine.Unity.Editor;
using static Spine.Unity.Editor.SpineEditorUtilities;
using UnityEditor.AnimatedValues;
using System.Linq;

namespace DigitalOpus.MB.MBEditor
{
    [CustomEditor(typeof(Custom_MeshBaker))]
    [CanEditMultipleObjects]
    public class Custom_MeshBakerEditor : Editor
    {
        MB3_MeshBakerEditorInternal mbe = new MB3_MeshBakerEditorInternal();
        MB_EditorStyles editorStyles = new MB_EditorStyles();
        static SkeletonRenderer skeletonRenderer;
        static List<string> NoFindBoneNames = new List<string>();
        static List<string> hideBoneNames = new List<string>();

        AnimBool showDrawOrderTree = new AnimBool(false);
        GUIStyle BoldFoldoutStyle;

        [MenuItem("GPUAnimation/Spine/TextureBaker and MeshBaker", false, 100)]
        public static GameObject CreateNewMeshBaker()
        {
            MB3_TextureBaker[] mbs = (MB3_TextureBaker[])GameObject.FindObjectsOfType(typeof(MB3_TextureBaker));
            Regex regex = new Regex(@"\((\d+)\)$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
            int largest = 0;
            try
            {
                for (int i = 0; i < mbs.Length; i++)
                {
                    Match match = regex.Match(mbs[i].name);
                    if (match.Success)
                    {
                        int val = Convert.ToInt32(match.Groups[1].Value);
                        if (val >= largest)
                            largest = val + 1;
                    }
                }
            }
            catch (Exception e)
            {
                if (e == null) e = null; //Do nothing supress compiler warning
            }
            GameObject nmb = new GameObject("TextureBaker (" + largest + ")");
            nmb.transform.position = Vector3.zero;
            MB3_TextureBaker tb = nmb.AddComponent<MB3_TextureBaker>();
            tb.packingAlgorithm = MB2_PackingAlgorithmEnum.MeshBakerTexturePacker;
            MB3_MeshBakerGrouper mbg = nmb.AddComponent<MB3_MeshBakerGrouper>();
            GameObject meshBaker = new GameObject("MeshBaker");
            Custom_MeshBaker mb = meshBaker.AddComponent<Custom_MeshBaker>();
            meshBaker.transform.parent = nmb.transform;
            mb.meshCombiner.settingsHolder = mbg;
            return nmb.gameObject;
        }

        void OnEnable()
        {
            editorStyles.Init();
            BoldFoldoutStyle = new GUIStyle(EditorStyles.foldout);
            BoldFoldoutStyle.fontStyle = FontStyle.Bold;
            BoldFoldoutStyle.stretchWidth = true;
            BoldFoldoutStyle.fixedWidth = 0;


            mbe.OnEnable(serializedObject);
        }

        void OnDisable()
        {
            mbe.OnDisable();
        }

        public override void OnInspectorGUI()
        {
            MB3_MeshBakerCommon momm = (MB3_MeshBakerCommon)target;
            mbe.OnInspectorGUI(serializedObject, (MB3_MeshBakerCommon)target, targets, typeof(MB3_MeshBakerEditorWindow));

            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("根据Spine Slot排序位置", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(editorStyles.editorBoxBackgroundStyle); 
            EditorGUILayout.Space();
            EditorGUILayout.Space();
 
            skeletonRenderer = (SkeletonRenderer)EditorGUILayout.ObjectField(skeletonRenderer, typeof(SkeletonRenderer), true);

            if (skeletonRenderer != null)
            {
                DrawSpineDrawOrderTree();
            }
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            
            
           
            GUI.color = Color.green;
            if (GUILayout.Button("SortSpineSlot"))
            {
                SortByDistanceAlongAxis(momm.GetObjectsToCombine());
            }

            GUI.color = Color.white;
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
            EditorGUILayout.Space();
        }

        void DrawSpineDrawOrderTree()
        {
            var skeleton = skeletonRenderer.skeleton;
            //展开，显示Spine的绘制排序树
            showDrawOrderTree.target = EditorGUILayout.Foldout(showDrawOrderTree.target, new GUIContent($"Draw Order and Separators({skeleton.DrawOrder.Count})", Icons.slotRoot), BoldFoldoutStyle);
            if (showDrawOrderTree.faded > 0)
            {
                using (new SpineInspectorUtility.IndentScope())
                {
                    using (new EditorGUILayout.FadeGroupScope(showDrawOrderTree.faded))
                    {
                        
                        const string SeparatorString = "------------- v SEPARATOR v -------------";
                        for (int i=0;  i< skeleton.DrawOrder.Count;i++ )
                        {
                            var slot = skeleton.DrawOrder.Items[i];
                            string[] slotNames = SkeletonRendererInspector.GetSeparatorSlotNames(skeletonRenderer);
                            for (int j = 0, n = slotNames.Length; j < n;j++)
                            {
                                if (string.Equals(slotNames[j], slot.Data.Name, System.StringComparison.Ordinal))
                                {
                                    EditorGUILayout.LabelField(SeparatorString);
                                    break;
                                }
                            }
                            using (new EditorGUI.DisabledScope(!slot.Bone.Active))
                            {
                                string bonename = slot.Data.Name;
                                string boneIndexName = $"({i})   {bonename}";
                                int index = NoFindBoneNames.FindIndex(p => p.Equals(bonename));
                                if (index>= 0)
                                {
                                    GUI.color = Color.red;
                                    EditorGUILayout.LabelField(new GUIContent(boneIndexName, Icons.slot), GUILayout.ExpandWidth(false));
                                    GUI.color = Color.white;
                                    continue;
                                }

                                //index = hideBoneNames.FindIndex(p=>p.Equals(bonename));
                                //if (index >= 0)
                                //{
                                //    GUI.color = Color.yellow;
                                //    EditorGUILayout.LabelField(new GUIContent(boneIndexName, Icons.slot), GUILayout.ExpandWidth(false));
                                //    GUI.color = Color.white;
                                //    continue;
                                //}

                                EditorGUILayout.LabelField(new GUIContent(boneIndexName, Icons.slot), GUILayout.ExpandWidth(false));

                            }
                        }
                    }
                }
            }
        }
        public void SortByDistanceAlongAxis(List<GameObject> gos)
        {
            NoFindBoneNames.Clear();
            hideBoneNames.Clear();


            var skeleton = skeletonRenderer.skeleton;
            List<string> showBoneNames = new List<string>();
            //spine骨骼节点名：附加物的父节点名称
            foreach (var slot in skeleton.DrawOrder)
            {
                if (slot.Bone.Active)
                {
                    showBoneNames.Add(slot.Data.Name);
                }
                else
                {
                    hideBoneNames.Add(slot.Data.Name);
                }
            }
            //因为所有的mesh都是spine的骨骼节点附加物，它们挂载在骨骼节点对象下，因此只需要比较它们的父对象名称即可
            hideBoneNames.ForEach(s => {
                gos.RemoveAll(p=>p.transform.parent.name.Equals(s));
                Debug.Log("------>Remvoe: " + s);
            });

            //根据排序名，把对象取出顺序保存
            List<GameObject> orderObjs = new List<GameObject>();
            showBoneNames.ForEach(s =>
            {
                var go = gos.Find(p=> p.transform.parent.name == s);
                if(go != null)
                {
                    orderObjs.Add(go);
                }
                else
                {
                    NoFindBoneNames.Add(s);
                    Debug.Log($"Dont Find Spine Order Obj: {s}");
                }
               
            });

            //查看下是否多出了未知的附加物对象，这时候就好好看看对象是什么了，如果渲染顺序不对，就手动在面板上调整位置吧。
            //这个我也没办法了，这里对未知的附加物对象都添加到最高渲染级别。
            List<GameObject> newBoneObjs = new List<GameObject>();
            foreach(var g in gos)
            {
                var index = orderObjs.FindIndex(p => p == g);
                if (index < 0)
                {
                    Debug.Log($"Find New attachment: {g.name}");
                    newBoneObjs.Add(g);
                }
            }
            gos.Clear();
            gos.AddRange(orderObjs);
            gos.AddRange(newBoneObjs);

            //最后一步，清除隐藏的附加物，对象未开启的就不渲染了。
            gos.RemoveAll(p => p.gameObject.activeSelf == false);
        }
    }
}