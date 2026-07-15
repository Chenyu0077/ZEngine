/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Hotfix.UI.Generate.Main
{
    public partial class UIScrollText : GComponent
    {
        public GRichTextField m_content;
        public const string URL = "ui://q0la9fq0hx1rj";

        public static UIScrollText CreateInstance()
        {
            return (UIScrollText)UIPackage.CreateObject("Main", "ScrollText");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_content = (GRichTextField)GetChildAt(0);
        }
    }
}