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
    /// 滑动条 UI 组件：包装原生 Slider。
    /// 用法：预制体子节点挂 Slider + 本组件，View 内 [UIBind("path")] UISliders;，
    /// s.SetValue(0.5f); s.OnValueChanged += v => ...;
    /// </summary>
    [RequireComponent(typeof(Slider))]
    public class UISlider : UIComponentBase
    {
        protected Slider _slider;
        protected Slider Sld => _slider != null ? _slider : (_slider = GetComponent<Slider>());

        /// <summary>值变化（0..1 或 min..max）</summary>
        public event Action<float> OnValueChanged;

        protected virtual void Awake()
        {
            _slider = GetComponent<Slider>();
        }

        /// <summary>接线 onValueChanged（Remove+Add 幂等）</summary>
        public override void OnInit()
        {
            var s = Sld;
            if (s == null)
                return;
            s.onValueChanged.RemoveListener(HandleValueChanged);
            s.onValueChanged.AddListener(HandleValueChanged);
        }

        public float Value
        {
            get => Sld != null ? Sld.value : 0f;
            set { if (Sld != null) Sld.value = value; }
        }

        /// <summary>设置值。notify=true 触发 OnValueChanged，false 静默</summary>
        public void SetValue(float value, bool notify = true)
        {
            var s = Sld;
            if (s == null)
                return;
            if (notify) s.value = value;
            else s.SetValueWithoutNotify(value);
        }

        public void SetRange(float min, float max)
        {
            var s = Sld;
            if (s == null)
                return;
            s.minValue = min;
            s.maxValue = max;
        }

        public void SetWholeNumbers(bool flag)
        {
            if (Sld != null)
                Sld.wholeNumbers = flag;
        }

        public void SetInteractable(bool flag)
        {
            if (Sld != null)
                Sld.interactable = flag;
        }

        private void HandleValueChanged(float v) => OnValueChanged?.Invoke(v);

        public override void OnRelease()
        {
            var s = Sld;
            if (s != null)
                s.onValueChanged.RemoveListener(HandleValueChanged);
            OnValueChanged = null;
        }
    }
}
