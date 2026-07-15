//------------------------------
// ZEngine
// 作者:
//------------------------------

using FairyGUI;
using ZEngine.Manager.UI;
using Hotfix.UI.Generate.Main;

namespace Hotfix.Main.UI
{
    public class TipView : BaseView
    {
        public override EUILayer LayerType => EUILayer.Window_Layer;
        public override bool IsSingleton => true;

        public override void Initialize()
        {
            base.Initialize();
            _pkgName = "Main";
            _resName = "TipView";
            ModelType = typeof(TipModel);
            ControllerType = typeof(TipController);
            MainBinder.BindAll();
        }

        public override void OnComplete()
        {
            base.OnComplete();
            _view.AddRelation(GRoot.inst, RelationType.Center_Center);
            SetToCenter();
        }
    }
}
