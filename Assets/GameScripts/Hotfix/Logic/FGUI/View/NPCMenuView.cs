using FairyGUI;
using Hotfix.UI.Generate.Main;
using UnityEngine;
using ZEngine.Manager.UI;

namespace Hotfix.Main.UI
{
    public class NPCMenuView : BaseView
    {
        public override EUILayer LayerType => EUILayer.Top_Layer;
        public override bool IsSingleton => true;
        public override void Initialize()
        {
            base.Initialize();
            _pkgName = "Main";
            _resName = "NPCMenuView";
            ModelType = typeof(NPCMenuModel);
            ControllerType = typeof(NPCMenuController);
            MainBinder.BindAll();
        }

        public override void OnComplete()
        {
            base.OnComplete();
            // 延迟设置位置，确保UI完全初始化
            Timers.inst.Add(0.01f, 1, FollowMouse);
        }

        private void FollowMouse(object param)
        {
            var menuView = GetView();
            if (menuView == null) return;

            // Unity 鼠标坐标（左下角原点）→ FairyGUI 全局坐标（左上角原点）
            var mousePos = Input.mousePosition;
            var globalPos = GRoot.inst.GlobalToLocal(new Vector2(mousePos.x, Screen.height - mousePos.y));

            float x = globalPos.x;
            float y = globalPos.y;

            // 超出右边界则向左展开
            if (x + menuView.width > GRoot.inst.width)
                x -= menuView.width;

            // 超出下边界则向上展开
            if (y + menuView.height > GRoot.inst.height)
                y -= menuView.height;

            menuView.SetXY(x, y);
        }
        
    }
}