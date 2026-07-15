//------------------------------
// AI Village Cost Display Model
// 作者:
//------------------------------

using ZEngine.Manager.UI;
using Hotfix.Main.Leiya;

namespace Hotfix.Main.UI
{
    /// <summary>
    /// 成本展示面板数据模型。仅在 <see cref="CostController.RefreshUI"/> 渲染时被映射到各子面板文本。
    /// 数据来源： <see cref="AIVillageClient.GetCost"/> 回调写入 <see cref="CostData"/>。
    /// </summary>
    public class CostModel : BaseModel
    {
        /// <summary>成本查询响应（服务端/离线模式返回）。可被外部刷新覆盖。</summary>
        public CostResponse CostData { get; set; }

        /// <summary>是否启用自动刷新（与 Header 面板的 Toggle 双向绑定）。</summary>
        public bool AutoRefreshEnabled { get; set; } = true;

        /// <summary>最近一次数据刷新的时间戳。</summary>
        public System.DateTime LastUpdateTime { get; set; }

        /// <summary>自动刷新间隔（秒），仅 <see cref="AutoRefreshEnabled"/> 为 true 时生效。</summary>
        public float RefreshInterval { get; set; } = 30f;

        public override void Initialize()
        {
            CostData = null;
        }

        public override void OnRelease()
        {
            CostData = null;
        }


        #region 数值格式化（CostBlock / Averages / CacheInfo 通用）

        /// <summary>千 / 百万 简写：1.2K / 3.4M，否则千分位。</summary>
        public static string FormatTokenCount(long tokens)
        {
            if (tokens >= 1000000)
                return $"{tokens / 1000000.0f:F1}M";
            if (tokens >= 1000)
                return $"{tokens / 1000.0f:F1}K";
            return tokens.ToString("N0");
        }

        /// <summary>货币显示。>=1000 保留 2 位，否则保留 6 位（小额更直观）。</summary>
        public static string FormatCurrency(float amount, string currency = "USD")
        {
            string symbol = currency == "CNY" ? "¥" : "$";
            return amount >= 1000 ? $"{symbol}{amount:F2}" : $"{symbol}{amount:F6}";
        }

        /// <summary>占比显示：rate*100 保留 2 位小数 + %。</summary>
        public static string FormatPercentage(float rate)
        {
            return $"{rate * 100:F2}%";
        }

        #endregion
    
        public CostResponse GetInitiazeData()
        {
            return new CostResponse
            {
                Success = false,
                CurrentDay = 0,
                NpcCount = 0,
                RawRecordsCount = 0,
                Cumulative = new CostBlock
                {
                    InputTokens = 0,
                    OutputTokens = 0,
                    TotalTokens = 0,
                    CallCount = 0,
                    CostUsd = 0f,
                    CostCny = 0f,
                    CacheHitTokens = 0,
                    CacheMissTokens = 0,
                    CacheHitRate = 0f,
                    AvgLatencyMs = 0f,
                },
                Averages = new CostAverages
                {
                    NpcCount = 0,
                    DaysSimulated = 0,
                    CostPerNpcPerDayUsd = 0f,
                    CostPerNpcPerDayCny = 0f,
                    TokensPerNpcPerDay = 0f,
                    DailyAvgCostUsd = 0f,
                    DailyAvgCostCny = 0f,
                },
                VillageGen = new VillageGenCost
                {
                    Tokens = 0,
                    Calls = 0,
                    CostUsd = 0f,
                    CostCny = 0f,
                },
                Cache = new CacheInfo
                {
                    HitTokens = 0,
                    HitRate = 0f,
                    SavedUsd = 0f,
                    SavedCny = 0f,
                },
                ByModel = new System.Collections.Generic.List<ModelCostBlock>(),
                Daily = new System.Collections.Generic.List<DailyCost>(),
            };
        }
    }
}