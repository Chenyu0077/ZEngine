using DG.Tweening;
using FairyGUI;
using Hotfix.FuncModule;
using Hotfix.Main.Leiya;
using Hotfix.UI.Generate.Main;
using Newtonsoft.Json;
using UnityEngine;
using ZEngine.Manager.Log;
using ZEngine.Manager.Timer;
using ZEngine.Manager.UI;

namespace Hotfix.Main.UI
{
    public class TaxController : BaseController
    {
        private UITaxView _compt;
        private TaxModel  _model;
        private Timer     _pollTimer;
        private const float PollInterval   = 2f;
        private const float ProgressBarMax = 440f;
        private const float SlideTime      = 1.5f;
        private bool _isCollapsed      = false;
        private bool _taxGameOverShown = false;

        public override void Initialize()
        {
            base.Initialize();
            _compt = _view.GetView() as UITaxView;
            _model = _view.Data as TaxModel;
            if (_compt == null) return;

            BindEvents();
            RestApiHelper.Instance.GetTaxConfig(resp => { if (_model != null) _model.ConfigData = resp; });
            PollTaxState();
            _pollTimer = TimerManager.Instance.CreatePepeatTimer(PollTaxState, 0f, PollInterval);
        }

        private void BindEvents()
        {
            _compt.m_settleBtn.onClick.Add(OnSettleBtnClick);
            _compt.m_TabFamily.onClick.Add(OnTabFamilyClick);
            _compt.m_TabHistory.onClick.Add(OnTabHistoryClick);
            _compt.m_popBtn.onClick.Add(OnPopBtnClick);
        }

        private void UnbindEvents()
        {
            if (_compt == null) return;
            _compt.m_settleBtn.onClick.Remove(OnSettleBtnClick);
            _compt.m_TabFamily.onClick.Remove(OnTabFamilyClick);
            _compt.m_TabHistory.onClick.Remove(OnTabHistoryClick);
            _compt.m_popBtn.onClick.Remove(OnPopBtnClick);
        }

        // ── 折叠/展开 ─────────────────────────────────────────────────

        private void OnPopBtnClick(EventContext context)
        {
            _isCollapsed = !_isCollapsed;
            float toY = _isCollapsed
                ? _view.GetPosition(BaseView.ScreenCorner.TopLeft, 0, -_compt.height).y
                : _view.GetPosition(BaseView.ScreenCorner.TopLeft, 0, 10f + _compt.height * 0.5f).y;
            _compt.DOMoveY(toY, SlideTime).SetEase(Ease.InOutQuad);
        }

        // ── 轮询 ──────────────────────────────────────────────────────

        private void PollTaxState()
        {
            RestApiHelper.Instance.GetTaxState(resp =>
            {
                if (_compt == null || resp == null) return;
                _model.StateData = resp;
                //LogManager.Instance.Info($"resp: {JsonConvert.SerializeObject(resp)}");
                if (!resp.Active)
                {
                    _compt.m_activeCtrol.selectedIndex = 0;
                    return;
                }

                _compt.m_activeCtrol.selectedIndex = 1;
                RefreshOverview(resp);
                RefreshCurrentTab();
                CheckGameOver(resp.GameOverPending);
            });
        }

        // ── 总览刷新 ──────────────────────────────────────────────────

        private void RefreshOverview(TaxStateResponse s)
        {
            _compt.m_cycleLabel.text    = $"第{s.Cycle}期 · 剩{s.DaysRemaining}天";
            _compt.m_deadlineLabel.text = $"截至第{s.DeadlineDay}天";
            _compt.m_progressText.text  = $"{s.Actual} / {s.Required}";

            float ratio = Mathf.Clamp01(s.Ratio);
            _compt.m_progressFill.width = ProgressBarMax * ratio;

            // 进度条颜色：满额绿/超半黄/不足红
            Color fillColor = s.Ratio >= 1f
                ? new Color32(0x44, 0xFF, 0x88, 0xFF)
                : (ratio < 0.5f ? new Color32(0xFF, 0x44, 0x44, 0xFF) : new Color32(0xFF, 0xDD, 0x00, 0xFF));
            _compt.m_progressFill.color = fillColor;

            _compt.m_debtLabel.text = s.RollingDebt > 0
                ? $"[color=#ffaa88]滚欠: {s.RollingDebt}[/color]"
                : "滚欠: 0";

            bool strikesMaxed = s.Strikes >= s.MaxStrikes;
            _compt.m_strikesLabel.text = strikesMaxed
                ? $"[color=#ff4444]记过: {s.Strikes}/{s.MaxStrikes}[/color]"
                : $"记过: {s.Strikes}/{s.MaxStrikes}";

            _compt.m_settleBtn.grayed = !string.IsNullOrEmpty(s.GameOverPending) || _model.IsSettling;
        }

        // ── Tab 切换 ──────────────────────────────────────────────────

        private void OnTabFamilyClick(EventContext context)
        {
            _compt.m_tabCtrol.selectedIndex = 0;
            LoadAndRefreshFamilies();
        }

        private void OnTabHistoryClick(EventContext context)
        {
            _compt.m_tabCtrol.selectedIndex = 1;
            LoadAndRefreshHistory();
        }

        private void RefreshCurrentTab()
        {
            if (_compt.m_tabCtrol.selectedIndex == 0)
                LoadAndRefreshFamilies();
            else
                LoadAndRefreshHistory();
        }

        private void LoadAndRefreshFamilies()
        {
            RestApiHelper.Instance.GetTaxFamilies(resp =>
            {
                if (_compt == null || resp == null) return;
                _model.FamiliesData = resp;
                if (!resp.Active || resp.Families == null) return;

                _compt.m_familyList.numItems = resp.Families.Count;
                _compt.m_familyList.itemRenderer = (index, obj) =>
                {
                    var item = obj as UITaxFamilyItem;
                    if (item == null || index >= resp.Families.Count) return;
                    var fam = resp.Families[index];
                    if (fam == null) return;

                    item.m_familyName.text    = fam.FamilyName ?? "";
                    item.m_tierLabel.text     = TaxModel.FormatTier(fam.Tier);
                    item.m_collectedText.text = $"{fam.CollectedThisCycle}/{fam.SuggestedQuota}";

                    float r = fam.SuggestedQuota > 0
                        ? Mathf.Clamp01((float)fam.CollectedThisCycle / fam.SuggestedQuota)
                        : 0f;
                    item.m_progressFill.width = item.m_progressBg.width * r;

                    item.m_statusLabel.text = fam.Status switch
                    {
                        "met"  => "[color=#44ff88]✓[/color]",
                        "over" => $"[color=#ffaa00]+{fam.CollectedThisCycle - fam.SuggestedQuota}[/color]",
                        _      => "",
                    };
                };
            });
        }

        private void LoadAndRefreshHistory()
        {
            RestApiHelper.Instance.GetTaxHistory(resp =>
            {
                if (_compt == null || resp == null) return;
                _model.HistoryData = resp;
                if (!resp.Active || resp.History == null) return;

                _compt.m_historyList.numItems = resp.History.Count;
                _compt.m_historyList.itemRenderer = (index, obj) =>
                {
                    var item = obj as UITaxHistoryItem;
                    if (item == null || index >= resp.History.Count) return;
                    var rec = resp.History[index];
                    if (rec == null) return;

                    item.m_cycleText.text    = $"第{rec.Cycle}期";
                    item.m_requiredText.text = $"应收{rec.Required}";
                    item.m_actualText.text   = $"实收{rec.Actual}";
                    item.m_ratioText.text    = $"{rec.Ratio * 100:F0}%";
                    item.m_outcomeText.text  = TaxModel.FormatOutcome(rec.Outcome);
                };
            });
        }

        // ── 上缴 ─────────────────────────────────────────────────────

        private void OnSettleBtnClick(EventContext context)
        {
            if (_model.IsSettling) return;
            _model.IsSettling         = true;
            _compt.m_settleBtn.grayed = true;

            RestApiHelper.Instance.PostTaxSettle(
                resp =>
                {
                    if (_compt == null) return;
                    _model.IsSettling = false;
                    if (resp?.State != null)
                    {
                        _model.StateData = resp.State;
                        RefreshOverview(resp.State);
                    }
                    CheckGameOver(resp?.GameOverPending);
                },
                error =>
                {
                    if (_compt == null) return;
                    _model.IsSettling         = false;
                    _compt.m_settleBtn.grayed = false;
                });
        }

        // ── 结局检测 ──────────────────────────────────────────────────

        private void CheckGameOver(string pending)
        {
            if (string.IsNullOrEmpty(pending) || _taxGameOverShown) return;
            _taxGameOverShown = true;

            // 面板滑回顶部隐藏（与 popBtn 点击效果一致）
            if (!_isCollapsed)
            {
                _isCollapsed = true;
                float toY = _view.GetPosition(BaseView.ScreenCorner.TopLeft, 0, -_compt.height).y;
                _compt.DOMoveY(toY, SlideTime).SetEase(Ease.InOutQuad);
            }

            string title  = _model.ConfigData?.TaxFailedTitle ?? "税额未达";
            string phrase = pending;
            if (_model.ConfigData?.Severities != null &&
                _model.ConfigData.Severities.TryGetValue(pending, out var sev))
                phrase = sev.Phrase;

            var model = new GameOverModel
            {
                IsTaxFailure = true,
                GameStatus   = new SimStatusResponse
                {
                    GameOverKind    = pending,
                    GameOverReason  = $"{title}：{phrase}",
                    GameOverSummary = "",
                }
            };
            UIManager.Instance.OpenViewSync<GameOverView>(model);
            AIVillageClient.Instance.StopSimulation();
        }

        // ── 生命周期 ──────────────────────────────────────────────────

        public override void OnRelease()
        {
            _pollTimer?.Kill();
            _pollTimer = null;
            UnbindEvents();
            _compt = null;
            _model = null;
            base.OnRelease();
        }
    }
}
