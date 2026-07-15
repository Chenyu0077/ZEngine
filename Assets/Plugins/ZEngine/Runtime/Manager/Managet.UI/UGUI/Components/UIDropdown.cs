//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace ZEngine.Manager.UI.UGUI.Components
{
    /// <summary>
    /// 下拉选择 UI 组件：包装 TMP_Dropdown。
    /// 用法：预制体子节点挂 TMP_Dropdown + 本组件，View 内 [UIBind("path")] UIDropdown dd;，
    /// dd.SetOptions(new List&lt;string&gt;{"a","b"}); dd.OnValueChanged += i => ...;
    /// </summary>
    [RequireComponent(typeof(TMP_Dropdown))]
    public class UIDropdown : UIComponentBase
    {
        protected TMP_Dropdown _dropdown;
        protected TMP_Dropdown Dd => _dropdown != null ? _dropdown : (_dropdown = GetComponent<TMP_Dropdown>());

        /// <summary>选中项变化（参数为选项索引）</summary>
        public event Action<int> OnValueChanged;

        protected virtual void Awake()
        {
            _dropdown = GetComponent<TMP_Dropdown>();
        }

        /// <summary>接线 onValueChanged（Remove+Add 幂等）</summary>
        public override void OnInit()
        {
            var d = Dd;
            if (d == null)
                return;
            d.onValueChanged.RemoveListener(HandleValueChanged);
            d.onValueChanged.AddListener(HandleValueChanged);
        }

        public int Value
        {
            get => Dd != null ? Dd.value : 0;
            set { if (Dd != null) Dd.value = value; }
        }

        /// <summary>设置选中索引。notify=true 触发 OnValueChanged，false 静默</summary>
        public void SetValue(int index, bool notify = true)
        {
            var d = Dd;
            if (d == null)
                return;
            if (notify) d.value = index;
            else d.SetValueWithoutNotify(index);
        }

        /// <summary>用字符串列表设置选项（自动生成 OptionData）</summary>
        public void SetOptions(List<string> optionTexts)
        {
            var d = Dd;
            if (d == null || optionTexts == null)
                return;
            d.ClearOptions();
            d.AddOptions(optionTexts);
        }

        /// <summary>直接设置选项数据（可携带图片等）</summary>
        public void SetOptions(List<TMP_Dropdown.OptionData> options)
        {
            var d = Dd;
            if (d == null || options == null)
                return;
            d.ClearOptions();
            d.AddOptions(options);
        }

        public void ClearOptions()
        {
            if (Dd != null)
                Dd.ClearOptions();
        }

        public void RefreshShownValue()
        {
            if (Dd != null)
                Dd.RefreshShownValue();
        }

        public void SetInteractable(bool flag)
        {
            if (Dd != null)
                Dd.interactable = flag;
        }

        /// <summary>当前选中项的文本（无选项时返回空串）</summary>
        public string GetSelectedText()
        {
            var d = Dd;
            if (d == null || d.value < 0 || d.value >= d.options.Count)
                return string.Empty;
            return d.options[d.value].text;
        }

        private void HandleValueChanged(int v) => OnValueChanged?.Invoke(v);

        public override void OnRelease()
        {
            var d = Dd;
            if (d != null)
                d.onValueChanged.RemoveListener(HandleValueChanged);
            OnValueChanged = null;
        }
    }
}
