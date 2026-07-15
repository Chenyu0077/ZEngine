//------------------------------
// ZEngine
// 作者:
//------------------------------

using FairyGUI;
using ZEngine.Manager.UI;

namespace Hotfix.Main.UI
{
    public class ShortTipView : BaseView
    {
        public override EUILayer LayerType => EUILayer.Top_Layer;
        public override bool IsSingleton => true;

        public override void Initialize()
        {
            base.Initialize();
            _pkgName = "Main";
            _resName = "ShortTipView";
            ModelType = typeof(ShortTipModel);
            ControllerType = typeof(ShortTipController);
        }

        public override void OnComplete()
        {
            base.OnComplete();
            // 水平居中，初始定位到屏幕顶部以上（隐藏区）
            SetToEdgeCenter(ScreenEdge.TopCenter, -_view.height * 0.5f);
        }
    }
}
