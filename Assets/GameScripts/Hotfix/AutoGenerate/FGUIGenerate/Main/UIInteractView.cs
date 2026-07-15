/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Hotfix.UI.Generate.Main
{
    public partial class UIInteractView : GComponent
    {
        public Controller m_tab;
        public Controller m_dialogPop;
        public Controller m_popCtrol;
        public Controller m_comCtrol;
        public Controller m_socialActionCtrol;
        public GGraph m_back;
        public GTextField m_iconTitle;
        public GTextField m_titleLabel;
        public GGraph m_tabDivider;
        public GGraph m_tabUnderline;
        public UIComBtn m_TabMove;
        public UIComBtn m_TabWork;
        public UIComBtn m_TabSurvive;
        public UIComBtn m_TabSocial;
        public UIComBtn m_TabObserve;
        public UIComBtn m_TabConflict;
        public GGraph m_tipBack;
        public GTextField m_toolLabel;
        public GComboBox m_ToolComboBox;
        public UIScrollText1 m_tipInfoCom;
        public GTextField m_moveTip;
        public GComboBox m_MoveComboBox;
        public GTextField m_farmTip;
        public GGraph m_farmInputback;
        public GTextInput m_farmInput;
        public GTextField m_surviceTip;
        public GComboBox m_SurviceComboBox;
        public GTextField m_surviceTip1;
        public GGraph m_surviceInputback;
        public GTextInput m_surviceInput;
        public GTextField m_socialTip;
        public GComboBox m_SocialComboBox;
        public GTextField m_socialTip1;
        public GGraph m_socialInputback;
        public GTextInput m_socialInput;
        public GTextField m_socialTip2;
        public GComboBox m_itemComboBox;
        public GGraph m_socialInputback2;
        public GTextInput m_socialInput2;
        public GTextField m_observeTip;
        public GGraph m_ovserveInputback;
        public GTextInput m_ovserveInput;
        public GTextField m_conflictTip;
        public GComboBox m_ConflictComboBox;
        public GTextField m_attactTip;
        public GComboBox m_attackComboBox;
        public GTextField m_conflictInputTitle;
        public GGraph m_cinflictInputback;
        public GTextInput m_cinflictInput;
        public GGraph m_btnDivider;
        public GGraph m_btnExecBack;
        public UIComBtn m_BtnExec;
        public UIComBtn m_BtnIdle;
        public GLoader m_btnPop;
        public UIActionSelectPanel m_socialActionPanel;
        public GGraph m_dialogback;
        public GGraph m_dialogback1;
        public GTextField m_dialogTitle;
        public GGraph m_dialogPopBtn;
        public UIScrollText1 m_history;
        public UIStatusPanel m_statusPanel;
        public GGraph m_mask;
        public const string URL = "ui://q0la9fq0jxy51f";

        public static UIInteractView CreateInstance()
        {
            return (UIInteractView)UIPackage.CreateObject("Main", "InteractView");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_tab = GetControllerAt(0);
            m_dialogPop = GetControllerAt(1);
            m_popCtrol = GetControllerAt(2);
            m_comCtrol = GetControllerAt(3);
            m_socialActionCtrol = GetControllerAt(4);
            m_back = (GGraph)GetChildAt(0);
            m_iconTitle = (GTextField)GetChildAt(1);
            m_titleLabel = (GTextField)GetChildAt(2);
            m_tabDivider = (GGraph)GetChildAt(3);
            m_tabUnderline = (GGraph)GetChildAt(4);
            m_TabMove = (UIComBtn)GetChildAt(5);
            m_TabWork = (UIComBtn)GetChildAt(6);
            m_TabSurvive = (UIComBtn)GetChildAt(7);
            m_TabSocial = (UIComBtn)GetChildAt(8);
            m_TabObserve = (UIComBtn)GetChildAt(9);
            m_TabConflict = (UIComBtn)GetChildAt(10);
            m_tipBack = (GGraph)GetChildAt(11);
            m_toolLabel = (GTextField)GetChildAt(12);
            m_ToolComboBox = (GComboBox)GetChildAt(13);
            m_tipInfoCom = (UIScrollText1)GetChildAt(14);
            m_moveTip = (GTextField)GetChildAt(15);
            m_MoveComboBox = (GComboBox)GetChildAt(16);
            m_farmTip = (GTextField)GetChildAt(17);
            m_farmInputback = (GGraph)GetChildAt(18);
            m_farmInput = (GTextInput)GetChildAt(19);
            m_surviceTip = (GTextField)GetChildAt(20);
            m_SurviceComboBox = (GComboBox)GetChildAt(21);
            m_surviceTip1 = (GTextField)GetChildAt(22);
            m_surviceInputback = (GGraph)GetChildAt(23);
            m_surviceInput = (GTextInput)GetChildAt(24);
            m_socialTip = (GTextField)GetChildAt(25);
            m_SocialComboBox = (GComboBox)GetChildAt(26);
            m_socialTip1 = (GTextField)GetChildAt(27);
            m_socialInputback = (GGraph)GetChildAt(28);
            m_socialInput = (GTextInput)GetChildAt(29);
            m_socialTip2 = (GTextField)GetChildAt(30);
            m_itemComboBox = (GComboBox)GetChildAt(31);
            m_socialInputback2 = (GGraph)GetChildAt(32);
            m_socialInput2 = (GTextInput)GetChildAt(34);
            m_observeTip = (GTextField)GetChildAt(35);
            m_ovserveInputback = (GGraph)GetChildAt(36);
            m_ovserveInput = (GTextInput)GetChildAt(37);
            m_conflictTip = (GTextField)GetChildAt(38);
            m_ConflictComboBox = (GComboBox)GetChildAt(39);
            m_attactTip = (GTextField)GetChildAt(40);
            m_attackComboBox = (GComboBox)GetChildAt(41);
            m_conflictInputTitle = (GTextField)GetChildAt(42);
            m_cinflictInputback = (GGraph)GetChildAt(43);
            m_cinflictInput = (GTextInput)GetChildAt(44);
            m_btnDivider = (GGraph)GetChildAt(45);
            m_btnExecBack = (GGraph)GetChildAt(46);
            m_BtnExec = (UIComBtn)GetChildAt(47);
            m_BtnIdle = (UIComBtn)GetChildAt(48);
            m_btnPop = (GLoader)GetChildAt(49);
            m_socialActionPanel = (UIActionSelectPanel)GetChildAt(50);
            m_dialogback = (GGraph)GetChildAt(51);
            m_dialogback1 = (GGraph)GetChildAt(52);
            m_dialogTitle = (GTextField)GetChildAt(53);
            m_dialogPopBtn = (GGraph)GetChildAt(54);
            m_history = (UIScrollText1)GetChildAt(55);
            m_statusPanel = (UIStatusPanel)GetChildAt(57);
            m_mask = (GGraph)GetChildAt(58);
        }
    }
}