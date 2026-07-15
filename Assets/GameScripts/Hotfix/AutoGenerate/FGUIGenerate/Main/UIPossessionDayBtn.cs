/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Hotfix.UI.Generate.Main
{
    public partial class UIPossessionDayBtn : GButton
    {
        public GGraph m_underline;
        public const string URL = "ui://q0la9fq0phs004";

        public static UIPossessionDayBtn CreateInstance()
        {
            return (UIPossessionDayBtn)UIPackage.CreateObject("Main", "PossessionDayBtn");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_underline = (GGraph)GetChildAt(3);
        }
    }
}