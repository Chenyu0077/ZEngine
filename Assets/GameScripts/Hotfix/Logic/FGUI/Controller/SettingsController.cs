//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using System;
using FairyGUI;
using Hotfix.UI.Generate.Main;
using UnityEngine;
using ZEngine.Manager.Log;
using ZEngine.Manager.UI;

namespace Hotfix.Main.UI
{
    public class SettingsController : BaseController
    {
        private static readonly (int w, int h)[] Resolutions =
        {
            (1920, 1080),
            (1680, 1050),
            (1600, 900),
            (1440, 900),
            (1366, 768),
            (1280, 720),
            (1024, 768),
        };

        private UISettingsView compt;
        private bool _isFullscreen;

        public override void Initialize()
        {
            base.Initialize();
            compt = _view.GetView() as UISettingsView;

            if (compt != null)
            {
                compt.m_CloseBtn.onClick.Add(OnCloseBtnEvent);
                compt.m_LangSettings.onChanged.Add(OnLangSettingsEvent);
                compt.m_MusicSlider.onChanged.Add(OnMusicSliderEvent);
                compt.m_VoiceSlider.onChanged.Add(OnVoiceSliderEvent);
                compt.m_ResolutionSettings.onChanged.Add(OnResolutionChangedEvent);
                compt.m_BtnFullscreen.onClick.Add(OnFullscreenBtnEvent);
                compt.m_BtnWindowed.onClick.Add(OnWindowedBtnEvent);
                compt.m_BtnConfirm.onClick.Add(OnConfirmBtnEvent);

                _isFullscreen = Screen.fullScreen;
                InitResolutionDropdown();
            }

            _view.OnChanged = (data) => { };
        }

        private void InitResolutionDropdown()
        {
            int curW = Screen.width;
            int curH = Screen.height;
            int bestIdx = 0;
            for (int i = 0; i < Resolutions.Length; i++)
            {
                if (Resolutions[i].w == curW && Resolutions[i].h == curH)
                {
                    bestIdx = i;
                    break;
                }
            }
            compt.m_ResolutionSettings.selectedIndex = bestIdx;
        }

        private void OnCloseBtnEvent(EventContext context)
        {
            UIManager.Instance.CloseView<SettingsView>();
        }

        private void OnLangSettingsEvent(EventContext context)
        {
            LogManager.Instance.Info("语言设置更改: " + compt.m_LangSettings.selectedIndex);
        }

        private void OnMusicSliderEvent(EventContext context)
        {
            int val = (int)compt.m_MusicSlider.value;
            compt.m_MusicVal.text = val.ToString();
        }

        private void OnVoiceSliderEvent(EventContext context)
        {
            int val = (int)compt.m_VoiceSlider.value;
            compt.m_VoiceVal.text = val.ToString();
        }

        private void OnResolutionChangedEvent(EventContext context)
        {
            LogManager.Instance.Info("分辨率选择: " + compt.m_ResolutionSettings.selectedIndex);
        }

        private void OnFullscreenBtnEvent(EventContext context)
        {
            _isFullscreen = true;
        }

        private void OnWindowedBtnEvent(EventContext context)
        {
            _isFullscreen = false;
        }

        private void OnConfirmBtnEvent(EventContext context)
        {
            int idx = compt.m_ResolutionSettings.selectedIndex;
            if (idx >= 0 && idx < Resolutions.Length)
            {
                var (w, h) = Resolutions[idx];
                Screen.SetResolution(w, h, _isFullscreen);
                LogManager.Instance.Info($"分辨率已设置: {w}x{h}, 全屏: {_isFullscreen}");
            }
            UIManager.Instance.CloseView<SettingsView>();
        }

        public override void OnRelease()
        {
            if (compt != null)
            {
                compt.m_CloseBtn.onClick.Remove(OnCloseBtnEvent);
                compt.m_LangSettings.onChanged.Remove(OnLangSettingsEvent);
                compt.m_MusicSlider.onChanged.Remove(OnMusicSliderEvent);
                compt.m_VoiceSlider.onChanged.Remove(OnVoiceSliderEvent);
                compt.m_ResolutionSettings.onChanged.Remove(OnResolutionChangedEvent);
                compt.m_BtnFullscreen.onClick.Remove(OnFullscreenBtnEvent);
                compt.m_BtnWindowed.onClick.Remove(OnWindowedBtnEvent);
                compt.m_BtnConfirm.onClick.Remove(OnConfirmBtnEvent);
                compt = null;
            }
            base.OnRelease();
        }
    }
}
