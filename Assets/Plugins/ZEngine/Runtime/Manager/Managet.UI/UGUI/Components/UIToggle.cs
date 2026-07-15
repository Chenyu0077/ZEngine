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
    /// 开关 UI 组件：包装原生 Toggle。
    /// 用法：预制体子节点挂 Toggle + 本组件，View 内 [UIBind("path")] UIToggle tg;，
    /// tg.SetValue(true); tg.OnValueChanged += b => ...;
    /// </summary>
    [RequireComponent(typeof(Toggle))]
    public class UIToggle : UIComponentBase
    {
        protected Toggle _toggle;
        protected Toggle Tgl => _toggle != null ? _toggle : (_toggle = GetComponent<Toggle>());

        /// <summary>开关状态变化</summary>
        public event Action<bool> OnValueChanged;

        protected virtual void Awake()
        {
            _toggle = GetComponent<Toggle>();
        }

        /// <summary>接线 onValueChanged（Remove+Add 幂等）</summary>
        public override void OnInit()
        {
            var t = Tgl;
            if (t == null)
                return;
            t.onValueChanged.RemoveListener(HandleValueChanged);
            t.onValueChanged.AddListener(HandleValueChanged);
        }

        public bool IsOn
        {
            get => Tgl != null && Tgl.isOn;
            set { if (Tgl != null) Tgl.isOn = value; }
        }

        /// <summary>设置开关状态。notify=true 触发 OnValueChanged，false 静默（程序化同步）</summary>
        public void SetValue(bool value, bool notify = true)
        {
            var t = Tgl;
            if (t == null)
                return;
            if (notify) t.isOn = value;
            else t.SetIsOnWithoutNotify(value);
        }

        public void SetInteractable(bool flag)
        {
            if (Tgl != null)
                Tgl.interactable = flag;
        }

        private void HandleValueChanged(bool v) => OnValueChanged?.Invoke(v);

        public override void OnRelease()
        {
            var t = Tgl;
            if (t != null)
                t.onValueChanged.RemoveListener(HandleValueChanged);
            OnValueChanged = null;
        }
    }
}
