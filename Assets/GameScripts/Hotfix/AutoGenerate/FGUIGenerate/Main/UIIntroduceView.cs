/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Hotfix.UI.Generate.Main
{
    public partial class UIIntroduceView : GComponent
    {
        public GGraph m_back;
        public GTextInput m_text;
        public GButton m_CloseBtn;
        public const string URL = "ui://q0la9fq0ixn43";

        public static UIIntroduceView CreateInstance()
        {
            return (UIIntroduceView)UIPackage.CreateObject("Main", "IntroduceView");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_back = (GGraph)GetChildAt(0);
            m_text = (GTextInput)GetChildAt(1);
            m_CloseBtn = (GButton)GetChildAt(2);
        }
    }
}