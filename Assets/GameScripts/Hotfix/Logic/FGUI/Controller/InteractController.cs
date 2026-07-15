//------------------------------
// ZEngine
// 作者:
//------------------------------

using DG.Tweening;
using FairyGUI;
using UnityEngine;
using Hotfix.FuncModule;
using Hotfix.Main.Leiya;
using Hotfix.UI.Generate.Main;
using Newtonsoft.Json;
using ZEngine.Manager.Log;
using ZEngine.Manager.Timer;
using ZEngine.Manager.UI;
using System.Collections.Generic;
using System;

namespace Hotfix.Main.UI
{
    /// <summary>
    /// 夺舍交互面板控制器。
    /// 对应 UIInteractView 字段：
    ///   m_tab              — Tab 控制器（move/work/survive/social/observe/conflict）
    ///   m_dialogPop        — 对话记录展开控制器
    ///   m_TabMove/Work/…   — Tab 按钮
    ///   m_ToolComboBox     — 通用工具下拉（进食/睡觉/休息/娱乐）
    ///   m_MoveComboBox     — 移动目标地点下拉
    ///   m_SurviceComboBox  — 生存（吃什么）下拉
    ///   m_SocialComboBox   — 社交目标下拉
    ///   m_ConflictComboBox — 冲突目标下拉
    ///   m_farmInput        — 劳作细节输入框
    ///   m_socialInput      — 社交话题输入框
    ///   m_ovserveInput     — 观察细节输入框
    ///   m_BtnExec          — 执行按钮
    ///   m_btnPop           — 侧边折叠按钮
    ///   m_socialActionPanel— 社交子动作面板（in_dialogue 时可用）
    ///   m_dialogPopBtn     — 对话记录折叠按钮
    ///   m_history          — 对话历史滚动文本
    /// </summary>
    public class InteractController : BaseController
    {
        #region 字段

        private UIInteractView _compt;
        private new InteractModel _model;

        // 定时器
        private Timer _pollTimer;
        private const float PollInterval = 2f;

        // 标记是否为玩家主动关闭对话（区分超时关闭）
        private bool _manualDialogueClose;

        // 各 Tab 对应的 tool 名（与 PossessionActRequest.Tool 一致）
        private static readonly string[] TabTools =
        {
            "move",     // 0: 移动
            "work",     // 1: 劳作
            "survive",  // 2: 生存（进食）
            "socialize",// 3: 社交
            "observe",  // 4: 观察
            "conflict", // 5: 冲突
        };

        // 各 Tab 下拉框的可选 tool 列表
        private static readonly string[] MoveToolsValues     = { "move", "flee" };
        private static readonly string[] WorkToolsValues     = { "farm", "work", "build", "craft", "gather" };
        private static readonly string[] SurviveToolsValues  = { "eat", "sleep", "rest", "entertain" };
        private static readonly string[] SocialToolsValues   = { "talk", "whisper", "socialize", "give", "trade", "read_notice" };
        private static readonly string[] ObserseToolsValues   = { "observe", "listen", "search" };
        private static readonly string[] ConflictToolsValues = { "attack", "insult", "get" };

        private static readonly string[] MoveTools     = { "移动(move)", "逃跑(flee)" };
        private static readonly string[] WorkTools     = { "务农(farm)", "工作(work)", "建造(build)", "制作物品(craft)", "采集资源(gather)" };
        private static readonly string[] SurviveTools  = { "进食(eat)", "睡觉(sleep)", "休息(rest)", "娱乐(entertain)" };
        private static readonly string[] SocialTools   = { "交谈(talk)", "耳语(whisper)", "社交(socialize)", "给(give)", "交易(trade)", "查看公示(read_notice)" };
        private static readonly string[] ObserseTools   = { "观察(observe)", "倾听(listen)", "搜查(search)" };
        private static readonly string[] ConflictTools = { "攻击(attack)", "辱骂(insult)", "抢夺(get)" };

        private static readonly string[] BodyPartValues = { "随机", "头", "胸", "左臂", "右臂", "左腿", "右腿" };
        private static readonly string[] BodyPartItems  = { "随机", "头", "胸", "左臂", "右臂", "左腿", "右腿" };
        #endregion

        #region 生命周期

        public override void Initialize()
        {
            base.Initialize();
            _compt = _view.GetView() as UIInteractView;
            _model = base._model as InteractModel;
            if (_compt == null) return;

            RefreshPossessionState();
            BindEvents();
            RefreshUI();
            SetPending(false);
            SetTipInfoContent("暂无内容");
            SetTextFormat(_compt.m_history.m_content);
            SetTextFormat(_compt.m_tipInfoCom.m_content);
            _pollTimer = TimerManager.Instance.CreatePepeatTimer(RefreshPossessionState, 0f, PollInterval);
        }

        public override void OnRelease()
        {
            if (_compt == null)
            {
                base.OnRelease();
                return;
            }

            _compt.m_TabMove.onClick.Remove(OnTabMove);
            _compt.m_TabWork.onClick.Remove(OnTabWork);
            _compt.m_TabSurvive.onClick.Remove(OnTabSurvive);
            _compt.m_TabSocial.onClick.Remove(OnTabSocial);
            _compt.m_TabObserve.onClick.Remove(OnTabObserve);
            _compt.m_TabConflict.onClick.Remove(OnTabConflict);

            _compt.m_ToolComboBox.onChanged.Remove(OnToolComboChanged);
            _compt.m_MoveComboBox.onChanged.Remove(OnMoveComboChanged);
            _compt.m_SurviceComboBox.onChanged.Remove(OnSurviveComboChanged);
            _compt.m_SocialComboBox.onChanged.Remove(OnSocialComboChanged);
            _compt.m_ConflictComboBox.onChanged.Remove(OnConflictComboChanged);
            _compt.m_attackComboBox.onChanged.Remove(OnAttackComboChanged);
            _compt.m_cinflictInput.onChanged.Remove(OnConflictInputChanged);
            _compt.m_itemComboBox.onChanged.Remove(OnItemComboChanged);

            _compt.m_farmInput.onChanged.Remove(OnFarmInputChanged);
            _compt.m_socialInput.onChanged.Remove(OnSocialInputChanged);
            _compt.m_ovserveInput.onChanged.Remove(OnObserveInputChanged);
            _compt.m_surviceInput.onChanged.Remove(OnSurviceInputChanged);
            _compt.m_socialInput2.onChanged.Remove(OnSocialInput2Changed);

            _compt.m_BtnExec.onClick.Remove(OnExec);
            _compt.m_BtnIdle.onClick.Remove(OnBtnIdleEvent);
            _compt.m_btnPop.onClick.Remove(OnBtnPopEvent);
            _compt.m_dialogPopBtn.onClick.Remove(OnDialogPopBtnEvent);

            if (_compt.m_socialActionPanel?.m_btnList != null)
                _compt.m_socialActionPanel.m_btnList.onClickItem.Remove(OnSubActionClicked);
            _compt.m_socialActionPanel.m_btnTalkOver.onClick.Remove(OnTalkOverClicked);
            _compt.m_socialActionPanel.m_btnExcuteTalk.onClick.Remove(OnExcuteTalklicked);
            _compt.m_statusPanel.m_bagToggleBtn.onClick.Remove(OnBagCaollapseClicked);

            _pollTimer?.Kill();
            _pollTimer = null;

            _compt = null;
            _model = null;
            base.OnRelease(); // 同时将父类 _model/_view 置 null
        }


        #endregion

        #region 初始化

        private void BindEvents()
        {
            // Tab按钮
            _compt.m_TabMove.onClick.Add(OnTabMove);
            _compt.m_TabWork.onClick.Add(OnTabWork);
            _compt.m_TabSurvive.onClick.Add(OnTabSurvive);
            _compt.m_TabSocial.onClick.Add(OnTabSocial);
            _compt.m_TabObserve.onClick.Add(OnTabObserve);
            _compt.m_TabConflict.onClick.Add(OnTabConflict);

            // 下拉框
            _compt.m_ToolComboBox.onChanged.Add(OnToolComboChanged);
            _compt.m_MoveComboBox.onChanged.Add(OnMoveComboChanged);
            _compt.m_SurviceComboBox.onChanged.Add(OnSurviveComboChanged);
            _compt.m_SocialComboBox.onChanged.Add(OnSocialComboChanged);
            _compt.m_ConflictComboBox.onChanged.Add(OnConflictComboChanged);
            _compt.m_attackComboBox.onChanged.Add(OnAttackComboChanged);
            _compt.m_cinflictInput.onChanged.Add(OnConflictInputChanged);
            _compt.m_itemComboBox.onChanged.Add(OnItemComboChanged);

            _compt.m_farmInput.onChanged.Add(OnFarmInputChanged);
            _compt.m_socialInput.onChanged.Add(OnSocialInputChanged);
            _compt.m_ovserveInput.onChanged.Add(OnObserveInputChanged);
            _compt.m_surviceInput.onChanged.Add(OnSurviceInputChanged);
            _compt.m_socialInput2.onChanged.Add(OnSocialInput2Changed);

            _compt.m_BtnExec.onClick.Add(OnExec);
            _compt.m_BtnIdle.onClick.Add(OnBtnIdleEvent);
            _compt.m_btnPop.onClick.Add(OnBtnPopEvent);
            _compt.m_dialogPopBtn.onClick.Add(OnDialogPopBtnEvent);

            if (_compt.m_socialActionPanel?.m_btnList != null)
                _compt.m_socialActionPanel.m_btnList.onClickItem.Add(OnSubActionClicked);

            _compt.m_socialActionPanel.m_btnTalkOver.onClick.Add(OnTalkOverClicked);
            _compt.m_socialActionPanel.m_btnExcuteTalk.onClick.Add(OnExcuteTalklicked);
            _compt.m_statusPanel.m_bagToggleBtn.onClick.Add(OnBagCaollapseClicked);

            SetComboBox(_compt.m_ToolComboBox,  MoveTools, MoveToolsValues);
        }


        private void SetComboBox(GComboBox box, string[] labels, string[] values)
        {
            box.items  = labels;
            box.values = values;
            if (labels.Length > 0)
            {
                box.selectedIndex = 0;
                // 同步 SelectedAction：FairyGUI 在程序赋值 selectedIndex 时不触发 onChanged
                if (box == _compt.m_ToolComboBox && _model != null)
                    _model.SelectedAction = values[0];
            }
        }

        // 设置提示信息内容
        private void SetTipInfoContent(string content)
        {
            if (_compt == null) return;
            _compt.m_tipInfoCom.m_content.text = content;
        }

        #endregion

        #region 状态轮询

        /// <summary>
        /// 拉取夺舍状态快照、刷新面板，并按 seq 去重消费 last_settlement（兜底迟到/超时结束记录）。
        /// </summary>
        private void RefreshPossessionState()
        {
            AIVillageClient.Instance.GetPossessionState(
                onSuccess: response =>
                {
                    if (_model == null) return;
                    ApplyStateResponse(response, null);
                },
                onError: err => LogManager.Instance.Error($"[Interact] GetPossessionState 失败: {err}")
            );
        }


        /// <summary>
        /// 统一处理 /state 响应：换局重置游标 → 写入快照 → 消费 last_settlement（seq 去重）→ 回调 → 刷新UI。
        /// </summary>
        private void ApplyStateResponse(PossessionStateResponse response, Action<PossessionStateResponse> onStateSuccess)
        {
            // 检测换局（possessed_uid 变化）重置 seq 游标
            _model.CheckResetSeqOnNewRound(response?.PossessedUid);

            // 检测对话是否被服务端结束（超时/引擎终止）：上一帧 in_dialogue，本帧已不在
            bool wasInDialogue = _model.Phase == "in_dialogue";
            bool nowInDialogue = response?.Phase == "in_dialogue";
            bool dialogueJustEnded = wasInDialogue && !nowInDialogue;

            _model.StateSnapshot = response;

            // 统一消费最近结算：seq > LastShownSeq 才弹，旧记录自动跳过
            ConsumeSettlement(response?.LastSettlement);

            if (onStateSuccess != null)
                onStateSuccess(response);

            RefreshUI();

            // 对话刚结束：关闭子动作面板 + 收起对话历史区
            // 主动关闭时面板已在 OnTalkOverClicked 里即时关闭，此处仅处理超时关闭
            if (dialogueJustEnded && _compt != null && !_manualDialogueClose)
            {
                _compt.m_socialActionCtrol.selectedIndex = 0;
                _compt.m_dialogPop.selectedIndex = 0;
                LogManager.Instance.Info("[Interact] 对话超时，关闭对话面板");
                ShowTipView("已关闭对话");
            }
            _manualDialogueClose = false;
        }

        #endregion

        #region UI 刷新

        private void RefreshUI()
        {
            if (_compt == null || _model == null) return;

            RefreshStatusPanel();
            RefreshDropdowns();
            RefreshDialoguePanel();
            RefreshTips();
            _compt.m_BtnExec.enabled = !_model.IsPending;
        }

        // 进度条最大宽度（与 StatusBarItem.xml barBg size 一致）
        private const float BarMaxWidth = 320f;
        // 低值警告阈值
        private const float WarnThreshold = 20f;

        private void RefreshStatusPanel()
        {
            var panel = _compt.m_statusPanel;
            if (panel == null) return;

            var stats = _model.Stats;
            if (stats != null)
            {
                SetBarItem(panel.m_health, "❤", "体力",  stats.Energy,        100f);
                SetBarItem(panel.m_energy, "⚡", "精力",  stats.Stamina,       100f);
                SetBarItem(panel.m_food,   "🍜", "食物", stats.Food,          100f);
                SetBarItem(panel.m_mood,   "😊", "心情", stats.Mood,          100f);
                SetBarItem(panel.m_rest,   "🌙", "休息", stats.Rest,          100f);
                SetBarItem(panel.m_play,   "🎮", "娱乐", stats.Entertainment, 100f);
            }

            RefreshInventory(panel);
        }

        private void SetBarItem(UIStatusBarItem item, string icon, string label, float value, float max)
        {
            if (item == null) return;
            item.m_icon.text  = icon;
            item.m_label.text = label;
            item.m_value.text = Mathf.RoundToInt(value).ToString();
            item.m_barFill.width = Mathf.Clamp01(value / max) * BarMaxWidth;
            item.m_warn.selectedIndex = value <= WarnThreshold ? 1 : 0;
        }

        // 背包中的物品
        private void RefreshInventory(UIStatusPanel panel)
        {
            var inventory = _model.Stats?.Inventory;
            int count = inventory?.Count ?? 0;

            panel.m_bagCount.text = $"({count} 件)";

            panel.m_itemList.itemRenderer = (index, obj) =>
            {
                if (obj is UIItemCategory item)
                    item.m_content.text = FormatItem(inventory[index]);
            };
            panel.m_itemList.numItems = count;
        }

        private static string FormatItem(PossessionInventoryItem item)
            => $"· {item.Name} × {item.Qty} [{item.Category}]";

        // 刷新下拉框数据
        private void RefreshDropdowns()
        {
            var outerCtx = _model.StateSnapshot?.OuterContext;
            if (outerCtx == null) return;

            // 移动目标
            if (outerCtx.NeighborLocations != null)
            {
                _compt.m_MoveComboBox.items  = outerCtx.NeighborLocations.ToArray();
                _compt.m_MoveComboBox.values = outerCtx.NeighborLocations.ToArray();
                if (_compt.m_MoveComboBox.items.Length > 0)
                    _compt.m_MoveComboBox.selectedIndex = _model.SelectedMoveIndex;
            }

            // 生存（背包中的食物）
            var inventory = _model.Stats?.Inventory;
            if (inventory != null && inventory.Count > 0)
            {
                var foodItems = new List<string>();
                foreach (var item in inventory)
                {
                    if (item.Category == "food")
                        foodItems.Add(item.Name);
                }

                _compt.m_SurviceComboBox.items  = foodItems.ToArray();
                _compt.m_SurviceComboBox.values = foodItems.ToArray();
                _compt.m_SurviceComboBox.selectedIndex = _model.SelectedSurviveIndex;
            }

            // give/trade 物品选择
            var inventoryForSocial = _model.Stats?.Inventory;
            if (inventoryForSocial != null && inventoryForSocial.Count > 0)
            {
                var itemNames = new string[inventoryForSocial.Count];
                for (int i = 0; i < inventoryForSocial.Count; i++)
                    itemNames[i] = inventoryForSocial[i].Name;
                _compt.m_itemComboBox.items  = itemNames;
                _compt.m_itemComboBox.values = itemNames;
                if (_compt.m_itemComboBox.selectedIndex < 0)
                    _compt.m_itemComboBox.selectedIndex = 0;
            }

            // 社交目标
            var allAgents = outerCtx.AllAgents;
            if (allAgents != null && allAgents.Count > 0)
            {
                var names = new string[allAgents.Count];
                for (int i = 0; i < allAgents.Count; i++)
                    names[i] = allAgents[i].Name;

                _compt.m_SocialComboBox.items  = names;
                _compt.m_SocialComboBox.values = names;
                _compt.m_SocialComboBox.selectedIndex = _model.SelectedSocialIndex;

                // 冲突目标与社交目标共用
                _compt.m_ConflictComboBox.items  = names;
                _compt.m_ConflictComboBox.values = names;
                _compt.m_ConflictComboBox.selectedIndex = _model.SelectedConflictIndex;
            }
        }

        private static readonly HashSet<string> DialogueSocialTools = new() { "talk", "whisper", "socialize" };

        private void RefreshDialoguePanel()
        {
            bool inDialogue = _model.Phase == "in_dialogue";
            if (inDialogue)
                _compt.m_history.m_content.text = _model.BuildDialogueText();

            bool showSocialPanel = inDialogue && DialogueSocialTools.Contains(_model.SelectedAction ?? string.Empty);
            _compt.m_socialActionCtrol.selectedIndex = showSocialPanel == true ? 1 : 0;
            SetBtnTouchable(showSocialPanel);
        }

        // 刷新提示语
        private void RefreshTips()
        {
            var tools = _model.StateSnapshot?.OuterContext?.AvailableTools;
            var action = _model.SelectedAction;
            var tool = tools?.Find(t => t.Tool == action);

            if (tool == null)
            {
                _compt.m_tipInfoCom.m_content.text = "";
                return;
            }

            var sb = new System.Text.StringBuilder();

            // 描述
            sb.AppendLine($"描述: {tool.Description}");

            // 消耗
            sb.Append("消耗: ");
            if (tool.Cost != null && tool.Cost.Count > 0)
            {
                bool hasCost = false;
                foreach (var kv in tool.Cost)
                {
                    if (kv.Value == 0) continue;
                    sb.Append($"{kv.Key}: {kv.Value:+0;-0}  ");
                    hasCost = true;
                }
                if (!hasCost) sb.Append("无");
            }
            else
            {
                sb.Append("无");
            }
            sb.AppendLine();

            // 效果
            if (tool.Effects != null && tool.Effects.Count > 0)
            {
                for (int i = 0; i < tool.Effects.Count; i++)
                {
                    var e = tool.Effects[i];
                    string prefix = i == 0 ? "效果: " : "      ";
                    string note = string.IsNullOrEmpty(e.Note) ? "" : $"({e.Note})";
                    sb.AppendLine($"{prefix}[{e.Target}] {e.Stat}: {e.Delta:+0.#;-0.#} {note}");
                }
            }
            else
            {
                sb.AppendLine("效果: 无");
            }

            _compt.m_tipInfoCom.m_content.text = sb.ToString().TrimEnd();
        }
        #endregion

        #region Tab 切换

        private void OnTabMove(EventContext ctx)
        {
            SwitchTab(0);
            SetComboBox(_compt.m_ToolComboBox, MoveTools, MoveToolsValues);
            _compt.m_comCtrol.selectedIndex = 0;
            RefreshTips();
        }
        private void OnTabWork(EventContext ctx)
        {
            SwitchTab(1);
            SetComboBox(_compt.m_ToolComboBox, WorkTools, WorkToolsValues);
            _compt.m_comCtrol.selectedIndex = 0;
            RefreshTips();
        }
        private void OnTabSurvive(EventContext ctx)
        {
            SwitchTab(2);
            SetComboBox(_compt.m_ToolComboBox, SurviveTools, SurviveToolsValues);
            // 默认第一项是 eat，entertain 才需要附加输入，初始隐藏
            _compt.m_comCtrol.selectedIndex = 0;
            RefreshTips();
        }
        private void OnTabSocial(EventContext ctx)
        {
            SwitchTab(3);
            SetComboBox(_compt.m_ToolComboBox, SocialTools, SocialToolsValues);
            // 默认第一项是 talk，give/trade 才需要物品+数量，初始隐藏
            _compt.m_comCtrol.selectedIndex = 1;
            RefreshTips();
        }
        private void OnTabObserve(EventContext ctx)
        {
            SwitchTab(4);
            SetComboBox(_compt.m_ToolComboBox, ObserseTools, ObserseToolsValues);
            _compt.m_comCtrol.selectedIndex = 0;
            RefreshTips();
        }
        private void OnTabConflict(EventContext ctx)
        {
            SwitchTab(5);
            SetComboBox(_compt.m_ToolComboBox, ConflictTools, ConflictToolsValues);
            SetComboBox(_compt.m_attackComboBox, BodyPartItems, BodyPartValues);
            _compt.m_comCtrol.selectedIndex = 0;
            RefreshTips();
        }

        private void SwitchTab(int index)
        {
            _compt.m_tab.selectedIndex = index;
            _model.SelectedTool = TabTools[index];
        }

        #endregion

        #region 下拉框 / 输入框事件

        private void OnToolComboChanged(EventContext ctx)
        {
            _model.SelectedAction = _compt.m_ToolComboBox.value;
            if (_model.SelectedTool == "survive")
            {
                if (_model.SelectedAction == "eat")
                    _compt.m_comCtrol.selectedIndex = 0;
                else if (_model.SelectedAction == "entertain")
                    _compt.m_comCtrol.selectedIndex = 1;
                else
                    _compt.m_comCtrol.selectedIndex = 2;
            }
            else if (_model.SelectedTool == "observe")
            {
                if (_model.SelectedAction == "observe")
                    _compt.m_comCtrol.selectedIndex = 1;
                else
                    _compt.m_comCtrol.selectedIndex = 0;
            }
            else if (_model.SelectedTool == "socialize")
            {
                if (_model.SelectedAction == "talk" || _model.SelectedAction == "whisper" || _model.SelectedAction == "socialize")
                    _compt.m_comCtrol.selectedIndex = 1;
                else if (_model.SelectedAction == "give" || _model.SelectedAction == "trade")
                    _compt.m_comCtrol.selectedIndex = 0;
                else
                    _compt.m_comCtrol.selectedIndex = 2;
            }
            else if (_model.SelectedTool == "conflict")
            {
                if (_model.SelectedAction == "insult" || _model.SelectedAction == "get")
                    _compt.m_comCtrol.selectedIndex = 1;
                else if (_model.SelectedAction == "attack")
                    _compt.m_comCtrol.selectedIndex = 0;
                else
                    _compt.m_comCtrol.selectedIndex = 2;
            }
        }

        private void OnMoveComboChanged(EventContext ctx)
        {
            _model.SelectedMoveIndex = _compt.m_MoveComboBox.selectedIndex;
        }

        private void OnSurviveComboChanged(EventContext ctx)
        {
            _model.SelectedSurviveIndex = _compt.m_SurviceComboBox.selectedIndex;
            // 输入组件切换
            if (_compt.m_SurviceComboBox.value == "entertain")
                _compt.m_comCtrol.selectedIndex = 1;
            else
                _compt.m_comCtrol.selectedIndex = 0;
        }

        private void OnSocialComboChanged(EventContext ctx)
        {
            _model.SelectedSocialIndex = _compt.m_SocialComboBox.selectedIndex;
            var agents = _model.StateSnapshot?.OuterContext?.AllAgents;
            if (agents != null && _model.SelectedSocialIndex < agents.Count)
                _model.SelectedTarget = agents[_model.SelectedSocialIndex];
            
            // 输入组件切换
            string curValue = _compt.m_SocialComboBox.value;
            if (curValue == "give" || curValue == "trade")
                _compt.m_comCtrol.selectedIndex = 0;
            else
                _compt.m_comCtrol.selectedIndex = 1;
        }

        private void OnConflictComboChanged(EventContext ctx)
        {
            _model.SelectedConflictIndex = _compt.m_ConflictComboBox.selectedIndex;
            var agents = _model.StateSnapshot?.OuterContext?.AllAgents;
            if (agents != null && _model.SelectedConflictIndex < agents.Count)
                _model.SelectedTarget = agents[_model.SelectedConflictIndex];
        }



        private void OnItemComboChanged(EventContext context) => _model.SelectedSocialItemIndex = _compt.m_itemComboBox.selectedIndex;
        private void OnAttackComboChanged(EventContext ctx) => _model.SelectedBodyPartIndex = _compt.m_attackComboBox.selectedIndex;


        private void OnFarmInputChanged(EventContext ctx)       => _model.FarmInputText       = _compt.m_farmInput.text;
        private void OnSocialInputChanged(EventContext ctx)    => _model.SocialInputText    = _compt.m_socialInput.text;
        private void OnObserveInputChanged(EventContext ctx)   => _model.ObserveInputText   = _compt.m_ovserveInput.text;
        private void OnSurviceInputChanged(EventContext ctx)   => _model.EntertainText      = _compt.m_surviceInput.text;
        private void OnSocialInput2Changed(EventContext ctx)   => _model.ItemQtyText        = _compt.m_socialInput2.text;
        private void OnConflictInputChanged(EventContext ctx)  => _model.ConflictText       = _compt.m_cinflictInput.text;

        #endregion

        #region 执行按钮

        // 挂机1h
        private void OnBtnIdleEvent(EventContext context)
        {
            if (_model.IsPending) return;
            PossessionActRequest request = new PossessionActRequest{ Idle = true};
            SetPending(true);
            RestApiHelper.Instance.PostPossessionAct(
                request,
                onSuccess: response =>
                {
                    SetPending(false);
                    if (response == null) return;

                    LogManager.Instance.Info($"[Interact] 执行Idle");
                    RefreshPossessionState();
                },
                onError: err =>
                {
                    SetPending(false);
                    LogManager.Instance.Error($"[Interact] PostPossessionAct 失败: {err}");
                    ShowTipView("动作执行失败");
                }
            );
        }

        private void OnExec(EventContext ctx)
        {
            if (_model.IsPending) return;

            if (_model.Phase == "in_dialogue")
                SubmitDialogueAction();
            else
                SubmitOuterAction();
        }

        // ── 外层动作（waiting_outer） ──
        private void SubmitOuterAction()
        {
            if (_model.IsPending) return;

            var tabIdx = _compt.m_tab.selectedIndex;
            var request = BuildActRequest(tabIdx);
            if (request == null) return;

            SetPending(true);
            RestApiHelper.Instance.PostPossessionAct(
                request,
                onSuccess: response =>
                {
                    SetPending(false);
                    if (response == null) return;
                    LogManager.Instance.Info($"[Interact] Act 结果: tool={response.Tool} entered_dialogue={response.EnteredDialogue} message={response.Message}");

                    // 外层动作为 social 下的 talk/whisper/socialize 会进入对话子动作流程，
                    // 此时还未结算，不打 ShortTipView；待对话结束时由子动作回调/轮询统一弹出对话总结。
                    if (response.EnteredDialogue && response.Dialogue != null)
                    {
                        _model.StateSnapshot.Dialogue = response.Dialogue;
                        RefreshPossessionState();
                        return;
                    }

                    // 即时动作：act 响应已带 settlement → 直接消费（seq 去重）
                    if (response.Settlement != null)
                    {
                        ConsumeSettlement(response.Settlement);
                        RefreshPossessionState();
                    }
                    else
                    {
                        // pending（仲裁>10s）或无 settlement：交给 /state 轮询兜底消费
                        RefreshPossessionState();
                    }
                },
                onError: err =>
                {
                    SetPending(false);
                    LogManager.Instance.Error($"[Interact] PostPossessionAct 失败: {err}");
                    ShowTipView("动作执行失败");
                }
            );
        }

        private PossessionActRequest BuildActRequest(int tabIdx)
        {
            var action = _model.SelectedAction ?? string.Empty;
            var req    = new PossessionActRequest { Tool = action };

            switch (tabIdx)
            {
                case 0: // 移动(move/flee)：目的地来自地点下拉
                    req.Location = _compt.m_MoveComboBox.value;
                    break;

                case 1: // 劳作(farm/work/build/craft/gather)：附加文本描述
                    req.ActionInput = _model.FarmInputText;
                    break;

                case 2: // 生存(eat/sleep/rest/entertain)：eat 需要选物品，其余无附加字段
                    if (action == "eat")
                        req.Item = _compt.m_SurviceComboBox.value;
                    else if (action == "entertain")
                        req.ActionInput = _compt.m_surviceInput.text;
                    break;

                case 3: // 社交(talk/whisper/socialize/give/trade/read_notice)
                    if (action != "read_notice")
                    {
                        if (_model.SelectedTarget == null)
                            SetDefaultSelectedTarget();
                        req.TargetUid  = _model.SelectedTarget?.Uid;
                        req.TargetName = _model.SelectedTarget?.Name;
                    }
                    if (action == "give" || action == "trade")
                    {
                        req.Item = _compt.m_itemComboBox.value;
                        var countStr = string.IsNullOrEmpty(_compt.m_socialInput2.text) ? "0" : _compt.m_socialInput2.text;
                        req.Qty = int.Parse(countStr);
                    }
                    req.Content = _model.SocialInputText;
                    break;

                case 4: // 观察(observe/listen/search)：文本描述
                    if (action != "observe")
                        req.ActionInput = _model.ObserveInputText;
                    break;

                case 5: // 冲突(attack/insult/get)：均需目标人
                    if (_model.SelectedTarget == null)
                            SetDefaultSelectedTarget();
                    req.TargetUid  = _model.SelectedTarget?.Uid;
                    req.TargetName = _model.SelectedTarget?.Name;
                    if (action == "attack")
                    {
                        if (_model.SelectedBodyPartIndex != 0)  // 随机部位不传值
                            req.ActionInput = BodyPartValues[_model.SelectedBodyPartIndex];
                    }
                    else if (action == "insult" || action == "get")
                        req.ActionInput = _model.ConflictText;
                    break;

                default:
                    return null;
            }
            return req;
        }

        // ── 对话内子动作（in_dialogue） ──
        private void SubmitDialogueAction()
        {
            // 默认用 speak + 社交输入框内容
            var request = new PossessionInteractRequest
            {
                Sub         = _model.SelectedAction,
                ActionInput = _model.SocialInputText,
            };

            SetPending(true);
            RestApiHelper.Instance.PostPossessionInteract(
                request,
                onSuccess: response =>
                {
                    SetPending(false);
                    LogManager.Instance.Info($"[Interact] Interact 结果: sub={response.Sub} accepted={response.Accepted}");
                    RefreshPossessionState();
                },
                onError: err =>
                {
                    SetPending(false);
                    LogManager.Instance.Error($"[Interact] PostPossessionInteract 失败: {err}");
                    ShowTipView("对话内子动作执行失败");
                }
            );
        }

        #endregion


        #region 社交子动作面板

        private void OnSubActionClicked(EventContext ctx)
        {
            var btn = ctx.data as GObject;
            if (btn == null) return;

            string sub = btn.name switch
            {
                "talkBtn"    => "talk",
                "whisperBtn" => "whisper",
                "scoldBtn"   => "insult",
                "attackBtn"  => "attack",
                "giftBtn"    => "give",
                "tradeBtn"   => "trade",
                "observeBtn" => "observe",
                "searchBtn"  => "search",
                "demandBtn"  => "get",
                "agreeBtn"   => "allow",
                "eatBtn"     => "eat",
                "fleeBtn"    => "flee",
                "restBtn"    => "rest",
                _            => "speak",
            };

            _model.SelectedSocialAction = sub;
            _compt.m_socialActionPanel.m_childActionTip.text = GetSubActionTip(sub);
        }

        // 子动作提示文本
        private string GetSubActionTip(string sub) => sub switch
        {
            "talk"    => "格式: 文本\n示例: \"周大哥，最近收成怎么样？\"",
            "whisper" => "格式: 文本\n示例: \"这事别让人知道\"",
            "insult"  => "格式: 文本\n示例: \"你这个老扣门\"",
            "attack"  => "格式: \"<部位> [叙述]\"，部位∈头/胸/左臂/右臂/左腿/右腿\n示例: \"头 一拳打过去\"",
            "give"    => "格式: \"<数量> <物品名>\"\n示例: \"2 粮食\"",
            "trade"   => "格式: \"<物品名> <数量> <提议>\"\n示例: \"粮食 2 你给我5个钱\"",
            "get"     => "格式: 想要的物品文本\n示例: \"粮食 1\"",
            "search"  => "格式: 无参",
            "observe" => "格式: 无参",
            "allow"   => "格式: 无参",
            "eat"     => "格式: 食物名\n示例: \"粮食\"",
            "flee"    => "格式: 无参（结束对话并离开）",
            "rest"    => "格式: 无参",
            "done"    => "格式: 无参（主动结束对话）",
            _         => "",
        };

        // 主动结束对话
        private void OnTalkOverClicked(EventContext ctx)
        {
            if (_model.IsPending) return;

            var request = new PossessionInteractRequest { Sub = "done", };
            SetPending(true);
            AIVillageClient.Instance.PostPossessionInteract(
                request,
                onSuccess: response =>
                {
                    SetPending(false);
                    LogManager.Instance.Info($"[Interact] SubAction={response.Sub} accepted={response.Accepted}");
                    // 立即关闭对话面板并提示
                    _manualDialogueClose = true;
                    _compt.m_socialActionCtrol.selectedIndex = 0;
                    _compt.m_dialogPop.selectedIndex = 0;
                    //ShowTipView("已关闭对话");
                    // 立即拉一次 /state，消费 last_settlement 里的对话总结（ConsumeSettlement 按 seq 去重）
                    RefreshPossessionState();
                },
                onError: err =>
                {
                    SetPending(false);
                    LogManager.Instance.Error($"[Interact] PostPossessionInteract 失败: {err}");
                }
            );
        }

        // 执行社交子动作
        private void OnExcuteTalklicked(EventContext context)
        {
            if (_model.IsPending) return;
            if (string.IsNullOrEmpty(_model.SocialInputText))
                _model.SocialInputText = _compt.m_socialInput.text;
            
            var request = new PossessionInteractRequest
            {
                Sub         = _model.SelectedSocialAction,
                ActionInput = _model.SocialInputText,
            };
            SetPending(true);
            AIVillageClient.Instance.PostPossessionInteract(
                request,
                onSuccess: response =>
                {
                    SetPending(false);
                    LogManager.Instance.Info($"[Interact] SubAction={response.Sub} accepted={response.Accepted}");
                    RefreshPossessionState();
                },
                onError: err =>
                {
                    SetPending(false);
                    LogManager.Instance.Error($"[Interact] PostPossessionInteract 失败: {err}");
                }
            );
        }

        #endregion

        #region 折叠按钮

        private void OnBtnPopEvent(EventContext ctx)
        {
            // 侧边三角按钮：收起/展开整个面板
            _compt.m_popCtrol.selectedIndex = _compt.m_popCtrol.selectedIndex == 0 ? 1 : 0;
            float screenWidth = GRoot.inst.width;                                                                                                                                                                             
            float screenHeight = GRoot.inst.height;
            if (_compt.m_popCtrol.selectedIndex == 0)
            {
                float toX = _view.GetPosition(BaseView.ScreenCorner.BottomLeft, -_compt.width, 10f).x;
                _compt.DOMoveX( toX, 1.5f).SetEase(Ease.InOutQuad);
            }
            else
            {
                float toX = _view.GetPosition(BaseView.ScreenCorner.BottomLeft, 5, 10f).x;
                _compt.DOMoveX(toX, 1.5f).SetEase(Ease.InOutQuad);
            }
        }

        private void OnDialogPopBtnEvent(EventContext ctx)
        {
            int cur = _compt.m_dialogPop.selectedIndex;
            _compt.m_dialogPop.selectedIndex = cur == 0 ? 1 : 0;
        }

        
        // 背包折叠
        private void OnBagCaollapseClicked(EventContext context)
        {
            _compt.m_statusPanel.m_bagCtrol.selectedIndex = _compt.m_statusPanel.m_bagCtrol.selectedIndex == 0 ? 1 : 0;
        }

        #endregion

        #region 工具方法

        // socialPanelOpen=true 时置灰禁用执行/挂机按钮，false 时恢复
        private void SetBtnTouchable(bool socialPanelOpen)
        {
            _compt.m_BtnExec.touchable = !socialPanelOpen;
            _compt.m_BtnExec.grayed    = socialPanelOpen;
            _compt.m_BtnExec.m_maskCtrol.selectedIndex = socialPanelOpen ? 1 : 0;
            _compt.m_BtnIdle.touchable = !socialPanelOpen;
            _compt.m_BtnIdle.grayed    = socialPanelOpen;
            _compt.m_BtnIdle.m_maskCtrol.selectedIndex = socialPanelOpen ? 1 : 0;
        }
        
        private void SetPending(bool pending)
        {
            if (_model == null) return;
            _model.IsPending         = pending;
            _compt.m_BtnExec.enabled = !pending;
            _compt.m_mask.visible    = pending;
            _compt.m_mask.touchable  = pending;
        }

        private static void SetTextFormat(GTextField field)
        {
            if (field == null) return;
            var textFmt = field.textFormat;
            textFmt.size  = 14;
            textFmt.color = new Color(0.4f, 0.4f, 0.4f);
            field.text = "";
        }

        private void ShowTipView(string text)
        {
            TipModel tipModel = new TipModel{ TipContent = text};
            UIManager.Instance.OpenViewSync<TipView>(tipModel);
        }

        // 提示
        private void ShowShortTipView(string text)
        {
            var shortTipModel = new ShortTipModel(){ Content = text};
            UIManager.Instance.OpenViewSync<ShortTipView>(shortTipModel);
        }

        /// <summary>
        /// 统一消费一条结算记录（seq 去重）：新记录才弹窗，旧记录跳过。
        /// 即时动作 → observation + delta_text；对话类 → interact_meta 三件套总结。
        /// 所有路径（外层即时动作 / 社交子动作结束 / state 轮询兜底）都汇流到此处。
        /// </summary>
        private void ConsumeSettlement(PossessionSettlement rec)
        {
            if (rec == null) return;
            if (_model == null) return;

            // seq 去重：仅当序号严格大于已处理游标才弹
            if (rec.Seq <= _model.LastShownSeq)
                return;

            _model.LastShownSeq = rec.Seq;

            // 挂机占位记录不弹窗（保持原有静默行为），仅推进游标标记已处理
            if (rec.Idle) return;

            string text = BuildSettlementTipText(rec);
            if (!string.IsNullOrEmpty(text))
                ShowShortTipView(text);
        }

        /// <summary>
        /// 将一条结算记录格式化为弹窗文本。
        /// 对话类记录（interact_meta 非空）展示"我方视角 / 对方视角 / 关键成果"；
        /// 普通即时动作展示 observation + delta_text。
        /// </summary>
        private string BuildSettlementTipText(PossessionSettlement rec)
        {
            var meta = rec.InteractMeta;
            if (meta != null)
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"{rec.ToolName}: ");
                sb.AppendLine($"我方视角：{meta.SummaryInitiator}");
                sb.AppendLine($"关键成果：{meta.KeyOutcomes}");
                /*if (meta.EndReason == "idle_timeout")
                    sb.AppendLine("（因超时自动结束）");
                else if (!string.IsNullOrEmpty(meta.EndReason) && meta.EndReason != "ok" && meta.EndReason != "player_done")
                    sb.AppendLine($"（结束原因：{meta.EndReason}）");*/
                return sb.ToString().TrimEnd();
            }

            // 即时动作
            var sb2 = new System.Text.StringBuilder();
            sb2.AppendLine($"{rec.ToolName}: ");
            if (!string.IsNullOrEmpty(rec.Observation))
                sb2.Append(rec.Observation);
            if (!string.IsNullOrEmpty(rec.DeltaText))
                sb2.Append($"\n{rec.DeltaText}");
            return sb2.ToString().TrimEnd();
        }
        
        // 设置默认被选择的目标Agent
        private void SetDefaultSelectedTarget()
        {
            if (_model.SelectedTarget == null)
            {
                var agents = _model.StateSnapshot?.OuterContext?.AllAgents;
                if (agents != null && _model.SelectedSocialIndex < agents.Count)
                    _model.SelectedTarget = agents[_model.SelectedSocialIndex];
            }
        }
        #endregion
    }
}
