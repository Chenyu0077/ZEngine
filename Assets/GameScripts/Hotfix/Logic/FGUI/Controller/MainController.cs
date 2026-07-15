//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using FairyGUI;
using Hotfix.FuncModule;
using Hotfix.UI.Generate.Main;
using UnityEngine;
using ZEngine.Config;
using ZEngine.Manager.Scene;
using ZEngine.Manager.UI;
using Hotfix.Main.Logic;

namespace Hotfix.Main.UI
{
    public class MainController : BaseController
    {
        private UIMainView compt;
        private GButton _startGameBtn;
        private bool _hasGameStarted;

        public override void Initialize()
        {
            base.Initialize();
            compt = _view.GetView() as UIMainView;

            if (compt != null)
            {
                _startGameBtn = compt.m_btnList.GetChild("StartGameBtn") as GButton;
                _startGameBtn.onClick.Add(OnStartGameBtnEvent);
                compt.m_btnList.GetChild("SettingsBtn").onClick.Add(OnSettingsBtnEvent);
                compt.m_btnList.GetChild("ExitBtn").onClick.Add(OnExitBtnEvent);
            }

            _view.OnChanged = DataChanged;
            DataChanged(_view?.Data);
        }


        private void DataChanged(BaseModel data)
        {
            if (data is MainModel model && model.HasGameStarted)
            {
                _hasGameStarted = true;
                if (_startGameBtn != null)
                    _startGameBtn.title = "继续游戏";
            }
        }
        
        private void OnStartGameBtnEvent(EventContext context)
        {
            if (_hasGameStarted)
            {
                OnContinueGameBtnEvent();
                return;
            }

            Debug.Log("开始游戏");
            LoadingModel loadingModel = new LoadingModel();
            loadingModel.OnLoadingCompleted = () =>
            {
                var storyModel = new StoryModel();
                UIManager.Instance.OpenViewSync<StoryView>(storyModel);
            };
            UIManager.Instance.OpenViewSync<LoadingView>(loadingModel, (view) =>
            {
                UIManager.Instance.CloseView<MainView>();
                SceneManager.Instance.ChangeMainScene(HotfixAssetPaths.ScenePath + "Map.unity", false, UnityEngine.SceneManagement.LocalPhysicsMode.None, (scenehandle) =>
                {
                    var model = view.Data as LoadingModel;
                    if (model != null)
                        model.CanLoaded = true;

                    GameLoad();
                }, null);
            });
        }

        private void OnContinueGameBtnEvent()
        {
            Debug.Log("继续游戏");
            UIManager.Instance.CloseView<MainView>();
            if (AIVillageClient.Instance.SimStatus == SimultionStatus.Paused)
            {
                AIVillageClient.Instance.ResumeSimulation();
            }
        }

        private void OnSettingsBtnEvent(EventContext context)
        {
            Debug.Log("设置");
            UIManager.Instance.OpenViewSync<SettingsView>();
        }

        private void OnExitBtnEvent(EventContext context)
        {
            Debug.Log("退出游戏");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            UnityEngine.Application.Quit();
#endif
        }


        private void GameLoad()
        {
            ModuleMgr.I.Fsm.Run("WorldSpawnNode");
        }


        public override void OnRelease()
        {
            if (compt != null)
            {
                if (_startGameBtn != null) _startGameBtn.onClick.Remove(OnStartGameBtnEvent);
                compt.m_btnList.GetChild("SettingsBtn").onClick.Remove(OnSettingsBtnEvent);
                compt.m_btnList.GetChild("ExitBtn").onClick.Remove(OnExitBtnEvent);
                compt = null;
            }
            base.OnRelease();
        }
    }
}
