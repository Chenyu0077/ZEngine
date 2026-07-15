/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Hotfix.UI.Generate.Cost
{
    public partial class UICostCachePanel : GComponent
    {
        public GGraph m_cacheBg;
        public GGraph m_titleBg;
        public GTextField m_titleIcon;
        public GTextField m_titleText;
        public GTextField m_hitTokenLabel;
        public GTextField m_hitTokenValue;
        public GTextField m_hitRateLabel;
        public GTextField m_hitRateValue;
        public GTextField m_savedTitle;
        public GTextField m_savedUsdLabel;
        public GTextField m_savedUsdValue;
        public GTextField m_savedCnyLabel;
        public GTextField m_savedCnyValue;
        public UIProgressBar m_efficiencyBar;
        public Transition m_update;
        public const string URL = "ui://cost_pkgcost_cache";

        public static UICostCachePanel CreateInstance()
        {
            return (UICostCachePanel)UIPackage.CreateObject("Cost", "CostCachePanel");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_cacheBg = (GGraph)GetChildAt(0);
            m_titleBg = (GGraph)GetChildAt(1);
            m_titleIcon = (GTextField)GetChildAt(2);
            m_titleText = (GTextField)GetChildAt(3);
            m_hitTokenLabel = (GTextField)GetChildAt(4);
            m_hitTokenValue = (GTextField)GetChildAt(5);
            m_hitRateLabel = (GTextField)GetChildAt(6);
            m_hitRateValue = (GTextField)GetChildAt(7);
            m_savedTitle = (GTextField)GetChildAt(8);
            m_savedUsdLabel = (GTextField)GetChildAt(9);
            m_savedUsdValue = (GTextField)GetChildAt(10);
            m_savedCnyLabel = (GTextField)GetChildAt(11);
            m_savedCnyValue = (GTextField)GetChildAt(12);
            m_efficiencyBar = (UIProgressBar)GetChildAt(13);
            m_update = GetTransitionAt(0);
        }
    }
}