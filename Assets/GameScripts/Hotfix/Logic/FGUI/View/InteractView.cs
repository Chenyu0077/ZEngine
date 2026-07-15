//------------------------------
// ZEngine
// 作者:
//------------------------------

using FairyGUI;
using ZEngine.Manager.UI;

namespace Hotfix.Main.UI
{
    public class InteractView : BaseView
    {
        public override EUILayer LayerType => EUILayer.Bottom_Layer;
        public override bool IsSingleton => true;

        public override void Initialize()
        {
            base.Initialize();
            _pkgName = "Main";
            _resName = "InteractView";
            ModelType = typeof(InteractModel);
            ControllerType = typeof(InteractController);
        }
 
        public override void OnComplete()
        {
            base.OnComplete();
            SetToCorner(ScreenCorner.BottomLeft, _view == null ? -516 : -_view.width, 10);
        }
    }
}
