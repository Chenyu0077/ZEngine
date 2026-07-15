//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using UnityEngine;
using ZEngine.Manager.UI.UGUI.Components;         // UIView / UIBind（enclosing 可见，显式标注）

namespace ZEngine.Manager.UI.UGUI.Components
{
    /// <summary>
    /// 弹窗基类：在 UIWindow 脚手架之上加半透明遮罩 + 点击遮罩关闭 + 模态控制。
    /// 约定：根节点下子节点 "Mask"（同时挂 Image + Button + UIImage + UIButton）作为遮罩与点击区。
    ///   - 模态（默认）：遮罩 Button 拦截下层点击，弹窗居中。
    ///   - 非模态：关闭遮罩 raycastTarget，放行下层交互。
    /// 同一 "Mask" 节点用两条 [UIBind] 绑定两个不同类型字段（UIImage 视觉、UIButton 点击），互不冲突。
    /// 子类加 [UIView]（通常 Window_Layer，isSingleton 视需要，isFullScreen 视是否需要遮挡下层）。
    /// </summary>
    public class UIPopup : UIWindow
    {
        [UIBind("Mask")] protected UIImage _maskImg;
        [UIBind("Mask")] protected UIButton _maskBtn;

        /// <summary>是否模态。默认 true：遮罩拦截下层点击。</summary>
        protected virtual bool IsModal => true;

        public override void OnComplete()
        {
            base.OnComplete();   // 接线 CloseBtn + OnOpenAnimation
            if (_maskBtn != null)
                _maskBtn.OnClick += Close;
            if (!IsModal && _maskImg != null)
                _maskImg.SetRaycastTarget(false);
        }
    }
}
