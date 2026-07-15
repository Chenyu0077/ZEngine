/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Hotfix.UI.Generate.Main
{
    public partial class UIArchiveCom : GComponent
    {
        public GGraph m_back;
        public GTextField m_showInfo;
        public const string URL = "ui://q0la9fq0ixn44";

        public static UIArchiveCom CreateInstance()
        {
            return (UIArchiveCom)UIPackage.CreateObject("Main", "ArchiveCom");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_back = (GGraph)GetChildAt(0);
            m_showInfo = (GTextField)GetChildAt(1);
        }
    }
}