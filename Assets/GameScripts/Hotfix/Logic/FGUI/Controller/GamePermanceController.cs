//------------------------------
// ZEngine
// 作者:
//------------------------------

using System;
using FairyGUI;
using Hotfix.FuncModule;
using Hotfix.UI.Generate.Main;
using UnityEngine;
using ZEngine.Manager.Log;
using ZEngine.Manager.Timer;
using ZEngine.Manager.UI;

namespace Hotfix.Main.UI
{
    public class GamePermanceController : BaseController
    {
        private const float PollInterval = 10f;
        private const float GameStatusInterval = 5.0f;

        private UIGamePermanceView _compt;
        private UITimeCom _timeCom;
        private Timer _pollTimer;
        private Timer _gameOverTimer;
        private bool _gameOverShown;

        public override void Initialize()
        {
            base.Initialize();
            _compt = _view.GetView() as UIGamePermanceView;
            if (_compt != null)
            {
                _compt.m_btnConfig.onClick.Add(OnConfigBtnEvent);
                _compt.m_btnCost.onClick.Add(OnCostBtnEvent);
                _compt.m_btnHistory.onClick.Add(OnHistoryBtnEvent);
                _timeCom = _compt.m_TimePanel;
            }

            _view.OnChanged += RefreshUI;

            RequestDays();
            _pollTimer = TimerManager.Instance.CreatePepeatTimer(RequestDays, 0f, PollInterval);
            _gameOverTimer = TimerManager.Instance.CreatePepeatTimer(RequestGameStatus, 0f, GameStatusInterval);
        }



        // 请求游戏天数
        private void RequestDays()
        {
            AIVillageClient.Instance.GetSimRuntime((response) =>
            {
                if (response == null) return;
                var clock = response.Clock;
                if (clock == null) return;
                if (_view.Data is not GamePermanceModel model) return;
                model.Days = clock.Day;
                model.Hours = clock.Hour ?? 0;
                model.TimeLabel = clock.Label;
                _view.OnChanged?.Invoke(model);
            }, null);
        }

        // 请求当前游戏状态
        private void RequestGameStatus()
        {
            AIVillageClient.Instance.GetSimStatus((response) =>
            {
                if (response == null) return;
                var model = _view.Data as GamePermanceModel;
                if (model == null) return;
                model.TotalCount = response.NPCCount;
                model.DeadCount = response.Dead;
                _view.OnChanged?.Invoke(model);

                // 游戏结束，打开结束面板（避免重复打开）
                if (response.GameOver && !_gameOverShown)
                {
                    _gameOverShown = true;
                    var overModel = new GameOverModel { GameStatus = response };
                    UIManager.Instance.OpenViewSync<GameOverView>(overModel);
                }
            }, null);
        }

        private void OnConfigBtnEvent(EventContext context)
        {
            UIManager.Instance.OpenViewSync<GameConfigView>();
        }

        private void OnCostBtnEvent(EventContext context)
        {
            UIManager.Instance.OpenViewSync<CostView>();
            AIVillageClient.Instance.GetCost((response) =>
            {
                if (response == null) return;
                CostModel model = new CostModel();
                model.CostData = response;
                UIManager.Instance.OpenViewSync<CostView>(model);
            }, (error) =>
            {
                TipModel tipModel = new TipModel{ TipContent = $"[color=#FF0000]Cost数据获取失败: {error}[/color]"};
                UIManager.Instance.OpenViewSync<TipView>(tipModel);
            });
        }

        private void OnHistoryBtnEvent(EventContext context)
        {
            UIManager.Instance.OpenViewSync<PossessionHistoryView>();
        }

        private void RefreshUI(BaseModel data)
        {
            var model = data as GamePermanceModel;
            if (model == null)  return;

            _timeCom.m_Days.text = $"{model.TimeLabel}";
            _timeCom.m_SurvivalRatio.text = $"{model.TotalCount - model.DeadCount}/{model.TotalCount}";
        }

        public override void OnRelease()
        {
            if (_pollTimer != null)
            {
                _pollTimer.Kill();
                _pollTimer = null;
            }

            if (_gameOverTimer != null)
            {
                _gameOverTimer.Kill();
                _gameOverTimer = null;
            }

            if (_compt == null) return;

            _view.OnChanged = null;
            _compt.m_btnConfig.onClick.Remove(OnConfigBtnEvent);
            _compt.m_btnCost.onClick.Remove(OnCostBtnEvent);
            _compt.m_btnHistory.onClick.Remove(OnHistoryBtnEvent);
            _compt = null;
            base.OnRelease();
        }
    }
}
