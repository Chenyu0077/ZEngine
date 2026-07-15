//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using FairyGUI;
using Hotfix.UI.Generate.Main;
using ZEngine.Manager.UI;

namespace Hotfix.Main.UI
{
    public class LoadingView : BaseView
    {
        public override EUILayer LayerType => EUILayer.Top_Layer;
        public override bool IsSingleton => true;

        public override void Initialize()
        {
            base.Initialize();
            _pkgName = "Main";
            _resName = "LoadingView";
            ModelType = typeof(LoadingModel);
            ControllerType = typeof(LoadingController);
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
