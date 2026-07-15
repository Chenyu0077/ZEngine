/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Hotfix.UI.Generate.Main
{
    public partial class UIItemCategory : GComponent
    {
        public GGraph m_back;
        public GTextField m_content;
        public const string URL = "ui://q0la9fq0gri91u";

        public static UIItemCategory CreateInstance()
        {
            return (UIItemCategory)UIPackage.CreateObject("Main", "ItemCategory");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_back = (GGraph)GetChildAt(0);
            m_content = (GTextField)GetChildAt(1);
        }
    }
}