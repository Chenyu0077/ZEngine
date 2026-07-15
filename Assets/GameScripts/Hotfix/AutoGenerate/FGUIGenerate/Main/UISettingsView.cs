/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Hotfix.UI.Generate.Main
{
    public partial class UISettingsView : GComponent
    {
        public GGraph m_back;
        public GButton m_CloseBtn;
        public GTextField m_PanelTitle;
        public GGraph m_titleLine;
        public GTextField m_LangLabel;
        public GTextField m_LangTip;
        public GComboBox m_LangSettings;
        public GGraph m_line0;
        public GTextField m_ResLabel;
        public GTextField m_ResTip;
        public GComboBox m_ResolutionSettings;
        public GButton m_BtnFullscreen;
        public GButton m_BtnWindowed;
        public GGraph m_line1;
        public GTextField m_MusicLabel;
        public GTextField m_MusicTip;
        public GSlider m_MusicSlider;
        public GTextField m_MusicVal;
        public GGraph m_line2;
        public GTextField m_VoiceLabel;
        public GTextField m_VoiceTip;
        public GSlider m_VoiceSlider;
        public GTextField m_VoiceVal;
        public GGraph m_line3;
        public UIComBtn m_BtnConfirm;
        public const string URL = "ui://q0la9fq0ixn42";

        public static UISettingsView CreateInstance()
        {
            return (UISettingsView)UIPackage.CreateObject("Main", "SettingsView");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_back = (GGraph)GetChildAt(0);
            m_CloseBtn = (GButton)GetChildAt(1);
            m_PanelTitle = (GTextField)GetChildAt(2);
            m_titleLine = (GGraph)GetChildAt(3);
            m_LangLabel = (GTextField)GetChildAt(4);
            m_LangTip = (GTextField)GetChildAt(5);
            m_LangSettings = (GComboBox)GetChildAt(6);
            m_line0 = (GGraph)GetChildAt(7);
            m_ResLabel = (GTextField)GetChildAt(8);
            m_ResTip = (GTextField)GetChildAt(9);
            m_ResolutionSettings = (GComboBox)GetChildAt(10);
            m_BtnFullscreen = (GButton)GetChildAt(11);
            m_BtnWindowed = (GButton)GetChildAt(12);
            m_line1 = (GGraph)GetChildAt(13);
            m_MusicLabel = (GTextField)GetChildAt(14);
            m_MusicTip = (GTextField)GetChildAt(15);
            m_MusicSlider = (GSlider)GetChildAt(16);
            m_MusicVal = (GTextField)GetChildAt(17);
            m_line2 = (GGraph)GetChildAt(18);
            m_VoiceLabel = (GTextField)GetChildAt(19);
            m_VoiceTip = (GTextField)GetChildAt(20);
            m_VoiceSlider = (GSlider)GetChildAt(21);
            m_VoiceVal = (GTextField)GetChildAt(22);
            m_line3 = (GGraph)GetChildAt(23);
            m_BtnConfirm = (UIComBtn)GetChildAt(24);
        }
    }
}