//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;
using ZEngine.Core;
using ZEngine.Manager.Timer;
using ZEngine.Reference;

namespace ZEngine.Manager.Timer
{
    public class TimerManager : ManagerSingleton<TimerManager>, IManager
    {
        private List<Timer> _timers = new List<Timer>();
        private List<Timer> _finishedTimers = new List<Timer>();

        public void OnInit(object param)
        {
            _root = new GameObject("[Z][TimerManager]");
            GameObject.DontDestroyOnLoad(_root);
        }

        public void OnUpdate()
        {

            foreach (var timer in _timers)
            {
                if (!timer.Update(Time.deltaTime))
                {
                    if (timer.IsOver)
                        _finishedTimers.Add(timer);
                }
            }

            foreach (var timer in _finishedTimers)
            {
                _timers.Remove(timer);
                ReferencePool.Release(timer);
            }
            _finishedTimers.Clear();
        }

        public void OnGUI()
        {

        }

        public void OnDestroy()
        {
            DestroySingleton();
        }

        /// <summary>
        /// 创建定时器
        /// </summary>
        /// <param name="delay">延迟时间</param>
        /// <param name="interval">间隔时间</param>
        /// <param name="duration">运行时间</param>
        /// <param name="maxTriggerCount">最大触发次数</param>
        /// <returns></returns>
        public Timer CreateTimer(Action callback, float delay, float interval = -1, float duration = -1, long maxTriggerCount = 1)
        {
            Timer timer = ReferencePool.Spawn(typeof(Timer)) as Timer;
            timer.Initialize(callback, delay, interval, duration, maxTriggerCount);
            _timers.Add(timer);
            return timer;
        }

        // <summary>
        /// 延迟后，触发一次
        /// </summary>
        public Timer CreateOnceTimer(Action callback, float delay)
        {
            return CreateTimer(callback, delay, -1, -1, 1);
        }

        /// <summary>
        /// 延迟后，永久性的间隔触发
        /// </summary>
        /// <param name="delay">延迟时间</param>
        /// <param name="interval">间隔时间</param>
        public Timer CreatePepeatTimer(Action callback, float delay, float interval)
        {
            return CreateTimer(callback, delay, interval, -1, -1);
        }

        /// <summary>
        /// 延迟后，在一段时间内间隔触发
        /// </summary>
        /// <param name="delay">延迟时间</param>
        /// <param name="interval">间隔时间</param>
        /// <param name="duration">触发周期</param>
        public Timer CreatePepeatTimer(Action callback, float delay, float interval, float duration)
        {
            return CreateTimer(callback, delay, interval, duration, -1);
        }

        /// <summary>
        /// 延迟后，间隔触发一定次数
        /// </summary>
        /// <param name="delay">延迟时间</param>
        /// <param name="interval">间隔时间</param>
        /// <param name="maxTriggerCount">最大触发次数</param>
        public Timer CreatePepeatTimer(Action callback, float delay, float interval, long maxTriggerCount)
        {
            return CreateTimer(callback, delay, interval, -1, maxTriggerCount);
        }

        /// <summary>
        /// 延迟后，在一段时间内触发
        /// </summary>
        /// <param name="delay">延迟时间</param>
        /// <param name="duration">触发周期</param>
        public Timer CreateDurationTimer(Action callback, float delay, float duration)
        {
            return CreateTimer(callback, delay, -1, duration, -1);
        }

        /// <summary>
        /// 延迟后，永久触发
        /// </summary>
        public Timer CreateForeverTimer(Action callback, float delay)
        {
            return CreateTimer(callback, delay, -1, -1, -1);
        }
    }
}
