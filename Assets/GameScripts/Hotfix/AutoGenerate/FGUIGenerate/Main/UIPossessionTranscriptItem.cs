/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Hotfix.UI.Generate.Main
{
    public partial class UIPossessionTranscriptItem : GComponent
    {
        public Controller m_side;
        public GGraph m_leftBubble;
        public GGraph m_rightBubble;
        public GTextField m_speakerLeft;
        public GTextField m_speakerRight;
        public GTextField m_textLeft;
        public GTextField m_textRight;
        public GTextField m_resultText;
        public const string URL = "ui://q0la9fq0phs003";

        public static UIPossessionTranscriptItem CreateInstance()
        {
            return (UIPossessionTranscriptItem)UIPackage.CreateObject("Main", "PossessionTranscriptItem");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_side = GetControllerAt(0);
            m_leftBubble = (GGraph)GetChildAt(0);
            m_rightBubble = (GGraph)GetChildAt(1);
            m_speakerLeft = (GTextField)GetChildAt(2);
            m_speakerRight = (GTextField)GetChildAt(3);
            m_textLeft = (GTextField)GetChildAt(4);
            m_textRight = (GTextField)GetChildAt(5);
            m_resultText = (GTextField)GetChildAt(6);
        }
    }
}