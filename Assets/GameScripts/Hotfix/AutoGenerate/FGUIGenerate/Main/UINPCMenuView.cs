/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Hotfix.UI.Generate.Main
{
    public partial class UINPCMenuView : GComponent
    {
        public GGraph m_back;
        public GButton m_BtnAttribute;
        public GButton m_BtnSchedule;
        public GButton m_BtnBackground;
        public const string URL = "ui://q0la9fq0o6bcb";

        public static UINPCMenuView CreateInstance()
        {
            return (UINPCMenuView)UIPackage.CreateObject("Main", "NPCMenuView");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_back = (GGraph)GetChildAt(0);
            m_BtnAttribute = (GButton)GetChildAt(1);
            m_BtnSchedule = (GButton)GetChildAt(2);
            m_BtnBackground = (GButton)GetChildAt(3);
        }
    }
}