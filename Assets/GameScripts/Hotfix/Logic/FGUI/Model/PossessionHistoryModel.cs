using System.Collections.Generic;
using Hotfix.Main.Leiya;
using UnityEngine;
using ZEngine.Manager.UI;

namespace Hotfix.Main.UI
{
    public class PossessionHistoryModel : BaseModel
    {
        // GET /api/possession/history?by_hour=1 的完整数据
        public PossessionHistoryByHourResponse HistoryData { get; set; }

        // 当前选中的游戏天（key 为字符串，如 "1"）
        public string SelectedDay { get; set; }

        // 当前展开的对话详情记录（点击 interact_meta 非空的 item 后赋值）
        public PossessionHistoryRecord ActiveDialogue { get; set; }

        // 所有天的 key 列表（按数值升序，如 ["1","2","3"]）
        public List<string> DayKeys { get; private set; } = new List<string>();

        // 从 HistoryData 重建 DayKeys 并保持数值排序
        public void RebuildDayKeys()
        {
            DayKeys.Clear();
            if (HistoryData?.History == null) return;
            foreach (var key in HistoryData.History.Keys)
                DayKeys.Add(key);
            DayKeys.Sort((a, b) =>
            {
                int.TryParse(a, out int ia);
                int.TryParse(b, out int ib);
                return ia.CompareTo(ib);
            });
        }

        // 取当前选中天的 hour→records 字典，不存在则返回 null
        public Dictionary<string, List<PossessionHistoryRecord>> GetSelectedDayHours()
        {
            if (HistoryData?.History == null || SelectedDay == null) return null;
            HistoryData.History.TryGetValue(SelectedDay, out var hours);
            return hours;
        }

        public static string FormatActionName(string toolName) => toolName switch
        {
            "work"      => "日常劳作",
            "farm"      => "耕种",
            "gather"    => "采集",
            "eat"       => "进食",
            "rest"      => "休息",
            "move"      => "移动",
            "socialize" => "社交",
            "talk"      => "对话",
            "whisper"   => "密语",
            "give"      => "赠予",
            "sleep"     => "入睡",
            _           => toolName ?? "",
        };

        public static string FormatActionIcon(string toolName, bool success) => toolName switch
        {
            "sleep"     => "眠",
            "talk"      or
            "socialize" or
            "whisper"   => "话",
            _           => success ? "OK" : "X",
        };

        public static Color FormatActionIconColor(string toolName, bool success)
        {
            if (toolName is "talk" or "socialize" or "whisper") return new Color32(0x88, 0xCC, 0xFF, 0xFF);
            if (toolName == "sleep") return new Color32(0x88, 0x99, 0xBB, 0xFF);
            return success ? new Color32(0x44, 0xFF, 0x88, 0xFF) : new Color32(0xFF, 0x44, 0x44, 0xFF);
        }

        public override void Initialize() { }

        public override void OnRelease()
        {
            HistoryData    = null;
            SelectedDay    = null;
            ActiveDialogue = null;
            DayKeys.Clear();
        }
    }
}
