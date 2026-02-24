using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZEngine.Timer
{
    /// <summary>
    /// Manages all timers in ZEngine.
    /// Supports one-shot and repeating timers.
    /// </summary>
    public class TimerManager : Core.MonoSingleton<TimerManager>
    {
        private readonly List<Timer> _timers = new List<Timer>();
        private readonly List<Timer> _pendingAdd = new List<Timer>();

        protected override void OnInit()
        {
            Debug.Log("[TimerManager] Initialized.");
        }

        /// <summary>
        /// Schedule a one-shot callback after a delay in seconds.
        /// </summary>
        /// <returns>The created Timer (can be used to cancel).</returns>
        public Timer Delay(float delay, Action callback)
        {
            var timer = new Timer(delay, callback, 1);
            _pendingAdd.Add(timer);
            return timer;
        }

        /// <summary>
        /// Schedule a repeating callback at a fixed interval.
        /// </summary>
        /// <param name="interval">Interval in seconds between callbacks.</param>
        /// <param name="callback">The callback to invoke.</param>
        /// <param name="repeatCount">Number of times to repeat. 0 or negative means infinite.</param>
        /// <returns>The created Timer (can be used to cancel/pause).</returns>
        public Timer Repeat(float interval, Action callback, int repeatCount = 0)
        {
            var timer = new Timer(interval, callback, repeatCount);
            _pendingAdd.Add(timer);
            return timer;
        }

        /// <summary>
        /// Cancel a timer by ID.
        /// </summary>
        public void Cancel(int timerId)
        {
            foreach (var t in _timers)
            {
                if (t.Id == timerId) { t.Cancel(); return; }
            }
            foreach (var t in _pendingAdd)
            {
                if (t.Id == timerId) { t.Cancel(); return; }
            }
        }

        /// <summary>
        /// Cancel all active timers.
        /// </summary>
        public void CancelAll()
        {
            foreach (var t in _timers) t.Cancel();
            _pendingAdd.Clear();
        }

        private void Update()
        {
            // Add pending timers
            if (_pendingAdd.Count > 0)
            {
                _timers.AddRange(_pendingAdd);
                _pendingAdd.Clear();
            }

            float dt = Time.deltaTime;
            for (int i = _timers.Count - 1; i >= 0; i--)
            {
                bool finished = _timers[i].Tick(dt);
                if (finished || !_timers[i].IsRunning)
                {
                    _timers.RemoveAt(i);
                }
            }
        }
    }
}
