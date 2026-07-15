/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Hotfix.UI.Generate.Main
{
    public partial class UIVillageLogView : GComponent
    {
        public GGraph m_mask;
        public GGraph m_back;
        public UIScrollText m_content;
        public const string URL = "ui://q0la9fq0wg5st";

        public static UIVillageLogView CreateInstance()
        {
            return (UIVillageLogView)UIPackage.CreateObject("Main", "VillageLogView");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_mask = (GGraph)GetChildAt(0);
            m_back = (GGraph)GetChildAt(1);
            m_content = (UIScrollText)GetChildAt(2);
        }
    }
}