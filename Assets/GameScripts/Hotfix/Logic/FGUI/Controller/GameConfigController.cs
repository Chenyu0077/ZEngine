using System;
using System.Diagnostics;
using FairyGUI;
using Hotfix.FuncModule;
using Hotfix.Main.Leiya;
using Hotfix.UI.Generate.Main;
using ZEngine.Manager.Log;
using ZEngine.Manager.UI;

namespace Hotfix.Main.UI
{
    /// <summary>
    /// 游戏配置面板控制器。
    /// 对应 UIGameConfigView 字段：
    ///   m_BtnClose     — 关闭按钮
    ///   m_commonModel  — 通用模型下拉（GComboBox）
    ///   m_reflectModel — 反思模型下拉（GComboBox）
    ///   m_btnMenual    — 手动模式按钮（互斥）
    ///   m_btnLLM       — LLM 模式按钮（互斥）
    ///   m_btnVillageLog — 打开村志按钮
    /// </summary>
    public class GameConfigController : BaseController
    {
        #region 字段

        private UIGameConfigView _compt;
        private GameConfigModel  _model;
        private string _customBaseUrl;
        private string _customKey;

        #endregion

        #region 生命周期

        public override void Initialize()
        {
            base.Initialize();
            _compt = _view.GetView() as UIGameConfigView;
            _model = _view.Data as GameConfigModel;
            if (_compt == null) return;

            PopulateDropdowns();
            RefreshFromModel();
            BindEvents();
        }

        public override void OnRelease()
        {
            if (_compt == null) return;

            _compt.m_BtnClose.onClick.Remove(OnClose);
            _compt.m_commonModel.onChanged.Remove(OnCommonModelChanged);
            _compt.m_reflectModel.onChanged.Remove(OnReflectModelChanged);
            _compt.m_registerNPCsModel.onChanged.Remove(OnRegisterNPCsModelChanged);
            _compt.m_presetMode.onChanged.Remove(OnPresetModeChanged);
            _compt.m_btnMenual.onClick.Remove(OnSelectManual);
            _compt.m_btnLLM.onClick.Remove(OnSelectLlm);
            _compt.m_btnVillageLog.onClick.Remove(OnVillageLog);
            
            _compt.m_baseUrlInput.onChanged.Remove(OnBaseUrlInputEvent);
            _compt.m_keyInput.onChanged.Remove(OnKeyInputEvent);
            _compt.m_btnKeySend.onClick.Remove(OnKeySendEvent);

            _compt = null;
            _model = null;
            base.OnRelease();
        }



        #endregion

        #region 初始化

        private void PopulateDropdowns()
        {
            var client = AIVillageClient.Instance;

            if (client.CommonModel != null)
            {
                _compt.m_commonModel.items  = client.CommonModel.ToArray();
                _compt.m_commonModel.values = client.CommonModel.ToArray();
            }

            if (client.ReflectModel != null)
            {
                _compt.m_reflectModel.items  = client.ReflectModel.ToArray();
                _compt.m_reflectModel.values = client.ReflectModel.ToArray();
            }

            if (client.RegisterModel != null)
            {
                _compt.m_registerNPCsModel.items = client.RegisterModel.ToArray();
                _compt.m_registerNPCsModel.values = client.RegisterModel.ToArray();
            }

            if (client.PresetModes != null)
            {
                _compt.m_presetMode.items = client.PresetModes.ToArray();
                _compt.m_presetMode.values = client.PresetModes.ToArray();
            }
        }

        private void RefreshFromModel()
        {
            if (_model == null) return;

            _compt.m_commonModel.selectedIndex       = _model.SelectedCommonModelIndex;
            _compt.m_reflectModel.selectedIndex      = _model.SelectedReflectModelIndex;
            _compt.m_registerNPCsModel.selectedIndex = _model.SelectedRegisterModelIndex;
            _compt.m_presetMode.selectedIndex        = _model.SelectedPresetModeIndex;

            // 互斥模式按钮：selected 表示当前激活
            _compt.m_btnMenual.selected = !_model.UseLlm;
            _compt.m_btnLLM.selected    =  _model.UseLlm;

            // 恢复上次填写的 BaseUrl / Key
            _compt.m_baseUrlInput.text = _model.CustomBaseUrl;
            _compt.m_keyInput.text     = _model.CustomApiKey;
            _customBaseUrl             = _model.CustomBaseUrl;
            _customKey                 = _model.CustomApiKey;
        }

        private void BindEvents()
        {
            _compt.m_BtnClose.onClick.Add(OnClose);
            _compt.m_commonModel.onChanged.Add(OnCommonModelChanged);
            _compt.m_reflectModel.onChanged.Add(OnReflectModelChanged);
            _compt.m_registerNPCsModel.onChanged.Add(OnRegisterNPCsModelChanged);
            _compt.m_presetMode.onChanged.Add(OnPresetModeChanged);
            _compt.m_btnMenual.onClick.Add(OnSelectManual);
            _compt.m_btnLLM.onClick.Add(OnSelectLlm);
            _compt.m_btnVillageLog.onClick.Add(OnVillageLog);

            _compt.m_baseUrlInput.onChanged.Add(OnBaseUrlInputEvent);
            _compt.m_keyInput.onChanged.Add(OnKeyInputEvent);
            _compt.m_btnKeySend.onClick.Add(OnKeySendEvent);
        }

        #endregion

        #region 事件处理

        private void OnCommonModelChanged(EventContext context)
        {
            if (_model == null) return;
            _model.SelectedCommonModelIndex = _compt.m_commonModel.selectedIndex;
            LogApplyConfig();
            //_model.Save();
        }

        private void OnReflectModelChanged(EventContext context)
        {
            if (_model == null) return;
            _model.SelectedRegisterModelIndex = _compt.m_registerNPCsModel.selectedIndex;
            LogApplyConfig();
            //_model.Save();
        }

        private void OnRegisterNPCsModelChanged(EventContext context)
        {
            if (_model == null) return;
            _model.SelectedReflectModelIndex = _compt.m_reflectModel.selectedIndex;
            LogApplyConfig();
            //_model.Save();
        }

        private void OnPresetModeChanged(EventContext context)
        {
            if (_model == null) return;
            _model.SelectedPresetModeIndex = _compt.m_presetMode.selectedIndex;
            LogApplyConfig();
            //_model.Save();
        }

        private void OnSelectManual(EventContext context)
        {
            if (_model == null) return;
            _model.UseLlm               = false;
            _compt.m_btnMenual.selected = true;
            _compt.m_btnLLM.selected    = false;
            LogApplyConfig();
            //_model.Save();
        }

        private void OnSelectLlm(EventContext context)
        {
            if (_model == null) return;
            _model.UseLlm               = true;
            _compt.m_btnMenual.selected = false;
            _compt.m_btnLLM.selected    = true;
            LogApplyConfig();
            //_model.Save();
        }

        private void OnVillageLog(EventContext context)
        {
            UIManager.Instance.OpenViewSync<VillageLogView>();
        }

        private void OnClose(EventContext context)
        {
            UIManager.Instance.CloseView<VillageLogView>();
            UIManager.Instance.CloseView<GameConfigView>();
        }

        

        private void OnBaseUrlInputEvent(EventContext context)
        {
            _customBaseUrl = _compt.m_baseUrlInput.text;
        }

        private void OnKeyInputEvent(EventContext context)
        {
            _customKey = _compt.m_keyInput.text;
        }

        private void OnKeySendEvent(EventContext context)
        {
            TipModel model = new TipModel();
            if (string.IsNullOrEmpty(_customBaseUrl))
            {
                model.TipContent = "自定义BaseUrl格式有问题";
                UIManager.Instance.OpenViewSync<TipView>(model);
                return;
            }
            if (string.IsNullOrEmpty(_customKey))
            {
                model.TipContent = "自定义Key格式有问题";
                UIManager.Instance.OpenViewSync<TipView>(model);
                return;
            }

            CustomKeyRequest request = new CustomKeyRequest()
            {
                BaseUrl = _customBaseUrl,
                APIKey = _customKey,
                Validate = false
            };
            AIVillageClient.Instance.PostCustomKey(request,
            (response)=>
            {
                if (response == null) return;
                if (response.Success)
                {
                    model.TipContent = "自定义BaseUrl和Key提交成功";
                    if (_model != null)
                    {
                        _model.CustomBaseUrl = _customBaseUrl;
                        _model.CustomApiKey  = _customKey;
                        //_model.Save();
                    }
                }
                else
                    model.TipContent = $"自定义BaseUrl和Key提交失败: {response.Message}";
                UIManager.Instance.OpenViewSync<TipView>(model);
            },
            (error) =>
            {
                model.TipContent = $"自定义BaseUrl和Key提交失败: {error}";
                UIManager.Instance.OpenViewSync<TipView>(model);
            });
        }


        #endregion

        #region 业务逻辑

        private void LogApplyConfig()
        {
            if (_model == null) return;

            var client      = AIVillageClient.Instance;
            string common   = GetModelName(client.CommonModel,  _model.SelectedCommonModelIndex,  "deepseek-v4-flash");
            string reflect  = GetModelName(client.ReflectModel, _model.SelectedReflectModelIndex, "deepseek-v4-pro");
            string register  = GetModelName(client.RegisterModel, _model.SelectedReflectModelIndex, "deepseek-v4-pro");
            string presetMode  = GetModelName(client.PresetModes, _model.SelectedPresetModeIndex, "fam4_test_villain");

            LogManager.Instance.Info($"[GameConfig] 通用模型={common}  反思模型={reflect} 注册NPC模型={register} LLM={_model.UseLlm} 预置模式={presetMode}");
            client.SelectedCommonModelIndex =_model.SelectedCommonModelIndex;
            client.SelectedReflectModelIndex =_model.SelectedReflectModelIndex;
            client.SelectedRegisterModelIndex = _model.SelectedRegisterModelIndex;
            client.SelectedPresetModeIndex = _model.SelectedPresetModeIndex;
            client.UseLlm = _model.UseLlm;
        }

        private static string GetModelName(System.Collections.Generic.List<string> list, int idx, string fallback)
            => list != null && idx >= 0 && idx < list.Count ? list[idx] : fallback;

        #endregion
    }
}
