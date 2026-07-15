/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Hotfix.UI.Generate.Main
{
    public partial class UIMaxTipView : GComponent
    {
        public GGraph m_back;
        public GGraph m_header;
        public GTextField m_title;
        public GButton m_closeBtn;
        public GRichTextField m_content;
        public const string URL = "ui://q0la9fq0gri91x";

        public static UIMaxTipView CreateInstance()
        {
            return (UIMaxTipView)UIPackage.CreateObject("Main", "MaxTipView");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_back = (GGraph)GetChildAt(0);
            m_header = (GGraph)GetChildAt(1);
            m_title = (GTextField)GetChildAt(2);
            m_closeBtn = (GButton)GetChildAt(3);
            m_content = (GRichTextField)GetChildAt(4);
        }
    }
}