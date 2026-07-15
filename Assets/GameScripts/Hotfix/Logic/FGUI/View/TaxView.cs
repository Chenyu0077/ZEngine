using FairyGUI;
using Hotfix.UI.Generate.Main;
using ZEngine.Manager.UI;

namespace Hotfix.Main.UI
{
    public class TaxView : BaseView
    {
        public override EUILayer LayerType => EUILayer.Middle_Layer;
        public override bool IsSingleton => true;

        public override void Initialize()
        {
            base.Initialize();
            _pkgName       = "Main";
            _resName       = "TaxView";
            ModelType      = typeof(TaxModel);
            ControllerType = typeof(TaxController);
            MainBinder.BindAll();
        }

        public override void OnComplete()
        {
            base.OnComplete();
            SetToEdgeCenter(ScreenEdge.TopCenter, -_view.height * 0.5f);
        }
    }
}
