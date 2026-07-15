/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Hotfix.UI.Generate.Main
{
    public partial class UIGamePermanceView : GComponent
    {
        public GGraph m_mask;
        public UITimeCom m_TimePanel;
        public GButton m_btnHistory;
        public GButton m_btnCost;
        public GButton m_btnConfig;
        public const string URL = "ui://q0la9fq0wg5su";

        public static UIGamePermanceView CreateInstance()
        {
            return (UIGamePermanceView)UIPackage.CreateObject("Main", "GamePermanceView");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_mask = (GGraph)GetChildAt(0);
            m_TimePanel = (UITimeCom)GetChildAt(1);
            m_btnHistory = (GButton)GetChildAt(2);
            m_btnCost = (GButton)GetChildAt(3);
            m_btnConfig = (GButton)GetChildAt(4);
        }
    }
}