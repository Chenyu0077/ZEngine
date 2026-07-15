/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Hotfix.UI.Generate.Common
{
    public partial class UICommonBtn1 : GButton
    {
        public GGraph m_mask;
        public const string URL = "ui://f70pfjtkixn42";

        public static UICommonBtn1 CreateInstance()
        {
            return (UICommonBtn1)UIPackage.CreateObject("Common", "CommonBtn1");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_mask = (GGraph)GetChildAt(4);
        }
    }
}