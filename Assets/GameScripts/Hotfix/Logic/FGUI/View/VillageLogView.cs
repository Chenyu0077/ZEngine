//------------------------------
// ZEngine
// 作者:
//------------------------------

using FairyGUI;
using ZEngine.Manager.UI;

namespace Hotfix.Main.UI
{
    public class VillageLogView : BaseView
    {
        public override EUILayer LayerType => EUILayer.Top_Layer;
        public override bool IsSingleton => true;

        public override void Initialize()
        {
            base.Initialize();
            _pkgName = "Main";
            _resName = "VillageLogView";
            ModelType = typeof(VillageLogModel);
            ControllerType = typeof(VillageLogController);
        }

        public override void OnComplete()
        {
            base.OnComplete();
            _view.AddRelation(GRoot.inst, RelationType.Center_Center);
            SetToCenter();
        }
    }
}
