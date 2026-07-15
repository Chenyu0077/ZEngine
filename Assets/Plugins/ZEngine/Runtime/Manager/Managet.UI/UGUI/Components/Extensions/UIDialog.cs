//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using UnityEngine;
using ZEngine.Manager.UI.UGUI;              // UIView / UIBind（enclosing 可见）
using ZEngine.Manager.UI.UGUI.Animation;     // UIAnimation

namespace ZEngine.Manager.UI.UGUI.Components
{
    /// <summary>
    /// 确认对话弹窗：UIPopup 的子类，预置标题+正文+确定/取消两个按钮。
    /// 约定子节点（在 Body 下）：Txt_Title(UIText) + Txt_Message(UIText) + Btn_Cancel(UIButton) + Btn_Confirm(UIButton)
    /// 另继承 UIPopup 的 Mask(根) + CloseBtn(根，由 UIWindow 绑定 "CloseBtn")
    /// 用法：UIDialog.Show("删除确认", "确定要删除这个物品吗？", onConfirm, onCancel);
    /// </summary>
    [UIView("UI/Prefabs/Dialog", UUILayer.Window_Layer, isSingleton: false, isFullScreen: false)]
    public class UIDialog : UIPopup
    {
        [UIBind("Body/Txt_Title")]            private UIText _txtTitle;
        [UIBind("Body/Txt_Message")]          private UIText _txtMessage;
        [UIBind("Body/Footer/Btn_Cancel")]    private UIButton _btnCancel;
        [UIBind("Body/Footer/Btn_Confirm")]   private UIButton _btnConfirm;

        private System.Action _onConfirm;
        private System.Action _onCancel;

        /// <summary>快捷静态方法打开确认弹窗。</summary>
        public static void Show(string title, string message, System.Action onConfirm = null, System.Action onCancel = null)
        {
            var dialog = UUIManager.Instance.OpenViewSync<UIDialog>();
            if (dialog != null)
                dialog.Set(title, message, onConfirm, onCancel);
        }

        public void Set(string title, string message, System.Action onConfirm, System.Action onCancel)
        {
            if (_txtTitle != null)   _txtTitle.SetText(title);
            if (_txtMessage != null) _txtMessage.SetText(message);
            _onConfirm = onConfirm;
            _onCancel = onCancel;
        }

        public override void OnComplete()
        {
            base.OnComplete();  // UIWindow.OnComplete: 接线 CloseBtn + 打开动画
            if (_btnConfirm != null) _btnConfirm.OnClick += HandleConfirm;
            if (_btnCancel != null)  _btnCancel.OnClick += HandleCancel;
        }

        private void HandleConfirm()
        {
            _onConfirm?.Invoke();
            base.Close();  // UIWindow.Close: 关闭动画 → CloseView
        }

        private void HandleCancel()
        {
            _onCancel?.Invoke();
            base.Close();
        }

        public override void OnRelease()
        {
            if (_btnConfirm != null) _btnConfirm.OnClick -= HandleConfirm;
            if (_btnCancel != null)  _btnCancel.OnClick -= HandleCancel;
            _onConfirm = null;
            _onCancel = null;
            base.OnRelease();
        }
    }
}
