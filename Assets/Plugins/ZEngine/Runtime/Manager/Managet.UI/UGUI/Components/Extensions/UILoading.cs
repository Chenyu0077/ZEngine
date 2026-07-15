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
    /// 全屏 Loading 门面。UILoading.Show() / Hide()。
    /// 优先走 UUIManager（单例全屏，[UIView] 声明 "UI/Loading"），预制体缺失则降级过程化。
    /// 预制体约定：根节点下可选名为 "Tip" 的子节点挂 UIText 用于显示文案。
    /// </summary>
    [UIView("UI/Loading", UUILayer.Max_Layer, isSingleton: true, isFullScreen: true)]
    public sealed class UILoading : UBaseView
    {
        [UIBind("Tip")] private UIText _tip;


        // 过程化单例引用（过程化路径不经 UUIManager 跟踪，需自行持有以便 Hide）
        private static UILoading _proceduralInstance;

        public static void Show(string tip = null)
        {
            UILoading view = null;
            try
            {
                view = UUIManager.Instance.OpenViewSync<UILoading>();
            }
            catch (Exception e)
            {
                LogManager.Instance.Warning($"[UILoading] 走 UUIManager 加载失败，降级过程化构建：{e.Message}");
            }

            if (view == null)
                view = BuildProcedural();
            if (view != null && tip != null && view._tip != null)
                view._tip.SetText(tip);
        }

        public static void Hide()
        {
            // 管理路径：manager 单例关闭（若存在则关，不存在静默）
            try
            {
                UUIManager.Instance.CloseView<UILoading>();
            }
            catch (Exception e)
            {
                LogManager.Instance.Warning($"[UILoading] CloseView 异常：{e.Message}");
            }

            // 过程化路径：销毁过程化实例
            if (_proceduralInstance != null)
            {
                Destroy(_proceduralInstance.gameObject);
                _proceduralInstance = null;
            }
        }

        /// <summary>
        /// 过程化构建：全屏半透明遮罩 + 居中 "Loading..." 文本（含 "Tip" 子节点供 [UIBind] 注入）。
        /// </summary>
        private static UILoading BuildProcedural()
        {
            var go = new GameObject("UILoading_Procedural");
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            var bgGo = new GameObject("BG");
            bgGo.transform.SetParent(rt, false);
            var bgRt = bgGo.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = new Color(0f, 0f, 0f, 0.6f);

            // Tip 子节点（名为 "Tip"），可承载文案；默认显示 Loading...
            var tipGo = new GameObject("Tip");
            tipGo.transform.SetParent(rt, false);
            var tipRt = tipGo.AddComponent<RectTransform>();
            tipRt.anchorMin = tipRt.anchorMax = new Vector2(0.5f, 0.5f);
            tipRt.sizeDelta = new Vector2(600, 80);
            var tmp = tipGo.AddComponent<TextMeshProUGUI>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.text = "Loading...";
            tmp.color = Color.white;
            tmp.fontSize = 40;
            tipGo.AddComponent<UIText>();

            var view = go.AddComponent<UILoading>();
            go.transform.SetParent(UUIManager.Instance.GetLayer(UUILayer.Max_Layer).transform, false);
            view.BuildChildCache();
            view.OnComplete();
            _proceduralInstance = view;
            return view;
        }
    }
}
