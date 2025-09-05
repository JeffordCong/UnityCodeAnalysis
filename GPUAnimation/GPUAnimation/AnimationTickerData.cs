using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace GPUAnimationLib
{
    [Serializable]
    public struct AnimationTickerClip
    {
        public string name;
        public int id;
        public int frameBegin;
        public int frameCount;
        public bool loop;
        public float length;
        public float frameRate;

        [Header("进入下一个动画过度持续时间")]
        /// <summary>
        /// 进入下一个动画过度持续时间
        /// </summary>
        public float CrossFadeDuration;
        [Header("需要过度的动画下标ID")]
        /// <summary>
        /// 需要过度的动画下标ID
        /// </summary>
        public int CrossFadeAnimationID;
        [Header("当前动画退出帧下标(退出时间x帧数)")]
        /// <summary>
        /// 当前动画退出帧下标
        /// </summary>
        public int CrossFadeExitFrame;


        public AnimationTickerEvent[] m_Events;
        public AnimationTickerClip(int _id,string _name, int _startFrame, float _frameRate, float _length, bool _loop, AnimationTickerEvent[] _events)
        {
            id = _id;
            name = _name;
            frameBegin = _startFrame;
            frameRate = _frameRate;
            frameCount = (int)(_frameRate * _length);
            m_Events = _events;
            length = _length;
            loop = _loop;
      
            CrossFadeDuration = 0;
            CrossFadeAnimationID = 0;
            CrossFadeExitFrame = 0;

        }

        /// <summary>
        /// 动画过度
        /// </summary>
        public void CrossFade(int animationId, float duration)
        {
            var CrossFadeExitTime = Mathf.Clamp01(1 - duration);
            CrossFadeDuration = duration;
            CrossFadeAnimationID = animationId;
            //当前动画退出时间
            float exitTime = CrossFadeExitTime * length;
            CrossFadeExitFrame = Mathf.FloorToInt(exitTime * frameRate);
        }
        /// <summary>
        /// 动画过度
        /// </summary>
        /// <param name="animationId">动画下标id</param>
        /// <param name="exitTime">当前动画退出时间</param>
        /// <param name="duration">动画过度时间</param>
        public void CrossFade(int animationId,float exitTime, float duration)
        {
            var CrossFadeExitTime = exitTime;
            CrossFadeDuration = duration;
            CrossFadeAnimationID = animationId;

            CrossFadeExitFrame = Mathf.FloorToInt(CrossFadeExitTime * frameRate);
        }

        public void RegisterEvents(string eventName,float time)
        {
            var animEvent = new AnimationEvent();
            animEvent.time = time;
            animEvent.functionName = eventName;
            Array.Resize(ref m_Events, m_Events.Length + 1);
            m_Events[m_Events.Length - 1] = new AnimationTickerEvent(animEvent, frameRate, frameCount);
        }
    }

    [Serializable]
    public struct AnimationTickerEvent
    {
        /// <summary>
        /// 触发事件的帧数下标
        /// </summary>
        public float keyFrame;
        public string functionName;
        public AnimationTickerEvent(AnimationEvent _event,float frameRate ,int frameCount)
        {
            keyFrame = Mathf.Min(_event.time * frameRate, frameCount);//避免超过总帧数
            functionName = _event.functionName;
        }
    }


    public struct AnimationTickerOutput
    {
        public int curFrame;
        public int nextFrame;
        public float interpolate;

        // 新增
        // public int crossCurFrame;
        // public int crossNextFrame;
        // public float crossInterpolate;
        // public float crossWeight; // B动画权重，A动画权重=1-crossWeight

    }

}