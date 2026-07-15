using UnityEditor;
using UnityEngine;

namespace GameScripts.Editor
{
    /// <summary>
    /// 通用工具面板窗口，包含多种实用工具
    /// </summary>
    public class ToolsPanelWindow : EditorWindow
    {
        private int _selectedTab;
        private readonly string[] _tabNames = { "Sprite渲染层级", "MVC脚本生成" };

        private SpriteRendererSortingTool _spriteSortingTool;
        private MVCGeneratorTool _mvcGeneratorTool;

        [MenuItem("ZEngineTools/工具面板")]
        private static void OpenWindow()
        {
            var win = GetWindow<ToolsPanelWindow>("工具面板");
            win.minSize = new Vector2(420, 300);
            win.Show();
        }

        private void OnEnable()
        {
            _spriteSortingTool = new SpriteRendererSortingTool();
            _mvcGeneratorTool = new MVCGeneratorTool();
        }

        private void OnGUI()
        {
            _selectedTab = GUILayout.Toolbar(_selectedTab, _tabNames);
            GUILayout.Space(8);

            switch (_selectedTab)
            {
                case 0:
                    _spriteSortingTool.OnGUI();
                    break;
                case 1:
                    _mvcGeneratorTool.OnGUI();
                    break;
            }
        }
    }
}
