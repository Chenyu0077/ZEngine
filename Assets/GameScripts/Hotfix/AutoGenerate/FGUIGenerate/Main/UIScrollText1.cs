/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Hotfix.UI.Generate.Main
{
    public partial class UIScrollText1 : GComponent
    {
        public GRichTextField m_content;
        public const string URL = "ui://q0la9fq0cmeio";

        public static UIScrollText1 CreateInstance()
        {
            return (UIScrollText1)UIPackage.CreateObject("Main", "ScrollText1");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_content = (GRichTextField)GetChildAt(0);
        }
    }
}