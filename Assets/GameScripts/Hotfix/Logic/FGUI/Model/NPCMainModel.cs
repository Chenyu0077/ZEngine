using System.Text;
using Hotfix.FuncModule;
using Hotfix.FuncModule.AITown;
using Hotfix.Main.Leiya;
using ZEngine.Manager.Log;
using ZEngine.Manager.UI;

namespace Hotfix.Main.UI
{
    public class NPCMainModel : BaseModel
    {
        /// <summary>
        /// 当前显示的NPC数据
        /// </summary>
        public AgentInfo Info { get; set; }

        /// <summary>
        /// 从服务器获取的NPC详细数据
        /// </summary>
        public NPCDetailData DetailData { get; set; }

        /// <summary>
        /// 单NPC交互信息
        /// </summary>
        public InteractResponse InteractData { get; set; }

        /// <summary>
        /// 当前选中的标签页：basic / abilities / mood / needs / assets / social / tags
        /// </summary>
        public string CurrentTab { get; set; } = "basic";

        public override void Initialize() { }

        public override void OnRelease()
        {
            Info = null;
            DetailData = null;
            CurrentTab = "basic";
        }

        // ── 标签页内容 ──────────────────────────────────────────
        public string GetTabContent(string tab)
        {
            if (DetailData != null)
                return BuildApiTab(tab);
            return Info != null ? BuildLocalTab(tab) : "";
        }


        // ── 日程面板 ────────────────────────────────────────────
        public string GetScheduleContent()
        {
            if (DetailData?.Schedule == null)
                return "暂无日程数据";

            var s = DetailData.Schedule;
            var sb = new StringBuilder();
            sb.AppendLine($"第{s.CurrentDay}天 {s.CurrentHour}:00");
            sb.AppendLine($"起床: {s.WakeHour}:00 | 睡觉: {s.BedtimeHour}:00");
            sb.AppendLine("睡眠中: " + (s.IsSleeping ? "是" : "否"));
            sb.AppendLine();

            if (s.TodaySchedule != null && s.TodaySchedule.Count > 0)
            {
                sb.AppendLine("今日日程：");
                foreach (var block in s.TodaySchedule)
                {
                    string line = $"{block.StartHour:D2}:00-{block.EndHour:D2}:00 {block.Activity}";
                    string status = block.Status ?? "";
                    if (status == "completed")
                        sb.AppendLine($"[{line}] √");
                    else if (status == "in_progress")
                        sb.AppendLine($"> {line}");
                    else
                        sb.AppendLine(line);

                    if (!string.IsNullOrEmpty(block.Details))
                        sb.AppendLine("  └ " + block.Details);
                }
            }

            return sb.ToString();
        }


        // ── 当前动作面板 ────────────────────────────────────────
        public string GetCurrentActionContent()
        {
            // 拼接当前NPC所有的Interactions
            if (InteractData?.Interactions == null || InteractData.Interactions.Count == 0)
            {
                return "暂无交互记录";
            }

            var sb = new StringBuilder();
            sb.AppendLine($"交互记录总数: {InteractData.Stats?.Total ?? InteractData.Interactions.Count}");
            sb.AppendLine();

            // 按照顺序拼接每个交互记录
            for (int i = 0; i < InteractData.Interactions.Count; i++)
            {
                var interaction = InteractData.Interactions[i];

                // 交互基本信息
                sb.AppendLine($"=== 交互 #{interaction.Id} ({interaction.Intent}) ===");
                sb.AppendLine($"发起方: {interaction.InitiatorName} → 目标方: {interaction.TargetName}");
                sb.AppendLine($"时间: 第{interaction.Day}天 {interaction.Hour}时 | 轮次: {interaction.Rounds}");
                sb.AppendLine();

                // 对话详情
                if (interaction.DialogueLog != null && interaction.DialogueLog.Count > 0)
                {
                    sb.AppendLine("对话详情:");
                    foreach (var step in interaction.DialogueLog)
                    {
                        sb.AppendLine($"  第{step.Round}轮 - {step.Actor}:");
                        sb.AppendLine($"    动作: {step.Action}");

                        if (!string.IsNullOrEmpty(step.ActionInput))
                        {
                            sb.AppendLine($"    内容: {step.ActionInput}");
                        }

                        if (!string.IsNullOrEmpty(step.ActorObs))
                        {
                            sb.AppendLine($"    观察: {step.ActorObs}");
                        }
                        sb.AppendLine();
                    }
                }

                // 发起方总结
                if (!string.IsNullOrEmpty(interaction.SummaryInitiator))
                {
                    sb.AppendLine($"发起方总结: {interaction.SummaryInitiator}");
                    sb.AppendLine();
                }

                // 关键结果
                if (!string.IsNullOrEmpty(interaction.KeyOutcomes))
                {
                    sb.AppendLine($"关键结果: {interaction.KeyOutcomes}");
                    sb.AppendLine();
                }

                // 分隔线（最后一个交互不添加）
                if (i < InteractData.Interactions.Count - 1)
                {
                    sb.AppendLine("─────────────────────────────────────");
                    sb.AppendLine();
                }
            }

            LogManager.Instance.Info($"Interact: {sb.ToString()}");
            return sb.ToString();
        }


        // ── 身体面板 ────────────────────────────────────────────
        public string GetBodyContent()
        {
            var health = DetailData?.Attributes?.Health;
            if (health == null) return "暂无健康数据";

            var sb = new StringBuilder();
            sb.AppendLine("整体: " + (health.OverallStatus ?? "正常"));
            if (health.Parts != null)
            {
                var p = health.Parts;
                sb.AppendLine("头部: "  + (p.Head      ?? "正常"));
                sb.AppendLine("躯干: "  + (p.Torso     ?? "正常"));
                sb.AppendLine("左臂: "  + (p.LeftArm   ?? "正常"));
                sb.AppendLine("右臂: "  + (p.RightArm  ?? "正常"));
                sb.AppendLine("左腿: "  + (p.LeftLeg   ?? "正常"));
                sb.AppendLine("右腿: "  + (p.RightLeg  ?? "正常"));
            }
            return sb.ToString();
        }


        // ── API 标签页拼接 ──────────────────────────────────────
        private string BuildApiTab(string tab)
        {
            return tab switch
            {
                "basic"     => BuildBasicInfo(),
                "abilities" => BuildAbilitiesInfo(),
                "mood"      => BuildMoodInfo(),
                "needs"     => BuildNeedsInfo(),
                "assets"    => BuildAssetsInfo(),
                "social"    => BuildSocialInfo(),
                "tags"      => BuildTagsInfo(),
                _           => "",
            };
        }

        private string BuildBasicInfo()
        {
            var sb = new StringBuilder();
            var p = DetailData.Profile;
            if (p != null)
            {
                sb.AppendLine("姓名: " + (p.Name ?? Info?.AgentName ?? "未知"));
                sb.AppendLine($"年龄: {p.Age} 岁 | 性别: {p.Gender ?? "?"}");
                sb.AppendLine("性格: " + (p.Personality    ?? "未知"));
                sb.AppendLine("抱负: " + (p.LifeAspiration ?? "未知"));
            }

            var health = DetailData.Attributes?.Health;
            if (health != null)
                sb.AppendLine("健康: " + (health.OverallStatus ?? "正常"));

            if (!string.IsNullOrEmpty(DetailData.Location))
                sb.Append("位置: " + DetailData.Location);

            return sb.ToString();
        }

        private string BuildAbilitiesInfo()
        {
            // abilities 字段当前协议未定义，留占位
            return "暂无能力数据";
        }

        private string BuildMoodInfo()
        {
            var mood = DetailData.Attributes?.Mood;
            if (mood == null) return "暂无情绪数据";

            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(mood.Summary))
            {
                sb.AppendLine(mood.Summary);
                sb.AppendLine();
            }
            sb.AppendLine($"心情值: {mood.Mood:F0}");
            if (mood.IsInsane)
            {
                sb.AppendLine("疯狂原型: " + (mood.InsanityArchetype ?? "未知"));
                sb.AppendLine($"剩余疯狂: {mood.InsanityRemaining:F1}");
                sb.AppendLine($"恢复进度: {mood.HealingProgress:F1}");
            }
            return sb.ToString();
        }

        private string BuildNeedsInfo()
        {
            var n = DetailData.Attributes?.Needs;
            if (n == null) return "暂无需求数据";

            var sb = new StringBuilder();
            sb.AppendLine($"食物: {n.Food:F0}% | 休息: {n.Rest:F0}%");
            sb.AppendLine($"娱乐: {n.Entertainment:F0}% | 体力: {n.Stamina:F0}%");
            sb.Append($"能量: {n.Energy:F0}%");
            return sb.ToString();
        }

        private string BuildAssetsInfo()
        {
            // assets 字段当前协议未定义，留占位
            return "暂无资产数据";
        }

        private string BuildSocialInfo()
        {
            var rels = DetailData.Social?.Relationships;
            if (rels == null || rels.Count == 0) return "暂无社交数据";

            var sb = new StringBuilder();
            foreach (var rel in rels)
            {
                sb.Append("• " + (rel.Name ?? "未知"));
                sb.Append($" (威望: {rel.Prestige}");
                if (!string.IsNullOrEmpty(rel.Kinship))
                    sb.Append(" | " + rel.Kinship);
                sb.AppendLine(")");
            }
            return sb.ToString();
        }

        private string BuildTagsInfo()
        {
            var tags = DetailData.Attributes?.Tags;
            if (tags == null || tags.Count == 0) return "暂无标签数据";

            var sb = new StringBuilder();
            foreach (var tag in tags)
            {
                if (!tag.Visible) continue;
                sb.AppendLine("【" + (tag.Name ?? "未知") + "】");
                if (!string.IsNullOrEmpty(tag.Description))
                    sb.AppendLine("  " + tag.Description);
                sb.AppendLine();
            }
            return sb.Length > 0 ? sb.ToString() : "暂无标签数据";
        }


        // ── 本地兜底（无服务器数据时）──────────────────────────
        private string BuildLocalTab(string tab)
        {
            if (tab != "basic") return "（需要从服务器获取）";

            var sb = new StringBuilder();
            sb.AppendLine("姓名: " + Info.AgentName);
            sb.Append("类型: " + Info.AgentType);
            return sb.ToString();
        }
    }
}
