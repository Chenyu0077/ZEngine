/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Hotfix.UI.Generate.Cost
{
    public partial class UICostVillagePanel : GComponent
    {
        public GGraph m_villageBg;
        public GGraph m_titleBg;
        public GTextField m_titleIcon;
        public GTextField m_titleText;
        public GTextField m_tokenLabel;
        public GTextField m_tokenValue;
        public GTextField m_callLabel;
        public GTextField m_callValue;
        public GGraph m_separator;
        public GTextField m_costUsdLabel;
        public GTextField m_costUsdValue;
        public GTextField m_costCnyLabel;
        public GTextField m_costCnyValue;
        public GTextField m_percentageLabel;
        public GTextField m_percentageValue;
        public Transition m_update;
        public const string URL = "ui://cost_pkgcost_village";

        public static UICostVillagePanel CreateInstance()
        {
            return (UICostVillagePanel)UIPackage.CreateObject("Cost", "CostVillagePanel");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_villageBg = (GGraph)GetChildAt(0);
            m_titleBg = (GGraph)GetChildAt(1);
            m_titleIcon = (GTextField)GetChildAt(2);
            m_titleText = (GTextField)GetChildAt(3);
            m_tokenLabel = (GTextField)GetChildAt(4);
            m_tokenValue = (GTextField)GetChildAt(5);
            m_callLabel = (GTextField)GetChildAt(6);
            m_callValue = (GTextField)GetChildAt(7);
            m_separator = (GGraph)GetChildAt(8);
            m_costUsdLabel = (GTextField)GetChildAt(9);
            m_costUsdValue = (GTextField)GetChildAt(10);
            m_costCnyLabel = (GTextField)GetChildAt(11);
            m_costCnyValue = (GTextField)GetChildAt(12);
            m_percentageLabel = (GTextField)GetChildAt(13);
            m_percentageValue = (GTextField)GetChildAt(14);
            m_update = GetTransitionAt(0);
        }
    }
}