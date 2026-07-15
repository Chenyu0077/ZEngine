/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Hotfix.UI.Generate.Main
{
    public partial class UIInventoryItem : GComponent
    {
        public GTextField m_dot;
        public GTextField m_itemName;
        public GTextField m_itemCount;
        public GTextField m_itemCategory;
        public const string URL = "ui://q0la9fq0jxy51g";

        public static UIInventoryItem CreateInstance()
        {
            return (UIInventoryItem)UIPackage.CreateObject("Main", "InventoryItem");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_dot = (GTextField)GetChildAt(0);
            m_itemName = (GTextField)GetChildAt(1);
            m_itemCount = (GTextField)GetChildAt(2);
            m_itemCategory = (GTextField)GetChildAt(3);
        }
    }
}