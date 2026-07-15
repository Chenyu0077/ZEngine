/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Hotfix.UI.Generate.Main
{
    public partial class UIPossessionHistoryItem : GComponent
    {
        public GGraph m_back;
        public GGraph m_accentBar;
        public GTextField m_hourLabel;
        public GTextField m_iconText;
        public GTextField m_actionLabel;
        public GTextField m_summaryText;
        public GGraph m_deltaBg;
        public GTextField m_deltaText;
        public GGraph m_dialogueBadgeBg;
        public GTextField m_dialogueBadge;
        public GGraph m_divider;
        public const string URL = "ui://q0la9fq0phs002";

        public static UIPossessionHistoryItem CreateInstance()
        {
            return (UIPossessionHistoryItem)UIPackage.CreateObject("Main", "PossessionHistoryItem");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_back = (GGraph)GetChildAt(0);
            m_accentBar = (GGraph)GetChildAt(1);
            m_hourLabel = (GTextField)GetChildAt(2);
            m_iconText = (GTextField)GetChildAt(3);
            m_actionLabel = (GTextField)GetChildAt(4);
            m_summaryText = (GTextField)GetChildAt(5);
            m_deltaBg = (GGraph)GetChildAt(6);
            m_deltaText = (GTextField)GetChildAt(7);
            m_dialogueBadgeBg = (GGraph)GetChildAt(8);
            m_dialogueBadge = (GTextField)GetChildAt(9);
            m_divider = (GGraph)GetChildAt(10);
        }
    }
}