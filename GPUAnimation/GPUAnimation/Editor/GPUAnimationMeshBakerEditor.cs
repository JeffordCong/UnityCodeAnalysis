using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System;
using UnityEngine.Rendering;
using System.Reflection;
using System.IO;
using Object = UnityEngine.Object;
using System.Text.RegularExpressions;

namespace GPUAnimationLib
{
    /// <summary>
    /// 对多Mesh和多Mat的Skin做GPU动画:需要MeshBaker插件
    /// </summary>

    public class GPUAnimationMeshBakerEditor : EditorWindow
    {

        [MenuItem("GPUAnimation/GPUAnimationBake_SkinMeshBaker插件")]
        private static void ShowWindow()
        {
            var window = GetWindow<GPUAnimationMeshBakerEditor>();
            window.titleContent = new GUIContent("GPUAnimationMeshBaker");
            window.Show();
        }

        private GameObject m_TargetPrefab;
        private GameObject m_SrcPrefab;

        private SkinnedMeshRenderer smr;
        private string m_BoneExposeRegex = "";
        private GPUAnimationMode m_Mode = GPUAnimationMode._ANIM_VERTEX;
        //蒙皮质量
        private GPUBoneMode m_skinQuality = GPUBoneMode._SKIN_BONE_4;
        private GPUColorMode m_colorMode = GPUColorMode._RGBAHALF;

        private int m_TextureWidth = 1024;

        /// <summary>
        /// 是否烘焙法线纹理动画:光照效果
        /// </summary>
        private bool m_BakeNormal = true;
        /// <summary>
        /// URP渲染管线
        /// </summary>
        private bool m_URPShader = false;

        private Vector3 boundsCenter;
        private float maxBound;


        [SerializeField]
        private AnimationClip[] m_TargetAnimations;

        private ReorderableList reorderableList;
        private SerializedObject serializedObject;
        private SerializedProperty animationProperty;
        private void OnEnable()
        {
            serializedObject = new SerializedObject(this);
            this.animationProperty = serializedObject.FindProperty("m_TargetAnimations");
            this.reorderableList = new ReorderableList(serializedObject, animationProperty);
            this.reorderableList.drawHeaderCallback = (rect) => { GUI.Label(rect, "Animation Clip List:"); };
            this.reorderableList.elementHeight = EditorGUIUtility.singleLineHeight;
            this.reorderableList.drawElementCallback = new ReorderableList.ElementCallbackDelegate(OnDrawElement);

            m_URPShader = GPUAnimationBakeEditor.DetectPipeline() != PipelineType.Default;
        }

        private void OnDisable()
        {
            serializedObject.Dispose();
            animationProperty.Dispose();
            serializedObject = null;
            reorderableList = null;
            animationProperty = null;
        }


        private void OnGUI()
        {
            EditorGUILayout.HelpBox("对多Mesh和多Mat的Skin做GPU动画，请先下载MeshBaker插件。\n"
            + "先用插件创建合并mesh的skin动画对象，再使用此插件创建GPU动画", MessageType.Warning);
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

            m_BakeNormal = EditorGUILayout.Toggle("是否需要光照(Normals)", m_BakeNormal);
            EditorGUILayout.Space();
            EditorGUILayout.Space();

            m_Mode = (GPUAnimationMode)EditorGUILayout.EnumPopup("GPU Animation Type:", m_Mode);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.Space();

            m_colorMode = (GPUColorMode)EditorGUILayout.EnumPopup("纹理保存格式:", m_colorMode);
            EditorGUILayout.Space();
            EditorGUILayout.Space();


            if (m_Mode == GPUAnimationMode._ANIM_VERTEX)
            {

                m_TextureWidth = EditorGUILayout.IntPopup("纹理宽度尺寸", m_TextureWidth, new string[] { "512", "1024", "2048", "4096" }, new int[] { 512, 1024, 2048, 4096 });

                EditorGUILayout.Space();
                EditorGUILayout.Space();
                //烘焙GPU顶点动画
                if (GUILayout.Button("烘焙顶点动画"))
                {

                    if (m_TargetAnimations == null || m_TargetAnimations.Length == 0 || m_TargetPrefab == null)
                    {
                        Msg($"资源没添加");
                        return;
                    }

                    GenerateVertexTexture(m_TargetPrefab, m_TargetAnimations);

                }
            }
            else
            {

                m_skinQuality = (GPUBoneMode)EditorGUILayout.EnumPopup("Skin Quality:", m_skinQuality);
                EditorGUILayout.Space();
                EditorGUILayout.Space();
                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.LabelField("挂载点Bone Regex:");
                m_BoneExposeRegex = GUILayout.TextArea(m_BoneExposeRegex);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space();
                EditorGUILayout.Space();
                EditorGUILayout.Space();

                //烘焙GPU骨骼动画
                if (GUILayout.Button("烘焙骨骼动画"))
                {

                    if (m_TargetAnimations == null || m_TargetAnimations.Length == 0 || m_TargetPrefab == null)
                    {
                        Msg($"资源没添加");
                        return;
                    }

                    GenerateBoneInstanceMeshAndTexture(m_TargetPrefab, m_TargetAnimations, m_BoneExposeRegex);


                }

            }
        }


        void GenerateVertexTexture(GameObject _targetFBX, AnimationClip[] _clips)
        {

            if (!SelectDirectory(m_SrcPrefab, out string savePath, out string meshName))
            {
                Msg("Invalid Folder Selected");
                return;
            }

            var instantiatedObj = _targetFBX;
            smr = instantiatedObj.GetComponentInChildren<SkinnedMeshRenderer>();

            // 每个顶点需要的元素数量（位置+法线）
            int vertexSize =  m_BakeNormal ? 2 : 1;//记录 vertex & normal

            var vertexCount = smr.sharedMesh.vertexCount;
            var totalVertexRecord = vertexCount * vertexSize;
            var totalFrame = GetInstanceParams(_clips, out AnimationTickerClip[] instanceParams);
            var textureFormat = GetTextureFormat(m_colorMode);

            // var atlasTexture = new Texture2D(Mathf.NextPowerOfTwo(totalVertexRecord), Mathf.NextPowerOfTwo(totalFrame), textureFormat, false)
            // {
            //     filterMode = FilterMode.Point,
            //     wrapModeU = TextureWrapMode.Clamp,
            //     wrapModeV = TextureWrapMode.Repeat
            // };
                
             //以m_TextureWidth划分总行数
            int lines = Mathf.CeilToInt((float)totalVertexRecord * totalFrame  / m_TextureWidth);
            var height = Mathf.NextPowerOfTwo(lines);
            var atlasTexture = new Texture2D(Mathf.NextPowerOfTwo(m_TextureWidth), height, textureFormat, false)
            {
                filterMode = FilterMode.Point,
                wrapModeU = TextureWrapMode.Clamp,
                wrapModeV = TextureWrapMode.Repeat
            };

            Color[] colors = new Color[m_TextureWidth * height];
            atlasTexture.SetPixels(colors);

            Vector3 m_BoundsMin = Vector3.zero;
            Vector3 m_BoundsMax = Vector3.zero;
            //统计最大，最小值
            for (int i = 0; i < _clips.Length; i++)
            {
                var clip = _clips[i];
                var vertexBakeMesh = new Mesh();
                var length = clip.length;
                var frameRate = clip.frameRate;
                var frameCount = (int)(length * frameRate);
                var startFrame = instanceParams[i].frameBegin;

                for (int j = 0; j < frameCount; j++)
                {
                    clip.SampleAnimation(m_SrcPrefab, length * j / frameCount);
                    smr.BakeMesh(vertexBakeMesh);
                    var vertices = vertexBakeMesh.vertices;

                    for (var k = 0; k < vertexCount; k++)
                    {
                        m_BoundsMin = Vector3.Min(m_BoundsMin, vertices[k]);
                        m_BoundsMax = Vector3.Max(m_BoundsMax, vertices[k]);

                    }
                    vertexBakeMesh.Clear();
                }
            }

            // 预计算归一化因子
            Vector3 boundsSize = Vector3.zero;
            boundsCenter = Vector3.zero;
            // 计算包围盒信息用于归一化
            boundsSize = m_BoundsMax - m_BoundsMin;
            boundsCenter = (m_BoundsMax + m_BoundsMin) * 0.5f;
           
            maxBound = Mathf.Max(boundsSize.x, Mathf.Max(boundsSize.y, boundsSize.z));
            maxBound = Mathf.Max(maxBound, 0.0001f); // 防止除零
        
            Debug.Log($"maxBound:{maxBound}, boundsCenter:{boundsCenter}");
            
              
            for (int i = 0; i < _clips.Length; i++)
            {
                var clip = _clips[i];
                var vertexBakeMesh = new Mesh();
                var length = clip.length;
                var frameRate = clip.frameRate;
                var frameCount = (int)(length * frameRate);
                var startFrame = instanceParams[i].frameBegin;
                for (int j = 0; j < frameCount; j++)
                {
                    clip.SampleAnimation(m_SrcPrefab, length * j / frameCount);//以原skin对象动画烘焙骨骼顶点动画
                    smr.BakeMesh(vertexBakeMesh);
                    var vertices = vertexBakeMesh.vertices;
                    var normals = vertexBakeMesh.normals;
                    for (var k = 0; k < vertexCount; k++)
                    {

                        var frame = startFrame + j;
                        // var pixel = new Vector2Int(k * vertexSize, frame);
                        // 计算顶点数据的一维下标
                        int vertexIndex = (frame * vertexCount * vertexSize) + (k * vertexSize);

                        if (m_colorMode == GPUColorMode._RGBAHALF)
                        {
                            // atlasTexture.SetPixel(pixel.x, pixel.y, new Color(vertices[k].x, vertices[k].y, vertices[k].z));
                            //转1维下标保存
                            colors[vertexIndex] = new Color(vertices[k].x, vertices[k].y, vertices[k].z);
                            if (m_BakeNormal)
                            {
                               // pixel = new Vector2Int(k * vertexSize + 1, frame);
                                //atlasTexture.SetPixel(pixel.x, pixel.y, new Color(normals[k].x, normals[k].y, normals[k].z));
                                //转1维下标保存
                                colors[vertexIndex+1] = new Color(normals[k].x, normals[k].y, normals[k].z);
                            }

                        }
                        else if (m_colorMode == GPUColorMode._RGBM)
                        {
                         
                            var v = vertices[k];
                            // RGBM编码顶点位置 (归一化到[0,1])
                            Vector3 normalizedPos = (v - boundsCenter) / maxBound * 0.5f + Vector3.one * 0.5f;
                            var encodedPos = EncodeRGBM(normalizedPos);
                            //atlasTexture.SetPixel(pixel.x, pixel.y, encodedPos);
                            //转1维下标保存
                            colors[vertexIndex] = encodedPos;
                            if (m_BakeNormal)
                            {
                                // RGBM编码法线 (已在单位球上，直接编码)
                                var n = normals[k];
                                normalizedPos = (n + Vector3.one) * 0.5f;
                                encodedPos = EncodeRGBM(normalizedPos);

                                //pixel = new Vector2Int(k * vertexSize + 1, frame);
                                //atlasTexture.SetPixel(pixel.x, pixel.y, encodedPos);
                                //转1维下标保存
                                colors[vertexIndex+1] = encodedPos;
                            }

                        }
                        
                    }

                    vertexBakeMesh.Clear();
                }

            }
            atlasTexture.SetPixels(colors);
            atlasTexture.Apply();

            var instanceMesh = smr.sharedMesh.Copy();
            instanceMesh.normals = null;
            instanceMesh.tangents = null;
            instanceMesh.boneWeights = null;
            instanceMesh.bindposes = null;
            instanceMesh.bounds = new Bounds((m_BoundsMin + m_BoundsMax) / 2, m_BoundsMax - m_BoundsMin);


            var _mat = CreateMaterial(m_BakeNormal, m_URPShader);
            _mat.name = meshName + "_Mat";

            var data = CreateInstance<GPUAnimationData>();
            data.m_Mode = m_Mode;
            data.m_AnimationClips = instanceParams;
            data.skinQuality = m_skinQuality;
            data.m_colorMode = m_colorMode;
            data.m_textureAlingMode = GPUTextureAlingMode._ALING_POW2;
            data.vertexCount = vertexCount;
            data.boundsCenter = boundsCenter;
            data.boundsRange = new Vector3(0,maxBound,0);

            atlasTexture.name = meshName + "_AnimationAtlas";
            instanceMesh.name = meshName + "_InstanceMesh";
            string pathname = string.Format("{0}{1}_GPU_Vertex{2}_Baker{3}{4}", savePath, meshName, (m_URPShader ? "_URP" : ""),m_colorMode.ToString(), (m_BakeNormal ? "_Lit.asset" : "_UnLit.asset"));
            data = CreateAssetCombination(pathname, data, new Object[] { _mat, atlasTexture, instanceMesh });
            var assets = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(data));
            foreach (var asset in assets)
            {
                var atlas = asset as Texture2D;
                var mesh = asset as Mesh;
                var mat = asset as Material;
                if (mat) data.m_BakeMat = mat;
                if (atlas)
                {
                    data.m_BakeTexture = atlas;
                }
                if (mesh)
                    data.m_BakedMesh = mesh;
            }
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
        }

        void GenerateBoneInstanceMeshAndTexture(GameObject _targetFBX, AnimationClip[] _clips, string exposeBones)
        {
            if (!SelectDirectory(m_SrcPrefab, out string savePath, out string meshName))
            {
                Msg("Invalid Folder Selected");
                return;
            }

            var instantiatedObj = _targetFBX;
            smr = instantiatedObj.GetComponentInChildren<SkinnedMeshRenderer>();

            try
            {
                //表示每个骨骼在绑定姿势（初始状态）下的变换矩阵。
                var bindPoses = smr.sharedMesh.bindposes;
                //包含了控制网格变形的所有骨骼的引用
                var bones = smr.bones;
                var exposeTransformParams = new List<GPUAnimationExposeBone>();
                if (!string.IsNullOrEmpty(exposeBones))
                {
                    var activeTransforms = m_SrcPrefab.GetComponentsInChildren<Transform>();
                    foreach (var t in activeTransforms)
                    {
                        if (!Regex.Match(t.name, exposeBones).Success)
                        {
                            continue;
                        }
                        var relativeBoneIndex = -1;
                        var relativeBone = t;
                        while (t != null)
                        {
                            relativeBoneIndex = bones.FindIndex(p => p == relativeBone);
                            if (relativeBoneIndex != -1) break;
                            relativeBone = relativeBone.parent;
                        }

                        if (relativeBoneIndex == -1) continue;
                        //将骨骼的世界坐标转换为模型根节点的局部坐标，以便在 GPU 动画计算中使用统一的坐标系，提高性能和稳定性。
                        var rootWorldToLocal = smr.transform.worldToLocalMatrix;
                        exposeTransformParams.Add(new GPUAnimationExposeBone()
                        {
                            index = relativeBoneIndex,
                            name = t.name,
                            position = rootWorldToLocal.MultiplyPoint(t.transform.position),
                            direction = rootWorldToLocal.MultiplyPoint(t.transform.forward),
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


                var _mat = CreateMaterial(m_BakeNormal, m_URPShader);
                _mat.name = meshName + "_Mat";


                boundsCenter = (m_BoundsMax + m_BoundsMin) * 0.5f;
                var data = CreateInstance<GPUAnimationData>();
                data.m_Mode = m_Mode;
                data.m_AnimationClips = instanceParams;
                data.m_ExposeTransforms = exposeTransformParams.ToArray();

                data.skinQuality = m_skinQuality;
                data.m_colorMode = m_colorMode;
                data.m_textureAlingMode = GPUTextureAlingMode._ALING_AUTO;
                data.boundsCenter = boundsCenter;
                data.boundsRange = new Vector3(minValue,maxValue,0);

                atlasTexture.name = meshName + "_AnimationAtlas";
                instanceMesh.name = meshName + "_InstanceMesh";
                string pathname = string.Format("{0}{1}_GPU_Bone{2}_Baker{3}{4}", savePath, meshName, (m_URPShader ? "_URP" : ""),m_colorMode.ToString(), (m_BakeNormal ? "_Lit.asset" : "_UnLit.asset"));

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
            catch (Exception ex)
            {
                Debug.LogError("Generate Failed:" + ex.Message);
                DestroyImmediate(instantiatedObj);
            }

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
                    instanceParams[i] = new AnimationTickerClip(i,clip.name, totalHeight, clip.frameRate, clip.length, clip.wrapMode == WrapMode.Loop, instanceEvents);
                }
                else
                {
                    instanceParams[i] = new AnimationTickerClip(i,clip.name, totalHeight, clip.frameRate, clip.length, clip.isLooping, instanceEvents);
                }
                
                totalHeight += frameCount;
            }
            return totalHeight;
        }


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
        /// <param name="value"></param>
        /// <param name="minValue"></param>
        /// <param name="maxValue"></param>
        /// <returns></returns>
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
                shaderName = m_BakeNormal ? "GPUAnimShader/Lit/GPUAnimation-Lit-URP" : "GPUAnimShader/Unlit/GPUAnimation-Unlit-URP";
            }
            else
            {
                shaderName = m_BakeNormal ? "GPUAnimShader/Lit/GPUAnimation-Lit" : "GPUAnimShader/Unlit/GPUAnimation-Unlit";
            }
            Material mat = new Material(Shader.Find(shaderName));
            mat.mainTexture = smr.sharedMaterial.mainTexture;
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

        private string RemoveExtension(string path)
        {
            int extensionIndex = path.LastIndexOf('.');
            if (extensionIndex >= 0)
                return path.Remove(extensionIndex);
            return path;
        }


        /// <summary>
        /// 检测渲染管线环境
        /// </summary>
        public static PipelineType DetectPipeline()
        {
#if UNITY_2019_1_OR_NEWER
            var rp = UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline;

#if UNITY_2021_3_OR_NEWER
            // Check if there is an override for this quality level
            rp = QualitySettings.renderPipeline != null ? QualitySettings.renderPipeline : rp;
#endif

            if (rp != null)
            {
                // SRP
                var srpType = rp.GetType().ToString();
                if (srpType.Contains("HDRenderPipelineAsset"))
                {
                    return PipelineType.HDRP;
                }
                else if (srpType.Contains("UniversalRenderPipelineAsset") || srpType.Contains("LightweightRenderPipelineAsset"))
                {
                    return PipelineType.URP;
                }
                else return PipelineType.Unsupported;
            }
#elif UNITY_2017_1_OR_NEWER
            if (UnityEngine.Rendering.GraphicsSettings.renderPipelineAsset != null) {
                // SRP not supported before 2019
                return PipelineType.Unsupported;
            }
#endif
            // no SRP
            return PipelineType.Default;
        }
        #endregion
    }

}