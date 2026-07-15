/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Hotfix.UI.Generate.Main
{
    public partial class UIArchiveView : GComponent
    {
        public GGraph m_back;
        public GButton m_CloseBtn;
        public GButton m_addBtn;
        public GList m_SaveList;
        public const string URL = "ui://q0la9fq0ixn41";

        public static UIArchiveView CreateInstance()
        {
            return (UIArchiveView)UIPackage.CreateObject("Main", "ArchiveView");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_back = (GGraph)GetChildAt(0);
            m_CloseBtn = (GButton)GetChildAt(1);
            m_addBtn = (GButton)GetChildAt(2);
            m_SaveList = (GList)GetChildAt(3);
        }
    }
}