/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Hotfix.UI.Generate.Cost
{
    public partial class UIToggleBtn : GButton
    {
        public Controller m_toggle;
        public GGraph m_offBg;
        public GGraph m_onBg;
        public GGraph m_offHandle;
        public GGraph m_onHandle;
        public GTextField m_label;
        public Transition m_toggleOn;
        public Transition m_toggleOff;
        public const string URL = "ui://cost_pkgcost_toggle";

        public static UIToggleBtn CreateInstance()
        {
            return (UIToggleBtn)UIPackage.CreateObject("Cost", "ToggleBtn");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_toggle = GetControllerAt(1);
            m_offBg = (GGraph)GetChildAt(0);
            m_onBg = (GGraph)GetChildAt(1);
            m_offHandle = (GGraph)GetChildAt(2);
            m_onHandle = (GGraph)GetChildAt(3);
            m_label = (GTextField)GetChildAt(4);
            m_toggleOn = GetTransitionAt(0);
            m_toggleOff = GetTransitionAt(1);
        }
    }
}