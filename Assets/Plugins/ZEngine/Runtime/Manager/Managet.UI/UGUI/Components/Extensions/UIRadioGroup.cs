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
    /// 单选组：管理多个 UIToggle 的互斥行为，确保同时只有一个选中。
    /// 不同于 UITab（Tab 带页面切换），RadioGroup 只关心"哪项被选中"。
    /// 用法：radioGroup.Add(checkbox1).Add(checkbox2).SetValue(toggle2);
    ///   radioGroup.OnValueChanged += toggle => { ... };
    /// </summary>
    public class UIRadioGroup : UIComponentBase
    {
        /// <summary>当前选中的 UIToggle 变化。参数为当前选中的 toggle，全取消时为 null。</summary>
        public event Action<UIToggle> OnValueChanged;

        private readonly List<UIToggle> _items = new List<UIToggle>();
        private UIToggle _current;

        /// <summary>注册一个 UIToggle 并自动监听其值变化。</summary>
        public UIRadioGroup Add(UIToggle toggle)
        {
            if (toggle != null && !_items.Contains(toggle))
            {
                _items.Add(toggle);
                // 用户点击某 toggle=true 时选中它，notify=true 使 OnValueChanged 通知订阅者。
                // Select 内部对其他 toggle 用 SetValue(silent) 不会回调本 lambda，故无递归。
                toggle.OnValueChanged += b => { if (b) Select(toggle, true); };
            }
            return this;
        }

        /// <summary>选中指定项。notify=true 触发 OnValueChanged, false 静默（程序化同步）。</summary>
        public void Select(UIToggle target, bool notify = true)
        {
            if (target == _current) return;
            _current = target;
            // 关闭其他
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i] != null)
                    _items[i].SetValue(_items[i] == target, false);
            }
            if (notify)
                OnValueChanged?.Invoke(target);
        }

        /// <summary>直接按索引选中。</summary>
        public void SelectByIndex(int index, bool notify = true)
        {
            if (index >= 0 && index < _items.Count)
                Select(_items[index], notify);
        }

        /// <summary>用值筛选选中（按 Toggle 所在的 GameObject 名称精确匹配）。</summary>
        public void SetValue(string toggleName, bool notify = false)
        {
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i] != null && _items[i].name == toggleName)
                {
                    Select(_items[i], notify);
                    return;
                }
            }
        }

        /// <summary>当前值（选中项的 GameObject 名称），无选中时返回 null。</summary>
        public string Value => _current != null ? _current.name : null;

        public int Count => _items.Count;

        public UIToggle Current => _current;

        public override void OnRelease()
        {
            // 解除所有监听（UIToggle 生命周期自行管理其 OnValueChanged 清理）
            OnValueChanged = null;
            _items.Clear();
            _current = null;
        }
    }
}
