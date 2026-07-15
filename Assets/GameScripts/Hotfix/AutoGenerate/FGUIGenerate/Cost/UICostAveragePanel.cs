/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Hotfix.UI.Generate.Cost
{
    public partial class UICostAveragePanel : GComponent
    {
        public GGraph m_averageBg;
        public GGraph m_titleBg;
        public GTextField m_titleIcon;
        public GTextField m_titleText;
        public GTextField m_npcDayTitle;
        public GTextField m_npcUsdLabel;
        public GTextField m_npcUsdValue;
        public GTextField m_npcCnyLabel;
        public GTextField m_npcCnyValue;
        public GGraph m_separator;
        public GTextField m_npcTokenLabel;
        public GTextField m_npcTokenValue;
        public GGraph m_separator2;
        public GTextField m_villageTitle;
        public GTextField m_villageUsdLabel;
        public GTextField m_villageUsdValue;
        public GTextField m_villageCnyLabel;
        public GTextField m_villageCnyValue;
        public Transition m_update;
        public const string URL = "ui://cost_pkgcost_average";

        public static UICostAveragePanel CreateInstance()
        {
            return (UICostAveragePanel)UIPackage.CreateObject("Cost", "CostAveragePanel");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_averageBg = (GGraph)GetChildAt(0);
            m_titleBg = (GGraph)GetChildAt(1);
            m_titleIcon = (GTextField)GetChildAt(2);
            m_titleText = (GTextField)GetChildAt(3);
            m_npcDayTitle = (GTextField)GetChildAt(4);
            m_npcUsdLabel = (GTextField)GetChildAt(5);
            m_npcUsdValue = (GTextField)GetChildAt(6);
            m_npcCnyLabel = (GTextField)GetChildAt(7);
            m_npcCnyValue = (GTextField)GetChildAt(8);
            m_separator = (GGraph)GetChildAt(9);
            m_npcTokenLabel = (GTextField)GetChildAt(10);
            m_npcTokenValue = (GTextField)GetChildAt(11);
            m_separator2 = (GGraph)GetChildAt(12);
            m_villageTitle = (GTextField)GetChildAt(13);
            m_villageUsdLabel = (GTextField)GetChildAt(14);
            m_villageUsdValue = (GTextField)GetChildAt(15);
            m_villageCnyLabel = (GTextField)GetChildAt(16);
            m_villageCnyValue = (GTextField)GetChildAt(17);
            m_update = GetTransitionAt(0);
        }
    }
}