/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Hotfix.UI.Generate.Cost
{
    public partial class UICostView : GComponent
    {
        public Controller m_stateCtrl;
        public GGraph m_mainBg;
        public UICostHeaderPanel m_headerPanel;
        public UICostBasicInfoPanel m_basicInfoPanel;
        public UICostCumulativePanel m_cumulativePanel;
        public UICostAveragePanel m_averagePanel;
        public UICostVillagePanel m_villagePanel;
        public UICostCachePanel m_cachePanel;
        public UICostModelsPanel m_modelsPanel;
        public Transition m_show;
        public Transition m_hide;
        public Transition m_dataUpdate;
        public const string URL = "ui://cost_pkgcost_main";

        public static UICostView CreateInstance()
        {
            return (UICostView)UIPackage.CreateObject("Cost", "CostView");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_stateCtrl = GetControllerAt(0);
            m_mainBg = (GGraph)GetChildAt(0);
            m_headerPanel = (UICostHeaderPanel)GetChildAt(1);
            m_basicInfoPanel = (UICostBasicInfoPanel)GetChildAt(2);
            m_cumulativePanel = (UICostCumulativePanel)GetChildAt(3);
            m_averagePanel = (UICostAveragePanel)GetChildAt(4);
            m_villagePanel = (UICostVillagePanel)GetChildAt(5);
            m_cachePanel = (UICostCachePanel)GetChildAt(6);
            m_modelsPanel = (UICostModelsPanel)GetChildAt(7);
            m_show = GetTransitionAt(0);
            m_hide = GetTransitionAt(1);
            m_dataUpdate = GetTransitionAt(2);
        }
    }
}