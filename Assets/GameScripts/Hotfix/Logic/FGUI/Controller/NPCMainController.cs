using System;
using System.Collections.Generic;
using DG.Tweening;
using FairyGUI;
using Hotfix.FuncModule;
using Hotfix.FuncModule.AITown;
using Main.FuncModule.Camera2D;
using Hotfix.Main.Leiya;
using ZEngine.Manager.UI;
using Hotfix.UI.Generate.Main;
using UnityEngine;
using ZEngine.Manager.Log;
using Newtonsoft.Json;
using ZEngine.Extension;

namespace Hotfix.Main.UI
{
    public class NPCMainController : BaseController
    {
        private UINPCMainView compt;
        private NPCMainModel model;

        public override void Initialize()
        {
            base.Initialize();
            compt = _view.GetView() as UINPCMainView;
            model = _view.Data as NPCMainModel;

            if (compt != null)
            {
                SetTextFormat(compt.m_currentActionContent.m_content);
                SetTextFormat(compt.m_npcPlanContent.m_content);
                SetTextFormat(compt.m_basicContent.m_content);
                SetTextFormat(compt.m_abilityContent.m_content);
                SetTextFormat(compt.m_emotionContent.m_content);
                SetTextFormat(compt.m_prospectContent.m_content);
                SetTextFormat(compt.m_assetsContent.m_content);
                SetTextFormat(compt.m_socialContent.m_content);
                SetTextFormat(compt.m_tagsContent.m_content);
                compt.m_basicBtn.onClick.Add(e => SwitchTab("basic"));
                compt.m_abilityBtn.onClick.Add(e => SwitchTab("abilities"));
                compt.m_emotionBtn.onClick.Add(e => SwitchTab("mood"));
                compt.m_prospectBtn.onClick.Add(e => SwitchTab("needs"));
                compt.m_assetsBtn.onClick.Add(e => SwitchTab("assets"));
                compt.m_socialBtn.onClick.Add(e => SwitchTab("social"));
                compt.m_tagsBtn.onClick.Add(e => SwitchTab("tags"));
                compt.m_btnPop.onClick.Add(BtnPopEvent);
                compt.m_popCtrol.selectedIndex = 0;
                if (compt.m_popCtrol.selectedIndex == 0)
                {
                    _view.SetToCorner(BaseView.ScreenCorner.BottomRight, -compt.width, 10);
                }
                compt.m_NPCCombox.onChanged.Add(OnNPCComboxEvent);
                compt.m_btnFollow.onClick.Add(OnBtnFollowEvent);


                string initContent = "暂无内容";
                compt.m_currentActionContent.m_content.text = initContent;
                compt.m_npcPlanContent.m_content.text = initContent;
                compt.m_basicContent.m_content.text = initContent;
                compt.m_abilityContent.m_content.text = initContent;
                compt.m_emotionContent.m_content.text = initContent;
                compt.m_prospectContent.m_content.text = initContent;
                compt.m_assetsContent.m_content.text = initContent;
                compt.m_socialContent.m_content.text = initContent;
                compt.m_tagsContent.m_content.text = initContent;
            }

            _view.OnChanged += UpdateMainPanel;
            UpdateMainPanel(model);
            RefreshUI();
            WebSocketMgr.Instance.OnNPCsRegistered += OnNPCRegistered;
        }

        public override void OnRelease()
        {
            WebSocketMgr.Instance.OnNPCsRegistered -= OnNPCRegistered;
            WebSocketMgr.Instance.OnNPCStatusResponse -= OnNPCStatusResponse;

            if (compt != null)
            {
                compt.m_basicBtn.onClick.Clear();
                compt.m_abilityBtn.onClick.Clear();
                compt.m_emotionBtn.onClick.Clear();
                compt.m_prospectBtn.onClick.Clear();
                compt.m_assetsBtn.onClick.Clear();
                compt.m_socialBtn.onClick.Clear();
                compt.m_tagsBtn.onClick.Clear();
                compt.m_btnPop.onClick.Clear();
                compt.m_NPCCombox.onChanged.Remove(OnNPCComboxEvent);
                compt.m_btnFollow.onClick.Remove(OnBtnFollowEvent);
                compt.DOKill();
                compt = null;
            }

            _view.OnChanged = null;
            base.OnRelease();
        }
        
        private void BtnPopEvent()
        {
            compt.m_popCtrol.selectedIndex = compt.m_popCtrol.selectedIndex == 0 ? 1 : 0;
            float screenWidth = GRoot.inst.width;                                                                                                                                                                             
            float screenHeight = GRoot.inst.height;
            if (compt.m_popCtrol.selectedIndex == 0)
            {
                float toX = _view.GetPosition(BaseView.ScreenCorner.BottomRight, -compt.width, 10f).x;
                compt.DOMoveX( toX, 1.5f).SetEase(Ease.InOutQuad);
            }
            else
            {
                float toX = _view.GetPosition(BaseView.ScreenCorner.BottomRight, 5, 10f).x;
                compt.DOMoveX(toX, 1.5f).SetEase(Ease.InOutQuad);
            }
        }

        private void OnNPCComboxEvent(EventContext ctx)
        {
            if (model == null) return;
            var agent = AITownManager.Instance.GetAgent(compt.m_NPCCombox.value);
            if (agent != null)
                model.Info = agent.State.Info;
            UpdateMainPanel(model);
        }

        private void OnBtnFollowEvent(EventContext context)
        {
            if (compt == null || model == null || model.Info == null) return;

            var camera = Camera.main;
            if (camera == null) return;
            var cameraControl = camera.GetComponent<Camera2DController>();
            if (cameraControl == null) return;

            bool follow = compt.m_btnFollow.selected;

            if (follow)
            {
                var agent = AITownManager.Instance.GetAgent(model.Info.AgentId);
                if (agent != null)
                {
                    Vector2 targetPos = new(agent.transform.position.x, agent.transform.position.y);
                    float followZoom = 5f;
                    cameraControl.MoveTo(targetPos, followZoom);
                    cameraControl.SetFollowTarget(agent.transform);
                }
                else
                {
                    LogManager.Instance.Warning("NPCMain: 跟随失败，未找到对应 Agent，回退到上帝视角");
                    compt.m_btnFollow.selected = false;
                    cameraControl.SetMode(Camera2DMode.GodView);
                }
            }
            else
            {
                float godViewZoom = 12f;
                Vector2 currentPos = new(camera.transform.position.x, camera.transform.position.y);
                cameraControl.MoveTo(currentPos, godViewZoom);
                cameraControl.SetMode(Camera2DMode.GodView);
            }
        }

        private void OnNPCRegistered(NPCsRegisteredResponse response)
        {
            RefreshUI();
        }

        #region 数据请求

        private void RequestNPCStatus()
        {
            if (model?.Info == null) return;

            var name = model.Info.AgentName;
            var id   = model.Info.AgentId;
            if (string.IsNullOrEmpty(name) && string.IsNullOrEmpty(id)) return;

            WebSocketMgr.Instance.OnNPCStatusResponse += OnNPCStatusResponse;
            AIVillageClient.Instance.QueryNPCStatus(name, id);
        }

        private void OnNPCStatusResponse(NPCStatusResponse response)
        {
            WebSocketMgr.Instance.OnNPCStatusResponse -= OnNPCStatusResponse;

            if (response == null || !response.Success || response.Data == null)
            {
                Debug.LogWarning($"[NPCMain] 查询失败: {response?.Message}");
                RefreshUI();
                return;
            }
 
            model.DetailData = response.Data;
            RefreshUI();
        }

        private void RequestInteractins()
        {
            InteractRequest request = new InteractRequest
            {
                Uid = model.Info.AgentId,
                Limit = 0,
            };
            AIVillageClient.Instance.GetInteractions(request, (response) =>
            {
                LogManager.Instance.Info($"Interact: {JsonConvert.SerializeObject(response)}");
                model.InteractData = response;
                RefreshUI();
            });
        }

        #endregion

        
        // 标签页切换
        private void SwitchTab(string tab)
        {
            if (model == null) return;
            model.CurrentTab = tab;
            RefreshTabContent();
            UpdateTabButtons();
        }

        // UI 刷新
        private void UpdateMainPanel(BaseModel data)
        {
            model = _view.Data as NPCMainModel;
            if (model?.Info == null || compt == null) return;
            RequestNPCStatus();
            RequestInteractins();
        }

        private void RefreshUI()
        {
            if (compt == null || model == null) return;

            string npcName = model.Info?.AgentName ?? "";

            // 标题
            compt.m_attributeTitle.text  = npcName + " - 属性";
            compt.m_npcPlanTitle.text    = npcName + " - 计划";
            compt.m_bodyTitle.text       = npcName + " - 身体";
            compt.m_currentActionTitle.text = "动作";

            // 各面板内容
            RefreshTabContent();
            compt.m_npcPlanContent.m_content.text = model.GetScheduleContent();
            compt.m_currentActionContent.m_content.text = model.GetCurrentActionContent();
            compt.m_bodyContent.text          = model.GetBodyContent();

            UpdateTabButtons();
            
            // 刷新NPCCombox
            var agents = AITownManager.Instance.GetAllAgentsWithoutVillageChief();
            List<string> names = new List<string>();
            List<string> ids = new List<string>();
            LogManager.Instance.Info($"{agents.Count}");

            for (int i = 0; i < agents.Count; i++)
            {
                if (agents[i] == null)
                   LogManager.Instance.Info($"agentIndex: {i}");
                names.Add(agents[i].State.AgentName);
                ids.Add(agents[i].State.AgentId);
            }
            compt.m_NPCCombox.items = names.ToArray();
            compt.m_NPCCombox.values = ids.ToArray();
            if (model != null && model.Info != null)
            {
                int selectedIndex = names.FindIndex(x => x == model.Info.AgentName);
                compt.m_NPCCombox.selectedIndex = selectedIndex;
            }
        }

        private void RefreshTabContent()
        {
            if (compt == null || model == null) return;

            // 每个 GTextField 对应一个标签页，只显示当前选中的
            compt.m_basicContent.m_content.text    = model.GetTabContent("basic");
            compt.m_abilityContent.m_content.text  = model.GetTabContent("abilities");
            compt.m_emotionContent.m_content.text  = model.GetTabContent("mood");
            compt.m_prospectContent.m_content.text = model.GetTabContent("needs");
            compt.m_assetsContent.m_content.text   = model.GetTabContent("assets");
            compt.m_socialContent.m_content.text   = model.GetTabContent("social");
            compt.m_tagsContent.m_content.text     = model.GetTabContent("tags");

            // 通过 Controller 控制显示哪个标签页
            if (compt.m_tagCtrol != null)
            {
                compt.m_tagCtrol.selectedIndex = TabToIndex(model.CurrentTab);
            }
        }

        private void UpdateTabButtons()
        {
            if (compt == null || model == null) return;
            string t = model.CurrentTab;
            compt.m_basicBtn.selected    = t == "basic";
            compt.m_abilityBtn.selected  = t == "abilities";
            compt.m_emotionBtn.selected  = t == "mood";
            compt.m_prospectBtn.selected = t == "needs";
            compt.m_assetsBtn.selected   = t == "assets";
            compt.m_socialBtn.selected   = t == "social";
            compt.m_tagsBtn.selected     = t == "tags";
        }

        private static int TabToIndex(string tab) => tab switch
        {
            "basic"     => 0,
            "abilities" => 1,
            "mood"      => 2,
            "needs"     => 3,
            "assets"    => 4,
            "social"    => 5,
            "tags"      => 6,
            _           => 0,
        };

        private static void SetTextFormat(GTextField field)
        {
            if (field == null) return;
            var textFmt = field.textFormat;
            textFmt.size  = 14;
            textFmt.color = new UnityEngine.Color(0.8f, 0.8f, 0.8f);
        }
    }
}
