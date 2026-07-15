//------------------------------
// ZEngine
// 作者:
//------------------------------

using System.Text;
using Hotfix.Main.Leiya;
using ZEngine.Manager.UI;

namespace Hotfix.Main.UI
{
    public class InteractModel : BaseModel
    {
        // ── 夺舍状态快照（轮询结果） ──
        public PossessionStateResponse StateSnapshot;

        // ── 当前 phase（waiting_outer / in_dialogue / sleeping 等） ──
        public string Phase => StateSnapshot?.Phase ?? "waiting_outer";

        // ── Action的进入状态 ──
        public int ActState = 0;

        // ── 角色属性（来自 StateSnapshot.Stats） ──
        public PossessionStats Stats => StateSnapshot?.Stats;

        // ── 对话快照（phase=in_dialogue 时非空） ──
        public PossessionDialogue Dialogue => StateSnapshot?.Dialogue;

        // ── 上一次结算结果 ──
        public PossessionSettlement LastSettlement => StateSnapshot?.LastSettlement;

        // ── 当前选中的社交目标 ──
        public AgentRef SelectedTarget;

        // ── 当前选中的外层 Tool 名 ──
        public string SelectedTool;
        
        // ── 当前选中的子动作名 ──        
        public string SelectedAction;

        // ── 当前选中的社交按钮子动作名 ──        
        public string SelectedSocialAction;

        // ── 下拉框选中索引 ──
        public int SelectedMoveIndex;
        public int SelectedSurviveIndex;
        public int SelectedSocialIndex;
        public int SelectedConflictIndex;
        public int SelectedSocialItemIndex;
        public int SelectedBodyPartIndex;

        // ── 文本输入框缓存 ──
        public string FarmInputText;
        public string SocialInputText;
        public string ObserveInputText;
        public string EntertainText;
        public string ItemQtyText;
        public string ConflictText;

        // ── 是否正在等待 API 响应（防重复提交） ──
        public bool IsPending;

        // ── 结果记录去重游标：仅当 rec.Seq > LastShownSeq 才弹窗，保证每条只弹一次 ──
        public int LastShownSeq = 0;

        // ── 上一次记录的 PossessedUid，用于检测换局重置游标 ──
        private string _lastPossessedUid;

        public override void Initialize() { }

        /// <summary>
        /// 检测夺舍目标是否换人（新开一局）后，把去重游标重置为 0。
        /// 应在每次拿到新的 PossessionStateResponse 时调用。
        /// </summary>
        public void CheckResetSeqOnNewRound(string possessedUid)
        {
            if (!string.IsNullOrEmpty(possessedUid)
                && _lastPossessedUid != null
                && _lastPossessedUid != possessedUid)
            {
                LastShownSeq = 0;
            }
            _lastPossessedUid = possessedUid;
        }

        public override void OnRelease()
        {
            StateSnapshot  = null;
            SelectedTarget = null;
            SelectedTool   = null;
            IsPending      = false;
            LastShownSeq   = 0;
            _lastPossessedUid = null;
        }

        // ── 将对话历史格式化为纯文本 ──
        public string BuildDialogueText()
        {
            var turns = Dialogue?.Turns;
            if (turns == null || turns.Count == 0)
                return "（暂无对话记录）";

            var sb = new StringBuilder();
            foreach (var t in turns)
            {
                string tag = t.IsMe ? "【村长】" : $"【{t.Speaker}】";
                sb.AppendLine($"{tag}[{GetSubActionLabel(t.Action)}({t.Action})]");
                if (!string.IsNullOrEmpty(t.Text))
                    sb.AppendLine($"  ↳ {t.Text}");
            }
            return sb.ToString();
        }

        // ── 将最新结算格式化为单行提示 ──
        public string BuildSettlementText()
        {
            if (LastSettlement == null) return string.Empty;
            return string.IsNullOrEmpty(LastSettlement.DeltaText)
                ? LastSettlement.Observation
                : $"{LastSettlement.Observation}\n{LastSettlement.DeltaText}";
        }

        

        // 子动作中文名
        public string GetSubActionLabel(string sub) => sub switch
        {
            "talk"    => "交谈",
            "whisper" => "耳语",
            "insult"  => "辱骂",
            "attack"  => "攻击",
            "give"    => "赠予",
            "trade"   => "交易",
            "observe" => "观察",
            "search"  => "搜查",
            "get"     => "索取",
            "allow"   => "同意",
            "eat"     => "进食",
            "flee"    => "逃离",
            "rest"    => "休息",
            "done"    => "结束对话",
            _         => sub,
        };
    }
}
