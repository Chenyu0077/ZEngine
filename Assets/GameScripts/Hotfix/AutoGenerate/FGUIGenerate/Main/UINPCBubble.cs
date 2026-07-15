/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Hotfix.UI.Generate.Main
{
    public partial class UINPCBubble : GComponent
    {
        public GGraph m_bg;
        public GGraph m_arrow;
        public GRichTextField m_txt;
        public Transition m_show;
        public Transition m_hide;
        public const string URL = "ui://q0la9fq0bbl0n";

        public static UINPCBubble CreateInstance()
        {
            return (UINPCBubble)UIPackage.CreateObject("Main", "NPCBubble");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_bg = (GGraph)GetChildAt(0);
            m_arrow = (GGraph)GetChildAt(1);
            m_txt = (GRichTextField)GetChildAt(2);
            m_show = GetTransitionAt(0);
            m_hide = GetTransitionAt(1);
        }
    }
}