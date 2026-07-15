/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Hotfix.UI.Generate.Main
{
    public partial class UINPCMainView : GComponent
    {
        public Controller m_tagCtrol;
        public Controller m_popCtrol;
        public GGraph m_mask;
        public GGraph m_currentActionBg;
        public GTextField m_currentActionTitle;
        public UIScrollText m_currentActionContent;
        public GGraph m_npcPlanBg;
        public GTextField m_npcPlanTitle;
        public UIScrollText m_npcPlanContent;
        public GGraph m_bodyBg;
        public GTextField m_bodyTitle;
        public GTextField m_bodyContent;
        public GGraph m_attributeBg;
        public GTextField m_attributeTitle;
        public GGraph m_contentBg;
        public GButton m_basicBtn;
        public GButton m_abilityBtn;
        public GButton m_emotionBtn;
        public GButton m_prospectBtn;
        public GButton m_assetsBtn;
        public GButton m_socialBtn;
        public GButton m_tagsBtn;
        public UIScrollText m_basicContent;
        public UIScrollText m_abilityContent;
        public UIScrollText m_emotionContent;
        public UIScrollText m_prospectContent;
        public UIScrollText m_assetsContent;
        public UIScrollText m_socialContent;
        public UIScrollText m_tagsContent;
        public GLoader m_btnPop;
        public GComboBox m_NPCCombox;
        public GTextField m_followTitle;
        public GButton m_btnFollow;
        public const string URL = "ui://q0la9fq0o6bcg";

        public static UINPCMainView CreateInstance()
        {
            return (UINPCMainView)UIPackage.CreateObject("Main", "NPCMainView");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_tagCtrol = GetControllerAt(0);
            m_popCtrol = GetControllerAt(1);
            m_mask = (GGraph)GetChildAt(0);
            m_currentActionBg = (GGraph)GetChildAt(1);
            m_currentActionTitle = (GTextField)GetChildAt(2);
            m_currentActionContent = (UIScrollText)GetChildAt(3);
            m_npcPlanBg = (GGraph)GetChildAt(4);
            m_npcPlanTitle = (GTextField)GetChildAt(5);
            m_npcPlanContent = (UIScrollText)GetChildAt(6);
            m_bodyBg = (GGraph)GetChildAt(7);
            m_bodyTitle = (GTextField)GetChildAt(8);
            m_bodyContent = (GTextField)GetChildAt(9);
            m_attributeBg = (GGraph)GetChildAt(10);
            m_attributeTitle = (GTextField)GetChildAt(11);
            m_contentBg = (GGraph)GetChildAt(12);
            m_basicBtn = (GButton)GetChildAt(13);
            m_abilityBtn = (GButton)GetChildAt(14);
            m_emotionBtn = (GButton)GetChildAt(15);
            m_prospectBtn = (GButton)GetChildAt(16);
            m_assetsBtn = (GButton)GetChildAt(17);
            m_socialBtn = (GButton)GetChildAt(18);
            m_tagsBtn = (GButton)GetChildAt(19);
            m_basicContent = (UIScrollText)GetChildAt(20);
            m_abilityContent = (UIScrollText)GetChildAt(21);
            m_emotionContent = (UIScrollText)GetChildAt(22);
            m_prospectContent = (UIScrollText)GetChildAt(23);
            m_assetsContent = (UIScrollText)GetChildAt(24);
            m_socialContent = (UIScrollText)GetChildAt(25);
            m_tagsContent = (UIScrollText)GetChildAt(26);
            m_btnPop = (GLoader)GetChildAt(27);
            m_NPCCombox = (GComboBox)GetChildAt(28);
            m_followTitle = (GTextField)GetChildAt(29);
            m_btnFollow = (GButton)GetChildAt(30);
        }
    }
}