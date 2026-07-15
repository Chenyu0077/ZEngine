/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Hotfix.UI.Generate.Cost
{
    public partial class UISmallProgress : GProgressBar
    {
        public GGraph m_bg;
        public const string URL = "ui://cost_pkgcost_small_progress";

        public static UISmallProgress CreateInstance()
        {
            return (UISmallProgress)UIPackage.CreateObject("Cost", "SmallProgress");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_bg = (GGraph)GetChildAt(0);
        }
    }
}