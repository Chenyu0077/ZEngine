/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Hotfix.UI.Generate.Main
{
    public partial class UITaxFamilyItem : GComponent
    {
        public GGraph m_back;
        public GTextField m_familyName;
        public GTextField m_tierLabel;
        public GTextField m_collectedText;
        public GGraph m_progressBg;
        public GGraph m_progressFill;
        public GRichTextField m_statusLabel;
        public const string URL = "ui://q0la9fq0tax002";

        public static UITaxFamilyItem CreateInstance()
        {
            return (UITaxFamilyItem)UIPackage.CreateObject("Main", "TaxFamilyItem");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_back = (GGraph)GetChildAt(0);
            m_familyName = (GTextField)GetChildAt(1);
            m_tierLabel = (GTextField)GetChildAt(2);
            m_collectedText = (GTextField)GetChildAt(3);
            m_progressBg = (GGraph)GetChildAt(4);
            m_progressFill = (GGraph)GetChildAt(5);
            m_statusLabel = (GRichTextField)GetChildAt(6);
        }
    }
}