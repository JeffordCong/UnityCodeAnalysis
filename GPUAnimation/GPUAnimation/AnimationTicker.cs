using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace GPUAnimationLib
{
    public class AnimationTicker
    {
        /// <summary>
        /// 当前动画播放下标
        /// </summary>
        public int m_AnimIndex { get; private set; }
        /// <summary>
        /// 从动画开始到当前时刻所经过的时间
        /// </summary>
        public float m_TimeElapsed {  get; private set; }

        public AnimationTickerClip m_Anim=> m_Animations[m_AnimIndex];

        private AnimationTickerClip[] m_Animations;


        #region CrossFade

        private float transitionProgress = 0f;
        private bool isCrossFading = false;
        private AnimationTickerClip crossAnimation;
        private int crossFadeNextFrame = 0;
        private float crossFadeTimeElapsed = 0f;
        #endregion

        public void Setup(AnimationTickerClip[] anims)
        {
            m_Animations = anims;
        }

        public void Reset()
        {
            m_AnimIndex = 0;
            m_TimeElapsed = 0;
        }

        public void SetTime(float time)
        {
            m_TimeElapsed = time;
        }

        public void SetNormalizedTime(float _scale)
        {
            if (m_AnimIndex < 0 || m_AnimIndex >= m_Animations.Length)
            {
                return;
            }

            m_TimeElapsed = m_Anim.length * _scale;
        }

        public float GetNormalizedTime()
        {
            if (m_AnimIndex < 0 || m_AnimIndex >= m_Animations.Length)
            {
                return 0f;
            }
            return m_TimeElapsed / m_Anim.length;
        }

        public void SetAnimation(int _animIndex)
        {
            m_TimeElapsed = 0;
            if(_animIndex <0 ||  _animIndex >= m_Animations.Length)
            {
                Debug.LogError("Invalid Animation index Found:"+ _animIndex );
                return;
            }
            m_AnimIndex = _animIndex;
        }

        /// <summary>
        /// 在动画播放过程中，根据时间的流逝检查是否有动画事件需要触发
        /// </summary>
        /// <param name="_tickerClip"></param>
        /// <param name="_timeElapsed">从动画开始到当前时刻所经过的时间</param>
        /// <param name="_deltaTime">从上一帧到当前帧所经过的时间间隔</param>
        /// <param name="_onEvents">动画事件</param>
        void TickEvents(AnimationTickerClip _tickerClip, float _timeElapsed, float _deltaTime, Action<string> _onEvents)
        {
            float lastFrame = _timeElapsed * _tickerClip.frameRate; // 上一帧数下标
            float nextFrame = lastFrame + _deltaTime * _tickerClip.frameRate; // 下一帧数下标

            // 处理循环动画：计算实际帧范围（取模确保在 [0, frameCount) 内）
            float effectiveLastFrame = _tickerClip.loop ? lastFrame % _tickerClip.frameCount : lastFrame;
            float effectiveNextFrame = _tickerClip.loop ? nextFrame % _tickerClip.frameCount : nextFrame;

            // 标记是否跨循环边界（例如从最后一帧到第一帧）
            bool crossedLoopBoundary = _tickerClip.loop && effectiveNextFrame < effectiveLastFrame;

            foreach (var animEvent in _tickerClip.m_Events)
            {
                float keyFrame = animEvent.keyFrame;

                // 对于循环动画，确保 keyFrame 在有效范围内（取模处理）
                float effectiveKeyFrame = _tickerClip.loop ? keyFrame % _tickerClip.frameCount : keyFrame;

                // 检查事件是否在当前帧范围内触发
                bool isEventInRange = false;

                if (crossedLoopBoundary)
                {
                    // 跨边界情况：事件在 lastFrame 到 frameCount 之间，或 0 到 nextFrame 之间
                    isEventInRange = (effectiveKeyFrame > effectiveLastFrame && effectiveKeyFrame < _tickerClip.frameCount) ||
                                     (effectiveKeyFrame >= 0 && effectiveKeyFrame <= effectiveNextFrame);
                }
                else
                {
                    // 普通情况：事件在 lastFrame 和 nextFrame 之间
                    isEventInRange = effectiveKeyFrame > effectiveLastFrame && effectiveKeyFrame <= effectiveNextFrame;
                }

                if (isEventInRange)
                {
                    _onEvents(animEvent.functionName);
                }
            }
        }
        /// <summary>
        /// 动画帧更新
        /// </summary>
        /// <param name="_deltaTime">从上一帧到当前帧所经过的时间间隔</param>
        /// <param name="output">输出当前动画帧的相关信息，包括当前帧、下一帧以及插值因子。</param>
        /// <param name="_onEvents">动画事件</param>
        /// <returns></returns>
        public bool Tick1(float _deltaTime,out AnimationTickerOutput output,Action<string> _onEvents = null)
        {
            output = default;
            if(m_AnimIndex <0 || m_AnimIndex >= m_Animations.Length)
            {
                return false;
            }

            var param = m_Anim;
            if(_onEvents != null)
            {
                TickEvents(param, m_TimeElapsed, _deltaTime, _onEvents);
            }
            //更新从动画开始到当前时刻所经过的总时间
            m_TimeElapsed += _deltaTime;

            //根据动画是否循环播放，分别计算当前帧和下一帧的索引。
            int curFrame, nextFrame;
            float framePassed;
            if(param.loop)
            {
                framePassed = (m_TimeElapsed % param.length) * param.frameRate;
                curFrame = Mathf.FloorToInt(framePassed) % param.frameCount;
                nextFrame = (curFrame+1)% param.frameCount;
            }
            else
            {
                framePassed= Mathf.Min(param.length, m_TimeElapsed) * param.frameRate;
                curFrame = Mathf.Min(Mathf.FloorToInt(framePassed), param.frameCount - 1);
                nextFrame = Mathf.Min(curFrame +1, param.frameCount -1);

            }

            curFrame += param.frameBegin;
            nextFrame += param.frameBegin;
            //将 framePassed 取余并限制在 [0, 1) 范围内以得到当前帧和下一帧之间的插值因子
            //当插值因子为 0 时，表示刚进入当前帧；当插值因子为 1 时，表示即将进入下一帧。
            //通过这个插值因子，可以在当前帧和下一帧的图像数据之间进行线性插值，从而实现平滑过渡。
            framePassed %= 1;

            output = new AnimationTickerOutput { curFrame = curFrame, nextFrame = nextFrame,interpolate = framePassed };
            return true;
        }



        public bool Tick(float _deltaTime, out AnimationTickerOutput output, Action<string> _onEvents = null)
        {
            output = default;
            if (m_AnimIndex < 0 || m_AnimIndex >= m_Animations.Length)
            {
                return false;
            }

            var param = m_Anim;
            if (_onEvents != null)
            {
                TickEvents(param, m_TimeElapsed, _deltaTime, _onEvents);
            }
            //更新从动画开始到当前时刻所经过的总时间
            m_TimeElapsed += _deltaTime;

            //根据动画是否循环播放，分别计算当前帧和下一帧的索引。
            int curFrame, nextFrame;
            float framePassed;
            if (param.loop)
            {
                framePassed = (m_TimeElapsed % param.length) * param.frameRate;
                curFrame = Mathf.FloorToInt(framePassed) % param.frameCount;
                nextFrame = (curFrame + 1) % param.frameCount;
            }
            else
            {
                framePassed = Mathf.Min(param.length, m_TimeElapsed) * param.frameRate;
                curFrame = Mathf.Min(Mathf.FloorToInt(framePassed), param.frameCount - 1);
                nextFrame = Mathf.Min(curFrame + 1, param.frameCount - 1);
            }

            curFrame += param.frameBegin;
            nextFrame += param.frameBegin;

            //将 framePassed 取余并限制在 [0, 1) 范围内以得到当前帧和下一帧之间的插值因子
            //当插值因子为 0 时，表示刚进入当前帧；当插值因子为 1 时，表示即将进入下一帧。
            //通过这个插值因子，可以在当前帧和下一帧的图像数据之间进行线性插值，从而实现平滑过渡。
            framePassed %= 1;

            //output = new AnimationTickerOutput { curFrame = curFrame, nextFrame = nextFrame, interpolate = framePassed };

            if (isCrossFading == false && param.CrossFadeExitFrame > 0 && curFrame >= param.CrossFadeExitFrame + param.frameBegin)
            {
                isCrossFading = true;
                transitionProgress = 0;
                crossAnimation = m_Animations[param.CrossFadeAnimationID];
                crossFadeNextFrame = crossAnimation.frameBegin;
                crossFadeTimeElapsed = 0f; // 新增
            }

            if (isCrossFading)
            {
                // 过渡期间，A动画
                int aCurFrame = curFrame;
                // int aNextFrame = nextFrame;
                // float aInterpolate = framePassed;

                // 过渡期间，B动画
                crossFadeTimeElapsed += _deltaTime;
                float crossFramePassed = (crossFadeTimeElapsed % crossAnimation.length) * crossAnimation.frameRate;
                int bCurFrame = Mathf.FloorToInt(crossFramePassed) % crossAnimation.frameCount + crossAnimation.frameBegin;
                // int bNextFrame = (Mathf.FloorToInt(crossFramePassed) + 1) % crossAnimation.frameCount + crossAnimation.frameBegin;
                // float bInterpolate = crossFramePassed % 1;

                // 计算权重
                transitionProgress = Mathf.Clamp01(transitionProgress + _deltaTime / param.CrossFadeDuration);

                // output = new AnimationTickerOutput
                // {
                //     curFrame = aCurFrame,
                //     nextFrame = aNextFrame,
                //     interpolate = aInterpolate,
                //     crossCurFrame = bCurFrame,
                //     crossNextFrame = bNextFrame,
                //     crossInterpolate = bInterpolate,
                //     crossWeight = transitionProgress
                // };
                output = new AnimationTickerOutput
                {
                    curFrame = aCurFrame,
                    nextFrame = bCurFrame,
                    interpolate = transitionProgress,
                };
                // 过渡结束
                if (transitionProgress >= 1f)
                {
                    isCrossFading = false;
                    m_AnimIndex = param.CrossFadeAnimationID;
                    m_TimeElapsed = crossFadeTimeElapsed; // 切换到B动画的时间
                }
                
            }
            else
            {
                output = new AnimationTickerOutput
                {
                    curFrame = curFrame,
                    nextFrame = nextFrame,
                    interpolate = framePassed,
                    // crossCurFrame = 0,
                    // crossNextFrame = 0,
                    // crossInterpolate = 0,
                    // crossWeight = 0
                };
           
            }

            return true;
        }

    }

}