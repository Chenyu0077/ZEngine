//------------------------------
// ZEngine - AI Generated
//------------------------------

using UnityEngine;
using ZEngine.Manager.UI.UGUI;
using ZEngine.Manager.UI.UGUI.Components;

namespace Hotfix.Logic.UI.MainMenu
{
    public class MainMenuPanelController : UBaseController
    {
        private new MainMenuPanelModel _model => (MainMenuPanelModel)base._model;
        private new MainMenuPanelView _view => (MainMenuPanelView)base._view;

        public override void Initialize()
        {
            base.Initialize();
            if (_view != null)
            {
                _view.OnStartGame += HandleStartGame;
                _view.OnOptions += HandleOptions;
                _view.OnCredits += HandleCredits;
            }
        }

        private void HandleStartGame()
        {
            // TODO: 开始游戏 → 场景跳转 / 建房间
            Debug.Log("开始游戏");
        }

        private void HandleOptions()
        {
            // TODO: 打开设置面板
            Debug.Log("打开设置面板");
        }

        private void HandleCredits()
        {
            // TODO: 打开制作人员面板
            Debug.Log("打开制作人员面板");
        }

        public override void OnUpdate() { base.OnUpdate(); }

        public override void OnRelease()
        {
            if (_view != null)
            {
                _view.OnStartGame -= HandleStartGame;
                _view.OnOptions -= HandleOptions;
                _view.OnCredits -= HandleCredits;
            }
            base.OnRelease();
        }
    }
}
