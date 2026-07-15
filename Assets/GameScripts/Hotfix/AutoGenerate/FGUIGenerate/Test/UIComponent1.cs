/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Hotfix.UI.Generate.Test
{
    public partial class UIComponent1 : GComponent
    {
        public GImage m_picture;
        public GButton m_reBtn;
        public const string URL = "ui://j7zuvthkisnh0";

        public static UIComponent1 CreateInstance()
        {
            return (UIComponent1)UIPackage.CreateObject("Test", "Component1");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_picture = (GImage)GetChildAt(0);
            m_reBtn = (GButton)GetChildAt(1);
        }
    }
}