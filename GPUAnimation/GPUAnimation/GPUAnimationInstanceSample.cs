using System.Collections;
using System.Collections.Generic;
using GPUAnimationLib;
using UnityEngine;

public class GPUAnimationInstanceSample : MonoBehaviour
{
    public int m_X = 32, m_Y = 32;
    public GPUAnimationData m_data;
    public Material m_mat;
    AnimationTicker[] m_Timers;

    Matrix4x4[] m_Matrixs;
    MaterialPropertyBlock m_Blocks;
    float[] curFrames;
    float[] nextFrames;
    float[] interpolates;

    void Awake()
    {
        int totalCount = m_X * m_Y;

        m_Matrixs = new Matrix4x4[totalCount];
        m_Timers = new AnimationTicker[totalCount];
        curFrames = new float[totalCount];
        nextFrames = new float[totalCount];
        interpolates = new float[totalCount];
        m_Blocks = new MaterialPropertyBlock();
        if (m_mat != null) m_mat.enableInstancing = true;
        for (int i = 0; i < m_X; i++)
        {
            for (int j = 0; j < m_Y; j++)
            {
                int index = i * m_Y + j;
                m_Matrixs[index] = Matrix4x4.TRS(transform.position + new Vector3(i * 10, 0, j * 10), transform.rotation, Vector3.one * (.8f + (Random.value * 2 - 1) * .2f));
                m_Timers[index] = new AnimationTicker();
                m_Timers[index].Setup(m_data.m_AnimationClips);
                m_Timers[index].SetAnimation(Random.Range(0, m_data.m_AnimationClips.Length));
                m_Timers[index].SetNormalizedTime(Random.value);
            }
        }

        m_data.ApplyMaterial(m_mat);
    }

    // Update is called once per frame
    void Update()
    {
        float dt = Time.deltaTime;

        for (int i = 0; i < m_X; i++)
        {
            for (int j = 0; j < m_Y; j++)
            {
                int index = i * m_Y + j;
                m_Timers[index].Tick(dt, out var output);
                //测试，非loop的动画也设置循环
                if (!m_Timers[index].m_Anim.loop && (m_Timers[index].m_TimeElapsed >= m_Timers[index].m_Anim.length))
                    m_Timers[index].SetNormalizedTime(0f);

                curFrames[index] = output.curFrame;
                nextFrames[index] = output.nextFrame;
                interpolates[index] = output.interpolate;
            }
        }


        m_Blocks.SetFloatArray("_AnimFrameBegin",curFrames);
        m_Blocks.SetFloatArray("_AnimFrameEnd",nextFrames);
  
        m_Blocks.SetFloatArray(GPUAnimationUtiliy.ID_FrameInterpolate,interpolates);
        Graphics.DrawMeshInstanced(m_data.m_BakedMesh, 0, m_mat, m_Matrixs,m_Matrixs.Length, m_Blocks, UnityEngine.Rendering.ShadowCastingMode.On,true);


    }
}
