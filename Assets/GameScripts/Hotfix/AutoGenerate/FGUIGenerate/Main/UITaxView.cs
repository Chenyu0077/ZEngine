/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Hotfix.UI.Generate.Main
{
    public partial class UITaxView : GComponent
    {
        public Controller m_tabCtrol;
        public Controller m_activeCtrol;
        public GGraph m_back;
        public GTextField m_title;
        public GTextField m_cycleLabel;
        public GTextField m_deadlineLabel;
        public GGraph m_progressBg;
        public GGraph m_progressFill;
        public GTextField m_progressText;
        public GRichTextField m_debtLabel;
        public GRichTextField m_strikesLabel;
        public UIComBtn m_settleBtn;
        public GGraph m_divider;
        public GButton m_TabFamily;
        public GButton m_TabHistory;
        public GGraph m_tabUnderline;
        public GList m_familyList;
        public GList m_historyList;
        public GLoader m_popBtn;
        public const string URL = "ui://q0la9fq0tax001";

        public static UITaxView CreateInstance()
        {
            return (UITaxView)UIPackage.CreateObject("Main", "TaxView");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_tabCtrol = GetControllerAt(0);
            m_activeCtrol = GetControllerAt(1);
            m_back = (GGraph)GetChildAt(0);
            m_title = (GTextField)GetChildAt(1);
            m_cycleLabel = (GTextField)GetChildAt(2);
            m_deadlineLabel = (GTextField)GetChildAt(3);
            m_progressBg = (GGraph)GetChildAt(4);
            m_progressFill = (GGraph)GetChildAt(5);
            m_progressText = (GTextField)GetChildAt(6);
            m_debtLabel = (GRichTextField)GetChildAt(7);
            m_strikesLabel = (GRichTextField)GetChildAt(8);
            m_settleBtn = (UIComBtn)GetChildAt(9);
            m_divider = (GGraph)GetChildAt(10);
            m_TabFamily = (GButton)GetChildAt(11);
            m_TabHistory = (GButton)GetChildAt(12);
            m_tabUnderline = (GGraph)GetChildAt(13);
            m_familyList = (GList)GetChildAt(14);
            m_historyList = (GList)GetChildAt(15);
            m_popBtn = (GLoader)GetChildAt(16);
        }
    }
}