using System.IO;
using Hotfix.FuncModule;
using Main.FuncModule.Building;
using Main.FuncModule;
using UnityEditor;
using UnityEngine;

namespace GameScripts.Editor
{
    public class MapEditorWindow : EditorWindow
    {
        // ── 单例 + 菜单 ──────────────────────────────────────────────────
        private static MapEditorWindow _instance;

        [MenuItem("ZEngineTools/Map/GridMap Editor")]
        public static void Open()
        {
            _instance = GetWindow<MapEditorWindow>("Map Editor");
            _instance.minSize = new Vector2(340f, 500f);
            _instance.Show();
        }

        // ── 子模块 ───────────────────────────────────────────────────────
        private MapEditorCore        _core;
        private LayerManagerPanel    _layerPanel;
        private TilePalettePanel     _tilePanel;
        private CellInspectorPanel   _cellPanel;
        private SpawnPointPanel      _spawnPanel;
        private ObjectPalettePanel   _objectPanel;
        private ObjectInspectorPanel _objInspPanel;

        // ── Config ───────────────────────────────────────────────────────
        private MapEditorConfig _config;
        private const string    ConfigSearchKey = "MapEditorConfig";

        // ── UI 状态 ──────────────────────────────────────────────────────
        private Vector2 _rightScroll;
        private Vector2 _leftScroll;
        private int     _leftTab;   // 0=图层 1=瓦片 2=对象 3=出生点
        private bool    _overlayFoldout = false;
        private bool    _dataRefreshFoldout = false;
        private bool    _refreshOnlyDefault = true; // true=仅刷新默认值字段，false=全量覆盖
        private static readonly string[] LeftTabs = { "图层", "瓦片", "对象", "出生点" };

        // ── 编辑模式快捷键 ───────────────────────────────────────────────
        private static readonly KeyCode[] ModeKeys =
        {
            KeyCode.T, KeyCode.E, KeyCode.F,
            KeyCode.O, KeyCode.D, KeyCode.C,
            KeyCode.S, KeyCode.V,
        };

        // ─────────────────────────────────────────────────────────────────
        private void OnEnable()
        {
            _core         = new MapEditorCore();
            _layerPanel   = new LayerManagerPanel();
            _tilePanel    = new TilePalettePanel();
            _cellPanel    = new CellInspectorPanel();
            _spawnPanel   = new SpawnPointPanel();
            _objectPanel  = new ObjectPalettePanel();
            _objInspPanel = new ObjectInspectorPanel();

            LoadOrCreateConfig();

            SceneView.duringSceneGui += OnSceneGUI;
            EditorApplication.projectChanged += OnProjectChanged;
            MapEditorConfig.OnConfigChanged += OnConfigChanged;

            // 恢复上次打开的地图路径
            string lastPath = EditorPrefs.GetString("MapEditor_LastPath", "");
            if (!string.IsNullOrEmpty(lastPath) && File.Exists(lastPath))
                TryLoadMap(lastPath);
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            EditorApplication.projectChanged -= OnProjectChanged;
            MapEditorConfig.OnConfigChanged -= OnConfigChanged;
        }

        private void OnProjectChanged()
        {
            LoadOrCreateConfig();
            Repaint();
        }

        // 当前使用的 Config 在 Inspector 中被修改时自动刷新 MapData
        private void OnConfigChanged(MapEditorConfig changed)
        {
            // 只响应当前正在使用的那个配置
            if (changed != _config) return;
            if (_core?.MapData == null) return;

            _core.RefreshSchema();
            _core.RebuildLayerArrays();
            SceneView.RepaintAll();
            Repaint();
        }

        // ── Config 加载 ──────────────────────────────────────────────────
        private void LoadOrCreateConfig()
        {
            // 若当前已有 config 引用则保留，不自动覆盖用户的选择
            if (_config != null) return;

            // 尝试找到项目中任意一个 MapEditorConfig
            string[] guids = AssetDatabase.FindAssets($"t:{nameof(MapEditorConfig)}");
            if (guids.Length > 0)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                _config = AssetDatabase.LoadAssetAtPath<MapEditorConfig>(assetPath);
            }

            // 找不到则自动创建默认配置
            if (_config == null)
            {
                _config = CreateInstance<MapEditorConfig>();
                _config.InitializeDefaults();
                string dir = "Assets/Settings";
                if (!AssetDatabase.IsValidFolder(dir))
                    AssetDatabase.CreateFolder("Assets", "Settings");
                AssetDatabase.CreateAsset(_config, $"{dir}/MapEditorConfig.asset");
                AssetDatabase.SaveAssets();
                Debug.Log("[MapEditor] 已自动创建 MapEditorConfig.asset");
            }

            _core.Config = _config;

            if (_core.MapData == null)
                _core.NewMap(_config);
        }

        // ── 配置切换 ─────────────────────────────────────────────────────
        private void SwitchConfig(MapEditorConfig newConfig)
        {
            _config      = newConfig;
            _core.Config = newConfig;
            _core.RefreshSchema();
            _core.RebuildLayerArrays();
            SceneView.RepaintAll();
            Repaint();
        }

        // ─────────────────────────────────────────────────────────────────
        private void OnGUI()
        {
            DrawToolbar();
            EditorGUILayout.Space(2);

            EditorGUILayout.BeginHorizontal();
            DrawLeftPanel();
            DrawRightPanel();
            EditorGUILayout.EndHorizontal();

            HandleKeyboardShortcuts();

            if (GUI.changed)
                SceneView.RepaintAll();
        }

        // ── 顶部工具栏（两行）────────────────────────────────────────────
        private void DrawToolbar()
        {
            // ── 第一行：地图操作 ──────────────────────────────────────────
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("新建", EditorStyles.toolbarButton, GUILayout.Width(42)))
                CmdNew();
            if (GUILayout.Button("打开", EditorStyles.toolbarButton, GUILayout.Width(42)))
                CmdOpen();

            Color bg = GUI.backgroundColor;
            GUI.backgroundColor = _core.IsDirty ? new Color(1f, 0.8f, 0.3f) : bg;
            if (GUILayout.Button("保存", EditorStyles.toolbarButton, GUILayout.Width(42)))
                CmdSave();
            GUI.backgroundColor = bg;

            GUILayout.Space(6);
            if (GUILayout.Button("导出 JSON", EditorStyles.toolbarButton, GUILayout.Width(72)))
                CmdExport();
            if (GUILayout.Button("导入 JSON", EditorStyles.toolbarButton, GUILayout.Width(72)))
                CmdImport();

            GUILayout.FlexibleSpace();

            if (_core.MapData != null)
            {
                string dirty = _core.IsDirty ? " *" : "";
                GUILayout.Label($"{_core.MapData.mapName}{dirty}", EditorStyles.toolbarButton);
            }

            EditorGUILayout.EndHorizontal();

            // ── 第二行：配置选择 ──────────────────────────────────────────
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            GUILayout.Label("配置:", EditorStyles.toolbarButton, GUILayout.Width(32));

            var newConfig = (MapEditorConfig)EditorGUILayout.ObjectField(
                _config, typeof(MapEditorConfig), false,
                GUILayout.ExpandWidth(true), GUILayout.Height(18));

            if (newConfig != null && newConfig != _config)
                SwitchConfig(newConfig);

            if (GUILayout.Button("新建配置", EditorStyles.toolbarButton, GUILayout.Width(60)))
                CmdCreateConfig();

            if (GUILayout.Button("⚙ 查看", EditorStyles.toolbarButton, GUILayout.Width(50)))
            {
                if (_config != null) Selection.activeObject = _config;
            }

            EditorGUILayout.EndHorizontal();
        }

        // ── 左侧面板（60% 宽，单一外层 ScrollView 动态高度）─────────────
        private void DrawLeftPanel()
        {
            float leftW = position.width * 0.60f;
            EditorGUILayout.BeginVertical(GUILayout.Width(leftW));

            DrawModeToolbar(leftW);
            EditorGUILayout.Space(6);

            _leftTab = GUILayout.Toolbar(_leftTab, LeftTabs);
            EditorGUILayout.Space(4);

            // 固定区高度：toolbar1(18)+toolbar2(18)+space(2)+
            //             modeLabel(18)+modeRow1(26)+modeRow2(26)+space(6)+
            //             tabBar(22)+space(4) ≈ 140
            const float kFixed = 140f;
            float scrollH = Mathf.Max(60f, position.height - kFixed);

            switch (_leftTab)
            {
                case 0:
                    _leftScroll = EditorGUILayout.BeginScrollView(
                        _leftScroll, GUILayout.Height(scrollH));
                    _layerPanel.Draw(_core);
                    EditorGUILayout.Space(6);
                    DrawOverlayToggles();
                    EditorGUILayout.EndScrollView();
                    break;
                case 1:
                    _tilePanel.Draw(_core, (int)leftW);
                    break;
                case 2:
                    _leftScroll = EditorGUILayout.BeginScrollView(
                        _leftScroll, GUILayout.Height(scrollH));
                    _objectPanel.Draw(_core, (int)leftW);
                    EditorGUILayout.EndScrollView();
                    break;
                case 3:
                    _leftScroll = EditorGUILayout.BeginScrollView(
                        _leftScroll, GUILayout.Height(scrollH));
                    _spawnPanel.Draw(_core);
                    EditorGUILayout.EndScrollView();
                    break;
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawModeToolbar(float panelWidth)
        {
            // 行1：瓦片 3 个，行2：对象 3 个，行3：其他 3 个
            float btnW = Mathf.Floor((panelWidth - 10f) / 3f);

            EditorGUILayout.LabelField("编辑模式", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            DrawModeBtn("画笔 T", EditMode.TilePaint,    new Color(0.4f, 0.8f, 0.4f), btnW);
            DrawModeBtn("擦除 E", EditMode.TileErase,    new Color(0.9f, 0.5f, 0.5f), btnW);
            DrawModeBtn("填充 F", EditMode.TileFill,     new Color(0.8f, 0.7f, 0.3f), btnW);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            DrawModeBtn("放对 O", EditMode.ObjectPlace,  new Color(0.6f, 0.4f, 0.9f), btnW);
            DrawModeBtn("选对 A", EditMode.ObjectSelect, new Color(0.4f, 0.6f, 1.0f), btnW);
            DrawModeBtn("删对 D", EditMode.ObjectErase,  new Color(0.9f, 0.4f, 0.6f), btnW);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            DrawModeBtn("格子 C", EditMode.CellEdit,     new Color(0.4f, 0.7f, 1f),   btnW);
            DrawModeBtn("出生 S", EditMode.SpawnEdit,    new Color(0.3f, 0.8f, 0.8f), btnW);
            DrawModeBtn("选择 V", EditMode.Select,       Color.white,                  btnW);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawModeBtn(string label, EditMode mode, Color activeColor, float width)
        {
            bool isActive = _core.EditMode == mode;
            Color bg = GUI.backgroundColor;
            GUI.backgroundColor = isActive ? activeColor : new Color(0.3f, 0.3f, 0.3f);
            if (GUILayout.Button(label, GUILayout.Height(22), GUILayout.Width(width)))
                SetMode(mode);
            GUI.backgroundColor = bg;
        }

        private void DrawOverlayToggles()
        {
            _overlayFoldout = EditorGUILayout.Foldout(_overlayFoldout, "叠加层显示");
            if (_overlayFoldout)
                _cellPanel.DrawOverlayToggles(_core);
        }

        // ── 右侧检查器 ───────────────────────────────────────────────────
        private void DrawRightPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            // 禁用横向滚动：水平 scrollbar 使用 GUIStyle.none
            _rightScroll = GUILayout.BeginScrollView(
                _rightScroll, false, false, GUIStyle.none, GUI.skin.verticalScrollbar);

            DrawMapInfo();
            EditorGUILayout.Space(6);

            if (_core.EditMode == EditMode.Select && _core.HasSelection)
            {
                DrawSelectionTileFill();
                EditorGUILayout.Space(4);
                _cellPanel.DrawBatchEdit(_core);
            }
            else if (_core.EditMode == EditMode.ObjectPlace
                  || _core.EditMode == EditMode.ObjectErase
                  || _core.EditMode == EditMode.ObjectSelect)
            {
                _objInspPanel.Draw(_core);
            }
            else
            {
                _cellPanel.Draw(_core);
            }

            EditorGUILayout.Space(6);
            _cellPanel.DrawEmptyCellBatchEdit(_core);

            EditorGUILayout.Space(6);
            DrawObjectDataRefresh();

            EditorGUILayout.Space(6);
            DrawUndoRedo();

            GUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawMapInfo()
        {
            if (_core.MapData == null) return;
            EditorGUILayout.LabelField("地图信息", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUI.BeginChangeCheck();
            _core.MapData.mapId   = EditorGUILayout.TextField("Map ID", _core.MapData.mapId);
            _core.MapData.mapName = EditorGUILayout.TextField("名称",   _core.MapData.mapName);
            if (EditorGUI.EndChangeCheck()) _core.IsDirty = true;

            EditorGUILayout.LabelField($"尺寸: {_core.MapData.width} × {_core.MapData.height}");
            EditorGUILayout.LabelField($"格子大小: {_core.MapData.cellSize}");

            // 绑定配置显示
            string boundName = string.IsNullOrEmpty(_core.MapData.configPath)
                ? "未绑定（导出后自动绑定）"
                : System.IO.Path.GetFileNameWithoutExtension(_core.MapData.configPath);
            EditorGUILayout.LabelField("绑定配置", boundName, EditorStyles.miniLabel);

            EditorGUILayout.EndVertical();

            // ── 配置默认参数 vs 当前地图 对比 ────────────────────────────
            if (_config == null) return;

            bool widthDiff    = _core.MapData.width  != _config.defaultWidth;
            bool heightDiff   = _core.MapData.height != _config.defaultHeight;
            bool cellSizeDiff = !Mathf.Approximately(_core.MapData.cellSize, _config.defaultCellSize);

            if (!widthDiff && !heightDiff && !cellSizeDiff) return;

            EditorGUILayout.Space(4);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("与配置默认值的差异", EditorStyles.boldLabel);

            if (widthDiff)
                EditorGUILayout.LabelField($"宽度:  当前 {_core.MapData.width}  →  配置默认 {_config.defaultWidth}");
            if (heightDiff)
                EditorGUILayout.LabelField($"高度:  当前 {_core.MapData.height}  →  配置默认 {_config.defaultHeight}");
            if (cellSizeDiff)
                EditorGUILayout.LabelField($"格子大小:  当前 {_core.MapData.cellSize}  →  配置默认 {_config.defaultCellSize}");

            Color bg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(1f, 0.75f, 0.3f);
            if (GUILayout.Button("应用配置默认值到当前地图", GUILayout.Height(26)))
                TryApplyConfigDefaults();
            GUI.backgroundColor = bg;

            EditorGUILayout.EndVertical();
        }

        private void TryApplyConfigDefaults()
        {
            var map = _core.MapData;
            bool shrink = _config.defaultWidth < map.width || _config.defaultHeight < map.height;

            string warning = shrink
                ? $"将把地图从 {map.width}×{map.height} 调整为 {_config.defaultWidth}×{_config.defaultHeight}，格子大小改为 {_config.defaultCellSize}。\n\n缩小范围外的 Tile / 格子 / 对象数据将永久丢失。"
                : $"将把地图从 {map.width}×{map.height} 调整为 {_config.defaultWidth}×{_config.defaultHeight}，格子大小改为 {_config.defaultCellSize}。\n\n新增区域初始化为空格子。";

            if (!EditorUtility.DisplayDialog("应用配置默认值", warning, "确认应用", "取消"))
                return;

            _core.ResizeMap(_config.defaultWidth, _config.defaultHeight, _config.defaultCellSize);
            SceneView.RepaintAll();
            Repaint();
        }

        private void DrawUndoRedo()
        {
            EditorGUILayout.BeginHorizontal();
            Color bg = GUI.backgroundColor;
            GUI.backgroundColor = _core.CanUndo ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            if (GUILayout.Button("↩ 撤销", GUILayout.Height(22))) { _core.Undo(); SceneView.RepaintAll(); }
            GUI.backgroundColor = _core.CanRedo ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            if (GUILayout.Button("↪ 重做", GUILayout.Height(22))) { _core.Redo(); SceneView.RepaintAll(); }
            GUI.backgroundColor = bg;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(10);
        }

        // ── 对象数据刷新 ─────────────────────────────────────────────────
        private void DrawObjectDataRefresh()
        {
            if (_core.MapData == null) return;

            _dataRefreshFoldout = EditorGUILayout.Foldout(_dataRefreshFoldout, "对象数据刷新", true);
            if (!_dataRefreshFoldout) return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            var objects = _core.MapData.objects;
            if (objects == null || objects.Count == 0)
            {
                EditorGUILayout.LabelField("当前地图没有放置任何对象。", EditorStyles.miniLabel);
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.LabelField($"已放置对象：{objects.Count} 个", EditorStyles.miniLabel);

            _refreshOnlyDefault = EditorGUILayout.ToggleLeft(
                "仅刷新值为默认值（Other）的 objectType",
                _refreshOnlyDefault);

            EditorGUILayout.Space(2);

            // 预检：统计各操作影响数量
            int typeCount = 0, sizeCount = 0;
            string basePath = _core.Config?.objectPalettePath ?? "Assets/Resources/Prefabs/MapObjects";
            foreach (var obj in objects)
            {
                var prefab = LoadPrefabForObject(basePath, obj.prefabId);
                if (prefab == null) continue;
                var cfg = prefab.GetComponent<PlacedObjConfig>();
                if (cfg == null) continue;

                bool typeNeedsUpdate = _refreshOnlyDefault
                    ? obj.objectType == MapObjectType.Other
                    : obj.objectType != cfg.ObjectType;
                if (typeNeedsUpdate) typeCount++;

                bool sizeNeedsUpdate = obj.width != cfg.SizeX || obj.height != cfg.SizeY
                    || obj.footprintW != cfg.FootprintX || obj.footprintH != cfg.FootprintY;
                if (sizeNeedsUpdate) sizeCount++;
            }

            EditorGUILayout.LabelField($"objectType 待更新：{typeCount} 个", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"尺寸数据待更新：{sizeCount} 个", EditorStyles.miniLabel);

            EditorGUILayout.Space(4);

            Color bg = GUI.backgroundColor;

            GUI.backgroundColor = typeCount > 0 ? new Color(0.6f, 0.8f, 1f) : new Color(0.5f, 0.5f, 0.5f);
            if (GUILayout.Button($"刷新 objectType（{typeCount} 个）", GUILayout.Height(24)))
            {
                if (typeCount > 0)
                    ExecuteObjectDataRefresh(basePath, refreshType: true, refreshSize: false);
            }

            GUI.backgroundColor = sizeCount > 0 ? new Color(0.6f, 1f, 0.7f) : new Color(0.5f, 0.5f, 0.5f);
            if (GUILayout.Button($"刷新尺寸数据（{sizeCount} 个）", GUILayout.Height(24)))
            {
                if (sizeCount > 0)
                    ExecuteObjectDataRefresh(basePath, refreshType: false, refreshSize: true);
            }

            GUI.backgroundColor = (typeCount + sizeCount) > 0 ? new Color(1f, 0.85f, 0.4f) : new Color(0.5f, 0.5f, 0.5f);
            if (GUILayout.Button($"全部刷新（{typeCount + sizeCount} 处变更）", GUILayout.Height(24)))
            {
                if (typeCount + sizeCount > 0)
                    ExecuteObjectDataRefresh(basePath, refreshType: true, refreshSize: true);
            }

            GUI.backgroundColor = bg;
            EditorGUILayout.EndVertical();
        }

        private void ExecuteObjectDataRefresh(string basePath, bool refreshType, bool refreshSize)
        {
            _core.PushUndo();
            int changed = 0;
            foreach (var obj in _core.MapData.objects)
            {
                var prefab = LoadPrefabForObject(basePath, obj.prefabId);
                if (prefab == null) continue;
                var cfg = prefab.GetComponent<PlacedObjConfig>();
                if (cfg == null) continue;

                if (refreshType)
                {
                    bool typeNeedsUpdate = _refreshOnlyDefault
                        ? obj.objectType == MapObjectType.Other
                        : obj.objectType != cfg.ObjectType;
                    if (typeNeedsUpdate)
                    {
                        obj.objectType = cfg.ObjectType;
                        changed++;
                    }
                }

                if (refreshSize)
                {
                    if (obj.width != cfg.SizeX || obj.height != cfg.SizeY
                        || obj.footprintW != cfg.FootprintX || obj.footprintH != cfg.FootprintY)
                    {
                        obj.width      = cfg.SizeX;
                        obj.height     = cfg.SizeY;
                        obj.footprintW = cfg.FootprintX;
                        obj.footprintH = cfg.FootprintY;
                        changed++;
                    }
                }
            }

            if (changed > 0)
            {
                _core.IsDirty = true;
                SceneView.RepaintAll();
                Repaint();
                Debug.Log($"[MapEditor] 对象数据刷新完成，共更新 {changed} 处。");
            }
        }

        private static GameObject LoadPrefabForObject(string basePath, string prefabId)
        {
            // 先尝试直接路径
            string assetPath = $"{basePath}/{prefabId}.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab != null) return prefab;

            // prefabId 可能只是文件名（无子目录）
            string fileName = prefabId;
            int slash = prefabId.LastIndexOf('/');
            if (slash >= 0) fileName = prefabId.Substring(slash + 1);

            prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{basePath}/{fileName}.prefab");
            if (prefab != null) return prefab;

            // 在常见子目录中查找
            string[] subDirs = { "Building", "Items", "Plant", "Decor", "Nature" };
            foreach (var dir in subDirs)
            {
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{basePath}/{dir}/{fileName}.prefab");
                if (prefab != null) return prefab;
            }

            return null;
        }

        private void DrawSelectionTileFill()
        {
            EditorGUILayout.LabelField("选区瓦片填充", EditorStyles.boldLabel);

            var layerDef = _core.Config?.layers?.Find(l => l.id == _core.ActiveLayerId);
            string layerName = layerDef != null ? layerDef.displayName : _core.ActiveLayerId;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField($"目标图层: {layerName}", EditorStyles.miniLabel);

            var tileEntry = _core.Config?.FindTileEntry(_core.SelectedTileId);
            if (tileEntry != null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("当前瓦片:", EditorStyles.miniLabel);
                if (tileEntry.sprite != null)
                {
                    var preview = AssetPreview.GetAssetPreview(tileEntry.sprite) ?? AssetPreview.GetMiniThumbnail(tileEntry.sprite);
                    GUILayout.Box(preview, GUILayout.Width(28), GUILayout.Height(28));
                }
                EditorGUILayout.LabelField(tileEntry.tileName, EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.LabelField("当前瓦片: (空/未选择)", EditorStyles.miniLabel);
            }

            EditorGUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();
            Color bg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
            if (GUILayout.Button("填充当前瓦片", GUILayout.Height(24)))
            {
                _core.FillSelectionWithTile(_core.ActiveLayerId, _core.SelectedTileId);
                SceneView.RepaintAll();
                Repaint();
            }
            GUI.backgroundColor = new Color(0.9f, 0.5f, 0.5f);
            if (GUILayout.Button("擦除选区瓦片", GUILayout.Height(24)))
            {
                _core.FillSelectionWithTile(_core.ActiveLayerId, -1);
                SceneView.RepaintAll();
                Repaint();
            }
            GUI.backgroundColor = bg;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void SetMode(EditMode mode)
        {
            _core.EditMode = mode;
            if (mode != EditMode.CellEdit)
                _core.SelectedCell = new Vector2Int(-1, -1);
            if (mode != EditMode.Select)
                _core.ClearSelection();
            if (mode != EditMode.ObjectPlace && mode != EditMode.ObjectErase && mode != EditMode.ObjectSelect)
                _core.SelectedObject = null;
            Repaint();
            SceneView.RepaintAll();
        }


        // ── 键盘快捷键 ───────────────────────────────────────────────────
        private void HandleKeyboardShortcuts()
        {
            var e = Event.current;
            if (e.type != EventType.KeyDown) return;

            switch (e.keyCode)
            {
                case KeyCode.T: SetMode(EditMode.TilePaint);   e.Use(); break;
                case KeyCode.E: SetMode(EditMode.TileErase);   e.Use(); break;
                case KeyCode.F: SetMode(EditMode.TileFill);    e.Use(); break;
                case KeyCode.O: SetMode(EditMode.ObjectPlace);  e.Use(); break;
                case KeyCode.A: SetMode(EditMode.ObjectSelect); e.Use(); break;
                case KeyCode.D: SetMode(EditMode.ObjectErase);  e.Use(); break;
                case KeyCode.C: SetMode(EditMode.CellEdit);    e.Use(); break;
                case KeyCode.S: if (e.control) CmdSave(); else SetMode(EditMode.SpawnEdit); e.Use(); break;
                case KeyCode.V: SetMode(EditMode.Select);      e.Use(); break;
                case KeyCode.Z: if (e.control) { _core.Undo(); SceneView.RepaintAll(); e.Use(); } break;
                case KeyCode.Y: if (e.control) { _core.Redo(); SceneView.RepaintAll(); e.Use(); } break;
            }
        }

        // ── Scene View 回调 ──────────────────────────────────────────────
        private void OnSceneGUI(SceneView sv)
        {
            if (_core?.MapData == null) return;

            // 先更新悬停坐标，再绘制，避免单帧延迟
            HandleSceneInput(sv);
            GridRenderer.DrawAll(_core, sv);

            if (_core.IsDirty) Repaint();
        }

        private void HandleSceneInput(SceneView sv)
        {
            Event e = Event.current;

            // 计算鼠标对应的格子坐标
            Ray      ray    = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            Vector3  hitPos = HitXYPlane(ray, GridRenderer.MapOrigin.z);
            var      coord  = _core.WorldToGrid(hitPos, GridRenderer.MapOrigin);

            // 悬停格子变化时请求重绘（覆盖 MouseMove 不自动触发重绘的情况）
            if (coord != _core.HoveredCell)
            {
                _core.HoveredCell = coord;
                sv.Repaint();
            }

            // Select 模式的 MouseUp 需要在过滤前处理
            if (e.type == EventType.MouseUp && e.button == 0 && _core.EditMode == EditMode.Select)
            {
                if (_core.IsSelecting)
                {
                    _core.UpdateSelection(coord.x, coord.y);
                    _core.EndSelection();
                    Repaint();
                    e.Use();
                }
                return;
            }

            // 只处理鼠标点击 / 拖拽
            if (e.type != EventType.MouseDown && e.type != EventType.MouseDrag) return;
            if (!_core.IsValidCell(coord.x, coord.y)) return;

            switch (_core.EditMode)
            {
                case EditMode.TilePaint:
                    if (e.button == 0)
                    {
                        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
                        if (_core.BrushTiles != null && _core.BrushTiles.Count > 1)
                            _core.StampBrush(_core.ActiveLayerId, coord.x, coord.y);
                        else
                            _core.SetTile(_core.ActiveLayerId, coord.x, coord.y, _core.SelectedTileId);
                        e.Use();
                    }
                    break;

                case EditMode.TileErase:
                    if (e.button == 0)
                    {
                        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
                        _core.SetTile(_core.ActiveLayerId, coord.x, coord.y, -1);
                        e.Use();
                    }
                    break;

                case EditMode.TileFill:
                    if (e.button == 0 && e.type == EventType.MouseDown)
                    {
                        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
                        _core.FloodFill(_core.ActiveLayerId, coord.x, coord.y, _core.SelectedTileId);
                        e.Use();
                    }
                    break;

                case EditMode.CellEdit:
                    if (e.button == 0 && e.type == EventType.MouseDown)
                    {
                        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
                        _core.SelectedCell = coord;
                        _leftTab = 0; // 切到图层标签，右侧 cell 检查器会更新
                        Repaint();
                        e.Use();
                    }
                    break;

                case EditMode.ObjectPlace:
                    if (e.type == EventType.MouseDown)
                    {
                        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
                        if (e.button == 0)
                        {
                            if (string.IsNullOrEmpty(_core.SelectedPrefabId))
                            {
                                _leftTab = 2;
                                Repaint();
                            }
                            else
                            {
                                int fpW = _core.PlacementFootprintW > 0 ? _core.PlacementFootprintW : _core.PlacementWidth;
                                int fpH = _core.PlacementFootprintH > 0 ? _core.PlacementFootprintH : _core.PlacementHeight;
                                if (!_core.IsFootprintValid(coord.x, coord.y, fpW, fpH))
                                {
                                    Debug.LogWarning(
                                        $"[MapEditor] 无法放置：({coord.x},{coord.y}) 占地 " +
                                        $"{fpW}×{fpH} 越界或与已有对象重叠。");
                                }
                                else
                                {
                                    var newObj = new ObjectSaveData
                                    {
                                        instanceId   = $"obj_{System.Guid.NewGuid().ToString("N")[..8]}",
                                        prefabId     = _core.SelectedPrefabId,
                                        objectType   = _core.PlacementObjectType,
                                        x            = coord.x,
                                        y            = coord.y,
                                        width        = _core.PlacementWidth,
                                        height       = _core.PlacementHeight,
                                        footprintW   = _core.PlacementFootprintW,
                                        footprintH   = _core.PlacementFootprintH,
                                    };
                                    _core.AddObject(newObj);
                                    _core.SelectedObject = newObj;
                                    Repaint();
                                }
                            }
                        }
                        else if (e.button == 1)
                        {
                            // 右键：选中对象以编辑 props
                            var hit = _core.MapData.objects.Find(o =>
                                coord.x >= o.x && coord.x < o.x + o.width &&
                                coord.y >= o.y && coord.y < o.y + o.height);
                            _core.SelectedObject = hit;
                            Repaint();
                        }
                        e.Use();
                    }
                    break;

                case EditMode.ObjectSelect:
                    if (e.button == 0 && e.type == EventType.MouseDown)
                    {
                        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
                        var hit = _core.MapData.objects.Find(o =>
                            coord.x >= o.x && coord.x < o.x + o.width &&
                            coord.y >= o.y && coord.y < o.y + o.height);
                        _core.SelectedObject = hit;
                        Repaint();
                        e.Use();
                    }
                    break;

                case EditMode.ObjectErase:
                    if (e.button == 0 && e.type == EventType.MouseDown)
                    {
                        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
                        var obj = _core.MapData.objects.Find(o =>
                            coord.x >= o.x && coord.x < o.x + o.width &&
                            coord.y >= o.y && coord.y < o.y + o.height);
                        if (obj != null)
                        {
                            if (_core.SelectedObject == obj) _core.SelectedObject = null;
                            _core.RemoveObject(obj);
                            Repaint();
                        }
                        e.Use();
                    }
                    break;

                case EditMode.Select:
                    HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
                    if (e.button == 0)
                    {
                        if (e.type == EventType.MouseDown)
                        {
                            _core.StartSelection(coord.x, coord.y);
                            e.Use();
                        }
                        else if (e.type == EventType.MouseDrag)
                        {
                            _core.UpdateSelection(coord.x, coord.y);
                            e.Use();
                        }
                    }
                    break;

                case EditMode.SpawnEdit:
                    if (e.button == 0 && e.type == EventType.MouseDown)
                    {
                        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
                        var pending = _core.PendingSpawnPoint ?? new SpawnPointSaveData();
                        var sp = new SpawnPointSaveData
                        {
                            id     = $"spawn_{System.Guid.NewGuid():N}",
                            x      = coord.x,
                            y      = coord.y,
                            type   = pending.type,
                            npcId  = pending.npcId,
                            facing = pending.facing,
                        };
                        _core.AddSpawnPoint(sp);
                        e.Use();
                    }
                    break;
            }

            SceneView.RepaintAll();
        }

        // ── 菜单命令 ─────────────────────────────────────────────────────
        private void CmdNew()
        {
            if (_core.IsDirty && !EditorUtility.DisplayDialog("未保存", "当前地图有未保存的更改，是否丢弃？", "丢弃", "取消"))
                return;
            _core.NewMap(_config);
            SceneView.RepaintAll();
        }

        private void CmdOpen()
        {
            string path = EditorUtility.OpenFilePanel("打开地图", Application.dataPath, "json");
            if (string.IsNullOrEmpty(path)) return;
            TryLoadMap(path);
        }

        private void TryLoadMap(string path)
        {
            var data = MapDataSerializer.Import(path, _config);
            if (data == null) return;

            // ── 配置绑定检测 ──────────────────────────────────────────────
            if (!string.IsNullOrEmpty(data.configPath))
            {
                var boundConfig = AssetDatabase.LoadAssetAtPath<MapEditorConfig>(data.configPath);
                if (boundConfig != null && boundConfig != _config)
                {
                    string currentName = _config != null
                        ? System.IO.Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(_config))
                        : "(无)";
                    string boundName = System.IO.Path.GetFileNameWithoutExtension(data.configPath);

                    int choice = EditorUtility.DisplayDialogComplex(
                        "配置不匹配",
                        $"此地图绑定的配置：{boundName}\n当前使用的配置：{currentName}\n\n建议切换到地图原配置以保证数据一致。",
                        "切换到地图配置",    // 0
                        "取消",              // 1
                        "强制用当前配置"     // 2
                    );

                    if (choice == 1) return;           // 取消，不打开
                    if (choice == 0) SwitchConfig(boundConfig);  // 切换配置再打开
                    // choice == 2：继续，用当前配置强制打开
                }
            }

            _core.LoadMapData(data, _config);
            _core.CurrentPath = path;
            EditorPrefs.SetString("MapEditor_LastPath", path);
            SceneView.RepaintAll();
        }

        private void CmdSave()
        {
            if (string.IsNullOrEmpty(_core.CurrentPath))
                CmdExport();
            else
            {
                BindConfigPath();
                MapDataSerializer.Export(_core.MapData, _config, _core.CurrentPath);
            }
            _core.IsDirty = false;
        }

        private void CmdExport()
        {
            string fileName = $"{_core.MapData.mapId}.json";
            string path     = EditorUtility.SaveFilePanel("导出地图", Application.dataPath, fileName, "json");
            if (string.IsNullOrEmpty(path)) return;

            _core.RefreshSchema();
            BindConfigPath();
            MapDataSerializer.Export(_core.MapData, _config, path);
            _core.CurrentPath = path;
            _core.IsDirty     = false;
            EditorPrefs.SetString("MapEditor_LastPath", path);
        }

        private void CmdImport()
        {
            string path = EditorUtility.OpenFilePanel("导入地图", Application.dataPath, "json");
            if (string.IsNullOrEmpty(path)) return;
            TryLoadMap(path);
        }

        private void CmdCreateConfig()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "新建 MapEditorConfig", "MapEditorConfig", "asset", "选择保存位置");
            if (string.IsNullOrEmpty(path)) return;

            var newCfg = CreateInstance<MapEditorConfig>();
            newCfg.InitializeDefaults();
            AssetDatabase.CreateAsset(newCfg, path);
            AssetDatabase.SaveAssets();
            SwitchConfig(newCfg);
            Selection.activeObject = newCfg;
        }

        // 将当前配置的 Asset 路径写入地图数据
        private void BindConfigPath()
        {
            if (_config == null || _core.MapData == null) return;
            _core.MapData.configPath = AssetDatabase.GetAssetPath(_config);
        }

        // ── 工具方法 ─────────────────────────────────────────────────────
        private static Vector3 HitXYPlane(Ray ray, float z)
        {
            if (Mathf.Abs(ray.direction.z) < 1e-6f) return ray.origin;
            float t = (z - ray.origin.z) / ray.direction.z;
            return ray.origin + ray.direction * t;
        }
    }
}
