/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Hotfix.UI.Generate.Main
{
    public partial class UILoadingView : GComponent
    {
        public GGraph m_back;
        public GTextField m_ProgressValue;
        public GSlider m_ProgressBar;
        public GTextField m_TipText;
        public const string URL = "ui://q0la9fq0ppje5";

        public static UILoadingView CreateInstance()
        {
            return (UILoadingView)UIPackage.CreateObject("Main", "LoadingView");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_back = (GGraph)GetChildAt(0);
            m_ProgressValue = (GTextField)GetChildAt(1);
            m_ProgressBar = (GSlider)GetChildAt(2);
            m_TipText = (GTextField)GetChildAt(3);
        }
    }
}