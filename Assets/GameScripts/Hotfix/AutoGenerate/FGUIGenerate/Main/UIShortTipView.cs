/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Hotfix.UI.Generate.Main
{
    public partial class UIShortTipView : GComponent
    {
        public GGraph m_back;
        public GRichTextField m_content;
        public const string URL = "ui://q0la9fq0gri91v";

        public static UIShortTipView CreateInstance()
        {
            return (UIShortTipView)UIPackage.CreateObject("Main", "ShortTipView");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_back = (GGraph)GetChildAt(0);
            m_content = (GRichTextField)GetChildAt(1);
        }
    }
}