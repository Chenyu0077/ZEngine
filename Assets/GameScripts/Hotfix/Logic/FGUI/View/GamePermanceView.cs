//------------------------------
// ZEngine
// 作者:
//------------------------------

using FairyGUI;
using ZEngine.Manager.UI;

namespace Hotfix.Main.UI
{
    public class GamePermanceView : BaseView
    {
        public override EUILayer LayerType => EUILayer.Bottom_Layer;
        public override bool IsSingleton => true;

        public override void Initialize()
        {
            base.Initialize();
            _pkgName = "Main";
            _resName = "GamePermanceView";
            ModelType = typeof(GamePermanceModel);
            ControllerType = typeof(GamePermanceController);
        }

        public override void OnComplete()
        {
            base.OnComplete();
            _view.AddRelation(GRoot.inst, RelationType.Center_Center);
            SetToCenter();
        }
    }
}
