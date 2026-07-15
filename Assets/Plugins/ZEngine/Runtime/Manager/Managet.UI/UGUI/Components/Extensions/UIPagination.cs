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
    /// 页码器：显示 "< 1 / 5 >" 结构的分页控件。
    /// 子层级约定：Btn_Prev(UIButton, "<") + Txt_Page(TMP, "1/5") + Btn_Next(UIButton, ">")
    /// 用法：pager.SetPage(current, total); pager.OnPageChanged += page => { ... };
    /// </summary>
    public class UIPagination : UIComponentBase
    {
        [SerializeField] private UIButton _btnPrev;
        [SerializeField] private UIButton _btnNext;
        [SerializeField] private TMPro.TextMeshProUGUI _txtPage;

        /// <summary>页码变化（current 从 0 开始）。</summary>
        public event Action<int> OnPageChanged;

        private int _current;
        private int _total;

        protected virtual void Awake()
        {
            if (_btnPrev == null) { var t = transform.Find("Btn_Prev"); if (t != null) _btnPrev = t.GetComponent<UIButton>(); }
            if (_btnNext == null) { var t = transform.Find("Btn_Next"); if (t != null) _btnNext = t.GetComponent<UIButton>(); }
            if (_txtPage == null) { var t = transform.Find("Txt_Page"); if (t != null) _txtPage = t.GetComponent<TMPro.TextMeshProUGUI>(); }
        }

        public override void OnInit()
        {
            base.OnInit();
            if (_btnPrev != null) _btnPrev.OnClick += Prev;
            if (_btnNext != null) _btnNext.OnClick += Next;
        }

        /// <summary>设置当前页和总页数（current 从 0 开始）。</summary>
        public void SetPage(int current, int total)
        {
            _current = Mathf.Clamp(current, 0, total - 1);
            _total = Mathf.Max(1, total);
            UpdateDisplay();
        }

        public void Prev()
        {
            if (_current > 0) { _current--; UpdateDisplay(); OnPageChanged?.Invoke(_current); }
        }

        public void Next()
        {
            if (_current < _total - 1) { _current++; UpdateDisplay(); OnPageChanged?.Invoke(_current); }
        }

        private void UpdateDisplay()
        {
            if (_txtPage != null)
                _txtPage.text = (_current + 1) + " / " + _total;
            if (_btnPrev != null) _btnPrev.SetInteractable(_current > 0);
            if (_btnNext != null) _btnNext.SetInteractable(_current < _total - 1);
        }

        public int CurrentPage => _current;
        public int TotalPages => _total;

        public override void OnRelease()
        {
            if (_btnPrev != null) _btnPrev.OnClick -= Prev;
            if (_btnNext != null) _btnNext.OnClick -= Next;
            OnPageChanged = null;
        }
    }
}
