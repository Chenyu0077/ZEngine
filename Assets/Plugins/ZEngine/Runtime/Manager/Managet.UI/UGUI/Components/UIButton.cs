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
    /// 按钮 UI 组件：包装原生 Button，提供类型化点击事件 + 音效钩子。
    /// 用法：预制体子节点上挂 Button + 本组件，View 内 [UIBind("path")] UIButton btn;，
    /// btn.OnClick += handler。
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class UIButton : UIComponentBase
    {
        protected Button _button;
        // 懒解析：Awake 未执行（如 EditMode）时 SetInteractable 等仍可用
        protected Button Btn => _button != null ? _button : (_button = GetComponent<Button>());

        /// <summary>点击事件（等价 Button.onClick，但类型化为 Action 便于业务订阅）</summary>
        public event Action OnClick;

        protected virtual void Awake()
        {
            // 仅提前缓存引用；点击转发在 OnInit 中接线，使对象池复用时能重新挂上
            _button = GetComponent<Button>();
        }

        /// <summary>
        /// 由宿主 View 在绑定后调用。接线 Button.onClick -> InvokeClick。
        /// 采用 Remove + Add 保证幂等：无论是否经过 OnRelease，始终恰好一份监听。
        /// </summary>
        public override void OnInit()
        {
            var btn = Btn;
            if (btn == null)
                return;
            btn.onClick.RemoveListener(InvokeClick);
            btn.onClick.AddListener(InvokeClick);
        }

        private void InvokeClick()
        {
            try
            {
                OnClickSound();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            OnClick?.Invoke();
        }

        /// <summary>
        /// 点击音效钩子，默认空实现，后续接音效系统时 override。
        /// </summary>
        protected virtual void OnClickSound() { }

        public void SetInteractable(bool flag)
        {
            var btn = Btn;
            if (btn != null)
                btn.interactable = flag;
        }

        public bool Interactable => Btn != null && Btn.interactable;

        public override void OnRelease()
        {
            var btn = Btn;
            if (btn != null)
                btn.onClick.RemoveListener(InvokeClick);
            OnClick = null;
        }
    }
}
