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
    /// 文本输入 UI 组件：包装 TMP_InputField。
    /// 用法：预制体子节点挂 TMP_InputField + 本组件，View 内 [UIBind("path")] UIInput input;，
    /// input.Value = "..."; input.OnValueChanged += ...;
    /// </summary>
    [RequireComponent(typeof(TMP_InputField))]
    public class UIInput : UIComponentBase
    {
        protected TMP_InputField _input;
        // 懒解析：Awake 未执行（如 EditMode）时仍可用
        protected TMP_InputField Field => _input != null ? _input : (_input = GetComponent<TMP_InputField>());

        /// <summary>内容变化（每次字符改动）</summary>
        public event Action<string> OnValueChanged;
        /// <summary>结束编辑（失焦/回车）</summary>
        public event Action<string> OnEndEdit;

        // SetValueWithoutNotify 期间置 true，HandleValueChanged 据此跳过，
        // 保证 wrapper 的 OnValueChanged 静默契约不依赖 TMP 的 SetTextWithoutNotify 行为
        // （某些 TMP 版本/裸 InputField 配置下 SetTextWithoutNotify 仍会触发 onValueChanged）。
        private bool _suppress;

        protected virtual void Awake()
        {
            _input = GetComponent<TMP_InputField>();
        }

        /// <summary>接线 onValueChanged/onEndEdit（Remove+Add 幂等）</summary>
        public override void OnInit()
        {
            var f = Field;
            if (f == null)
                return;
            f.onValueChanged.RemoveListener(HandleValueChanged);
            f.onValueChanged.AddListener(HandleValueChanged);
            f.onEndEdit.RemoveListener(HandleEndEdit);
            f.onEndEdit.AddListener(HandleEndEdit);
        }

        public string Value
        {
            get => Field != null ? Field.text : string.Empty;
            set { if (Field != null) Field.text = value; }
        }

        public void SetValue(string value) => Value = value;

        /// <summary>设值但不触发 OnValueChanged（如程序化同步 UI）</summary>
        public void SetValueWithoutNotify(string value)
        {
            var f = Field;
            if (f == null)
                return;
            _suppress = true;
            try { f.SetTextWithoutNotify(value); }
            finally { _suppress = false; }
        }

        public void SetInteractable(bool flag)
        {
            if (Field != null)
                Field.interactable = flag;
        }

        public void SetCharacterLimit(int limit)
        {
            if (Field != null)
                Field.characterLimit = limit;
        }

        public void SetContentType(TMP_InputField.ContentType type)
        {
            if (Field != null)
                Field.contentType = type;
        }

        private void HandleValueChanged(string v)
        {
            if (_suppress)
                return;
            OnValueChanged?.Invoke(v);
        }
        private void HandleEndEdit(string v) => OnEndEdit?.Invoke(v);

        public override void OnRelease()
        {
            var f = Field;
            if (f != null)
            {
                f.onValueChanged.RemoveListener(HandleValueChanged);
                f.onEndEdit.RemoveListener(HandleEndEdit);
            }
            OnValueChanged = null;
            OnEndEdit = null;
        }
    }
}
