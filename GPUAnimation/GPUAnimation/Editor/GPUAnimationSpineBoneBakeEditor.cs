using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System;
using System.IO;
using Object = UnityEngine.Object;
using System.Text.RegularExpressions;
using Spine;
using Spine.Unity;
using System.Linq;

namespace GPUAnimationLib
{

    public class GPUAnimationSpineBoneBakeEditor : EditorWindow
    {

        [MenuItem("GPUAnimation/GPUAnimationBake_Spine骨骼动画")]
        private static void ShowWindow()
        {
            var window = GetWindow<GPUAnimationSpineBoneBakeEditor>();
            window.titleContent = new GUIContent("GPUAnimationSpineBoneBakeEditor");
            window.Show();
        }
        private GameObject m_SrcPrefab;
        private GameObject m_TargetPrefab;

        private SkinnedMeshRenderer smr;

        //蒙皮质量
        private GPUBoneMode m_skinQuality = GPUBoneMode._SKIN_BONE_4;
        private GPUAnimationMode m_Mode = GPUAnimationMode._ANIM_BONE;
        private GPUColorMode m_colorMode = GPUColorMode._RGBAHALF;
        //Spine动画默认30FPS。可以通过 skeletonData.Fps获取
        private int frameRate = 60;
        //Bone纹理不需要
        private GPUTextureAlingMode m_textureAlingMode = GPUTextureAlingMode._ALING_AUTO;
        

        /// <summary>
        /// URP渲染管线
        /// </summary>
        private bool m_URPShader = false;

        private Vector3 boundsCenter;
        private float maxBound;

        private SkeletonDataAsset skeletonDataAsset;
        [SerializeField]
        public AnimationClip[] m_TargetAnimations;

        private ReorderableList reorderableList;
        private SerializedObject serializedObject;
        private SerializedProperty animationProperty;

        private GUIStyle editorBoxBackgroundStyle = new GUIStyle();

        private void OnEnable()
        {
            serializedObject = new SerializedObject(this);
            this.animationProperty = serializedObject.FindProperty("m_TargetAnimations");
            this.reorderableList = new ReorderableList(serializedObject, animationProperty);
            this.reorderableList.drawHeaderCallback = (rect) => { GUI.Label(rect, "Animation Clip List:"); };
            this.reorderableList.elementHeight = EditorGUIUtility.singleLineHeight;
            this.reorderableList.drawElementCallback = new ReorderableList.ElementCallbackDelegate(OnDrawElement);

            m_URPShader = GPUAnimationBakeEditor.DetectPipeline() != PipelineType.Default;

            editorBoxBackgroundStyle.normal.background = Texture2D.grayTexture;
            editorBoxBackgroundStyle.border = new RectOffset(0, 0, 0, 0);
            editorBoxBackgroundStyle.margin = new RectOffset(5, 5, 5, 5);
            editorBoxBackgroundStyle.padding = new RectOffset(10, 10, 10, 10);

            InitBoneTreeStyle();
        }

        private void OnDisable()
        {
            serializedObject.Dispose();
            animationProperty?.Dispose();
            serializedObject = null;
            reorderableList = null;
            animationProperty = null;
        }

        
        private void OnGUI()
        {
            EditorGUILayout.HelpBox("对Spine动画做GPU骨骼动画功能", MessageType.Warning);
            EditorGUILayout.Space();

            DrawGUI();
        }

        private void OnDrawElement(Rect rect, int index, bool isActive, bool isForce)
        {
            var element = this.animationProperty.GetArrayElementAtIndex(index);
            EditorGUI.PropertyField(rect, element, GUIContent.none);
        }

        private static void Msg(string info)
        {
            EditorUtility.DisplayDialog("Title", info, "ok");
        }

        private void DrawGUI()
        {

            EditorGUILayout.BeginVertical(editorBoxBackgroundStyle);

            skeletonDataAsset = (SkeletonDataAsset)EditorGUILayout.ObjectField("Skeleton Data Asset:", skeletonDataAsset, typeof(SkeletonDataAsset), true);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("1.先把Spine动画转Animator动画");
            if (GUILayout.Button("打开SpineBakeAnimator面板"))
            {
                Spine.Unity.Editor.SkeletonBakingWindow1.Init(new MenuCommand(skeletonDataAsset));
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("2.创建 TextureBaker And MeshBaker 工具对象");
            if (GUILayout.Button("创建 TextureBaker And MeshBaker"))
            {
                DigitalOpus.MB.MBEditor.Custom_MeshBakerEditor.CreateNewMeshBaker();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();



            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("场景中原实例对象");
            m_SrcPrefab = (GameObject)EditorGUILayout.ObjectField(m_SrcPrefab, typeof(GameObject), true);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("场景中渲染对象");
            m_TargetPrefab = (GameObject)EditorGUILayout.ObjectField(m_TargetPrefab, typeof(GameObject), true);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.Space();

            reorderableList.DoLayoutList();
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.Space();

            EditorGUILayout.BeginVertical();

            frameRate = EditorGUILayout.IntPopup("Spine帧率", frameRate, new string[] { "30", "60" }, new int[] { 30, 60 });

            EditorGUILayout.Space();
            EditorGUILayout.Space();


            EditorGUILayout.LabelField("GPU Animation Type:", "骨骼动画");
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.Space();

            m_colorMode = (GPUColorMode)EditorGUILayout.EnumPopup("纹理保存格式:", m_colorMode);
            EditorGUILayout.Space();
            EditorGUILayout.Space();


            EditorGUILayout.LabelField("挂载点Bone :");

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            DrawExposeBoneTree(m_SrcPrefab);

            //烘焙GPU骨骼动画
            if (GUILayout.Button("烘焙骨骼动画"))
            {

                if (m_TargetAnimations == null || m_TargetAnimations.Length == 0 || m_TargetPrefab == null)
                {
                    Msg($"资源没添加");
                    return;
                }

                GenerateBoneInstanceMeshAndTexture(m_TargetPrefab, m_TargetAnimations);


            }

        }


        void GenerateBoneInstanceMeshAndTexture(GameObject _targetFBX, AnimationClip[] _clips)
        {
            if (!SelectDirectory(m_SrcPrefab, out string savePath, out string meshName))
            {
                Msg("Invalid Folder Selected");
                return;
            }

            var instantiatedObj = _targetFBX;
            smr = instantiatedObj.GetComponentInChildren<SkinnedMeshRenderer>();

        
                //表示每个骨骼在绑定姿势（初始状态）下的变换矩阵。
                var bindPoses = smr.sharedMesh.bindposes;
                //包含了控制网格变形的所有骨骼的引用
                var bones = smr.bones;

                var exposeBones = exposeBoneList.Where(p=>p.Value).Select(p=>p.Key).ToList();
                var exposeTransformParams = new List<GPUAnimationExposeBone>();
                if (exposeBones.Count>0)
                {
                    var activeTransforms = m_SrcPrefab.GetComponentsInChildren<Transform>();
                    foreach (var t in activeTransforms)
                    {
                        if (!exposeBones.Contains(t.GetInstanceID())) continue;

                        var relativeBoneIndex = -1;
                        var relativeBone = t;
                        //检查，挂载点是否已包含在骨骼节点列表内，不在则用它的父骨骼节点
                        while (t != null)
                        {
                            relativeBoneIndex = bones.FindIndex(p => p.name == relativeBone.name);
                            if (relativeBoneIndex != -1) break;
                            if (relativeBone.parent == null) break;
                            relativeBone = relativeBone.parent;
                        }

                        if (relativeBoneIndex == -1) continue;
                        //将点从骨骼的绑定姿势空间转换到模型的局部空间
                        var bindPoseToLocal = bindPoses[relativeBoneIndex];
                        //将点从模型的局部空间转换到当前骨骼的世界空间
                        var localToBoneAnimated = bones[relativeBoneIndex].localToWorldMatrix;
                        //将顶点从绑定姿势位置变换到动画位置
                        //"骨骼在绑定姿势下的位置" 到 "骨骼在当前动画帧中的位置" 的变换。
                        var bindPoseToBoneAnimated = localToBoneAnimated * bindPoseToLocal;
                        exposeTransformParams.Add(new GPUAnimationExposeBone()
                        {
                            index = relativeBoneIndex,
                            name = t.name,
                            position = bindPoseToBoneAnimated.GetPosition(),
                            direction = bindPoseToBoneAnimated.rotation.eulerAngles,
                        });

                    }
                }

                var transformCount = smr.sharedMesh.bindposes.Length;
                int colorCount = 3;
                if (m_colorMode == GPUColorMode._RGBAHALF)
                {
                    //以Half存储纹理，则16bit精度,一个color可以保存3个float数据，matrix最后一个float默认是(0,0,0,1),则3个color即可
                    colorCount = 3;
                }
                else if (m_colorMode == GPUColorMode._RGBM)
                {
                    //以RGBM存储，则8bit精度，则1个float转换精度，保存到一个color中，矩阵matrix3x4 数据3xfloat4,则需要12个color
                    colorCount = 3 * 4;
                }

                var totalWidth = transformCount * colorCount;
                var totalFrame = GetInstanceParams(_clips, out AnimationTickerClip[] instanceParams);
                var textureFormat = GetTextureFormat(m_colorMode);

                var atlasTexture = new Texture2D(Mathf.NextPowerOfTwo(totalWidth), Mathf.NextPowerOfTwo(totalFrame), textureFormat, false)
                {
                    filterMode = FilterMode.Point,
                    wrapModeU = TextureWrapMode.Clamp,
                    wrapModeV = TextureWrapMode.Repeat
                };
                Vector3 m_BoundsMin = Vector3.zero;
                Vector3 m_BoundsMax = Vector3.zero;

                // 1. 统计所有矩阵元素的最大最小值
                float minValue = float.MaxValue;
                float maxValue = float.MinValue;
                for (int i = 0; i < _clips.Length; i++)
                {
                    var clip = _clips[i];
                    var length = clip.length;
                    var frameRate = clip.frameRate;
                    var frameCount = (int)(length * frameRate);
                    var startFrame = instanceParams[i].frameBegin;
                    for (int j = 0; j < frameCount; j++)
                    {
                        clip.SampleAnimation(m_SrcPrefab, length * j / frameCount);
                        for (var k = 0; k < transformCount; k++)
                        {
                            var bindPoseToLocal = bindPoses[k];
                            var localToBoneAnimated = bones[k].localToWorldMatrix;
                            var bindPoseToBoneAnimated = localToBoneAnimated * bindPoseToLocal;
                            for (int row = 0; row < 3; row++)
                            {
                                for (int col = 0; col < 4; col++)
                                {
                                    float v = bindPoseToBoneAnimated[row, col];
                                    if (v < minValue) minValue = v;
                                    if (v > maxValue) maxValue = v;
                                }
                            }
                        }
                    }
                }

                // 2. 写入纹理
                for (int i = 0; i < _clips.Length; i++)
                {
                    var clip = _clips[i];
                    var length = clip.length;
                    var frameRate = clip.frameRate;
                    var frameCount = (int)(length * frameRate);
                    var startFrame = instanceParams[i].frameBegin;
                    for (int j = 0; j < frameCount; j++)
                    {
                        clip.SampleAnimation(m_SrcPrefab, length * j / frameCount);
                        for (var k = 0; k < transformCount; k++)
                        {
                            //将点从骨骼的绑定姿势空间转换到模型的局部空间
                            var bindPoseToLocal = bindPoses[k];
                            //将点从模型的局部空间转换到当前骨骼的世界空间
                            var localToBoneAnimated = bones[k].localToWorldMatrix;
                            //将顶点从绑定姿势位置变换到动画位置
                            //"骨骼在绑定姿势下的位置" 到 "骨骼在当前动画帧中的位置" 的变换。
                            var bindPoseToBoneAnimated = localToBoneAnimated * bindPoseToLocal;



                            if (m_colorMode == GPUColorMode._RGBAHALF)
                            {
                                var frame = startFrame + j;
                                var pixel = new Vector2Int(k * 3, frame);
                                var m0 = bindPoseToBoneAnimated.GetRow(0);
                                atlasTexture.SetPixel(pixel.x, pixel.y, new Color(m0.x, m0.y, m0.z, m0.w));
                                pixel = new Vector2Int(k * 3 + 1, frame);
                                var m1 = bindPoseToBoneAnimated.GetRow(1);
                                atlasTexture.SetPixel(pixel.x, pixel.y, new Color(m1.x, m1.y, m1.z, m1.w));
                                pixel = new Vector2Int(k * 3 + 2, frame);
                                var m2 = bindPoseToBoneAnimated.GetRow(2);
                                atlasTexture.SetPixel(pixel.x, pixel.y, new Color(m2.x, m2.y, m2.z, m2.w));
                            }
                            else if (m_colorMode == GPUColorMode._RGBM)
                            {

                                for (int row = 0; row < 3; row++)
                                {
                                    for (int col = 0; col < 4; col++)
                                    {
                                        float v = bindPoseToBoneAnimated[row, col];
                                        Color c = EncodeFloatToRGBA32(v, minValue, maxValue);
                                        var frame = startFrame + j;
                                        var pixel = new Vector2Int(k * 12 + row * 4 + col, frame);
                                        atlasTexture.SetPixel(pixel.x, pixel.y, c);
                                    }
                                }
                            }

                        }

                        var boundsCheckMesh = new Mesh();
                        smr.BakeMesh(boundsCheckMesh);
                        var vertices = boundsCheckMesh.vertices;
                        var normals = boundsCheckMesh.normals;
                        for (var k = 0; k < vertices.Length; k++)
                        {
                            var _src = vertices[k];
                            var _tar = smr.transform.localScale;
                            var _v = new Vector3(_src.x / _tar.x, _src.y / _tar.y, _src.z / _tar.z);
                            m_BoundsMin = Vector3.Min(m_BoundsMin, _v);
                            m_BoundsMax = Vector3.Max(m_BoundsMax, _v);
                        }

                        boundsCheckMesh.Clear();
                    }
                }
                atlasTexture.Apply();

                var instanceMesh = smr.sharedMesh.Copy();
                var transformWeights = instanceMesh.boneWeights;
                var uv1 = new Vector4[transformWeights.Length];
                var uv2 = new Vector4[transformWeights.Length];
                for (var i = 0; i < transformWeights.Length; i++)
                {
                    var t = transformWeights[i];
                    uv1[i] = new Vector4(t.boneIndex0, t.boneIndex1, t.boneIndex2, t.boneIndex3);
                    uv2[i] = new Vector4(t.weight0, t.weight1, t.weight2, t.weight3);
                }
                instanceMesh.SetUVs(1, uv1);
                instanceMesh.SetUVs(2, uv2);
                instanceMesh.boneWeights = null;
                instanceMesh.bindposes = null;
                instanceMesh.bounds = new Bounds((m_BoundsMin + m_BoundsMax) / 2, m_BoundsMax - m_BoundsMin);


                var _mat = CreateMaterial(false, m_URPShader);
                _mat.name = meshName + "_Mat";
                _mat.mainTexture = smr.sharedMaterial.mainTexture;

                boundsCenter = (m_BoundsMax + m_BoundsMin) * 0.5f;
                var data = CreateInstance<GPUAnimationData>();
                data.m_Type = GPUAnimationType._2D;
                data.m_Mode = m_Mode;
                data.m_AnimationClips = instanceParams;
                data.m_ExposeTransforms = exposeTransformParams.ToArray();

                data.skinQuality = m_skinQuality;
                data.m_colorMode = m_colorMode;
                data.m_textureAlingMode = GPUTextureAlingMode._ALING_AUTO;
                data.boundsCenter = boundsCenter;
                data.boundsRange = new Vector3(minValue, maxValue, 0);

                atlasTexture.name = meshName + "_AnimationAtlas";
                instanceMesh.name = meshName + "_InstanceMesh";
                string pathname = string.Format("{0}{1}{2}_GPU_Bone{3}{4}", savePath, meshName, (m_URPShader ? "_URP" : ""), m_colorMode.ToString(), m_textureAlingMode.ToString() + "_UnLit.asset");

                data = CreateAssetCombination(pathname, data, new Object[] { _mat, atlasTexture, instanceMesh });
                var assets = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(data));
                foreach (var asset in assets)
                {
                    var atlas = asset as Texture2D;
                    var mesh = asset as Mesh;
                    var mat = asset as Material;
                    if (mat) data.m_BakeMat = mat;
                    if (atlas)
                        data.m_BakeTexture = atlas;
                    if (mesh)
                        data.m_BakedMesh = mesh;
                }
                EditorUtility.SetDirty(data);
                AssetDatabase.SaveAssets();


        }


        private int GetInstanceParams(AnimationClip[] _clips, out AnimationTickerClip[] instanceParams)
        {
            var totalHeight = 0;
            instanceParams = new AnimationTickerClip[_clips.Length];
            for (int i = 0; i < _clips.Length; i++)
            {
                var clip = _clips[i];
                var frameCount = (int)(clip.length * clip.frameRate);
                var instanceEvents = new AnimationTickerEvent[clip.events.Length];
                for (var j = 0; j < clip.events.Length; j++)
                {
                    instanceEvents[j] = new AnimationTickerEvent(clip.events[j], clip.frameRate, frameCount);
                }


                if (clip.legacy)
                {
                    instanceParams[i] = new AnimationTickerClip(i, clip.name, totalHeight, clip.frameRate, clip.length, clip.wrapMode == WrapMode.Loop, instanceEvents);
                }
                else
                {
                    instanceParams[i] = new AnimationTickerClip(i, clip.name, totalHeight, clip.frameRate, clip.length, clip.isLooping, instanceEvents);
                }

                totalHeight += frameCount;
            }
            return totalHeight;
        }


        #region Bone Tree
        private Transform targetTransform;
        private Vector2 scrollPosition;
        private Dictionary<int, bool> foldoutStates = new Dictionary<int, bool>();
        private Dictionary<int, bool> exposeBoneList = new Dictionary<int, bool>();
        private List<Transform> allChildren = new List<Transform>();
        private GUIStyle toggleStyle;
        private Texture2D prefabIcon;

        private void InitBoneTreeStyle()
        {
            // 获取Unity内置图标
            prefabIcon = EditorGUIUtility.FindTexture("PrefabModel Icon");

            // 自定义Toggle样式
            toggleStyle = new GUIStyle(EditorStyles.toggle)
            {
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(0, 0, 0, 0)
            };
        }
        private void DrawExposeBoneTree(GameObject target)
        {
            if (target == null) return;
            targetTransform = target.transform;
            if (allChildren.Count == 0)
            {
                UpdateChildList();
            }

            // 工具栏
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton))
            {
                UpdateChildList();
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Expand All", EditorStyles.toolbarButton))
            {
                foreach (var child in allChildren)
                {
                    if (child.childCount > 0)
                        foldoutStates[child.GetInstanceID()] = true;
                }
            }

            if (GUILayout.Button("Collapse All", EditorStyles.toolbarButton))
            {
                foreach (var child in allChildren)
                {
                    foldoutStates[child.GetInstanceID()] = false;
                }
            }
            EditorGUILayout.EndHorizontal();


            // 绘制树形结构
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            EditorGUI.BeginChangeCheck();
            DrawTransformTree();
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(targetTransform);
            }

            EditorGUILayout.EndScrollView();

        }

        private void DrawTransformTree()
        {
            if (allChildren.Count == 0) return;

            // 绘制子节点
            foreach (Transform child in targetTransform)
            {
                DrawTransformNode(child, 1);
            }
        }
        private void DrawTransformNode(Transform transform, int depth)
        {
            if (transform == null) return;

            int id = transform.GetInstanceID();
            bool hasChildren = transform.childCount > 0;

            // 初始化折叠状态
            if (!foldoutStates.ContainsKey(id))
                foldoutStates[id] = false;

            if (!exposeBoneList.ContainsKey(id))
            {
                exposeBoneList[id] = false;
            }

            EditorGUILayout.BeginHorizontal();

            // 缩进
            GUILayout.Space(depth * 16);

            // 选择和激活状态
            bool isSelected = Selection.activeTransform == transform;


            // 折叠/展开按钮
            if (hasChildren)
            {
                foldoutStates[id] = EditorGUILayout.Foldout(foldoutStates[id], transform.name, true, EditorStyles.foldout);
            }
            else
            {
                GUILayout.Space(16); // 占位保持对齐

                // 图标
                GUILayout.Label(prefabIcon, GUILayout.Width(16), GUILayout.Height(16));

                // 选择框
                EditorGUI.BeginChangeCheck();
                exposeBoneList[id] = EditorGUILayout.Toggle(exposeBoneList[id], toggleStyle, GUILayout.Width(14), GUILayout.Height(14));
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(transform.gameObject, "Toggle Active State");
                    Selection.activeTransform = transform;
                }
                // 名称标签
                GUIStyle nameStyle = new GUIStyle(EditorStyles.label);
                if (isSelected)
                {
                    nameStyle.normal.textColor = EditorGUIUtility.isProSkin
                        ? new Color(0.2f, 0.6f, 1f)
                        : new Color(0f, 0.3f, 0.6f);
                }

                EditorGUILayout.LabelField(transform.name, nameStyle);
            }


            EditorGUILayout.EndHorizontal();

            // 绘制子节点
            if (hasChildren && foldoutStates[id])
            {
                foreach (Transform child in transform)
                {
                    DrawTransformNode(child, depth + 1);
                }
            }
        }

        private void UpdateChildList()
        {
            allChildren.Clear();
            foldoutStates.Clear();
            exposeBoneList.Clear();
            if (targetTransform == null) return;

            // 递归收集所有子节点
            CollectChildrenRecursive(targetTransform);
        }

        private void CollectChildrenRecursive(Transform parent)
        {
            if (parent == null) return;

            allChildren.Add(parent);

            foreach (Transform child in parent)
            {
                CollectChildrenRecursive(child);
            }
        }

        private void OnSelectionChange()
        {
            Repaint();
        }

        #endregion
        #region common


        /// <summary>
        /// 将颜色分解为可表示的近似值（r1）和剩余误差（r2）
        /// </summary>
        /// <param name="s"></param>
        /// <param name="r1"></param>
        /// <param name="r2"></param>
        private void SplitColor(Color s, out Color r1, out Color r2)
        {
            r1 = new Color();
            r2 = new Color();
            for (int i = 0; i < 3; i++)
            {
                float t = s[i];
                int a = Mathf.FloorToInt(t * 256);//取整数部分
                r1[i] = a * 1.0f / 256; //并重新归一化到 [0, 1]

                r2[i] = t * 256 - Mathf.Floor(t * 256); //取小数部分，范围为 [0, 1)
            }
        }

        private void SplitColor(Color s, out Color r1, out Color r2, out Color r3)
        {
            r1 = new Color();
            r2 = new Color();
            r3 = new Color();
            for (int i = 0; i < 3; i++)
            {
                float t = s[i];
                r1[i] = Mathf.Floor(t * 256) / 256;
                r2[i] = t * 256 - Mathf.Floor(t * 256);
                r3[i] = t * 65536 - Mathf.Floor(t * 65536);
            }
        }

        private Color MergeColor(Color r1, Color r2)
        {
            Color result = new Color();
            for (int i = 0; i < 3; i++)
            {
                // 还原公式：s = (r1 * 256 + r2) / 256
                result[i] = r1[i] + r2[i] / 256;
            }
            return result;
        }
        /// <summary>
        /// 将一个float类型值转RGBA32
        /// </summary>
        Color EncodeFloatToRGBA32(float value, float minValue, float maxValue)
        {
            // 归一化到0~1
            float normalized = Mathf.Clamp01((value - minValue) / (maxValue - minValue));
            uint enc = (uint)(normalized * 4294967295.0f); // 0xFFFFFFFF
            byte r = (byte)((enc >> 24) & 0xFF);
            byte g = (byte)((enc >> 16) & 0xFF);
            byte b = (byte)((enc >> 8) & 0xFF);
            byte a = (byte)(enc & 0xFF);
            return new Color32(r, g, b, a);
        }

        private TextureFormat GetTextureFormat(GPUColorMode gPUColorMode)
        {
            switch (gPUColorMode)
            {
                case GPUColorMode._RGBAHALF:
                    {
                        return TextureFormat.RGBAHalf;
                    }
                case GPUColorMode._RGBM:
                    {
                        return TextureFormat.RGBA32;
                    }

                default:
                    return TextureFormat.RGBAHalf;
            }
        }

        // RGBM编码函数（标准实现）
        Color EncodeRGBM(Vector3 value)
        {
            float maxRange = 1.0f; //已经归一化了，不需要范围了
            Vector3 rgb = value / maxRange;
            float m = Mathf.Clamp01(Mathf.Max(rgb.x, Mathf.Max(rgb.y, rgb.z)));
            m = Mathf.Ceil(m * 255.0f) / 255.0f;
            rgb /= m > 0 ? m : 1.0f;
            return new Color(rgb.x, rgb.y, rgb.z, m);
        }
        private Material CreateMaterial(bool m_BakeNormal, bool isURP)
        {
            string shaderName = "";
            if (isURP)
            {
                shaderName = "GPUAnimShader/Spine/Skeleton-URP";
            }
            else
            {
                shaderName = "GPUAnimShader/Spine/Skeleton";
            }
            Material mat = new Material(Shader.Find(shaderName));
            return mat;
        }

        public GPUAnimationData CreateAssetCombination(string path, GPUAnimationData _mainAsset, IEnumerable<UnityEngine.Object> _subAssets, bool ping = true)
        {

            _mainAsset.name = Path.GetFileNameWithoutExtension(path);

            var previousAsset = AssetDatabase.LoadMainAssetAtPath(path);
            if (previousAsset != null)
            {
                AssetDatabase.DeleteAsset(path);
            }
            AssetDatabase.CreateAsset(_mainAsset, path);
            var mainAsset = AssetDatabase.LoadMainAssetAtPath(path) as GPUAnimationData;
            if (!mainAsset)
                throw new Exception("Invalid Main Assets:" + path);

            foreach (var dstAsset in _subAssets)
            {
                AssetDatabase.AddObjectToAsset(dstAsset, mainAsset);
            }
            EditorUtility.SetDirty(mainAsset);
            AssetDatabase.SaveAssetIfDirty(mainAsset);

            if (ping)
                EditorGUIUtility.PingObject(mainAsset);

            return mainAsset;
        }


        private bool SelectDirectory(UnityEngine.Object _srcAsset, out string directoryPath, out string objName)
        {
            directoryPath = "";
            objName = "";
            string folderPath = EditorUtility.SaveFilePanelInProject("Select Directory", _srcAsset.name, "asset", "Please enter a file name to save");
            if (folderPath.Length == 0)
                return false;
            directoryPath = Path.GetDirectoryName(folderPath) + "/";
            directoryPath = directoryPath.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            objName = Path.GetFileNameWithoutExtension(folderPath);
            Debug.Log($"folderPath: {folderPath}");
            Debug.Log($"directoryPath: {directoryPath} ");
            Debug.Log($"objName: {objName}");
            return true;
        }

        private string FileToAssetPath(string assetPath)
        {
            int assetIndex = assetPath.IndexOf("/Assets", StringComparison.Ordinal) + 1;
            assetPath = assetPath.Replace(@"\", @"/");
            if (assetIndex != 0)
                assetPath = assetPath.Substring(assetIndex, assetPath.Length - assetIndex);
            return assetPath;
        }

        private string GetPathName(string path)
        {
            path = RemoveExtension(path);
            int folderIndex = path.LastIndexOf('/');
            if (folderIndex >= 0)
                path = path.Substring(folderIndex + 1, path.Length - folderIndex - 1);
            return path;
        }
        private string RemoveExtension(string path)
        {
            int extensionIndex = path.LastIndexOf('.');
            if (extensionIndex >= 0)
                return path.Remove(extensionIndex);
            return path;
        }


        #endregion
    }

}