using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace GPUAnimationLib
{
    public enum GPUAnimationMode
    {
        /// <summary>
        /// GPU顶点动画模式
        /// </summary>
        _ANIM_VERTEX = 1,
        /// <summary>
        /// GPU骨骼动画模式
        /// </summary>
        _ANIM_BONE = 2,
    }

    public enum GPUAnimationType
    {
        _2D=0,
        _3D, 
    }

    public enum GPUBoneMode
    {
        
        _SKIN_BONE_1 = 1,
        //
        // 摘要:
        //     Use 2 bones to deform a single vertex. (The most important bones will be used).
        _SKIN_BONE_2 = 2,
        //
        // 摘要:
        //     Use 4 bones to deform a single vertex.
        _SKIN_BONE_4 = 4
    }

    /// <summary>
    /// 设置数据保存到纹理上的精度
    /// </summary>
    public enum GPUColorMode
    {
        //16bit
        _RGBAHALF = 0,
        //8bit RGBM编码
        _RGBM = 1,
    }
    /// <summary>
    /// 纹理宽度对齐方式
    /// </summary>
    public enum GPUTextureAlingMode
    {
        //默认顶点数对齐
        _ALING_AUTO =0,
        //2的幂次方对齐
        _ALING_POW2 =1,
    }

    public class GPUAnimationData : ScriptableObject
    {
        public GPUAnimationType m_Type = GPUAnimationType._3D;
        public GPUAnimationMode m_Mode;
        public Mesh m_BakedMesh;
        public Texture2D m_BakeTexture;
        public Material m_BakeMat;
        public GPUBoneMode skinQuality;
      //  [HideInInspector]
        public GPUColorMode m_colorMode;
        public GPUTextureAlingMode m_textureAlingMode = GPUTextureAlingMode._ALING_POW2;
        //shader data
        public int vertexCount;
        public Vector3 boundsCenter;
        /// <summary>
        /// x: min, y:max
        /// </summary> <summary>
        public Vector3 boundsRange;

        public AnimationTickerClip[] m_AnimationClips;
        public GPUAnimationExposeBone[] m_ExposeTransforms;
    }


    [Serializable]
    /// <summary>
    /// 骨骼挂载节点：武器，特效。。。
    /// </summary>
    public struct GPUAnimationExposeBone
    {
        public string name;
        public int index;
        public Vector3 position;
        public Vector3 direction;
    }
}