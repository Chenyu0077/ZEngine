/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Hotfix.UI.Generate.Main
{
    public partial class UIComboBox1_popup : GComponent
    {
        public GList m_list;
        public const string URL = "ui://q0la9fq0wg5sr";

        public static UIComboBox1_popup CreateInstance()
        {
            return (UIComboBox1_popup)UIPackage.CreateObject("Main", "ComboBox1_popup");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_list = (GList)GetChildAt(1);
        }
    }
}