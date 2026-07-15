/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Hotfix.UI.Generate.Main
{
    public partial class UIStatusPanel : GComponent
    {
        public Controller m_bagCtrol;
        public GGraph m_back;
        public UIStatusBarItem m_health;
        public UIStatusBarItem m_energy;
        public UIStatusBarItem m_food;
        public UIStatusBarItem m_mood;
        public UIStatusBarItem m_rest;
        public UIStatusBarItem m_play;
        public GGraph m_bagBg;
        public GGraph m_bagBgCollapsed;
        public GTextField m_bagArrowExpand;
        public GTextField m_bagArrowCollapse;
        public GTextField m_bagIcon;
        public GTextField m_bagTitle;
        public GTextField m_bagCount;
        public GGraph m_bagToggleBtn;
        public GGraph m_bagDivider;
        public GList m_itemList;
        public const string URL = "ui://q0la9fq0jxy51i";

        public static UIStatusPanel CreateInstance()
        {
            return (UIStatusPanel)UIPackage.CreateObject("Main", "StatusPanel");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_bagCtrol = GetControllerAt(0);
            m_back = (GGraph)GetChildAt(0);
            m_health = (UIStatusBarItem)GetChildAt(1);
            m_energy = (UIStatusBarItem)GetChildAt(2);
            m_food = (UIStatusBarItem)GetChildAt(3);
            m_mood = (UIStatusBarItem)GetChildAt(4);
            m_rest = (UIStatusBarItem)GetChildAt(5);
            m_play = (UIStatusBarItem)GetChildAt(6);
            m_bagBg = (GGraph)GetChildAt(7);
            m_bagBgCollapsed = (GGraph)GetChildAt(8);
            m_bagArrowExpand = (GTextField)GetChildAt(9);
            m_bagArrowCollapse = (GTextField)GetChildAt(10);
            m_bagIcon = (GTextField)GetChildAt(11);
            m_bagTitle = (GTextField)GetChildAt(12);
            m_bagCount = (GTextField)GetChildAt(13);
            m_bagToggleBtn = (GGraph)GetChildAt(14);
            m_bagDivider = (GGraph)GetChildAt(15);
            m_itemList = (GList)GetChildAt(16);
        }
    }
}