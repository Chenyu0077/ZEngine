/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Hotfix.UI.Generate.Main
{
    public partial class UIWaitingView : GComponent
    {
        public GGraph m_back;
        public GTextInput m_content;
        public const string URL = "ui://q0la9fq0broxm";

        public static UIWaitingView CreateInstance()
        {
            return (UIWaitingView)UIPackage.CreateObject("Main", "WaitingView");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_back = (GGraph)GetChildAt(0);
            m_content = (GTextInput)GetChildAt(1);
        }
    }
}