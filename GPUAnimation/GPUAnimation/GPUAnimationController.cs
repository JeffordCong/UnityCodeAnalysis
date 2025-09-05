using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions.Must;
namespace GPUAnimationLib
{
    /// <summary>
    /// GPU动画控制
    /// </summary>
    [RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
    public class GPUAnimationController : MonoBehaviour
    {
        public GPUAnimationData m_Data;
        public MeshFilter m_MeshFilter;
        public MeshRenderer m_MeshRenderer;
        public Action<string> OnAnimEvent;
        public AnimationTicker m_Ticker { get; private set; } = new AnimationTicker();

        MaterialPropertyBlock block;
        
        //hit 
        public bool isHit = false;
        public float transitionProgress = 0;
        public float hitTime = 0.5f;
        public Color hitColor = Color.red;

        protected void OnValidate() => Init();

        public void Init()
        {
            m_MeshFilter = GetComponent<MeshFilter>();
            m_MeshRenderer = GetComponent<MeshRenderer>();

            if (!m_Data) return;
            m_MeshFilter.sharedMesh = m_Data.m_BakedMesh;
            m_MeshRenderer.sharedMaterial = m_Data.m_BakeMat;
            m_Data.ApplyMaterial(m_MeshRenderer.sharedMaterial);
            m_Ticker.Setup(m_Data.m_AnimationClips);
            //shader效果不佳
            if (m_Data.m_Mode == GPUAnimationMode._ANIM_BONE)
            {
                m_Data.ApplySkinQualityMaterial(m_MeshRenderer.sharedMaterial,m_Data.skinQuality);
            }
            InitExposeBones();

        }


        private void Awake()
        {
            block = new MaterialPropertyBlock();

            // 新设备（Android 6.0+/iOS 8+）：基本支持rgbHalf，可放心使用。占用内存过于巨大，基本用骨骼动画，不用顶点动画
            // 旧设备或兼容性敏感场景：建议使用 RGBM 或 ASTC HDR 作为替代。
            // Unity示例：检查RGBAHalf支持
            bool supportsRGBAHalf = SystemInfo.SupportsTextureFormat(TextureFormat.RGBAHalf);
            if (supportsRGBAHalf)
            {
                // 使用RGBAHalf纹理
                Debug.Log("使用RGBAHalf纹理");
            }
            else
            {
                // 回退到RGBM或RGBA32
                Debug.Log("回退到RGBM或RGBA32纹理");
            }
        }

        public void Tick(float _deltaTime)
        {
            if (!m_Ticker.Tick(Time.deltaTime, out var output, OnAnimEvent)) return;
            if (block == null) block = new MaterialPropertyBlock();

            block.Clear();
            CheckHit(Time.deltaTime);
            output.ApplyPropertyBlock(block);
            if(m_MeshRenderer != null) m_MeshRenderer.SetPropertyBlock(block);
            TickExposeBones(output);
            
        }

        public void SetTime(float _time) => m_Ticker.SetTime(_time);
        public void SetTimeScale(float _scale) => m_Ticker.SetNormalizedTime(_scale);
        public float GetScale() => m_Ticker.GetNormalizedTime();


        public GPUAnimationController SetAnimation(int _animIndex)
        {
            m_Ticker.SetAnimation(_animIndex);
            return this;
        }

        #region ExposeBones
        [SerializeField]
        private Transform m_ExposeBoneParent;
        [SerializeField]
        private Transform[] m_ExposeBones;
        /// <summary>
        /// 创建骨骼挂载点：用于挂载武器，衣服，等节点父对象
        /// </summary>
       private void InitExposeBones()
        {
            var exposeBoners = m_Data.m_ExposeTransforms;
            if (exposeBoners == null || exposeBoners.Length <= 0)
            {
                m_ExposeBoneParent = null;
                m_ExposeBones = null;
                return;
            }
            if (m_ExposeBoneParent != null) return;
            m_ExposeBoneParent = new GameObject("Bones").transform;
            m_ExposeBoneParent.SetParent(transform);
            m_ExposeBoneParent.localPosition = Vector3.zero;
            m_ExposeBoneParent.localRotation = Quaternion.identity;
            m_ExposeBoneParent.localScale = Vector3.one;

            m_ExposeBones = new Transform[exposeBoners.Length];
            for (int i = 0; i < exposeBoners.Length; i++)
            {
                m_ExposeBones[i] = new GameObject(exposeBoners[i].name).transform;
                m_ExposeBones[i].SetParent(m_ExposeBoneParent);
            }

        }

        void TickExposeBones(AnimationTickerOutput _output)
        {
            var exposeBoners = m_Data.m_ExposeTransforms;
            if (exposeBoners == null || exposeBoners.Length <= 0)
            {
                return;
            }
            if (m_ExposeBones == null||m_ExposeBones.Length == 0) return;
            var is3D = m_Data.m_Type == GPUAnimationType._3D;
            Matrix4x4 recordMatrix = new Matrix4x4();
            for (int i = 0; i < exposeBoners.Length; i++)
            {
                if (m_ExposeBones[i] == null) continue;
                var t = exposeBoners[i];
                int boneIndex = t.index;

                recordMatrix.SetRow(0, Vector4.Lerp(ReadAnimationTexture(boneIndex, 0, _output.curFrame), ReadAnimationTexture(boneIndex, 0, _output.nextFrame), _output.interpolate));
                recordMatrix.SetRow(1, Vector4.Lerp(ReadAnimationTexture(boneIndex, 1, _output.curFrame), ReadAnimationTexture(boneIndex, 1, _output.nextFrame), _output.interpolate));
                recordMatrix.SetRow(2, Vector4.Lerp(ReadAnimationTexture(boneIndex, 2, _output.curFrame), ReadAnimationTexture(boneIndex, 2, _output.nextFrame), _output.interpolate));

                recordMatrix.SetRow(3, new Vector4(0, 0, 0, 1));

                if (is3D)
                {
                    m_ExposeBones[i].localPosition = recordMatrix.MultiplyPoint(t.position);
                    m_ExposeBones[i].localRotation = Quaternion.LookRotation(recordMatrix.MultiplyVector(t.direction));
                }
                else
                {
                    m_ExposeBones[i].localPosition = recordMatrix.GetPosition(); 
                    m_ExposeBones[i].localRotation = Quaternion.Euler(recordMatrix.rotation.eulerAngles);
                }

            }

        }

        Vector4 ReadAnimationTexture(int boneIndex, int row, int frame)
        {
            var pixel = new Vector2Int(boneIndex * 3 + row, frame);
            return m_Data.m_BakeTexture.GetPixel(pixel.x, pixel.y);
        }

        #endregion

        #region Hit
        public void EnableHit()
        {
            isHit = true;
            transitionProgress = 0;
        }
        private void CheckHit(float dt)
        {
            if (isHit)
            {
                // 计算权重
                transitionProgress = Mathf.Clamp01(transitionProgress + dt / hitTime);
                block.SetColor(GPUAnimationUtiliy.ID_RimColor, hitColor);
                block.SetFloat(GPUAnimationUtiliy.ID_RimTime, transitionProgress);

                // 过渡结束
                if (transitionProgress >= 1f)
                {
                    isHit = false;
                    transitionProgress = 0;
                    block.SetColor(GPUAnimationUtiliy.ID_RimColor, Color.white);
                    block.SetFloat(GPUAnimationUtiliy.ID_RimTime, 0);
                }
            }
        }

        #endregion

        void OnDestroy()
        {

            // #if UNITY_EDITOR
            //             UnityEditor.EditorApplication.delayCall += () =>
            //             {
            //                 if (m_ExposeBoneParent != null) DestroyImmediate(m_ExposeBoneParent.gameObject);
            //             };
            // #else
            //             if (m_ExposeBoneParent != null) Destroy(m_ExposeBoneParent.gameObject);
            // #endif

        }
    }
}