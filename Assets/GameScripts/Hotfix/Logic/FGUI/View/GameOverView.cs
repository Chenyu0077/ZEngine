//------------------------------
// ZEngine
// 作者:
//------------------------------

using FairyGUI;
using Hotfix.UI.Generate.Main;
using ZEngine.Manager.UI;

namespace Hotfix.Main.UI
{
    public class GameOverView : BaseView
    {
        public override EUILayer LayerType => EUILayer.Top_Layer;
        public override bool IsSingleton => true;

        public override void Initialize()
        {
            base.Initialize();
            _pkgName = "Main";
            _resName = "GameOverView";
            ModelType = typeof(GameOverModel);
            ControllerType = typeof(GameOverController);
            MainBinder.BindAll();
        }

        public override void OnComplete()
        {
            base.OnComplete();
            SetToCenter();
        }
    }
}
