/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Hotfix.UI.Generate.Main
{
    public partial class UIActionSelectPanel : GComponent
    {
        public GGraph m_back;
        public GTextField m_titleIcon;
        public GTextField m_title;
        public GGraph m_divider;
        public GList m_btnList;
        public GGraph m_bottomDivider;
        public UIComBtn m_btnExcuteTalk;
        public UIComBtn m_btnTalkOver;
        public GTextField m_childActionTip;
        public const string URL = "ui://q0la9fq0jxy51k";

        public static UIActionSelectPanel CreateInstance()
        {
            return (UIActionSelectPanel)UIPackage.CreateObject("Main", "ActionSelectPanel");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_back = (GGraph)GetChildAt(0);
            m_titleIcon = (GTextField)GetChildAt(1);
            m_title = (GTextField)GetChildAt(2);
            m_divider = (GGraph)GetChildAt(3);
            m_btnList = (GList)GetChildAt(4);
            m_bottomDivider = (GGraph)GetChildAt(5);
            m_btnExcuteTalk = (UIComBtn)GetChildAt(6);
            m_btnTalkOver = (UIComBtn)GetChildAt(7);
            m_childActionTip = (GTextField)GetChildAt(8);
        }
    }
}