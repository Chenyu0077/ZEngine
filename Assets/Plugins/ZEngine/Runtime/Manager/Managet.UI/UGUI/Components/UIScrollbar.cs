//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using System;
using UnityEngine;
using UnityEngine.UI;

namespace ZEngine.Manager.UI.UGUI.Components
{
    /// <summary>
    /// 独立滚动条：包装原生 Scrollbar，提供 OnValueChanged 事件。
    /// 适用于音量调节、亮度调节等非 ScrollRect 场景。
    /// 用法：scrollbar.OnValueChanged += v => audioMixer.SetFloat("Volume", v);
    /// </summary>
    [RequireComponent(typeof(Scrollbar))]
    public class UIScrollbar : UIComponentBase
    {
        protected Scrollbar _scrollbar;
        protected Scrollbar Bar => _scrollbar != null ? _scrollbar : (_scrollbar = GetComponent<Scrollbar>());

        /// <summary>值变化 (0~1)。</summary>
        public event Action<float> OnValueChanged;

        protected virtual void Awake()
        {
            _scrollbar = GetComponent<Scrollbar>();
        }

        /// <summary>接线 onValueChanged（Remove+Add 幂等）。</summary>
        public override void OnInit()
        {
            var b = Bar;
            if (b == null) return;
            b.onValueChanged.RemoveListener(HandleValueChanged);
            b.onValueChanged.AddListener(HandleValueChanged);
        }

        public float Value
        {
            get => Bar != null ? Bar.value : 0f;
            set { if (Bar != null) Bar.value = value; }
        }

        public void SetValue(float value, bool notify = true)
        {
            var b = Bar;
            if (b == null) return;
            if (notify) b.value = value;
            else b.SetValueWithoutNotify(value);
        }

        public void SetDirection(Scrollbar.Direction dir)
        {
            if (Bar != null) Bar.direction = dir;
        }

        private void HandleValueChanged(float v) => OnValueChanged?.Invoke(v);

        public override void OnRelease()
        {
            var b = Bar;
            if (b != null) b.onValueChanged.RemoveListener(HandleValueChanged);
            OnValueChanged = null;
        }
    }
}
