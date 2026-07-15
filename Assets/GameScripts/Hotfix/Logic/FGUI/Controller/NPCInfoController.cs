using FairyGUI;
using Hotfix.FuncModule.AITown;
using Hotfix.UI.Generate.Main;
using UnityEngine;
using ZEngine.Manager.UI;

namespace Hotfix.Main.UI
{
    public class NPCInfoController : BaseController
    {
        private UINPCInfoView compt;
        private NPCInfoModel model;

        public override void Initialize()
        {
            base.Initialize();
            compt = _view.GetView() as UINPCInfoView;
            model = _view.Data as NPCInfoModel;

            if (compt != null)
            {
                compt.m_BtnClose.onClick.Add(OnCloseBtnPressed);

                // 连接背景遮罩点击事件（点击空白处关闭）
                if (compt.m_mask != null)
                    compt.m_mask.onClick.Add(OnMaskClicked);
            }

            _view.OnChanged = (data) =>
            {
                UpdateData();
            };

            // 初始化时更新数据显示
            UpdateData();
        }

        /// <summary>
        /// 显示NPC信息
        /// </summary>
        /// <param name="agentInfo">NPC数据</param>
        /// <param name="infoType">信息类型：attributes, schedule, background</param>
        public void ShowNPCInfo(AgentInfo agentInfo, string infoType)
        {
            if (model != null)
            {
                model.Info = agentInfo;
                model.InfoType = infoType;
            }
            _view.OnChanged?.Invoke(model);
        }

        /// <summary>
        /// 更新UI显示数据
        /// </summary>
        private void UpdateData()
        {
            if (compt == null || model == null)
                return;

            Debug.Log("🔄 更新NPC信息显示");

            // 更新标题
            string title = model.GetCurrentTitle();
            compt.m_Title.text = title;

            // 更新内容
            string content = model.GetCurrentContent();
            compt.m_ScrollText.m_content.text = content;
        }

        /// <summary>
        /// 关闭按钮点击事件
        /// </summary>
        /// <param name="context"></param>
        private void OnCloseBtnPressed(EventContext context)
        {
            UIManager.Instance.CloseView<NPCInfoView>();
        }

        /// <summary>
        /// 背景遮罩点击事件（点击空白处关闭）
        /// </summary>
        /// <param name="context"></param>
        private void OnMaskClicked(EventContext context)
        {
            UIManager.Instance.CloseView<NPCInfoView>();
        }

        /// <summary>
        /// 设置信息类型并刷新显示
        /// </summary>
        /// <param name="infoType">信息类型</param>
        public void SetInfoType(string infoType)
        {
            if (model != null)
            {
                model.InfoType = infoType;
                _view.OnChanged?.Invoke(model);
            }
        }

        public override void OnRelease()
        {
            if (compt != null)
            {
                // 移除事件监听
                compt.m_BtnClose.onClick.Remove(OnCloseBtnPressed);
                if (compt.m_mask != null)
                    compt.m_mask.onClick.Remove(OnMaskClicked);
                compt = null;
            }

            base.OnRelease();
        }
    }
}