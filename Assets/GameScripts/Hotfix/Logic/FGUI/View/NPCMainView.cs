using FairyGUI;
using Hotfix.UI.Generate.Main;
using ZEngine.Manager.UI;

namespace Hotfix.Main.UI
{
    public class NPCMainView : BaseView
    {
        public override EUILayer LayerType => EUILayer.Bottom_Layer;
        public override bool IsSingleton => true;
        public override void Initialize()
        {
            base.Initialize();
            _pkgName = "Main";
            _resName = "NPCMainView";
            ModelType = typeof(NPCMainModel);
            ControllerType = typeof(NPCMainController);
            MainBinder.BindAll();
        }
        
        public override void OnComplete()
        {
            base.OnComplete();

            SetToCorner(ScreenCorner.BottomRight, _view == null ? 800 : _view.width, 20);
        }
    }
}