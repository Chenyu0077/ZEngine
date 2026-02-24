//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using System;
using ZEngine.Reference;

namespace ZEngine.Manager.Timer
{
    /// <summary>
	///  综合计时器
	/// </summary>
	public sealed class Timer : IReference
    {

        private float _intervalTime;
        private float _durationTime;
        private long _maxTriggerCount;

        // 需要重置的变量
        private float _delayTimer = 0;
        private float _durationTimer = 0;
        private float _intervalTimer = 0;
        private long _triggerCount = 0;

        /// <summary>
        /// 延迟时间
        /// </summary>
        public float DelayTime { private set; get; }

        /// <summary>
        /// 是否已经结束
        /// </summary>
        public bool IsOver { private set; get; }

        /// <summary>
        /// 是否已经暂停
        /// </summary>
        public bool IsPause { private set; get; }

        /// <summary>
        /// 延迟剩余时间
        /// </summary>
        public float Remaining
        {
            get
            {
                if (IsOver)
                    return 0f;
                else
                    return System.Math.Max(0f, DelayTime - _delayTimer);
            }
        }

        /// <summary>
        /// 回调函数
        /// </summary>
        public Action CallBack { private set; get; }

        /// <summary>
        /// 计时器
        /// </summary>
        /// <param name="delay">延迟时间</param>
        /// <param name="interval">间隔时间</param>
        /// <param name="duration">运行时间</param>
        /// <param name="maxTriggerCount">最大触发次数</param>
        public Timer(Action callback, float delay, float interval, float duration, long maxTriggerCount)
        {
            CallBack = callback;
            DelayTime = delay;
            _intervalTime = interval;
            _durationTime = duration;
            _maxTriggerCount = maxTriggerCount;
        }

        public Timer()
        {

        }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="callback">回调函数</param>
        /// <param name="delay">延迟时间</param>
        /// <param name="interval">间隔时间</param>
        /// <param name="duration">运行时间</param>
        /// <param name="maxTriggerCount">最大触发次数</param>
        public void Initialize(Action callback, float delay, float interval, float duration, long maxTriggerCount)
        {
            CallBack = callback;
            DelayTime = delay;
            _intervalTime = interval;
            _durationTime = duration;
            _maxTriggerCount = maxTriggerCount;
        }

        /// <summary>
        /// 暂停计时器
        /// </summary>
        public void Pause()
        {
            IsPause = true;
        }

        /// <summary>
        /// 恢复计时器
        /// </summary>
        public void Resume()
        {
            IsPause = false;
        }

        /// <summary>
        /// 结束计时器
        /// </summary>
        public void Kill()
        {
            CallBack?.Invoke();
            IsOver = true;
        }

        /// <summary>
        /// 重置计时器
        /// </summary>
        public void Reset()
        {
            _delayTimer = 0;
            _durationTimer = 0;
            _intervalTimer = 0;
            _triggerCount = 0;
            IsOver = false;
            IsPause = false;
        }

        /// <summary>
        /// 更新计时器
        /// </summary>
        public bool Update(float deltaTime)
        {
            if (IsOver || IsPause)
                return false;

            _delayTimer += deltaTime;
            if (_delayTimer < DelayTime)
                return false;

            if (_intervalTime > 0)
                _intervalTimer += deltaTime;
            if (_durationTime > 0)
                _durationTimer += deltaTime;

            // 检测间隔执行
            if (_intervalTime > 0)
            {
                if (_intervalTimer < _intervalTime)
                    return false;
                _intervalTimer = 0;
            }

            // 检测结束条件
            if (_durationTime > 0)
            {
                if (_durationTimer >= _durationTime)
                    Kill();
            }

            // 检测结束条件
            if (_maxTriggerCount > 0)
            {
                _triggerCount++;
                if (_triggerCount >= _maxTriggerCount)
                    Kill();
            }

            return true;
        }

        public void OnRelease()
        {
            Reset();
        }
    }
}
