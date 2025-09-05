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

    public class GPUAnimationSpineVertexBakeEditor : EditorWindow
    {

        [MenuItem("GPUAnimation/GPUAnimationBake_Spine顶点动画")]
        private static void ShowWindow()
        {
            var window = GetWindow<GPUAnimationSpineVertexBakeEditor>();
            window.titleContent = new GUIContent("GPUAnimationSpineVertexBakeEditor");
            window.Show();
        }

        private GameObject m_SrcPrefab;
        private SkeletonAnimation spineAnimation;
        private MeshFilter meshFilter;
        private MeshRenderer smr;

        private string m_BoneExposeRegex = "";
        private GPUAnimationMode m_Mode = GPUAnimationMode._ANIM_VERTEX;

        private GPUColorMode m_colorMode = GPUColorMode._RGBAHALF;
        //Spine动画默认30FPS。可以通过 skeletonData.Fps获取
        private int frameRate = 60;
        //开启纹理宽度2的幂次方：有的spine顶点数非常少，则开启控制宽度
        private GPUTextureAlingMode m_textureAlingMode = GPUTextureAlingMode._ALING_POW2;
        private int m_TextureWidth = 1024;

        /// <summary>
        /// URP渲染管线
        /// </summary>
        private bool m_URPShader = false;

        private Vector3 boundsCenter;
        private float maxBound;


        [SerializeField]
        public AnimationReferenceAsset[] m_TargetAnimations;

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
            animationProperty?.Dispose();
            serializedObject = null;
            reorderableList = null;
            animationProperty = null;
        }

        
        private void OnGUI()
        {
            EditorGUILayout.HelpBox("对Spine动画做GPU顶点动画功能", MessageType.Warning);
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
            EditorGUILayout.LabelField("Spine实例对象");
            m_SrcPrefab = (GameObject)EditorGUILayout.ObjectField(m_SrcPrefab, typeof(GameObject), true);
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


            EditorGUILayout.LabelField("GPU Animation Type:", "顶点动画");
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.Space();

            m_colorMode = (GPUColorMode)EditorGUILayout.EnumPopup("纹理保存格式:", m_colorMode);
            EditorGUILayout.Space();
            EditorGUILayout.Space();


            EditorGUILayout.LabelField("纹理宽度设置：有的Spine动画总帧数比顶点数大的时候，开启设置纹理宽度，避免创建的纹理图片内存过大");

            m_textureAlingMode = (GPUTextureAlingMode)EditorGUILayout.EnumPopup("纹理宽度设置:", m_textureAlingMode);
            if (m_textureAlingMode == GPUTextureAlingMode._ALING_POW2)
            {
                EditorGUILayout.Space();
                EditorGUI.indentLevel++;
                m_TextureWidth = EditorGUILayout.IntPopup("纹理宽度尺寸", m_TextureWidth, new string[] { "512", "1024", "2048", "4096" }, new int[] { 512, 1024, 2048, 4096 });
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();
            EditorGUILayout.Space();
            //烘焙GPU顶点动画
            if (GUILayout.Button("烘焙顶点动画"))
            {

                if (m_TargetAnimations == null || m_TargetAnimations.Length == 0 || m_SrcPrefab == null)
                {
                    Msg("资源没添加");
                    return;
                }

                GenerateVertexTexture(m_SrcPrefab, m_TargetAnimations);

            }


        }
        

        void GenerateVertexTexture(GameObject _targetFBX, AnimationReferenceAsset[] animationAssets)
        {

            if (!SelectDirectory(_targetFBX, out string savePath, out string meshName))
            {
                Msg("Invalid Folder Selected");
                return;
            }
            var _clips = animationAssets.Select(p=>p.Animation).ToArray();

            var instantiatedObj = _targetFBX;
            spineAnimation = instantiatedObj.GetComponent<SkeletonAnimation>();
            smr = instantiatedObj.GetComponent<MeshRenderer>();
            meshFilter = instantiatedObj.GetComponent<MeshFilter>();
            var mesh = meshFilter.sharedMesh;

            var vertexCount = mesh.vertexCount;
            var totalVertexRecord = vertexCount ;
            var totalFrame = GetInstanceParams(_clips, out AnimationTickerClip[] instanceParams);
            var textureFormat = GetTextureFormat(m_colorMode);
            int width = Mathf.NextPowerOfTwo(totalVertexRecord);
            int height = Mathf.NextPowerOfTwo(totalFrame);

            if (m_textureAlingMode == GPUTextureAlingMode._ALING_POW2)
            {
                //以m_TextureWidth划分总行数
                int lines = Mathf.CeilToInt((float)totalVertexRecord * totalFrame / m_TextureWidth);
                height = Mathf.NextPowerOfTwo(lines);
                width = Mathf.NextPowerOfTwo(m_TextureWidth);
            }

            var atlasTexture = new Texture2D(width, height, textureFormat, false)
            {
                filterMode = FilterMode.Point,
                wrapModeU = TextureWrapMode.Clamp,
                wrapModeV = TextureWrapMode.Repeat
            };

            Color[] colors = new Color[width * height];
            atlasTexture.SetPixels(colors);


            float BakeIncrement = 1f / frameRate;

            // 预计算归一化因子
            Vector3 boundsSize = Vector3.zero;
            boundsCenter = Vector3.zero;
            var bounds = mesh.bounds;
            // 计算包围盒信息用于归一化
            boundsSize = bounds.size;
            boundsCenter = bounds.center;

            maxBound = Mathf.Max(boundsSize.x, Mathf.Max(boundsSize.y, boundsSize.z));
            maxBound = Mathf.Max(maxBound, 0.0001f); // 防止除零

            Debug.Log($"maxBound:{maxBound}, boundsCenter:{boundsCenter}");

            for (int i = 0; i < _clips.Length; i++)
            {
                var clip = _clips[i];
                var length = clip.Duration;

                var frameCount = (int)(length * frameRate);
                var startFrame = instanceParams[i].frameBegin;
                //归位
                clip.Apply(spineAnimation.skeleton, 0, 0, false, null, 1f, MixBlend.Setup, MixDirection.In);
                spineAnimation.skeleton.UpdateWorldTransform(Skeleton.Physics.Update);
                // 更新网格
                spineAnimation.LateUpdate();

                float currentTime = 0;
                int steps = frameCount;
              
                //每帧采样动画
                for (int j = 1; j <= steps; j++)
                {
                    currentTime += BakeIncrement;
                    if (j == steps)
                        currentTime = clip.Duration;

                    clip.Apply(spineAnimation.skeleton, 0, currentTime, true, null, 1f, MixBlend.Setup, MixDirection.In);
                    spineAnimation.skeleton.UpdateWorldTransform(Skeleton.Physics.Update);
                    // 更新网格
                    spineAnimation.LateUpdate();
                    
                    var vertices = mesh.vertices;
                    
                    for (var k = 0; k < vertexCount; k++)
                    {
                        var frame = startFrame + j - 1;
                        if (m_colorMode == GPUColorMode._RGBAHALF)
                        {
                            if (m_textureAlingMode == GPUTextureAlingMode._ALING_POW2)
                            {
                                // 计算顶点数据的一维下标
                                int vertexIndex = (frame * vertexCount) + k;
                                //转1维下标保存
                                colors[vertexIndex] = new Color(vertices[k].x, vertices[k].y, vertices[k].z);
                            }
                            else
                            {
                                var pixel = new Vector2Int(k, frame);
                                atlasTexture.SetPixel(pixel.x, pixel.y, new Color(vertices[k].x, vertices[k].y, vertices[k].z));
                            }
                        }
                        else if (m_colorMode == GPUColorMode._RGBM)
                        {
                            if (m_textureAlingMode == GPUTextureAlingMode._ALING_POW2)
                            {
                                // 计算顶点数据的一维下标
                                int vertexIndex = (frame * vertexCount) + k;
                                var v = vertices[k];
                                // RGBM编码顶点位置 (归一化到[0,1])
                                Vector3 normalizedPos = (v - boundsCenter) / maxBound * 0.5f + Vector3.one * 0.5f;
                                var encodedPos = EncodeRGBM(normalizedPos);
                                //转1维下标保存
                                colors[vertexIndex] = encodedPos;
                            }
                            else
                            {
                                var v = vertices[k];
                                var pixel = new Vector2Int(k, frame);
                                // RGBM编码顶点位置 (归一化到[0,1])
                                Vector3 normalizedPos = (v - boundsCenter) / maxBound * 0.5f + Vector3.one * 0.5f;
                                var encodedPos = EncodeRGBM(normalizedPos);
                                atlasTexture.SetPixel(pixel.x, pixel.y, encodedPos);
                            }
                        }
                    }
                }

            }

            if (m_textureAlingMode == GPUTextureAlingMode._ALING_POW2) atlasTexture.SetPixels(colors);

            atlasTexture.Apply();

            var instanceMesh = mesh.Copy();
            //instanceMesh.normals = null;
            //instanceMesh.tangents = null;
            //instanceMesh.boneWeights = null;
            //instanceMesh.bindposes = null;
            //instanceMesh.bounds = new Bounds((m_BoundsMin + m_BoundsMax) / 2, m_BoundsMax - m_BoundsMin);

            var _mat = CreateMaterial(false, m_URPShader);
            _mat.name = meshName + "_Mat";
            _mat.mainTexture = smr.sharedMaterial.mainTexture;

            var data = CreateInstance<GPUAnimationData>();
            data.m_Mode = m_Mode;
            data.m_AnimationClips = instanceParams;
            data.m_colorMode = m_colorMode;
            data.m_textureAlingMode = m_textureAlingMode;
            data.vertexCount = vertexCount;
            data.boundsCenter = boundsCenter;
            data.boundsRange = new Vector3(0,maxBound,0);
            data.m_Type = GPUAnimationType._2D;

            atlasTexture.name = meshName + "_AnimationAtlas";
            instanceMesh.name = meshName + "_InstanceMesh";
            string pathname = string.Format("{0}{1}{2}_GPU_Vertex{3}{4}", savePath, meshName, (m_URPShader ? "_URP" : ""),m_colorMode.ToString(),  m_textureAlingMode.ToString()+"_UnLit.asset");
            data = CreateAssetCombination(pathname, data, new Object[] { _mat, atlasTexture, instanceMesh });
            var assets = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(data));
            foreach (var asset in assets)
            {
                var atlas = asset as Texture2D;
                var mesh1 = asset as Mesh;
                var mat = asset as Material;
                if (mat) data.m_BakeMat = mat;
                if (atlas)
                {
                    data.m_BakeTexture = atlas;
                }
                if (mesh1)
                    data.m_BakedMesh = mesh1;
            }
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
        }


        private int GetInstanceParams(Spine.Animation[] _clips, out AnimationTickerClip[] instanceParams)
        {
            var totalHeight = 0;
            instanceParams = new AnimationTickerClip[_clips.Length];
            for (int i = 0; i < _clips.Length; i++)
            {
                var clip = _clips[i];
                var frameCount = (int)(clip.Duration * frameRate);
                List<AnimationTickerEvent> instanceEvents = new List<AnimationTickerEvent>();
                foreach (var t in clip.Timelines)
                {
                    if (t is EventTimeline)
                    {
                        var eventTimeLines = (EventTimeline)t;
                        for(int j = 0; j < eventTimeLines.Events.Length; j++)
                        {
                            var e = eventTimeLines.Events[j];
                            var time = eventTimeLines.Frames[j];
                            var animEvent = new AnimationEvent();
                            animEvent.time = time;// e.Time;
                            animEvent.functionName = e.Data.Name;
                            instanceEvents.Add(new AnimationTickerEvent(animEvent, frameRate, frameCount));
                        }
                        
                    }
                }

                
                instanceParams[i] = new AnimationTickerClip(i, clip.Name, totalHeight,frameRate, clip.Duration, false, instanceEvents.ToArray());

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