//------------------------------
// ZEngine
// 作者:
//------------------------------

using FairyGUI;
using ZEngine.Manager.UI;
using Hotfix.UI.Generate.Main;

namespace Hotfix.Main.UI
{
    public class MaxTipView : BaseView
    {
        public override EUILayer LayerType => EUILayer.Window_Layer;
        public override bool IsSingleton => true;

        public override void Initialize()
        {
            base.Initialize();
            _pkgName = "Main";
            _resName = "MaxTipView";
            ModelType = typeof(MaxTipModel);
            ControllerType = typeof(MaxTipController);
            MainBinder.BindAll();
        }

        public override void OnComplete()
        {
            base.OnComplete();
            SetToCenter();
        }
    }
}
