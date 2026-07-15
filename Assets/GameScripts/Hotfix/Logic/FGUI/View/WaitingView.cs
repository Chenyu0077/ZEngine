using FairyGUI;
using Hotfix.UI.Generate.Main;
using ZEngine.Manager.UI;

namespace Hotfix.Main.UI
{
    public class WaitingView : BaseView
    {
        public override EUILayer LayerType => EUILayer.Max_Layer;
        public override bool IsSingleton => true;

        public override void Initialize()
        {
            base.Initialize();
            _pkgName = "Main";
            _resName = "WaitingView";
            ModelType = typeof(WaitingModel);
            ControllerType = typeof(WaitingController);
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