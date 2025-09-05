using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace GPUAnimationLib
{
    public static class GPUAnimationUtiliy
    {
        public static readonly int ID_AnimationTex = Shader.PropertyToID("_AnimTex");//GPU动画纹理
        public static readonly int ID_FrameBegin = Shader.PropertyToID("_AnimFrameBegin");
        static readonly int ID_FrameEnd = Shader.PropertyToID("_AnimFrameEnd");

        public static readonly int ID_RimColor = Shader.PropertyToID("_RimColor");
        public static readonly int ID_RimTime = Shader.PropertyToID("_RimTime");

        public static readonly int ID_FrameInterpolate = Shader.PropertyToID("_AnimFrameInterpolate");
       public static readonly int ID_BoundsCenter = Shader.PropertyToID("_BoundsCenter");
       public static readonly int ID_BoundsRange = Shader.PropertyToID("_BoundsRange");
        public static readonly int ID_VertexCount = Shader.PropertyToID("_VertexCount");


        // public static readonly int ID_AnimCurFrameB = Shader.PropertyToID("_AnimCurFrameB");
        // public static readonly int ID_AnimNextFrameB = Shader.PropertyToID("_AnimNextFrameB");
        // public static readonly int ID_AnimLerpB = Shader.PropertyToID("_AnimLerpB");
        // public static readonly int ID_AnimCrossWeight = Shader.PropertyToID("_AnimCrossWeight");

        public static void ApplyMaterial(this GPUAnimationData _data, Material material)
        {
            material.SetTexture(ID_AnimationTex, _data.m_BakeTexture);
            material.EnableKeywords(_data.m_Mode);
            material.EnableKeywords(_data.m_colorMode);
            if(_data.m_textureAlingMode == GPUTextureAlingMode._ALING_POW2)
                material.EnableKeywords(_data.m_textureAlingMode);
            material.SetVector(ID_BoundsCenter, _data.boundsCenter);
            material.SetVector(ID_BoundsRange, _data.boundsRange);
            material.SetInt(ID_VertexCount, _data.vertexCount);
        }

        public static void ApplyPropertyBlock(this AnimationTickerOutput _output, MaterialPropertyBlock _block)
        {
            _block.SetInt(ID_FrameBegin, _output.curFrame);
            _block.SetInt(ID_FrameEnd, _output.nextFrame);
            _block.SetFloat(ID_FrameInterpolate, _output.interpolate);

            // _block.SetInt(ID_AnimCurFrameB, _output.crossCurFrame);
            // _block.SetInt(ID_AnimNextFrameB, _output.crossNextFrame);
            // _block.SetFloat(ID_AnimLerpB, _output.crossInterpolate);

            // _block.SetFloat(ID_AnimCrossWeight, _output.crossWeight);

        }


        public static void ApplySkinQualityMaterial(this GPUAnimationData _data, Material material, GPUBoneMode gPUBoneMode)
        {
            material.EnableKeywords(_data.skinQuality);
        }

        public static Vector2Int GetTransformPixel(int _transformIndex, int row, int frame)
        {
            return new Vector2Int(_transformIndex * 3 + row, frame);
        }
        public static Vector2Int GetVertexPositionPixel(int _vertexIndex, int frame)
        {
            return new Vector2Int(_vertexIndex * 2, frame);
        }
        public static Vector2Int GetVertexNormalPixel(int _vertexIndex, int frame)
        {
            return new Vector2Int(_vertexIndex * 2 + 1, frame);
        }
    }
}