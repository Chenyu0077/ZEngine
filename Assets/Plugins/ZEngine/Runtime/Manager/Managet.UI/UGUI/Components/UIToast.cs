//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ZEngine.Manager.UI;          // UBaseView
using ZEngine.Manager.UI.UGUI;       // UUIManager / UUILayer / UIViewAttribute / UIBindAttribute
using ZEngine.Manager.Log;

namespace ZEngine.Manager.UI.UGUI.Components
{
    /// <summary>
    /// Toast 静态门面。调用 UIToast.Show(msg) 即可。
    /// 优先走 UUIManager 加载预制体（[UIView] 声明的 "UI/Toast" 路径），
    /// 预制体缺失或资源系统未就绪时降级为过程化构建，保证框架可立即使用。
    /// 待美术资源就绪后，只需提供 "UI/Toast" 预制体（根节点下名为 "Text" 的子节点挂 UIText）即可。
    /// </summary>
    public static class UIToast
    {
        public static void Show(string message, float duration = 2f)
        {
            UIToastView view = null;
            try
            {
                view = UUIManager.Instance.OpenViewSync<UIToastView>();
            }
            catch (Exception e)
            {
                LogManager.Instance.Warning($"[UIToast] 走 UUIManager 加载失败，降级过程化构建：{e.Message}");
            }

            if (view == null)
                view = UIToastView.BuildProcedural();
            if (view != null)
                view.Show(message, duration);
        }
    }

    /// <summary>
    /// Toast 视图。挂在 Max 层，定时后自关闭。
    /// 预制体约定：根节点下需有名为 "Text" 的子节点并挂 UIText 组件。
    /// </summary>
    [UIView("UI/Toast", UUILayer.Max_Layer, isSingleton: false)]
    public class UIToastView : UBaseView
    {
        [UIBind("Text")] private UIText _text;

        private float _expireTime;
        private bool _timing;
        // false = 过程化构建，不走 UUIManager 生命周期，需自销毁
        private bool _managedByManager = true;

        public void Show(string message, float duration)
        {
            if (_text != null)
                _text.SetText(message);
            _expireTime = Time.time + duration;
            _timing = true;
        }

        private void Update()
        {
            if (!_timing)
                return;
            if (Time.time >= _expireTime)
            {
                _timing = false;
                if (_managedByManager)
                    CanRemoved = true;          // manager 下一帧 OnUpdate 检测后 CloseView
                else
                    Destroy(gameObject);        // 过程化自管理
            }
        }

        /// <summary>
        /// 过程化构建（不依赖 Yoo 预制体）：根 + BG + Text(含 UIText)，
        /// 手动走 BuildChildCache（含 [UIBind] 注入）+ OnComplete，挂在 Max 层。
        /// </summary>
        internal static UIToastView BuildProcedural()
        {
            var go = new GameObject("UIToast_Procedural");
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(640, 140);

            // 背景层（在前 = 低 sibling index）
            var bgGo = new GameObject("BG");
            bgGo.transform.SetParent(rt, false);
            var bgRt = bgGo.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = new Color(0f, 0f, 0f, 0.75f);

            // 文本层（后 = 高 sibling index），名为 "Text" 以匹配 [UIBind("Text")]
            var txtGo = new GameObject("Text");
            txtGo.transform.SetParent(rt, false);
            var txtRt = txtGo.AddComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = txtRt.offsetMax = Vector2.zero;
            var tmp = txtGo.AddComponent<TextMeshProUGUI>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.fontSize = 38;
            txtGo.AddComponent<UIText>();   // 包装组件，供 [UIBind] 注入

            var view = go.AddComponent<UIToastView>();
            view._managedByManager = false;
            go.transform.SetParent(UUIManager.Instance.GetLayer(UUILayer.Max_Layer).transform, false);
            // 手动走绑定+完成（过程化路径不经 manager.InitializeView）
            view.BuildChildCache();
            view.OnComplete();
            return view;
        }
    }
}
