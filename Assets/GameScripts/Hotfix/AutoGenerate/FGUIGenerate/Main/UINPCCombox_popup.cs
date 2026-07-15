/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Hotfix.UI.Generate.Main
{
    public partial class UINPCCombox_popup : GComponent
    {
        public GList m_list;
        public const string URL = "ui://q0la9fq0kgdi1q";

        public static UINPCCombox_popup CreateInstance()
        {
            return (UINPCCombox_popup)UIPackage.CreateObject("Main", "NPCCombox_popup");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_list = (GList)GetChildAt(1);
        }
    }
}