/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Hotfix.UI.Generate.Cost
{
    public partial class UIModelListItem : GComponent
    {
        public GGraph m_itemBg;
        public GTextField m_txtModelName;
        public GTextField m_txtTokens;
        public GTextField m_txtCostUsd;
        public GTextField m_txtCostCny;
        public GTextField m_txtCacheRate;
        public UIProgressBar m_cacheProgressBar;
        public GTextField m_txtCallCount;
        public GGraph m_separator1;
        public GGraph m_separator2;
        public GGraph m_separator3;
        public GGraph m_separator4;
        public GGraph m_separator5;
        public Transition m_hover;
        public Transition m_leave;
        public const string URL = "ui://cost_pkgcost_list_item";

        public static UIModelListItem CreateInstance()
        {
            return (UIModelListItem)UIPackage.CreateObject("Cost", "ModelListItem");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_itemBg = (GGraph)GetChildAt(0);
            m_txtModelName = (GTextField)GetChildAt(1);
            m_txtTokens = (GTextField)GetChildAt(2);
            m_txtCostUsd = (GTextField)GetChildAt(3);
            m_txtCostCny = (GTextField)GetChildAt(4);
            m_txtCacheRate = (GTextField)GetChildAt(5);
            m_cacheProgressBar = (UIProgressBar)GetChildAt(6);
            m_txtCallCount = (GTextField)GetChildAt(7);
            m_separator1 = (GGraph)GetChildAt(8);
            m_separator2 = (GGraph)GetChildAt(9);
            m_separator3 = (GGraph)GetChildAt(10);
            m_separator4 = (GGraph)GetChildAt(11);
            m_separator5 = (GGraph)GetChildAt(12);
            m_hover = GetTransitionAt(0);
            m_leave = GetTransitionAt(1);
        }
    }
}