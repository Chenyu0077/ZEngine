using Main.FuncModule;
using UnityEditor;
using UnityEngine;

namespace GameScripts.Editor
{
    /// <summary>
    /// MapEditorConfig 配置美化
    /// </summary>
    [CustomEditor(typeof(MapEditorConfig))]
    public class MapEditorConfigEditor : UnityEditor.Editor
    {
        private bool _layersFoldout      = false;
        private bool _schemaFoldout      = false;
        private bool _objSchemaFoldout   = false;
        private bool _terrainFoldout     = false;
        private bool _zoneFoldout        = false;
        private bool _poiFoldout         = false;
        private bool _spawnFoldout       = false;
        private bool _tileSetFoldout     = true;
        private bool _settingsFoldout    = true;

        private Vector2 _scroll;

        public override void OnInspectorGUI()
        {
            var config = (MapEditorConfig)target;
            // 不调用 serializedObject.Update/ApplyModifiedProperties：
            // 所有字段通过直接赋值 + Undo.RecordObject + SetDirty 管理，
            // 调用 ApplyModifiedProperties 会把旧快照回写覆盖直接赋值。

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Map Editor Config", EditorStyles.whiteLargeLabel);
            EditorGUILayout.Space(4);

            if (GUILayout.Button("重置为默认值", GUILayout.Height(28)))
            {
                if (EditorUtility.DisplayDialog("确认重置", "将覆盖所有当前配置，是否继续？", "重置", "取消"))
                {
                    Undo.RecordObject(config, "Reset MapEditorConfig");
                    config.InitializeDefaults();
                    EditorUtility.SetDirty(config);
                }
            }
            EditorGUILayout.Space(6);

            DrawLayersSection(config);
            DrawSchemaSection(config);
            DrawObjectSchemaSection(config);
            DrawTerrainSection(config);
            DrawZoneSection(config);
            DrawPOISection(config);
            DrawSpawnSection(config);
            DrawTileSetSection(config);
            DrawSettingsSection(config);

            EditorGUILayout.EndScrollView();

            // 其余各 Section 已在内部自行调用 Undo.RecordObject + SetDirty
        }

        // ── 图层配置 ──────────────────────────────────────────────────
        private void DrawLayersSection(MapEditorConfig config)
        {
            _layersFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_layersFoldout, $"图层配置  [{config.layers.Count}]");
            if (_layersFoldout)
            {
                EditorGUI.indentLevel++;
                int removeIdx = -1;
                for (int i = 0; i < config.layers.Count; i++)
                {
                    var layer = config.layers[i];
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"图层 {i}", EditorStyles.boldLabel, GUILayout.Width(60));
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("↑", GUILayout.Width(24)) && i > 0)
                    {
                        Undo.RecordObject(config, "Move Layer");
                        (config.layers[i], config.layers[i - 1]) = (config.layers[i - 1], config.layers[i]);
                    }
                    if (GUILayout.Button("↓", GUILayout.Width(24)) && i < config.layers.Count - 1)
                    {
                        Undo.RecordObject(config, "Move Layer");
                        (config.layers[i], config.layers[i + 1]) = (config.layers[i + 1], config.layers[i]);
                    }
                    Color bg = GUI.backgroundColor;
                    GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
                    if (GUILayout.Button("删除", GUILayout.Width(40))) removeIdx = i;
                    GUI.backgroundColor = bg;
                    EditorGUILayout.EndHorizontal();

                    layer.id          = EditorGUILayout.TextField("ID",     layer.id);
                    layer.displayName = EditorGUILayout.TextField("显示名", layer.displayName);
                    layer.sortOrder   = EditorGUILayout.IntField("排序",    layer.sortOrder);
                    layer.debugColor  = EditorGUILayout.ColorField("调试色", layer.debugColor);
                    layer.defaultVisible = EditorGUILayout.Toggle("默认可见", layer.defaultVisible);
                    layer.defaultLocked  = EditorGUILayout.Toggle("默认锁定", layer.defaultLocked);
                    layer.defaultOpacity = EditorGUILayout.Slider("默认透明度", layer.defaultOpacity, 0f, 1f);
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space(2);
                }

                if (removeIdx >= 0)
                {
                    Undo.RecordObject(config, "Remove Layer");
                    config.layers.RemoveAt(removeIdx);
                }

                if (GUILayout.Button("+ 添加图层", GUILayout.Height(26)))
                {
                    Undo.RecordObject(config, "Add Layer");
                    config.layers.Add(new LayerDefinition
                    {
                        id          = $"layer_{config.layers.Count}",
                        displayName = "新图层",
                        sortOrder   = config.layers.Count * 10,
                        debugColor  = Random.ColorHSV(0f, 1f, 0.5f, 0.8f, 0.6f, 1f),
                    });
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        // ── 格子属性 Schema ───────────────────────────────────────────
        private void DrawSchemaSection(MapEditorConfig config)
        {
            _schemaFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_schemaFoldout, $"格子属性 Schema  [{config.cellPropertySchema.Count}]");
            if (_schemaFoldout)
            {
                EditorGUI.indentLevel++;
                int removeIdx = -1;
                for (int i = 0; i < config.cellPropertySchema.Count; i++)
                {
                    var def = config.cellPropertySchema[i];
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"属性 {i}", EditorStyles.boldLabel, GUILayout.Width(60));
                    GUILayout.FlexibleSpace();
                    Color bg = GUI.backgroundColor;
                    GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
                    if (GUILayout.Button("删除", GUILayout.Width(40))) removeIdx = i;
                    GUI.backgroundColor = bg;
                    EditorGUILayout.EndHorizontal();

                    def.key          = EditorGUILayout.TextField("Key",       def.key);
                    def.displayName  = EditorGUILayout.TextField("显示名",   def.displayName);
                    def.valueType    = (PropType)EditorGUILayout.EnumPopup("类型", def.valueType);
                    def.defaultValue = EditorGUILayout.TextField("默认值",   def.defaultValue);

                    if (def.valueType == PropType.Enum)
                        def.enumOptionsRef = EditorGUILayout.TextField("枚举引用", def.enumOptionsRef);

                    def.enableOverlay = EditorGUILayout.Toggle("启用叠加色", def.enableOverlay);
                    if (def.enableOverlay)
                    {
                        EditorGUI.indentLevel++;
                        def.overlayTrueValue = EditorGUILayout.TextField("触发值", def.overlayTrueValue);
                        def.overlayColor     = EditorGUILayout.ColorField("叠加色", def.overlayColor);
                        EditorGUI.indentLevel--;
                    }
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space(2);
                }

                if (removeIdx >= 0)
                {
                    Undo.RecordObject(config, "Remove CellPropertyDef");
                    config.cellPropertySchema.RemoveAt(removeIdx);
                }

                if (GUILayout.Button("+ 添加属性", GUILayout.Height(26)))
                {
                    Undo.RecordObject(config, "Add CellPropertyDef");
                    config.cellPropertySchema.Add(new CellPropertyDef
                    {
                        key         = $"prop_{config.cellPropertySchema.Count}",
                        displayName = "新属性",
                        valueType   = PropType.Bool,
                        defaultValue= "false",
                    });
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        // ── 对象属性 Schema ───────────────────────────────────────────
        private void DrawObjectSchemaSection(MapEditorConfig config)
        {
            _objSchemaFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_objSchemaFoldout, $"对象属性 Schema  [{config.objectPropertySchema.Count}]");
            if (_objSchemaFoldout)
            {
                EditorGUI.indentLevel++;
                int removeIdx = -1;
                for (int i = 0; i < config.objectPropertySchema.Count; i++)
                {
                    var def = config.objectPropertySchema[i];
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"属性 {i}", EditorStyles.boldLabel, GUILayout.Width(60));
                    GUILayout.FlexibleSpace();
                    Color bg = GUI.backgroundColor;
                    GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
                    if (GUILayout.Button("删除", GUILayout.Width(40))) removeIdx = i;
                    GUI.backgroundColor = bg;
                    EditorGUILayout.EndHorizontal();

                    EditorGUI.BeginChangeCheck();
                    def.key          = EditorGUILayout.TextField("Key",     def.key);
                    def.displayName  = EditorGUILayout.TextField("显示名",  def.displayName);
                    def.valueType    = (PropType)EditorGUILayout.EnumPopup("类型", def.valueType);
                    def.defaultValue = EditorGUILayout.TextField("默认值",  def.defaultValue);
                    if (def.valueType == PropType.Enum)
                        def.enumOptionsRef = EditorGUILayout.TextField("枚举引用", def.enumOptionsRef);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(config, "Modify ObjectPropertyDef");
                        EditorUtility.SetDirty(config);
                    }

                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space(2);
                }

                if (removeIdx >= 0)
                {
                    Undo.RecordObject(config, "Remove ObjectPropertyDef");
                    config.objectPropertySchema.RemoveAt(removeIdx);
                    EditorUtility.SetDirty(config);
                }

                if (GUILayout.Button("+ 添加对象属性", GUILayout.Height(26)))
                {
                    Undo.RecordObject(config, "Add ObjectPropertyDef");
                    config.objectPropertySchema.Add(new ObjectPropertyDef
                    {
                        key          = $"objProp_{config.objectPropertySchema.Count}",
                        displayName  = "新属性",
                        valueType    = PropType.String,
                        defaultValue = "",
                    });
                    EditorUtility.SetDirty(config);
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        // ── 地形类型 ──────────────────────────────────────────────────
        private void DrawTerrainSection(MapEditorConfig config)
        {
            _terrainFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_terrainFoldout, $"地形类型(格子属性)  [{config.terrainTypes.Count}]");
            if (_terrainFoldout)
            {
                EditorGUI.indentLevel++;
                int removeIdx = -1;
                for (int i = 0; i < config.terrainTypes.Count; i++)
                {
                    var t = config.terrainTypes[i];
                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                    t.id               = EditorGUILayout.TextField(t.id,              GUILayout.Width(80));
                    t.displayName      = EditorGUILayout.TextField(t.displayName,     GUILayout.Width(60));
                    t.mapColor         = EditorGUILayout.ColorField(t.mapColor,       GUILayout.Width(50));
                    t.defaultPathWeight= EditorGUILayout.FloatField(t.defaultPathWeight, GUILayout.Width(40));
                    Color bg = GUI.backgroundColor;
                    GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
                    if (GUILayout.Button("✕", GUILayout.Width(22))) removeIdx = i;
                    GUI.backgroundColor = bg;
                    EditorGUILayout.EndHorizontal();
                }
                if (removeIdx >= 0) { Undo.RecordObject(config, "Remove Terrain"); config.terrainTypes.RemoveAt(removeIdx); }
                if (GUILayout.Button("+ 添加地形", GUILayout.Height(22)))
                {
                    Undo.RecordObject(config, "Add Terrain");
                    config.terrainTypes.Add(new TerrainTypeDef { id = "new_terrain", displayName = "新地形", defaultPathWeight = 1f });
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        // ── 区域类型 ──────────────────────────────────────────────────
        private void DrawZoneSection(MapEditorConfig config)
        {
            _zoneFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_zoneFoldout, $"区域类型(格子属性)  [{config.zoneTypes.Count}]");
            if (_zoneFoldout)
            {
                EditorGUI.indentLevel++;
                int removeIdx = -1;
                for (int i = 0; i < config.zoneTypes.Count; i++)
                {
                    var z = config.zoneTypes[i];
                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                    z.id          = EditorGUILayout.TextField(z.id,          GUILayout.Width(100));
                    z.displayName = EditorGUILayout.TextField(z.displayName, GUILayout.Width(80));
                    z.debugColor  = EditorGUILayout.ColorField(z.debugColor, GUILayout.Width(50));
                    Color bg = GUI.backgroundColor;
                    GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
                    if (GUILayout.Button("✕", GUILayout.Width(22))) removeIdx = i;
                    GUI.backgroundColor = bg;
                    EditorGUILayout.EndHorizontal();
                }
                if (removeIdx >= 0) { Undo.RecordObject(config, "Remove Zone"); config.zoneTypes.RemoveAt(removeIdx); }
                if (GUILayout.Button("+ 添加区域", GUILayout.Height(22)))
                {
                    Undo.RecordObject(config, "Add Zone");
                    config.zoneTypes.Add(new ZoneTypeDef { id = "new_zone", displayName = "新区域" });
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        // ── POI 类型 ──────────────────────────────────────────────────
        private void DrawPOISection(MapEditorConfig config)
        {
            _poiFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_poiFoldout, $"POI 类型（对象枚举引用）  [{config.poiTypes.Count}]");
            if (_poiFoldout)
            {
                EditorGUI.indentLevel++;
                int removeIdx = -1;
                for (int i = 0; i < config.poiTypes.Count; i++)
                {
                    var p = config.poiTypes[i];
                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                    EditorGUI.BeginChangeCheck();
                    p.id          = EditorGUILayout.TextField(p.id,          GUILayout.Width(100));
                    p.displayName = EditorGUILayout.TextField(p.displayName, GUILayout.Width(80));
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(config, "Modify POITypeDef");
                        EditorUtility.SetDirty(config);
                    }
                    Color bg = GUI.backgroundColor;
                    GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
                    if (GUILayout.Button("✕", GUILayout.Width(22))) removeIdx = i;
                    GUI.backgroundColor = bg;
                    EditorGUILayout.EndHorizontal();
                }
                if (removeIdx >= 0)
                {
                    Undo.RecordObject(config, "Remove POITypeDef");
                    config.poiTypes.RemoveAt(removeIdx);
                    EditorUtility.SetDirty(config);
                }
                if (GUILayout.Button("+ 添加 POI 类型", GUILayout.Height(22)))
                {
                    Undo.RecordObject(config, "Add POITypeDef");
                    config.poiTypes.Add(new POITypeDef { id = "new_poi", displayName = "新POI" });
                    EditorUtility.SetDirty(config);
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        // ── 出生点类型 ────────────────────────────────────────────────
        private void DrawSpawnSection(MapEditorConfig config)
        {
            _spawnFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_spawnFoldout, $"出生点类型  [{config.spawnPointTypes.Count}]");
            if (_spawnFoldout)
            {
                EditorGUI.indentLevel++;
                int removeIdx = -1;
                for (int i = 0; i < config.spawnPointTypes.Count; i++)
                {
                    var sp = config.spawnPointTypes[i];
                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                    sp.id          = EditorGUILayout.TextField(sp.id,          GUILayout.Width(80));
                    sp.displayName = EditorGUILayout.TextField(sp.displayName, GUILayout.Width(60));
                    sp.gizmoColor  = EditorGUILayout.ColorField(sp.gizmoColor, GUILayout.Width(50));
                    Color bg = GUI.backgroundColor;
                    GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
                    if (GUILayout.Button("✕", GUILayout.Width(22))) removeIdx = i;
                    GUI.backgroundColor = bg;
                    EditorGUILayout.EndHorizontal();
                }
                if (removeIdx >= 0) { Undo.RecordObject(config, "Remove SpawnType"); config.spawnPointTypes.RemoveAt(removeIdx); }
                if (GUILayout.Button("+ 添加出生点类型", GUILayout.Height(22)))
                {
                    Undo.RecordObject(config, "Add SpawnType");
                    config.spawnPointTypes.Add(new SpawnPointTypeDef { id = "new_type", displayName = "新类型", gizmoColor = Color.white });
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        // ── TileSet 引用 ──────────────────────────────────────────────
        private void DrawTileSetSection(MapEditorConfig config)
        {
            _tileSetFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_tileSetFoldout, $"TileSet 资产引用  [{config.tileSets.Count}]");
            if (_tileSetFoldout)
            {
                EditorGUI.indentLevel++;
                int removeIdx = -1;
                for (int i = 0; i < config.tileSets.Count; i++)
                {
                    var ts = config.tileSets[i];
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    ts.id          = EditorGUILayout.TextField("ID",   ts.id);
                    ts.displayName = EditorGUILayout.TextField("名称", ts.displayName);
                    ts.tileSet     = (TileSetData)EditorGUILayout.ObjectField("TileSet", ts.tileSet, typeof(TileSetData), false);
                    if (ts.tileSet != null)
                    {
                        EditorGUI.BeginChangeCheck();
                        int newW = Mathf.Max(1, EditorGUILayout.IntField("网格宽", ts.tileSet.gridWidth));
                        int newH = Mathf.Max(1, EditorGUILayout.IntField("网格高", ts.tileSet.gridHeight));
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(ts.tileSet, "Modify TileSet Grid");
                            ts.tileSet.gridWidth = newW;
                            ts.tileSet.gridHeight = newH;
                            EditorUtility.SetDirty(ts.tileSet);
                        }
                    }
                    EditorGUILayout.HelpBox("tileId 全局唯一，可画到任意图层。", MessageType.None);
                    Color bg = GUI.backgroundColor;
                    GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
                    if (GUILayout.Button("删除", GUILayout.Width(40))) removeIdx = i;
                    GUI.backgroundColor = bg;
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space(2);
                }
                if (removeIdx >= 0) { Undo.RecordObject(config, "Remove TileSet"); config.tileSets.RemoveAt(removeIdx); }
                if (GUILayout.Button("+ 添加 TileSet", GUILayout.Height(26)))
                {
                    Undo.RecordObject(config, "Add TileSet");
                    config.tileSets.Add(new TileSetReference { id = $"ts_{config.tileSets.Count}", displayName = "新 TileSet" });
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        // ── 全局设置 ──────────────────────────────────────────────────
        private void DrawSettingsSection(MapEditorConfig config)
        {
            _settingsFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_settingsFoldout, "全局设置");
            if (_settingsFoldout)
            {
                EditorGUI.indentLevel++;

                // 先读临时值，EndChangeCheck 后再统一 Record + 赋值 + SetDirty
                int    newWidth    = EditorGUILayout.IntField  ("默认宽度",        config.defaultWidth);
                int    newHeight   = EditorGUILayout.IntField  ("默认高度",        config.defaultHeight);
                float  newCellSize = EditorGUILayout.FloatField("格子大小",        config.defaultCellSize);
                string newExport   = EditorGUILayout.TextField ("导出路径",        config.exportPath);
                Color  newLineCol  = EditorGUILayout.ColorField("网格线颜色",      config.gridLineColor);
                float  newLineW    = EditorGUILayout.FloatField("网格线宽",        config.gridLineWidth);
                bool   newCoords   = EditorGUILayout.Toggle    ("显示坐标",        config.showCellCoords);
                string newObjPath  = EditorGUILayout.TextField ("对象 Prefab 路径",config.objectPalettePath);

                bool changed =
                    newWidth    != config.defaultWidth    ||
                    newHeight   != config.defaultHeight   ||
                    !Mathf.Approximately(newCellSize, config.defaultCellSize) ||
                    newExport   != config.exportPath      ||
                    newLineCol  != config.gridLineColor   ||
                    !Mathf.Approximately(newLineW, config.gridLineWidth) ||
                    newCoords   != config.showCellCoords  ||
                    newObjPath  != config.objectPalettePath;

                if (changed)
                {
                    Undo.RecordObject(config, "Modify MapEditorConfig Settings");
                    config.defaultWidth      = newWidth;
                    config.defaultHeight     = newHeight;
                    config.defaultCellSize   = newCellSize;
                    config.exportPath        = newExport;
                    config.gridLineColor     = newLineCol;
                    config.gridLineWidth     = newLineW;
                    config.showCellCoords    = newCoords;
                    config.objectPalettePath = newObjPath;
                    EditorUtility.SetDirty(config);
                }

                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
    }
}
