/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Hotfix.UI.Generate.Main
{
    public partial class UIGameControlView : GComponent
    {
        public Controller m_popCtrol;
        public GGraph m_back;
        public GTextInput m_faimlyCount;
        public GSlider m_faimlySlider;
        public GList m_btnList;
        public GButton m_controlModeBtn;
        public GLoader m_popBtn;
        public GRichTextField m_statusText;
        public const string URL = "ui://q0la9fq0w3fri";

        public static UIGameControlView CreateInstance()
        {
            return (UIGameControlView)UIPackage.CreateObject("Main", "GameControlView");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_popCtrol = GetControllerAt(0);
            m_back = (GGraph)GetChildAt(0);
            m_faimlyCount = (GTextInput)GetChildAt(2);
            m_faimlySlider = (GSlider)GetChildAt(3);
            m_btnList = (GList)GetChildAt(4);
            m_controlModeBtn = (GButton)GetChildAt(5);
            m_popBtn = (GLoader)GetChildAt(7);
            m_statusText = (GRichTextField)GetChildAt(8);
        }
    }
}