//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using UnityEngine;
using TMPro;

namespace ZEngine.Manager.UI.UGUI.Components
{
    /// <summary>
    /// 文本 UI 组件：包装 TextMeshProUGUI，提供常用文本/颜色/显隐接口。
    /// 用法：预制体子节点上挂 TMP + 本组件，View 内 [UIBind("path")] UIText txt;，
    /// txt.SetText("...");
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class UIText : UIComponentBase
    {
        protected TextMeshProUGUI _tmp;
        // 懒解析：Awake 未执行（如 EditMode / 动态添加）时也能正确工作
        protected TextMeshProUGUI Tmp => _tmp != null ? _tmp : (_tmp = GetComponent<TextMeshProUGUI>());

        protected virtual void Awake()
        {
            _tmp = GetComponent<TextMeshProUGUI>();
        }

        public void SetText(string content)
        {
            var tmp = Tmp;
            if (tmp != null)
                tmp.text = content;
        }

        public void SetText(int value)
        {
            var tmp = Tmp;
            if (tmp != null)
                tmp.text = value.ToString();
        }

        /// <summary>
        /// 格式化文本（注意：每次调用会生成格式串，高频场景请直接 SetText）
        /// </summary>
        public void SetText(string format, params object[] args)
        {
            var tmp = Tmp;
            if (tmp != null)
                tmp.text = string.Format(format, args);
        }

        public void SetColor(Color color)
        {
            var tmp = Tmp;
            if (tmp != null)
                tmp.color = color;
        }

        public void SetActive(bool flag)
        {
            var tmp = Tmp;
            if (tmp != null)
                tmp.gameObject.SetActive(flag);
        }

        public string GetText()
        {
            var tmp = Tmp;
            return tmp != null ? tmp.text : string.Empty;
        }
    }
}
