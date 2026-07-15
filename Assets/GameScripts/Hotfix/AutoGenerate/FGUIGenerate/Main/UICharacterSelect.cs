/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Hotfix.UI.Generate.Main
{
    public partial class UICharacterSelect : GComponent
    {
        public GList m_cardList;
        public GTextField m_content;
        public UIComBtn m_confirmBtn;
        public const string URL = "ui://q0la9fq0k4vs7";

        public static UICharacterSelect CreateInstance()
        {
            return (UICharacterSelect)UIPackage.CreateObject("Main", "CharacterSelect");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_cardList = (GList)GetChildAt(0);
            m_content = (GTextField)GetChildAt(1);
            m_confirmBtn = (UIComBtn)GetChildAt(2);
        }
    }
}