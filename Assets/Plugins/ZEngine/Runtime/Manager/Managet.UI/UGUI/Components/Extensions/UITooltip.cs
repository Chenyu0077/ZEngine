//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using UnityEngine;
using TMPro;

namespace ZEngine.Manager.UI.UGUI.Components
{
    /// <summary>
    /// 悬停提示浮层：定位在目标 RectTransform 上方的轻量 Tooltip，超出屏幕顶部时自动翻转到目标下方。
    /// 用法：tooltip.Show("攻击力: 150", targetRect); tooltip.Hide();
    /// 注：本实现按目标定位（非逐帧跟随指针），如需跟随鼠标请在调用方 OnDrag/Update 中反复调 PositionAbove。
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class UITooltip : UIComponentBase
    {
        [SerializeField] private TextMeshProUGUI _label;
        [SerializeField] private UnityEngine.UI.Image _background;

        private RectTransform _rt;
        private Canvas _canvas;
        private bool _visible;

        protected virtual void Awake()
        {
            _rt = GetComponent<RectTransform>();
            _canvas = GetComponentInParent<Canvas>();
            if (_label == null)
            {
                var lbl = transform.Find("Label");
                if (lbl != null) _label = lbl.GetComponent<TextMeshProUGUI>();
            }
            if (_background == null)
            {
                var bg = transform.Find("Background");
                if (bg != null) _background = bg.GetComponent<UnityEngine.UI.Image>();
            }
            // 不在此处 SetActive(false)：避免编辑器手动创建后立即隐藏无法调整。
            // 运行时若需默认隐藏，由调用方在 OnComplete 里调 Hide()。
        }

        /// <summary>显示提示文本，定位在 target 上方。</summary>
        public void Show(string text, RectTransform target = null)
        {
            if (_label != null) _label.text = text;
            gameObject.SetActive(true);
            _visible = true;
            if (target != null)
                PositionAbove(target);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            _visible = false;
        }

        public bool IsVisible => _visible;

        /// <summary>设置文本不立即显示（供按键前预填充）。</summary>
        public void SetText(string text)
        {
            if (_label != null) _label.text = text;
        }

        /// <summary>定位在目标上方（ScreenSpaceOverlay 下按世界坐标换算）。</summary>
        public void PositionAbove(RectTransform target)
        {
            if (_rt == null || target == null) return;
            var targetPos = target.position;
            _rt.position = targetPos + new Vector3(0, target.rect.height * 0.5f + _rt.rect.height * 0.5f + 6f, 0);
            // 简单边界修复：超出屏幕顶部则反转到目标下方
            var screenPoint = _rt.position;
            if (screenPoint.y + _rt.rect.height * 0.5f > Screen.height)
                _rt.position = targetPos - new Vector3(0, target.rect.height * 0.5f + _rt.rect.height * 0.5f + 6f, 0);
        }
    }
}
