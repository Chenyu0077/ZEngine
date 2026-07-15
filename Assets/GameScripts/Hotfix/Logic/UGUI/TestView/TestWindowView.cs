//------------------------------
// ZEngine - 全组件测试：窗口族具体 View 子类
// 用途：UIWindow/UIPopup/UIDialog 本身是抽象脚手架（无 [UIView] 或固定路径），
//       无法直接经 UUIManager 打开。这里给出具体子类，指向 ComponentTestPrefabGenerator
//       生成的测试预制体（"Prefabs/UI/Test/Test_UIWindow" 等），由 UUIManager 加载并驱动生命周期/动画。
// 继承：[UIBind] 脚手架字段（CloseBtn/Mask/Body/Txt_*/Btn_*）沿基类链由 UIBinder 自动绑定，子类无需重复声明。
//------------------------------

using UnityEngine;
using ZEngine.Manager.UI;                       // UBaseView
using ZEngine.Manager.UI.UGUI;                  // UIView / UUILayer
using ZEngine.Manager.UI.UGUI.Components;       // UIWindow / UIPopup / UIDialog

namespace Hotfix.Logic.UI
{
    /// <summary>
    /// 测试窗口：经 UUIManager 打开，CloseBtn 自动接线关闭（UIWindow.OnComplete），默认缩放弹出/收缩动画。
    /// 预制体约定（由工厂生成 + 生成器剥离 UIWindow 组件）：根级 "CloseBtn"(Button+UIButton+X 标签)。
    /// </summary>
    [UIView("Prefabs/UI/Test/Test_UIWindow", UUILayer.Window_Layer, isSingleton: false)]
    public class TestUIWindowView : UIWindow
    {
        public override void OnComplete()
        {
            base.OnComplete(); // 接线 _closeBtn.OnClick += Close + 打开动画
            Debug.Log("[TestUIWindow] 经 UUIManager 打开 ✓ —— 点右上 X 或 CloseBtn 关闭（带收缩动画）");
        }
    }

    /// <summary>
    /// 测试弹窗：继承 UIPopup 的 Mask(点击遮罩关闭)+Body+CloseBtn 脚手架。模态（遮罩拦截下层点击）。
    /// </summary>
    [UIView("Prefabs/UI/Test/Test_UIPopup", UUILayer.Window_Layer, isSingleton: false)]
    public class TestUIPopupView : UIPopup
    {
        public override void OnComplete()
        {
            base.OnComplete(); // 接线 CloseBtn + Mask 点击关闭 + 打开动画
            Debug.Log("[TestUIPopup] 经 UUIManager 打开 ✓ —— 点遮罩或 X 关闭");
        }
    }

    /// <summary>
    /// 测试确认弹窗：继承 UIDialog 的标题/正文/确定/取消脚手架。
    /// 打开后由 ComponentTestView 调用 Set(title, message, onConfirm, onCancel) 设置内容与回调。
    /// </summary>
    [UIView("Prefabs/UI/Test/Test_UIDialog", UUILayer.Window_Layer, isSingleton: false)]
    public class TestUIDialogView : UIDialog
    {
        public override void OnComplete()
        {
            base.OnComplete(); // 接线 CloseBtn + Mask + Btn_Confirm/Btn_Cancel（确认/取消均触发关闭动画）
            Debug.Log("[TestUIDialog] 经 UUIManager 打开 ✓ —— 等待 Set() 注入标题/正文/回调");
        }
    }
}
