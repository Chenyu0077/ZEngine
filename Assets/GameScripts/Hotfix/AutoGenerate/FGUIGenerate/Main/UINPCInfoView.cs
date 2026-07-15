/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Hotfix.UI.Generate.Main
{
    public partial class UINPCInfoView : GComponent
    {
        public GGraph m_mask;
        public GGraph m_back;
        public GTextField m_Title;
        public UIScrollText m_ScrollText;
        public GButton m_BtnClose;
        public const string URL = "ui://q0la9fq0o6bce";

        public static UINPCInfoView CreateInstance()
        {
            return (UINPCInfoView)UIPackage.CreateObject("Main", "NPCInfoView");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_mask = (GGraph)GetChildAt(0);
            m_back = (GGraph)GetChildAt(1);
            m_Title = (GTextField)GetChildAt(2);
            m_ScrollText = (UIScrollText)GetChildAt(3);
            m_BtnClose = (GButton)GetChildAt(4);
        }
    }
}