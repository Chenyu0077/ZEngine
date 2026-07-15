/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Hotfix.UI.Generate.Cost
{
    public partial class UIProgressBar : GProgressBar
    {
        public GGraph m_bg;
        public const string URL = "ui://cost_pkgcost_progress";

        public static UIProgressBar CreateInstance()
        {
            return (UIProgressBar)UIPackage.CreateObject("Cost", "ProgressBar");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_bg = (GGraph)GetChildAt(0);
        }
    }
}