//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZEngine.Manager.UI.UGUI.Components
{
    /// <summary>
    /// 选项卡 UI 组件：注册多组 (UIToggle, GameObject页) ，选中某 Tab 时显示其页、隐藏其余页，
    /// 并静默同步其余 Toggle（避免递归）。不依赖 ToggleGroup，任意 Toggle 均可。
    /// 用法：预制体根节点挂本组件，View 内 [UIBind("path")] UITab tab;，
    /// tab.AddTab(toggle0, page0); tab.AddTab(toggle1, page1); tab.Select(0);
    /// </summary>
    public class UITab : UIComponentBase
    {
        /// <summary>切换 Tab（参数为索引）</summary>
        public event Action<int> OnTabChanged;

        private readonly List<UIToggle> _toggles = new List<UIToggle>();
        private readonly List<GameObject> _pages = new List<GameObject>();
        private int _current = -1;

        /// <summary>注册一个 Tab：按钮 toggle + 对应显示的 page。page 初始被隐藏。</summary>
        public void AddTab(UIToggle toggle, GameObject page)
        {
            int idx = _toggles.Count;
            _toggles.Add(toggle);
            _pages.Add(page);
            if (toggle != null)
                toggle.OnValueChanged += b => { if (b) Select(idx, true); };
            if (page != null)
                page.SetActive(false);
        }

        /// <summary>
        /// 按预制体约定自动配对 Tab：在 stripPath 子节点下找 UIToggle（按层级顺序），
        /// 在 pagesPath 子节点下取子 GameObject（按层级顺序），按下标一一 AddTab。
        /// 配合 UIComponentMenu 创建的 UITab 脚手架使用（TabStrip + 样例 toggle、Pages + 样例 page）。
        /// 在宿主 View 的 OnComplete 里调用（此时 toggle 已被 OnInit 接线）。
        /// </summary>
        public void AutoBind(string stripPath = "TabStrip", string pagesPath = "Pages")
        {
            var strip = transform.Find(stripPath);
            var pages = transform.Find(pagesPath);
            if (strip == null || pages == null)
                return;
            var toggles = strip.GetComponentsInChildren<UIToggle>(true);
            var pageList = new List<GameObject>();
            for (int i = 0; i < pages.childCount; i++)
                pageList.Add(pages.GetChild(i).gameObject);
            int n = Mathf.Min(toggles.Length, pageList.Count);
            for (int i = 0; i < n; i++)
                AddTab(toggles[i], pageList[i]);
        }


        /// <summary>选中指定 Tab。notify=true 触发 OnTabChanged，false 静默（程序化同步）。</summary>
        public void Select(int index, bool notify = true)
        {
            if (index < 0 || index >= _toggles.Count)
                return;
            for (int i = 0; i < _pages.Count; i++)
            {
                if (_pages[i] != null)
                    _pages[i].SetActive(i == index);
            }
            // 静默同步各 Toggle 选中态（SetValue(silent) 不会回调 Select，避免递归）
            for (int i = 0; i < _toggles.Count; i++)
            {
                if (_toggles[i] != null)
                    _toggles[i].SetValue(i == index, false);
            }
            _current = index;
            if (notify)
                OnTabChanged?.Invoke(index);
        }

        public int Current => _current;
        public int Count => _toggles.Count;

        public override void OnRelease()
        {
            // 订阅挂在各 UIToggle 上，Toggle 随 GO 销毁失效，这里仅清本地引用与事件
            OnTabChanged = null;
            _toggles.Clear();
            _pages.Clear();
            _current = -1;
        }
    }
}
