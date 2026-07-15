/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Hotfix.UI.Generate.Main
{
    public partial class UIPossessionHistoryView : GComponent
    {
        public GGraph m_back;
        public GGraph m_rectback;
        public GGraph m_titleBar;
        public GGraph m_titleBarBottom;
        public GTextField m_title;
        public GTextField m_charName;
        public GButton m_btnClose;
        public GGraph m_dayBarBg;
        public GList m_dayList;
        public GList m_timelineList;
        public GGraph m_dividerH;
        public GGraph m_detailBg;
        public GTextField m_detailTitle;
        public GTextField m_keyOutcomes;
        public GTextField m_summaryInitiator;
        public GList m_transcriptList;
        public GTextField m_emptyHint;
        public GTextField m_loadingHint;
        public const string URL = "ui://q0la9fq0phs001";

        public static UIPossessionHistoryView CreateInstance()
        {
            return (UIPossessionHistoryView)UIPackage.CreateObject("Main", "PossessionHistoryView");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_back = (GGraph)GetChildAt(0);
            m_rectback = (GGraph)GetChildAt(1);
            m_titleBar = (GGraph)GetChildAt(2);
            m_titleBarBottom = (GGraph)GetChildAt(3);
            m_title = (GTextField)GetChildAt(4);
            m_charName = (GTextField)GetChildAt(5);
            m_btnClose = (GButton)GetChildAt(6);
            m_dayBarBg = (GGraph)GetChildAt(7);
            m_dayList = (GList)GetChildAt(8);
            m_timelineList = (GList)GetChildAt(9);
            m_dividerH = (GGraph)GetChildAt(10);
            m_detailBg = (GGraph)GetChildAt(11);
            m_detailTitle = (GTextField)GetChildAt(12);
            m_keyOutcomes = (GTextField)GetChildAt(13);
            m_summaryInitiator = (GTextField)GetChildAt(14);
            m_transcriptList = (GList)GetChildAt(15);
            m_emptyHint = (GTextField)GetChildAt(16);
            m_loadingHint = (GTextField)GetChildAt(17);
        }
    }
}