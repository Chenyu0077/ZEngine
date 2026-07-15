/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Hotfix.UI.Generate.Main
{
    public partial class UITimeCom : GComponent
    {
        public GGraph m_back;
        public GTextField m_title1;
        public GTextField m_Days;
        public GTextField m_title2;
        public GTextField m_SurvivalRatio;
        public const string URL = "ui://q0la9fq0k2bkv";

        public static UITimeCom CreateInstance()
        {
            return (UITimeCom)UIPackage.CreateObject("Main", "TimeCom");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_back = (GGraph)GetChildAt(0);
            m_title1 = (GTextField)GetChildAt(1);
            m_Days = (GTextField)GetChildAt(2);
            m_title2 = (GTextField)GetChildAt(3);
            m_SurvivalRatio = (GTextField)GetChildAt(4);
        }
    }
}