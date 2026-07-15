//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using System;
using UnityEngine;
using TMPro;

namespace ZEngine.Manager.UI.UGUI.Components
{
    /// <summary>
    /// 倒计时文本：包装 TextMeshProUGUI，提供按秒递减的倒计时并自动格式化 mm:ss。
    /// 到达 0 时自动触发 OnEnd 事件。isRunning 控制启停。
    /// 用法：countdown.SetCountdown(90).Begin(); // 1分30秒
    /// 注意：启动方法名为 Begin() 而非 Start()，避免与 Unity MonoBehaviour.Start() 魔术方法冲突导致自动启动。
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class UICountdown : UIComponentBase
    {
        protected TextMeshProUGUI _tmp;
        protected TextMeshProUGUI Tmp => _tmp != null ? _tmp : (_tmp = GetComponent<TextMeshProUGUI>());

        /// <summary>倒计时归零时触发。</summary>
        public event Action OnEnd;

        private float _remaining;
        private bool _isRunning;

        public bool IsRunning => _isRunning;

        /// <summary>设置倒计时秒数并立即刷新显示。不自动开始。</summary>
        public UICountdown SetCountdown(float seconds)
        {
            _remaining = Mathf.Max(0, seconds);
            UpdateDisplay();
            return this;
        }

        /// <summary>开始倒计时（不命名 Start 以避免 Unity 自动调用）。</summary>
        public void Begin()
        {
            if (_remaining > 0) _isRunning = true;
        }

        public void Pause() => _isRunning = false;
        public void Stop()
        {
            _isRunning = false;
            _remaining = 0;
            UpdateDisplay();
        }

        private void Update()
        {
            if (!_isRunning) return;
            _remaining -= Time.deltaTime;
            if (_remaining <= 0)
            {
                _remaining = 0;
                _isRunning = false;
                UpdateDisplay();
                OnEnd?.Invoke();
            }
            else
            {
                UpdateDisplay();
            }
        }

        private void UpdateDisplay()
        {
            if (Tmp == null) return;
            int total = Mathf.CeilToInt(_remaining);
            int min = total / 60;
            int sec = total % 60;
            Tmp.text = string.Format("{0:00}:{1:00}", min, sec);
        }

        public override void OnRelease()
        {
            _isRunning = false;
            OnEnd = null;
        }
    }
}
