/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Hotfix.UI.Generate.Cost
{
    public partial class UICostCumulativePanel : GComponent
    {
        public GGraph m_cumulativeBg;
        public GGraph m_titleBg;
        public GTextField m_titleIcon;
        public GTextField m_titleText;
        public GTextField m_totalTokenLabel;
        public GTextField m_totalTokenValue;
        public GTextField m_inputTokenLabel;
        public GTextField m_inputTokenValue;
        public GTextField m_outputTokenLabel;
        public GTextField m_outputTokenValue;
        public GGraph m_separator;
        public GTextField m_costUsdLabel;
        public GTextField m_costUsdValue;
        public GTextField m_costCnyLabel;
        public GTextField m_costCnyValue;
        public GGraph m_separator2;
        public GTextField m_cacheLabel;
        public GTextField m_cacheValue;
        public UIProgressBar m_cacheProgressBar;
        public GTextField m_latencyLabel;
        public Transition m_update;
        public const string URL = "ui://cost_pkgcost_cumulative";

        public static UICostCumulativePanel CreateInstance()
        {
            return (UICostCumulativePanel)UIPackage.CreateObject("Cost", "CostCumulativePanel");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_cumulativeBg = (GGraph)GetChildAt(0);
            m_titleBg = (GGraph)GetChildAt(1);
            m_titleIcon = (GTextField)GetChildAt(2);
            m_titleText = (GTextField)GetChildAt(3);
            m_totalTokenLabel = (GTextField)GetChildAt(4);
            m_totalTokenValue = (GTextField)GetChildAt(5);
            m_inputTokenLabel = (GTextField)GetChildAt(6);
            m_inputTokenValue = (GTextField)GetChildAt(7);
            m_outputTokenLabel = (GTextField)GetChildAt(8);
            m_outputTokenValue = (GTextField)GetChildAt(9);
            m_separator = (GGraph)GetChildAt(10);
            m_costUsdLabel = (GTextField)GetChildAt(11);
            m_costUsdValue = (GTextField)GetChildAt(12);
            m_costCnyLabel = (GTextField)GetChildAt(13);
            m_costCnyValue = (GTextField)GetChildAt(14);
            m_separator2 = (GGraph)GetChildAt(15);
            m_cacheLabel = (GTextField)GetChildAt(16);
            m_cacheValue = (GTextField)GetChildAt(17);
            m_cacheProgressBar = (UIProgressBar)GetChildAt(18);
            m_latencyLabel = (GTextField)GetChildAt(19);
            m_update = GetTransitionAt(0);
        }
    }
}