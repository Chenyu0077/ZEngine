//------------------------------
// AI Village Cost Display View
// 作者: AI Assistant
//------------------------------

using FairyGUI;
using ZEngine.Manager.UI;
using Hotfix.UI.Generate.Cost;

namespace Hotfix.Main.UI
{
    /// <summary>
    /// 成本展示面板视图
    /// </summary>
    public class CostView : BaseView
    {
        public override EUILayer LayerType => EUILayer.Top_Layer;
        public override bool IsSingleton => true;

        public override void Initialize()
        {
            base.Initialize();
            _pkgName = "Cost";
            _resName = "CostView";
            ModelType = typeof(CostModel);
            ControllerType = typeof(CostController);
            CostBinder.BindAll();
        }

        public override void OnComplete()
        {
            base.OnComplete();

            // 设置面板位置和关系
            _view.AddRelation(GRoot.inst, RelationType.Center_Center);
            SetToCenter();
        }
    }
}