/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Hotfix.UI.Generate.Main
{
    public partial class UIComBtn : GButton
    {
        public Controller m_maskCtrol;
        public GGraph m_mask;
        public const string URL = "ui://q0la9fq0k4vsa";

        public static UIComBtn CreateInstance()
        {
            return (UIComBtn)UIPackage.CreateObject("Main", "ComBtn");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_maskCtrol = GetControllerAt(1);
            m_mask = (GGraph)GetChildAt(4);
        }
    }
}