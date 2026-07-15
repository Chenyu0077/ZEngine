using Hotfix.UI.Generate.Main;
using ZEngine.Manager.UI;

namespace Hotfix.Main.UI
{
    public class NPCCommandView : BaseView
    {
        public override EUILayer LayerType => EUILayer.Middle_Layer;
        public override bool     IsSingleton => true;

        public override void Initialize()
        {
            base.Initialize();
            _pkgName       = "Main";
            _resName       = "NPCCommandView";
            ModelType      = typeof(NPCCommandModel);
            ControllerType = typeof(NPCCommandController);
            MainBinder.BindAll();
        }
        
        public override void OnComplete()
        {
            base.OnComplete();
            SetToCorner(ScreenCorner.BottomLeft, 20, 20);
        }
    }
}
