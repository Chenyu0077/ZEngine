//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using System.Numerics;
using FairyGUI;
using Hotfix.UI.Generate.Main;
using UnityEngine;
using ZEngine.Manager.UI;

namespace Hotfix.Main.UI
{
    public class MainView : BaseView
    {
        public override EUILayer LayerType => EUILayer.Top_Layer;
        public override bool IsSingleton => true;
        public override void Initialize()
        {
            base.Initialize();
            _pkgName = "Main";
            _resName = "MainView";
            ModelType = typeof(MainModel);
            ControllerType = typeof(MainController);
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
