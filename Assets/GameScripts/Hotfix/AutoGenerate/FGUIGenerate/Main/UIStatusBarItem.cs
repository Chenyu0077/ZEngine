/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Hotfix.UI.Generate.Main
{
    public partial class UIStatusBarItem : GComponent
    {
        public Controller m_warn;
        public GTextField m_icon;
        public GTextField m_label;
        public GTextField m_value;
        public GTextField m_warnIcon;
        public GGraph m_barBg;
        public GGraph m_barFill;
        public const string URL = "ui://q0la9fq0jxy51h";

        public static UIStatusBarItem CreateInstance()
        {
            return (UIStatusBarItem)UIPackage.CreateObject("Main", "StatusBarItem");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_warn = GetControllerAt(0);
            m_icon = (GTextField)GetChildAt(0);
            m_label = (GTextField)GetChildAt(1);
            m_value = (GTextField)GetChildAt(2);
            m_warnIcon = (GTextField)GetChildAt(3);
            m_barBg = (GGraph)GetChildAt(4);
            m_barFill = (GGraph)GetChildAt(5);
        }
    }
}