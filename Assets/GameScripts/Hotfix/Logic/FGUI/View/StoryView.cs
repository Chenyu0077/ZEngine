//------------------------------
// ZEngine
// 作者:
//------------------------------

using FairyGUI;
using ZEngine.Manager.UI;

namespace Hotfix.Main.UI
{
    public class StoryView : BaseView
    {
        public override EUILayer LayerType => EUILayer.Top_Layer;
        public override bool IsSingleton => true;

        public override void Initialize()
        {
            base.Initialize();
            _pkgName       = "Main";
            _resName       = "StoryView";
            ModelType      = typeof(StoryModel);
            ControllerType = typeof(StoryController);
        }

        public override void OnComplete()
        {
            base.OnComplete();
            SetToCenter();
        }
    }
}
