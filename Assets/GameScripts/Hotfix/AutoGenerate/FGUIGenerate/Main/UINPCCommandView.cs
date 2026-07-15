/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Hotfix.UI.Generate.Main
{
    public partial class UINPCCommandView : GComponent
    {
        public GGraph m_back;
        public GGraph m_listBack;
        public GTextField m_Title;
        public GList m_HistoryList;
        public GButton m_BtnSend;
        public GRichTextField m_ResultText;
        public GButton m_BtnClose;
        public GGraph m_inputback;
        public GTextInput m_Input;
        public GGraph m_hintback;
        public UIScrollText1 m_HintText;
        public const string URL = "ui://q0la9fq0ghhpk";

        public static UINPCCommandView CreateInstance()
        {
            return (UINPCCommandView)UIPackage.CreateObject("Main", "NPCCommandView");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_back = (GGraph)GetChildAt(0);
            m_listBack = (GGraph)GetChildAt(1);
            m_Title = (GTextField)GetChildAt(2);
            m_HistoryList = (GList)GetChildAt(3);
            m_BtnSend = (GButton)GetChildAt(4);
            m_ResultText = (GRichTextField)GetChildAt(5);
            m_BtnClose = (GButton)GetChildAt(6);
            m_inputback = (GGraph)GetChildAt(7);
            m_Input = (GTextInput)GetChildAt(8);
            m_hintback = (GGraph)GetChildAt(9);
            m_HintText = (UIScrollText1)GetChildAt(10);
        }
    }
}