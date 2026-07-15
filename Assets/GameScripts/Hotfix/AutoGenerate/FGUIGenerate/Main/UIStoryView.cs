/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Hotfix.UI.Generate.Main
{
    public partial class UIStoryView : GComponent
    {
        public GGraph m_back;
        public UIScrollText1 m_storyScroll;
        public GTextField m_skipHint;
        public GGraph m_skipBtn;
        public Transition m_show;
        public Transition m_hide;
        public const string URL = "ui://q0la9fq0jxy51n";

        public static UIStoryView CreateInstance()
        {
            return (UIStoryView)UIPackage.CreateObject("Main", "StoryView");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_back = (GGraph)GetChildAt(0);
            m_storyScroll = (UIScrollText1)GetChildAt(1);
            m_skipHint = (GTextField)GetChildAt(2);
            m_skipBtn = (GGraph)GetChildAt(3);
            m_show = GetTransitionAt(0);
            m_hide = GetTransitionAt(1);
        }
    }
}