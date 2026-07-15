//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using UnityEngine;
using DG.Tweening;
using ZEngine.Manager.UI;              // UBaseView（enclosing 可见，显式标注便于阅读）
using ZEngine.Manager.UI.UGUI;         // UIView / UIBind（enclosing 可见）
using ZEngine.Manager.UI.UGUI.Animation; // UIAnimation

namespace ZEngine.Manager.UI.UGUI.Components
{
    /// <summary>
    /// 窗口基类：在 UBaseView 之上提供标准窗口脚手架 + 开关动画（DOTween）。
    /// 约定：根节点下子节点 "CloseBtn"（挂 Button + UIButton）自动接线点击关闭。
    /// 子类加 [UIView] 声明路径/层级/单例/全屏，并可继续用 [UIBind] 绑定业务字段。
    /// 基类声明的 [UIBind] 脚手架字段（如 _closeBtn）经 UIBinder 沿基类链绑定到子类实例。
    /// 动画：默认缩放弹出/收缩，子类 override OnOpenAnimation/OnCloseAnimation 返回自定义 Tween 或 null（无动画）。
    /// </summary>
    public class UIWindow : UBaseView
    {
        [UIBind("CloseBtn")] protected UIButton _closeBtn;

        // 进行中的打开动画，释放时 kill 避免对已销毁 GO 操作
        private Tween _openTween;

        public override void OnComplete()
        {
            base.OnComplete();
            if (_closeBtn != null)
                _closeBtn.OnClick += Close;
            // 打开动画：返回的 Tween 交给 DOTween 自动播放，SetLink 绑定 GO 生命周期
            _openTween = OnOpenAnimation();
            if (_openTween != null && _openTween.IsActive())
                _openTween.SetLink(gameObject);
        }

        /// <summary>关闭本窗口。先跑关闭动画（若有），完成回调里交由 UUIManager 按 ID 关闭。</summary>
        public virtual void Close()
        {
            var closeTween = OnCloseAnimation();
            if (closeTween != null && closeTween.IsActive())
            {
                closeTween.SetLink(gameObject)
                    .OnComplete(DoClose);
            }
            else
            {
                DoClose();
            }
        }

        private void DoClose()
        {
            UUIManager.Instance.CloseView(ID);
        }

        /// <summary>打开动画钩子。返回 DOTween Tween 或 null（无动画）。默认缩放弹出。</summary>
        protected virtual Tween OnOpenAnimation()
        {
            return UIAnimation.PopOpen(transform);
        }

        /// <summary>关闭动画钩子。返回 DOTween Tween 或 null（无动画）。默认收缩。</summary>
        protected virtual Tween OnCloseAnimation()
        {
            return UIAnimation.PopClose(transform);
        }

        public override void OnRelease()
        {
            UIAnimation.Kill(ref _openTween);
            base.OnRelease();
        }
    }
}
