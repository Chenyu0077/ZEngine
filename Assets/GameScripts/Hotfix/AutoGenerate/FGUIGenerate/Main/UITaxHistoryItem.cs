/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Hotfix.UI.Generate.Main
{
    public partial class UITaxHistoryItem : GComponent
    {
        public GGraph m_back;
        public GTextField m_cycleText;
        public GTextField m_requiredText;
        public GTextField m_actualText;
        public GTextField m_ratioText;
        public GRichTextField m_outcomeText;
        public const string URL = "ui://q0la9fq0tax003";

        public static UITaxHistoryItem CreateInstance()
        {
            return (UITaxHistoryItem)UIPackage.CreateObject("Main", "TaxHistoryItem");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_back = (GGraph)GetChildAt(0);
            m_cycleText = (GTextField)GetChildAt(1);
            m_requiredText = (GTextField)GetChildAt(2);
            m_actualText = (GTextField)GetChildAt(3);
            m_ratioText = (GTextField)GetChildAt(4);
            m_outcomeText = (GRichTextField)GetChildAt(5);
        }
    }
}