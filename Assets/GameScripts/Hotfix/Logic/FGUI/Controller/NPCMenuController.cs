using FairyGUI;
using Hotfix.FuncModule;
using Hotfix.FuncModule.AITown;
using Hotfix.Main.Leiya;
using ZEngine.Manager.UI;
using Hotfix.UI.Generate.Main;
using UnityEngine;
using ZEngine.Reference;

namespace Hotfix.Main.UI
{
    public class NPCMenuController : BaseController
    {
        private UINPCMenuView compt;
        private NPCMenuModel model;
        private string infoType;
        
        public override void Initialize()
        {
            base.Initialize();
            compt = _view.GetView() as UINPCMenuView;
            model = _view.Data as NPCMenuModel;

            if (compt != null)
            {
                compt.m_BtnAttribute.onClick.Add(OnBtnAttributeBtnEvent);
                compt.m_BtnSchedule.onClick.Add(OnScheduleBtnEvent);
                compt.m_BtnBackground.onClick.Add(OnBackgroundBtnEvent);
            }

            _view.OnChanged = (data) =>
            {
                // 数据变更时的处理
            };
        }



        /// <summary>
        /// 属性按钮点击事件（对应Godot的_on_attributes_pressed）
        /// </summary>
        /// <param name="context"></param>
        private void OnBtnAttributeBtnEvent(EventContext context)
        {
            ShowInfoPanel("attributes");
        }

        /// <summary>
        /// 日程按钮点击事件
        /// </summary>
        /// <param name="context"></param>
        private void OnScheduleBtnEvent(EventContext context)
        {
            ShowInfoPanel("schedule");
        }

        /// <summary>
        /// 背景按钮点击事件
        /// </summary>
        /// <param name="context"></param>
        private void OnBackgroundBtnEvent(EventContext context)
        {
            ShowInfoPanel("background");
        }

        /// <summary>
        /// 显示信息面板
        /// </summary>
        /// <param name="infoType">信息类型：attributes, schedule, background</param>
        private void ShowInfoPanel(string infoType)
        {
            if (model?.AgentInfo == null) return;
            this.infoType = infoType;

            var agentName = model.AgentInfo.AgentName;
            var agentId   = model.AgentInfo.AgentId;
            if (string.IsNullOrEmpty(agentName) && string.IsNullOrEmpty(agentId)) return;
            Debug.Log($"{model.AgentInfo.AgentName}-{model.AgentInfo.AgentId}");

            WebSocketMgr.Instance.OnNPCStatusResponse += OnNPCStatusResponse;
            AIVillageClient.Instance.QueryNPCStatus(agentName, agentId);
        }

        private void OnNPCStatusResponse(NPCStatusResponse response)
        {
            WebSocketMgr.Instance.OnNPCStatusResponse -= OnNPCStatusResponse;

            if (response == null || !response.Success || response.Data == null)
            {
                Debug.LogWarning($"[NPCMenu] NPC状态查询失败: {response?.Message}");
                return;
            }

            if (model == null) return;

            var infoModel = ReferencePool.Spawn<NPCInfoModel>();
            infoModel.DetailData = response.Data;
            infoModel.InfoType   = infoType;
            infoModel.Info       = model.AgentInfo;
            HideMenu();
            UIManager.Instance.OpenViewSync<NPCInfoView>(infoModel);
        }
        

        /// <summary>
        /// 隐藏菜单
        /// </summary>
        private void HideMenu()
        {
            UIManager.Instance.CloseView<NPCMenuView>();
        }

        public override void OnRelease()
        {
            if (compt != null)
            {
                compt.m_BtnAttribute.onClick.Remove(OnBtnAttributeBtnEvent);
                compt.m_BtnSchedule.onClick.Remove(OnScheduleBtnEvent);
                compt.m_BtnBackground.onClick.Remove(OnBackgroundBtnEvent);
                compt = null;
            }
            base.OnRelease();
        }
    }
}