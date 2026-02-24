using System;

namespace ZEngine.Timer
{
    /// <summary>
    /// Represents a single timer task managed by <see cref="TimerManager"/>.
    /// </summary>
    public class Timer
    {
        private static int _nextId = 1;

        public int Id { get; }
        public float Interval { get; }
        public int RepeatCount { get; }
        public bool IsRunning { get; private set; }
        public bool IsPaused { get; private set; }

        private readonly Action _callback;
        private float _elapsed;
        private int _executedCount;

        internal Timer(float interval, Action callback, int repeatCount = 1)
        {
            Id = _nextId++;
            Interval = interval;
            RepeatCount = repeatCount;
            _callback = callback;
            IsRunning = true;
        }

        /// <summary>
        /// Advance the timer by deltaTime. Returns true when the timer has finished.
        /// </summary>
        internal bool Tick(float deltaTime)
        {
            if (!IsRunning || IsPaused) return false;

            _elapsed += deltaTime;
            if (_elapsed >= Interval)
            {
                _elapsed -= Interval;
                _executedCount++;
                _callback?.Invoke();

                // RepeatCount <= 0 means infinite repeat
                if (RepeatCount > 0 && _executedCount >= RepeatCount)
                {
                    IsRunning = false;
                    return true;
                }
            }
            return false;
        }

        public void Pause() => IsPaused = true;
        public void Resume() => IsPaused = false;

        public void Cancel()
        {
            IsRunning = false;
        }
    }
}
