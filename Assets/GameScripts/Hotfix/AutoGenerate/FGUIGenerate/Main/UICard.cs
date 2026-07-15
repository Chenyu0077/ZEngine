/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Hotfix.UI.Generate.Main
{
    public partial class UICard : GComponent
    {
        public GGraph m_back;
        public GLoader m_image;
        public GTextField m_name;
        public const string URL = "ui://q0la9fq0k4vs8";

        public static UICard CreateInstance()
        {
            return (UICard)UIPackage.CreateObject("Main", "Card");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_back = (GGraph)GetChildAt(0);
            m_image = (GLoader)GetChildAt(1);
            m_name = (GTextField)GetChildAt(2);
        }
    }
}