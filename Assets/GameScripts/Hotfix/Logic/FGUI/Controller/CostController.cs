//------------------------------
// AI Village Cost Display Controller
// 作者:
//------------------------------

using FairyGUI;
using Hotfix.FuncModule;
using Hotfix.Main.Leiya;
using Hotfix.UI.Generate.Cost;
using ZEngine.Manager.UI;
using ZEngine.Manager.Timer;
using UnityEngine;

namespace Hotfix.Main.UI
{
    /// <summary>
    /// 成本展示面板控制器。
    /// 数据来源： <see cref="AIVillageClient.GetCost"/> 回调写入 <see cref="CostModel.CostData"/>，
    /// 通过 <see cref="BaseView.OnChanged"/> 触发 <see cref="RefreshUI"/> 全量映射到各子面板。
    /// </summary>
    public class CostController : BaseController
    {
        private UICostView _compt;
        private CostModel _model;
        private Timer _autoRefreshTimer;


        public override void Initialize()
        {
            base.Initialize();
            _compt = _view.GetView() as UICostView;
            _model = _view.Data as CostModel;

            if (_compt != null)
            {
                BindEvents();
            }

            _view.OnChanged += OnModelChanged;

            // 首次渲染（CostView 经由 GamePermanceController 打开时已携带 CostData）
            if (_model == null || _model.CostData == null)
            {
                AIVillageClient.Instance.GetCost((response) =>
                {
                    if (response == null || _model == null) return;
                    _model.CostData = response;
                    RefreshUI(_model);
                }, (error) =>
                {
                    _model.CostData = _model.GetInitiazeData();
                });
            }
            OnModelChanged(_model);

            if (_model != null && _model.AutoRefreshEnabled)
            {
                StartAutoRefresh();
            }
        }

        private void BindEvents()
        {
            // Header
            var header = _compt.m_headerPanel;
            header.m_closeBtn.onClick.Add(OnCloseBtnClick);
            header.m_refreshBtn.onClick.Add(OnRefreshBtnClick);
            header.m_autoRefreshToggle.onClick.Add(OnAutoRefreshToggle);
        }

        private void UnbindEvents()
        {
            if (_compt == null) return;
            // Header
            var header = _compt.m_headerPanel;
            header.m_closeBtn.onClick.Remove(OnCloseBtnClick);
            header.m_refreshBtn.onClick.Remove(OnRefreshBtnClick);
            header.m_autoRefreshToggle.onClick.Remove(OnAutoRefreshToggle);
        }


        #region 事件回调

        private void OnCloseBtnClick(EventContext context)
        {
            UIManager.Instance.CloseView<CostView>();
        }

        /// <summary>手动刷新：复用 GamePermanceController 的请求入口。</summary>
        private void OnRefreshBtnClick(EventContext context)
        {
            RequestCost();
        }

        /// <summary>自动刷新开关切换：同步到 Model 并启停定时器。</summary>
        private void OnAutoRefreshToggle(EventContext context)
        {
            if (_model == null) return;
            var refreshBtn = _compt.m_headerPanel.m_autoRefreshToggle;
            bool enabled = refreshBtn.selected;
            refreshBtn.m_toggle.selectedIndex = enabled ? 1 : 0;
            _model.AutoRefreshEnabled = enabled;
            if (enabled) StartAutoRefresh();
            else StopAutoRefresh();
        }

        private void OnModelChanged(BaseModel data)
        {
            if (data is CostModel cm) RefreshUI(cm);
        }

        #endregion


        #region 数据请求

        private void RequestCost()
        {
            AIVillageClient.Instance.GetCost((response) =>
            {
                if (response == null) return;
                if (_model == null) _model = _view.Data as CostModel;
                if (_model == null) return;
                _model.CostData = response;
                _model.LastUpdateTime = System.DateTime.Now;
                _view.OnChanged?.Invoke(_model);
            }, null);
        }

        private void StartAutoRefresh()
        {
            if (_model == null || !_model.AutoRefreshEnabled) return;
            StopAutoRefresh();
            float interval = _model.RefreshInterval > 0 ? _model.RefreshInterval : 30f;
            _autoRefreshTimer = TimerManager.Instance.CreatePepeatTimer(RequestCost, 0f, interval);
        }

        private void StopAutoRefresh()
        {
            if (_autoRefreshTimer != null)
            {
                _autoRefreshTimer.Kill();
                _autoRefreshTimer = null;
            }
        }

        #endregion


        #region UI 渲染

        private void RefreshUI(CostModel model)
        {
            if (_compt == null || model == null) return;
            var data = model.CostData;
            if (data == null) return;

            RefreshHeader(model);
            RefreshBasicInfo(data);
            RefreshCumulative(data);
            RefreshAverages(data);
            RefreshVillageGen(data);
            RefreshCache(data);
            RefreshModels(data);

            _compt.m_dataUpdate?.Play();
        }

        private void RefreshHeader(CostModel model)
        {
            var panel = _compt.m_headerPanel;
            var refreshToggle = panel.m_autoRefreshToggle;
            refreshToggle.selected = model.AutoRefreshEnabled;
            refreshToggle.m_toggle.selectedIndex = model.AutoRefreshEnabled ? 1 : 0;
            string timeStr = model.LastUpdateTime == default
                ? "--:--:--"
                : model.LastUpdateTime.ToString("HH:mm:ss");
            panel.m_lastUpdateTime.text = $"最后更新: {timeStr}";
        }

        private void RefreshBasicInfo(CostResponse data)
        {
            var panel = _compt.m_basicInfoPanel;
            panel.m_dayValue.text = $"第 {data.CurrentDay} 天";
            panel.m_npcValue.text = data.NpcCount.ToString();
            panel.m_callValue.text = data.RawRecordsCount.ToString();
            // 运行时长 / 状态由业务侧补充，暂以天数作为占位
            panel.m_runtimeValue.text = $"{data.CurrentDay} 天";
            panel.m_runStatus.selectedIndex = data.Success ? 0 : 1;
            panel.m_statusValue.text = data.Success ? "运行中" : "异常";
        }

        private void RefreshCumulative(CostResponse data)
        {
            if (data.Cumulative == null) return;
            var c = data.Cumulative;
            var panel = _compt.m_cumulativePanel;
            panel.m_totalTokenValue.text = CostModel.FormatTokenCount(c.TotalTokens);
            panel.m_inputTokenValue.text = CostModel.FormatTokenCount(c.InputTokens);
            panel.m_outputTokenValue.text = CostModel.FormatTokenCount(c.OutputTokens);
            panel.m_costUsdValue.text = CostModel.FormatCurrency(c.CostUsd, "USD");
            panel.m_costCnyValue.text = CostModel.FormatCurrency(c.CostCny, "CNY");
            // 缓存命中率进度条 + 文本共用一处显示
            panel.m_cacheValue.text = CostModel.FormatPercentage(c.CacheHitRate);
            if (panel.m_cacheProgressBar != null)
                panel.m_cacheProgressBar.value = c.CacheHitRate * 100f;
            // 平均延迟：面板仅 m_latencyLabel，合并数值写入标签
            panel.m_latencyLabel.text = $"平均延迟 {c.AvgLatencyMs:F0} ms";
        }

        private void RefreshAverages(CostResponse data)
        {
            if (data.Averages == null) return;
            var a = data.Averages;
            var panel = _compt.m_averagePanel;
            panel.m_npcUsdValue.text = CostModel.FormatCurrency(a.CostPerNpcPerDayUsd, "USD");
            panel.m_npcCnyValue.text = CostModel.FormatCurrency(a.CostPerNpcPerDayCny, "CNY");
            panel.m_npcTokenValue.text = CostModel.FormatTokenCount((long)a.TokensPerNpcPerDay);
            panel.m_villageUsdValue.text = CostModel.FormatCurrency(a.DailyAvgCostUsd, "USD");
            panel.m_villageCnyValue.text = CostModel.FormatCurrency(a.DailyAvgCostCny, "CNY");
        }

        private void RefreshVillageGen(CostResponse data)
        {
            if (data.VillageGen == null) return;
            var v = data.VillageGen;
            var panel = _compt.m_villagePanel;
            panel.m_tokenValue.text = CostModel.FormatTokenCount(v.Tokens);
            panel.m_callValue.text = v.Calls.ToString();
            panel.m_costUsdValue.text = CostModel.FormatCurrency(v.CostUsd, "USD");
            panel.m_costCnyValue.text = CostModel.FormatCurrency(v.CostCny, "CNY");
            // 村庄生成开销占比：相对累计 USD（若累计可用）
            float pctValue = 0f;
            if (data.Cumulative != null && data.Cumulative.CostUsd > 0f)
                pctValue = v.CostUsd / data.Cumulative.CostUsd;
            panel.m_percentageValue.text = CostModel.FormatPercentage(pctValue);
        }

        private void RefreshCache(CostResponse data)
        {
            if (data.Cache == null) return;
            var k = data.Cache;
            var panel = _compt.m_cachePanel;
            panel.m_hitTokenValue.text = CostModel.FormatTokenCount(k.HitTokens);
            panel.m_hitRateValue.text = CostModel.FormatPercentage(k.HitRate);
            panel.m_savedUsdValue.text = CostModel.FormatCurrency(k.SavedUsd, "USD");
            panel.m_savedCnyValue.text = CostModel.FormatCurrency(k.SavedCny, "CNY");
            if (panel.m_efficiencyBar != null)
                panel.m_efficiencyBar.value = k.HitRate * 100f;
        }

        private void RefreshModels(CostResponse data)
        {
            if (data.ByModel == null) return;
            var list = _compt.m_modelsPanel.m_modelList;
            list.SetVirtual();
            list.itemRenderer = RenderModelItem;
            list.numItems = data.ByModel.Count;
        }

        private void RenderModelItem(int index, GObject item)
        {
            if (_model?.CostData?.ByModel == null) return;
            if (index < 0 || index >= _model.CostData.ByModel.Count) return;

            var row = item as UIModelListItem;
            if (row == null) return;
            var m = _model.CostData.ByModel[index];

            row.m_txtModelName.text = m.Model;
            row.m_txtTokens.text = CostModel.FormatTokenCount(m.TotalTokens);
            row.m_txtCostUsd.text = CostModel.FormatCurrency(m.CostUsd, "USD");
            row.m_txtCostCny.text = CostModel.FormatCurrency(m.CostCny, "CNY");
            row.m_txtCacheRate.text = CostModel.FormatPercentage(m.CacheHitRate);
            row.m_txtCallCount.text = m.CallCount.ToString();
            if (row.m_cacheProgressBar != null)
                row.m_cacheProgressBar.value = m.CacheHitRate * 100f;
        }

        #endregion


        public override void OnRelease()
        {
            StopAutoRefresh();
            UnbindEvents();
            if (_compt?.m_modelsPanel.m_modelList != null)
                _compt.m_modelsPanel.m_modelList.itemRenderer = null;
            _view.OnChanged = null;
            _compt = null;
            _model = null;
            base.OnRelease();
        }
    }
}