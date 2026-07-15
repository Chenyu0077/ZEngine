//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using Hotfix.UI.Generate.Main;
using ZEngine.Manager.UI;

namespace Hotfix.Main.UI
{
    public class ArchiveView : BaseView
    {
        public override EUILayer LayerType => EUILayer.Top_Layer;
        public override bool IsSingleton => true;
        public override void Initialize()
        {
            base.Initialize();
            _pkgName = "Main";
            _resName = "ArchiveView";
            ModelType = typeof(ArchiveModel);
            ControllerType = typeof(ArchiveController);
            MainBinder.BindAll();
        }
    }
}
