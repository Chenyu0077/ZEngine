/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Hotfix.UI.Generate.Common
{
    public partial class UICommonBtn2 : GButton
    {
        public Controller m_maskCtrol;
        public GGraph m_mask;
        public const string URL = "ui://f70pfjtkcmein";

        public static UICommonBtn2 CreateInstance()
        {
            return (UICommonBtn2)UIPackage.CreateObject("Common", "CommonBtn2");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_maskCtrol = GetControllerAt(1);
            m_mask = (GGraph)GetChildAt(4);
        }
    }
}