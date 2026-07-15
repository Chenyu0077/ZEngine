/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Hotfix.UI.Generate.Main
{
    public partial class UIGameConfigView : GComponent
    {
        public GGraph m_mask;
        public GGraph m_back;
        public GRichTextField m_title1;
        public GTextField m_tip1;
        public GComboBox m_commonModel;
        public GRichTextField m_title2;
        public GTextField m_tip2;
        public GComboBox m_reflectModel;
        public GRichTextField m_title3;
        public GTextField m_tip3;
        public GComboBox m_registerNPCsModel;
        public GComboBox m_presetMode;
        public GButton m_btnMenual;
        public GButton m_btnLLM;
        public GRichTextField m_title4;
        public UIComBtn m_btnVillageLog;
        public GRichTextField m_title5;
        public GTextInput m_baseUrlInput;
        public GTextInput m_keyInput;
        public UIComBtn m_btnKeySend;
        public GButton m_BtnClose;
        public GGraph m_line;
        public GGraph m_line1;
        public GGraph m_line2;
        public GGraph m_line3;
        public const string URL = "ui://q0la9fq0wg5sp";

        public static UIGameConfigView CreateInstance()
        {
            return (UIGameConfigView)UIPackage.CreateObject("Main", "GameConfigView");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_mask = (GGraph)GetChildAt(0);
            m_back = (GGraph)GetChildAt(1);
            m_title1 = (GRichTextField)GetChildAt(2);
            m_tip1 = (GTextField)GetChildAt(3);
            m_commonModel = (GComboBox)GetChildAt(4);
            m_title2 = (GRichTextField)GetChildAt(5);
            m_tip2 = (GTextField)GetChildAt(6);
            m_reflectModel = (GComboBox)GetChildAt(7);
            m_title3 = (GRichTextField)GetChildAt(8);
            m_tip3 = (GTextField)GetChildAt(9);
            m_registerNPCsModel = (GComboBox)GetChildAt(11);
            m_presetMode = (GComboBox)GetChildAt(12);
            m_btnMenual = (GButton)GetChildAt(13);
            m_btnLLM = (GButton)GetChildAt(14);
            m_title4 = (GRichTextField)GetChildAt(15);
            m_btnVillageLog = (UIComBtn)GetChildAt(16);
            m_title5 = (GRichTextField)GetChildAt(17);
            m_baseUrlInput = (GTextInput)GetChildAt(20);
            m_keyInput = (GTextInput)GetChildAt(21);
            m_btnKeySend = (UIComBtn)GetChildAt(22);
            m_BtnClose = (GButton)GetChildAt(23);
            m_line = (GGraph)GetChildAt(24);
            m_line1 = (GGraph)GetChildAt(25);
            m_line2 = (GGraph)GetChildAt(26);
            m_line3 = (GGraph)GetChildAt(27);
        }
    }
}