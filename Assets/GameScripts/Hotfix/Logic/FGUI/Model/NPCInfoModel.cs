using Hotfix.FuncModule.AITown;
using Hotfix.Main.Leiya;
using ZEngine.Manager.UI;

namespace Hotfix.Main.UI
{
    public class NPCInfoModel : BaseModel
    {
        /// <summary>
        /// 信息类型：attributes, schedule, background
        /// </summary>
        public string InfoType { get; set; } = "attributes";

        /// <summary>
        /// 当前显示的NPC数据
        /// </summary>
        public AgentInfo Info { get; set; }

        /// <summary>
        /// 从服务器获取的NPC详细数据
        /// </summary>
        public NPCDetailData DetailData { get; set; }

        /// <summary>
        /// 获取当前信息的标题
        /// </summary>
        public string GetCurrentTitle()
        {
            if (Info == null) return "";
            return InfoType switch
            {
                "attributes" => Info.AgentName + " - 属性",
                "schedule"   => Info.AgentName + " - 日程安排",
                "background" => Info.AgentName + " - 背景故事",
                _            => Info.AgentName,
            };
        }

        /// <summary>
        /// 获取当前信息的内容
        /// </summary>
        public string GetCurrentContent()
        {
            if (Info == null) return "";
            if (DetailData != null)
            {
                return InfoType switch
                {
                    "attributes" => BuildAttributesText(DetailData, Info),
                    "schedule"   => BuildScheduleText(DetailData),
                    "background" => BuildBackgroundText(DetailData),
                    _            => "",
                };
            }
            return InfoType switch
            {
                "attributes" => BuildLocalAttributesText(Info),
                "schedule"   => BuildLocalScheduleText(Info),
                "background" => BuildLocalBackgroundText(Info),
                _            => "",
            };
        }

        public override void Initialize() { }

        public override void OnRelease()
        {
            Info = null;
            DetailData = null;
        }

        #region API数据拼接

        private static string BuildAttributesText(NPCDetailData data, AgentInfo info)
        {
            var sb = new System.Text.StringBuilder();

            // 基本信息
            if (data.Profile != null)
            {
                var p = data.Profile;
                sb.AppendLine("=== 基本信息 ===");
                sb.AppendLine("姓名: "   + (p.Name          ?? info.AgentName));
                sb.AppendLine("年龄: "   + p.Age + " 岁");
                sb.AppendLine("性别: "   + (p.Gender         ?? "未知"));
                sb.AppendLine("职业: "   + (p.Occupation     ?? info.AgentType));
                sb.AppendLine("性格: "   + (p.Personality    ?? "未知"));
                sb.AppendLine("人生抱负: " + (p.LifeAspiration ?? "未知"));
                sb.AppendLine();
            }

            if (data.Attributes != null)
            {
                var attr = data.Attributes;

                // 健康状态
                if (attr.Health != null)
                {
                    sb.AppendLine("=== 健康状态 ===");
                    sb.AppendLine("整体状态: " + (attr.Health.OverallStatus ?? "未知"));
                    sb.AppendLine();
                }

                // 需求状态
                if (attr.Needs != null)
                {
                    var n = attr.Needs;
                    sb.AppendLine("=== 需求状态 ===");
                    sb.AppendLine($"食物: {n.Food:F1}%");
                    sb.AppendLine($"休息: {n.Rest:F1}%");
                    sb.AppendLine($"娱乐: {n.Entertainment:F1}%");
                    sb.AppendLine($"体力: {n.Stamina:F1}%");
                    sb.AppendLine($"能量: {n.Energy:F1}%");
                    sb.AppendLine();
                }

                // 情绪状态
                if (attr.Mood != null)
                {
                    var m = attr.Mood;
                    sb.AppendLine("=== 情绪状态 ===");
                    if (!string.IsNullOrEmpty(m.Summary))
                        sb.AppendLine("情绪概况: " + m.Summary);
                    sb.AppendLine($"心情值: {m.Mood:F1}");
                    if (m.IsInsane)
                    {
                        sb.AppendLine("=== 精神状态 ===");
                        sb.AppendLine("疯狂原型: " + (m.InsanityArchetype ?? "未知"));
                        sb.AppendLine($"剩余疯狂: {m.InsanityRemaining:F1}");
                        sb.AppendLine($"恢复进度: {m.HealingProgress:F1}");
                    }
                    sb.AppendLine();
                }

                // 标签
                if (attr.Tags != null && attr.Tags.Count > 0)
                {
                    sb.AppendLine("=== 标签 ===");
                    foreach (var tag in attr.Tags)
                    {
                        if (!tag.Visible) continue;
                        sb.Append("• " + (tag.Name ?? "未知"));
                        if (!string.IsNullOrEmpty(tag.Description))
                            sb.Append(" - " + tag.Description);
                        sb.AppendLine();
                    }
                    sb.AppendLine();
                }
            }

            // 社交关系
            if (data.Social?.Relationships != null && data.Social.Relationships.Count > 0)
            {
                sb.AppendLine("=== 社交关系 ===");
                foreach (var rel in data.Social.Relationships)
                {
                    sb.Append("• " + (rel.Name ?? "未知"));
                    sb.Append($" (威望: {rel.Prestige}");
                    if (!string.IsNullOrEmpty(rel.Kinship))
                        sb.Append(", " + rel.Kinship);
                    sb.AppendLine(")");
                }
                sb.AppendLine();
            }

            // 位置 & 步数
            if (!string.IsNullOrEmpty(data.Location))
                sb.AppendLine("当前位置: " + data.Location);
            sb.Append("步数: " + data.StepCount);

            return sb.ToString();
        }

        private static string BuildScheduleText(NPCDetailData data)
        {
            var sb = new System.Text.StringBuilder();

            if (data.Schedule == null)
                return "暂无日程数据";

            var s = data.Schedule;
            sb.AppendLine("=== 时间信息 ===");
            sb.AppendLine($"当前: 第{s.CurrentDay}天 {s.CurrentHour}:00");
            sb.AppendLine($"起床时间: {s.WakeHour}:00");
            sb.AppendLine($"就寝时间: {s.BedtimeHour}:00");
            sb.AppendLine("是否睡眠中: " + (s.IsSleeping ? "是" : "否"));
            sb.AppendLine();

            if (s.TodaySchedule != null && s.TodaySchedule.Count > 0)
            {
                sb.AppendLine("=== 今日日程 ===");
                foreach (var block in s.TodaySchedule)
                {
                    sb.AppendLine($"{block.StartHour:D2}:00 - {block.EndHour:D2}:00  {block.Activity}");
                    if (!string.IsNullOrEmpty(block.Details))
                        sb.AppendLine("  └ " + block.Details);
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }

        private static string BuildBackgroundText(NPCDetailData data)
        {
            var sb = new System.Text.StringBuilder();
            
            if (data.Profile != null)
            {
                // 背景故事
                if (!string.IsNullOrEmpty(data.Profile.Background))
                {
                    sb.AppendLine(data.Profile.Background);
                    sb.AppendLine();
                }

                // 目标
                if (data.Profile.Goals != null && data.Profile.Goals.Count > 0)
                {
                    sb.AppendLine("=== 人生目标 ===");
                    foreach (var goal in data.Profile.Goals)
                        sb.AppendLine("• " + goal);
                    sb.AppendLine();
                }
            }

            // 当前任务目标
            if (data.Goals != null && data.Goals.Count > 0)
            {
                sb.AppendLine("=== 当前任务 ===");
                foreach (var goal in data.Goals)
                {
                    sb.AppendLine("• " + (goal.Description ?? "未知"));
                    sb.AppendLine($"  进度: {goal.Progress * 100:F0}%");
                    sb.AppendLine("  状态: " + (goal.Completed ? "已完成" : "进行中"));
                }
            }

            // 记忆片段
            if (data.Memories != null)
            {
                if (data.Memories.Important != null && data.Memories.Important.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("=== 重要记忆 ===");
                    foreach (var mem in data.Memories.Important)
                        sb.AppendLine($"• 第{mem.Day}天 {mem.Hour}:00 - {mem.Content ?? "未知"}");
                }

                if (data.Memories.Recent != null && data.Memories.Recent.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("=== 近期记忆 ===");
                    int max = System.Math.Min(5, data.Memories.Recent.Count);
                    for (int i = 0; i < max; i++)
                    {
                        var mem = data.Memories.Recent[i];
                        sb.AppendLine($"• 第{mem.Day}天 {mem.Hour}:00 - {mem.Content ?? "未知"}");
                    }
                }
            }

            return sb.Length > 0 ? sb.ToString() : "暂无背景数据";
        }

        #endregion

        #region 本地数据拼接（无服务器数据时的兜底）

        private static string BuildLocalAttributesText(AgentInfo info)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("姓名: " + info.AgentName);
            sb.AppendLine("类型: " + info.AgentType);
            sb.AppendLine();
            sb.AppendLine("分配的房屋: 无");
            sb.AppendLine("分配的田地: 无");
            sb.AppendLine();
            sb.Append("当前状态: 未知");
            return sb.ToString();
        }

        private static string BuildLocalScheduleText(AgentInfo info)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"这是 {info.AgentName} 的默认日程：");
            sb.AppendLine();
            sb.AppendLine("06:00 - 起床");
            sb.AppendLine("07:00 - 早餐");
            sb.AppendLine("08:00 - 前往田地工作");
            sb.AppendLine("12:00 - 午餐休息");
            sb.AppendLine("13:00 - 继续工作");
            sb.AppendLine("18:00 - 返回家中");
            sb.AppendLine("19:00 - 晚餐");
            sb.AppendLine("21:00 - 休息活动");
            sb.AppendLine("22:00 - 睡觉");
            sb.AppendLine();
            sb.Append("（可以通过服务器或配置文件自定义日程）");
            return sb.ToString();
        }

        private static string BuildLocalBackgroundText(AgentInfo info)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"{info.AgentName} 是村庄中的一员。");
            sb.AppendLine();
            switch (info.AgentType)
            {
                case "farmer":
                case "农民":
                    sb.AppendLine($"作为一名勤劳的农民，{info.AgentName} 每天都在田间辛勤劳作，");
                    sb.Append("种植各种作物，为村庄提供粮食。虽然工作辛苦，但看到丰收的喜悦总能让人忘记疲惫。");
                    break;
                case "merchant":
                case "商人":
                    sb.AppendLine($"{info.AgentName} 是一位精明的商人，经常往来于各个村镇之间，");
                    sb.Append("买卖各种货物。凭借敏锐的商业嗅觉和诚信的经营理念，在村民中赢得了良好的声誉。");
                    break;
                case "villager":
                case "村民":
                    sb.AppendLine($"{info.AgentName} 是村庄中的普通村民，过着简单而充实的生活。");
                    sb.Append("喜欢和邻居们聊天，分享生活中的点点滴滴。");
                    break;
                case "elder":
                case "长者":
                    sb.AppendLine($"{info.AgentName} 是村中德高望重的长者，见证了村庄的发展变迁。");
                    sb.Append("经常给年轻人讲述过去的故事，传授生活的智慧。");
                    break;
                case "woodcutter":
                case "樵夫":
                    sb.AppendLine($"{info.AgentName} 是一名强壮的樵夫，每天进入森林砍伐木材。");
                    sb.Append("对森林的每一个角落都了如指掌，是村民们建造房屋的重要帮手。");
                    break;
                case "fisherman":
                case "渔夫":
                    sb.AppendLine($"{info.AgentName} 是一位经验丰富的渔夫，熟悉水域的每一个鱼群。");
                    sb.Append("清晨出船，傍晚归来，总能带回新鲜的鱼获。");
                    break;
                default:
                    sb.Append($"{info.AgentName} 在村庄中有着自己的角色和故事。");
                    break;
            }
            sb.AppendLine();
            sb.AppendLine();
            sb.Append("（可以通过服务器或配置文件自定义背景故事）");
            return sb.ToString();
        }

        #endregion
    }
}
