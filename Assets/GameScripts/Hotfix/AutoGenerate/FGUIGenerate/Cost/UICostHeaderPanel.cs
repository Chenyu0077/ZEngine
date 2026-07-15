/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Hotfix.UI.Generate.Cost
{
    public partial class UICostHeaderPanel : GComponent
    {
        public GGraph m_headerBg;
        public GTextField m_windowTitle;
        public UIToggleBtn m_autoRefreshToggle;
        public GTextField m_toggleLabel;
        public GTextField m_lastUpdateTime;
        public GButton m_refreshBtn;
        public GButton m_closeBtn;
        public Transition m_refreshing;
        public const string URL = "ui://cost_pkgcost_header";

        public static UICostHeaderPanel CreateInstance()
        {
            return (UICostHeaderPanel)UIPackage.CreateObject("Cost", "CostHeaderPanel");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_headerBg = (GGraph)GetChildAt(0);
            m_windowTitle = (GTextField)GetChildAt(1);
            m_autoRefreshToggle = (UIToggleBtn)GetChildAt(2);
            m_toggleLabel = (GTextField)GetChildAt(3);
            m_lastUpdateTime = (GTextField)GetChildAt(4);
            m_refreshBtn = (GButton)GetChildAt(5);
            m_closeBtn = (GButton)GetChildAt(6);
            m_refreshing = GetTransitionAt(0);
        }
    }
}