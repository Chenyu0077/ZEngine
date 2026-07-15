/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Hotfix.UI.Generate.Main
{
    public partial class UIGameOverView : GComponent
    {
        public GGraph m_back;
        public GGraph m_overBack;
        public GTextField m_title;
        public UIScrollText1 m_scrollContent;
        public GButton m_ExitBtn;
        public const string URL = "ui://q0la9fq0jxy51o";

        public static UIGameOverView CreateInstance()
        {
            return (UIGameOverView)UIPackage.CreateObject("Main", "GameOverView");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_back = (GGraph)GetChildAt(0);
            m_overBack = (GGraph)GetChildAt(1);
            m_title = (GTextField)GetChildAt(2);
            m_scrollContent = (UIScrollText1)GetChildAt(3);
            m_ExitBtn = (GButton)GetChildAt(4);
        }
    }
}