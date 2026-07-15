/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Hotfix.UI.Generate.Cost
{
    public partial class UICostModelsPanel : GComponent
    {
        public GGraph m_modelsBg;
        public GGraph m_titleBg;
        public GTextField m_titleIcon;
        public GTextField m_titleText;
        public GGraph m_headerBg;
        public GTextField m_modelHeader;
        public GTextField m_tokenHeader;
        public GTextField m_usdHeader;
        public GTextField m_cnyHeader;
        public GTextField m_cacheHeader;
        public GTextField m_callsHeader;
        public GGraph m_headerLine1;
        public GGraph m_headerLine2;
        public GGraph m_headerLine3;
        public GGraph m_headerLine4;
        public GGraph m_headerLine5;
        public GList m_modelList;
        public Transition m_sortAsc;
        public Transition m_sortDesc;
        public const string URL = "ui://cost_pkgcost_models";

        public static UICostModelsPanel CreateInstance()
        {
            return (UICostModelsPanel)UIPackage.CreateObject("Cost", "CostModelsPanel");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_modelsBg = (GGraph)GetChildAt(0);
            m_titleBg = (GGraph)GetChildAt(1);
            m_titleIcon = (GTextField)GetChildAt(2);
            m_titleText = (GTextField)GetChildAt(3);
            m_headerBg = (GGraph)GetChildAt(4);
            m_modelHeader = (GTextField)GetChildAt(5);
            m_tokenHeader = (GTextField)GetChildAt(6);
            m_usdHeader = (GTextField)GetChildAt(7);
            m_cnyHeader = (GTextField)GetChildAt(8);
            m_cacheHeader = (GTextField)GetChildAt(9);
            m_callsHeader = (GTextField)GetChildAt(10);
            m_headerLine1 = (GGraph)GetChildAt(11);
            m_headerLine2 = (GGraph)GetChildAt(12);
            m_headerLine3 = (GGraph)GetChildAt(13);
            m_headerLine4 = (GGraph)GetChildAt(14);
            m_headerLine5 = (GGraph)GetChildAt(15);
            m_modelList = (GList)GetChildAt(16);
            m_sortAsc = GetTransitionAt(0);
            m_sortDesc = GetTransitionAt(1);
        }
    }
}