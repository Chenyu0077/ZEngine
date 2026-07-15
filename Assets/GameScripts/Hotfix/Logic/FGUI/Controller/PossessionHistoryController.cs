using FairyGUI;
using Hotfix.FuncModule;
using Hotfix.Main.Leiya;
using ZEngine.Manager.Log;
using ZEngine.Manager.Timer;
using ZEngine.Manager.UI;

namespace Hotfix.Main.UI
{
    public class PossessionHistoryController : BaseController
    {
        private GComponent             _compt;
        private PossessionHistoryModel _model;
        private Timer                  _pollTimer;
        private const float            PollInterval = 5f;

        // 缓存常用控件引用（FairyGUI 自动代码未生成前用 GetChild 访问）
        private GList       _dayList;
        private GList       _timelineList;
        private GList       _transcriptList;
        private GTextField  _loadingHint;
        private GObject     _detailBg;
        private GTextField  _detailTitle;
        private GTextField  _keyOutcomes;
        private GTextField  _summaryInitiator;
        private GObject     _emptyHint;
        private GButton     _btnClose;

        public override void Initialize()
        {
            base.Initialize();
            _compt = _view.GetView() as GComponent;
            _model = _view.Data as PossessionHistoryModel;
            if (_compt == null) return;

            CacheRefs();
            BindEvents();
            ShowLoading(true);
            FetchHistory();
            _pollTimer = TimerManager.Instance.CreatePepeatTimer(FetchHistory, PollInterval, PollInterval);
        }

        // ── 控件缓存 ─────────────────────────────────────────────────

        private void CacheRefs()
        {
            _dayList          = _compt.GetChild("dayList")          as GList;
            _timelineList     = _compt.GetChild("timelineList")     as GList;
            _transcriptList   = _compt.GetChild("transcriptList")   as GList;
            _loadingHint      = _compt.GetChild("loadingHint")      as GTextField;
            _detailBg         = _compt.GetChild("detailBg");
            _detailTitle      = _compt.GetChild("detailTitle")      as GTextField;
            _keyOutcomes      = _compt.GetChild("keyOutcomes")      as GTextField;
            _summaryInitiator = _compt.GetChild("summaryInitiator") as GTextField;
            _emptyHint        = _compt.GetChild("emptyHint");
            _btnClose         = _compt.GetChild("btnClose")         as GButton;
        }

        // ── 事件绑定 ─────────────────────────────────────────────────

        private void BindEvents()
        {
            _btnClose?.onClick.Add(OnCloseBtnClick);
            _dayList?.onClickItem.Add(OnDayItemClick);
            _timelineList?.onClickItem.Add(OnTimelineItemClick);
        }

        private void UnbindEvents()
        {
            _btnClose?.onClick.Remove(OnCloseBtnClick);
            _dayList?.onClickItem.Remove(OnDayItemClick);
            _timelineList?.onClickItem.Remove(OnTimelineItemClick);
        }

        // ── 数据拉取 ─────────────────────────────────────────────────

        private void FetchHistory()
        {
            RestApiHelper.Instance.GetPossessionHistory(
                day: null,
                byHour: true,
                onFlat: null,
                onByHour: resp =>
                {
                    if (_compt == null || resp == null) return;
                    _model.HistoryData = resp;
                    _model.RebuildDayKeys();

                    ShowLoading(false);

                    // 首次打开默认选最后一天
                    if (_model.SelectedDay == null && _model.DayKeys.Count > 0)
                        _model.SelectedDay = _model.DayKeys[_model.DayKeys.Count - 1];

                    RefreshDayList();
                    RefreshTimeline();
                },
                onError: err =>
                {
                    if (_compt == null) return;
                    ShowLoading(false);
                    LogManager.Instance.Error($"[PossessionHistory] 拉取失败: {err}");
                });
        }

        // ── 天数列表刷新 ─────────────────────────────────────────────

        private void RefreshDayList()
        {
            if (_dayList == null) return;

            _dayList.itemRenderer = (index, obj) =>
            {
                var btn = obj as GButton;
                if (btn == null || index >= _model.DayKeys.Count) return;
                btn.title = $"第{_model.DayKeys[index]}天";
            };
            _dayList.numItems = _model.DayKeys.Count;

            // 同步选中态
            int selIdx = _model.DayKeys.IndexOf(_model.SelectedDay);
            if (selIdx >= 0)
                _dayList.selectedIndex = selIdx;
        }

        private void OnDayItemClick(EventContext context)
        {
            int idx = _dayList.selectedIndex;
            if (idx < 0 || idx >= _model.DayKeys.Count) return;
            _model.SelectedDay    = _model.DayKeys[idx];
            _model.ActiveDialogue = null;
            RefreshTimeline();
            ShowDialogueDetail(false);
        }

        // ── 时间轴刷新 ───────────────────────────────────────────────
        private void RefreshTimeline()
        {
            LogManager.Instance.Info("时间轴刷新");
            if (_timelineList == null) return;

            var hours = _model.GetSelectedDayHours();

            // 固定 17 格：06 ~ 22
            _timelineList.itemRenderer = (index, obj) =>
            {
                int hour = 6 + index;  // 0→6, 1→7, …, 16→22
                var item = obj as GComponent;
                if (item == null) return;

                PossessionHistoryRecord rec = null;
                if (hours != null)
                {
                    string hKey = hour.ToString();
                    if (hours.TryGetValue(hKey, out var recs) && recs != null && recs.Count > 0)
                        rec = recs[0];
                }

                var hourLabel      = item.GetChild("hourLabel")      as GTextField;
                var iconText       = item.GetChild("iconText")       as GTextField;
                var actionLabel    = item.GetChild("actionLabel")    as GTextField;
                var summaryText    = item.GetChild("summaryText")    as GTextField;
                var deltaText      = item.GetChild("deltaText")      as GTextField;
                var deltaBg        = item.GetChild("deltaBg");
                var dialogueBadge  = item.GetChild("dialogueBadge")  as GTextField;
                var dialogueBadgeBg = item.GetChild("dialogueBadgeBg");
                var accentBar      = item.GetChild("accentBar");

                if (hourLabel != null)
                    hourLabel.text = $"{hour:00}:00";

                if (rec == null)
                {
                    // 空格：灰色占位
                    if (iconText      != null) iconText.text       = "—";
                    if (actionLabel   != null) actionLabel.text    = "";
                    if (summaryText   != null) summaryText.text    = "";
                    if (deltaBg       != null) deltaBg.visible     = false;
                    if (dialogueBadgeBg != null) dialogueBadgeBg.visible = false;
                    if (dialogueBadge != null) dialogueBadge.visible = false;
                    if (accentBar     != null) accentBar.visible   = false;
                    return;
                }

                bool hasDialogue = rec.InteractMeta != null;

                if (iconText != null)
                {
                    iconText.text  = PossessionHistoryModel.FormatActionIcon(rec.ToolName, rec.Success);
                    iconText.color = PossessionHistoryModel.FormatActionIconColor(rec.ToolName, rec.Success);
                }
                if (actionLabel != null) actionLabel.text = PossessionHistoryModel.FormatActionName(rec.ToolName);

                if (summaryText != null)
                {
                    summaryText.text = hasDialogue
                        ? (rec.InteractMeta.SummaryInitiator ?? rec.Observation ?? "")
                        : (rec.Observation ?? "");
                }

                bool hasDelta = !string.IsNullOrEmpty(rec.DeltaText);
                if (deltaBg   != null) deltaBg.visible   = hasDelta;
                if (deltaText != null)
                {
                    deltaText.visible = hasDelta;
                    deltaText.text    = rec.DeltaText ?? "";
                }

                if (dialogueBadgeBg != null) dialogueBadgeBg.visible = hasDialogue;
                if (dialogueBadge   != null) dialogueBadge.visible   = hasDialogue;
                if (accentBar       != null) accentBar.visible       = true;
            };
            _timelineList.numItems = 17;
        }

        private void OnTimelineItemClick(EventContext context)
        {
            int idx  = _timelineList.selectedIndex;
            int hour = 6 + idx;
            var hours = _model.GetSelectedDayHours();
            if (hours == null) return;

            string hKey = hour.ToString();
            if (!hours.TryGetValue(hKey, out var recs) || recs == null || recs.Count == 0)
            {
                ShowDialogueDetail(false);
                return;
            }

            var rec = recs[0];
            if (rec.InteractMeta == null)
            {
                ShowDialogueDetail(false);
                return;
            }

            _model.ActiveDialogue = rec;
            RefreshDialogueDetail(rec);
            ShowDialogueDetail(true);
        }

        // ── 对话详情抽屉 ─────────────────────────────────────────────

        private void RefreshDialogueDetail(PossessionHistoryRecord rec)
        {
            var meta = rec.InteractMeta;
            if (_detailTitle      != null) _detailTitle.text      = $"💬 与 {meta.TargetName} 的对话";
            if (_keyOutcomes      != null) _keyOutcomes.text      = meta.KeyOutcomes ?? "";
            if (_summaryInitiator != null) _summaryInitiator.text = meta.SummaryInitiator ?? "";

            if (_transcriptList == null || meta.Transcript == null) return;

            _transcriptList.itemRenderer = (index, obj) =>
            {
                var item = obj as GComponent;
                if (item == null || index >= meta.Transcript.Count) return;
                var turn = meta.Transcript[index];

                // controller "side"：0=left(对方), 1=right(村长)
                var sideCtrl = item.GetController("side");
                sideCtrl?.SetSelectedIndex(turn.IsMe ? 1 : 0);

                var speakerLeft  = item.GetChild("speakerLeft")  as GTextField;
                var speakerRight = item.GetChild("speakerRight") as GTextField;
                var textLeft     = item.GetChild("textLeft")     as GTextField;
                var textRight    = item.GetChild("textRight")    as GTextField;
                var resultText   = item.GetChild("resultText")   as GTextField;

                if (turn.IsMe)
                {
                    if (speakerRight != null) speakerRight.text = turn.Speaker ?? "村长";
                    if (textRight    != null) textRight.text    = turn.Text    ?? "";
                }
                else
                {
                    if (speakerLeft != null) speakerLeft.text = turn.Speaker ?? "";
                    if (textLeft    != null) textLeft.text    = turn.Text    ?? "";
                }

                if (resultText != null) resultText.text = turn.Result ?? "";
            };
            _transcriptList.numItems = meta.Transcript.Count;
        }

        private void ShowDialogueDetail(bool show)
        {
            if (_detailBg         != null) _detailBg.visible         = show;
            if (_detailTitle      != null) _detailTitle.visible      = show;
            if (_keyOutcomes      != null) _keyOutcomes.visible      = show;
            if (_summaryInitiator != null) _summaryInitiator.visible = show;
            if (_transcriptList   != null) _transcriptList.visible   = show;
            if (_emptyHint        != null) _emptyHint.visible        = !show;
        }

        // ── 加载态 ───────────────────────────────────────────────────

        private void ShowLoading(bool loading)
        {
            if (_loadingHint  != null) _loadingHint.visible  = loading;
            if (_timelineList != null) _timelineList.visible = !loading;
        }

        // ── 按钮回调 ─────────────────────────────────────────────────

        private void OnCloseBtnClick(EventContext context)
        {
            UIManager.Instance.CloseView<PossessionHistoryView>();
        }

        // ── 生命周期 ─────────────────────────────────────────────────

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
