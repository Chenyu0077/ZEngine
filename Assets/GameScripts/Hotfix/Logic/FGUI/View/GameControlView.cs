using FairyGUI;
using Hotfix.UI.Generate.Common;
using Hotfix.UI.Generate.Main;
using UnityEngine;
using ZEngine.Manager.UI;

namespace Hotfix.Main.UI
{
    public class GameControlView : BaseView
    {
        public override EUILayer LayerType => EUILayer.Bottom_Layer;
        public override bool IsSingleton => true;
        public override void Initialize()
        {
            base.Initialize();
            _pkgName = "Main";
            _resName = "GameControlView";
            ModelType = typeof(GameControlModel);
            ControllerType = typeof(GameControlController);
            CommonBinder.BindAll();
            MainBinder.BindAll();
        }
        
        public override void OnComplete()
        {
            base.OnComplete();
            SetToCorner(ScreenCorner.TopLeft, 10, 10);
        }
    }
}