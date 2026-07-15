//------------------------------
// ZEngine - AI Generated
// Panel: MainMenuPanel (Game Main Menu)
//------------------------------

using System;
using ZEngine.Manager.UI;
using ZEngine.Manager.UI.UGUI;
using ZEngine.Manager.UI.UGUI.Components;

namespace Hotfix.Logic.UI.MainMenu
{
    /// <summary>
    /// MainMenu 面板 View。
    /// 组件：TitleText(UIText), Btn_StartGame(UIButton), Btn_Options(UIButton), Btn_Credits(UIButton)
    /// </summary>
    [UIView("Prefabs/UI/MainMenuPanel", UUILayer.Middle_Layer, isSingleton: true)]
    public class MainMenuPanelView : UBaseView
    {
        [UIBind("BgPanel/TitleBar/TitleText")] private UIText _titleText;
        [UIBind("BgPanel/ContentArea/Btn_StartGame")] private UIButton _btnStartGame;
        [UIBind("BgPanel/ContentArea/Btn_Options")]  private UIButton _btnOptions;
        [UIBind("BgPanel/ContentArea/Btn_Credits")]  private UIButton _btnCredits;

        public override Type ModelType => typeof(MainMenuPanelModel);
        public override Type ControllerType => typeof(MainMenuPanelController);

        public override void Initialize()
        {
            base.Initialize();
            if (_titleText != null) _titleText.SetText("Game Title1");
        }

        public override void OnComplete()
        {
            base.OnComplete();
            if (_btnStartGame != null) _btnStartGame.OnClick += OnStartGameClick;
            if (_btnOptions != null) _btnOptions.OnClick += OnOptionsClick;
            if (_btnCredits != null) _btnCredits.OnClick += OnCreditsClick;
        }

        public event Action OnStartGame;
        public event Action OnOptions;
        public event Action OnCredits;

        private void OnStartGameClick() => OnStartGame?.Invoke();
        private void OnOptionsClick() => OnOptions?.Invoke();
        private void OnCreditsClick() => OnCredits?.Invoke();

        public override void OnRelease()
        {
            if (_btnStartGame != null) _btnStartGame.OnClick -= OnStartGameClick;
            if (_btnOptions != null) _btnOptions.OnClick -= OnOptionsClick;
            if (_btnCredits != null) _btnCredits.OnClick -= OnCreditsClick;
            OnStartGame = null;
            OnOptions = null;
            OnCredits = null;
            base.OnRelease();
        }
    }
}
