/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Hotfix.UI.Generate.Main
{
    public partial class UIMainView : GComponent
    {
        public GGraph m_back;
        public GList m_btnList;
        public const string URL = "ui://q0la9fq0ixn40";

        public static UIMainView CreateInstance()
        {
            return (UIMainView)UIPackage.CreateObject("Main", "MainView");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_back = (GGraph)GetChildAt(0);
            m_btnList = (GList)GetChildAt(1);
        }
    }
}