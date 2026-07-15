using Hotfix.FuncModule;
using ZEngine.Manager.UI;
using ZEngine.Module.Archive;

namespace Hotfix.Main.UI
{
    public class GameConfigModel : BaseModel
    {
        /// <summary>当前选中的通用模型索引。</summary>
        public int SelectedCommonModelIndex = 0;

        /// <summary>当前选中的反思模型索引。</summary>
        public int SelectedReflectModelIndex = 0;

        /// <summary>当前选中的注册NPC模型索引。</summary>
        public int SelectedRegisterModelIndex = 0;

        /// <summary>当前选中的预制模式索引。</summary>
        public int SelectedPresetModeIndex = 0;

        /// <summary>是否启用 LLM。</summary>
        public bool UseLlm = false;

        /// <summary>自定义 BaseUrl。</summary>
        public string CustomBaseUrl = string.Empty;

        /// <summary>自定义 API Key。</summary>
        public string CustomApiKey = string.Empty;

        public override void Initialize()
        {
            /*var slot = ArchiveManager.Instance.LoadSync<ArchiveSlot>(
                $"{ArchiveSlot.SystemConfigSlotName}_{ArchiveSlot.SystemConfigId}");

            if (slot?.System == null) return;

            var s = slot.System;
            SelectedCommonModelIndex   = s.ConfigData.CommonModelIndex;
            SelectedReflectModelIndex  = s.ConfigData.ReflectModelIndex;
            SelectedRegisterModelIndex = s.ConfigData.RegisterModelIndex;
            SelectedPresetModeIndex    = s.ConfigData.PresetModeIndex;
            UseLlm                     = s.ConfigData.UseLlm;
            CustomBaseUrl              = s.ConfigData.CustomBaseUrl  ?? string.Empty;
            CustomApiKey               = s.ConfigData.CustomApiKey   ?? string.Empty;*/
        }

        /*public void Save()
        {
            // 尝试取已缓存的 slot，取不到则新建
            var slot = ArchiveManager.Instance.GetSlot(ArchiveSlot.SystemConfigId) as ArchiveSlot;

            var s = slot.System;
            if (s.ConfigData == null)
                s.ConfigData = new ConfigArchiveData();
            s.ConfigData.CommonModelIndex   = SelectedCommonModelIndex;
            s.ConfigData.ReflectModelIndex  = SelectedReflectModelIndex;
            s.ConfigData.RegisterModelIndex = SelectedRegisterModelIndex;
            s.ConfigData.PresetModeIndex    = SelectedPresetModeIndex;
            s.ConfigData.UseLlm             = UseLlm;
            s.ConfigData.CustomBaseUrl      = CustomBaseUrl;
            s.ConfigData.CustomApiKey       = CustomApiKey;

            ArchiveManager.Instance.SaveSync(slot);
        }*/
        

        public override void OnRelease() { }
    }
}
