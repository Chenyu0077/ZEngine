/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Hotfix.UI.Generate.Cost
{
    public partial class UICostBasicInfoPanel : GComponent
    {
        public Controller m_runStatus;
        public GGraph m_basicBg;
        public GTextField m_dayLabel;
        public GTextField m_dayValue;
        public GGraph m_separator1;
        public GTextField m_npcLabel;
        public GTextField m_npcValue;
        public GGraph m_separator2;
        public GTextField m_callLabel;
        public GTextField m_callValue;
        public GGraph m_separator3;
        public GTextField m_runtimeLabel;
        public GTextField m_runtimeValue;
        public GGraph m_separator4;
        public GTextField m_statusLabel;
        public GGraph m_statusIndicator;
        public GTextField m_statusValue;
        public const string URL = "ui://cost_pkgcost_basic";

        public static UICostBasicInfoPanel CreateInstance()
        {
            return (UICostBasicInfoPanel)UIPackage.CreateObject("Cost", "CostBasicInfoPanel");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_runStatus = GetControllerAt(0);
            m_basicBg = (GGraph)GetChildAt(0);
            m_dayLabel = (GTextField)GetChildAt(1);
            m_dayValue = (GTextField)GetChildAt(2);
            m_separator1 = (GGraph)GetChildAt(3);
            m_npcLabel = (GTextField)GetChildAt(4);
            m_npcValue = (GTextField)GetChildAt(5);
            m_separator2 = (GGraph)GetChildAt(6);
            m_callLabel = (GTextField)GetChildAt(7);
            m_callValue = (GTextField)GetChildAt(8);
            m_separator3 = (GGraph)GetChildAt(9);
            m_runtimeLabel = (GTextField)GetChildAt(10);
            m_runtimeValue = (GTextField)GetChildAt(11);
            m_separator4 = (GGraph)GetChildAt(12);
            m_statusLabel = (GTextField)GetChildAt(13);
            m_statusIndicator = (GGraph)GetChildAt(14);
            m_statusValue = (GTextField)GetChildAt(15);
        }
    }
}