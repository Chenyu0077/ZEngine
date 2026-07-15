using FairyGUI;
using Hotfix.UI.Generate.Main;
using ZEngine.Manager.UI;

namespace Hotfix.Main.UI
{
    public class PossessionHistoryView : BaseView
    {
        public override EUILayer LayerType => EUILayer.Middle_Layer;
        public override bool IsSingleton => true;

        public override void Initialize()
        {
            base.Initialize();
            _pkgName       = "Main";
            _resName       = "PossessionHistoryView";
            ModelType      = typeof(PossessionHistoryModel);
            ControllerType = typeof(PossessionHistoryController);
            MainBinder.BindAll();
        }

        public override void OnComplete()
        {
            base.OnComplete();
            SetToCenter();
        }
    }
}
