using System.Collections.Generic;
using System.IO;
using Main.FuncModule;
using UnityEditor;
using UnityEngine;

namespace GameScripts.Editor
{
    /// <summary>
    /// Scene View 绘制。
    /// Grid 线用 Handles（3D 空间），其余全部在 Handles.BeginGUI() 的 GUI 层绘制，
    /// 保证 Sprite → 叠加色 → 悬停/选中 的正确层叠顺序。
    /// </summary>
    public static class GridRenderer
    {
        private static readonly Color HoverOutlineColor    = new Color(1f,    1f,    1f,   0.9f);
        private static readonly Color HoverFillColor       = new Color(1f,    1f,    1f,   0.15f);
        private static readonly Color SelectedOutlineColor = new Color(0.15f, 0.85f, 1f,   1f);
        private static readonly Color SelectedFillColor    = new Color(0.15f, 0.85f, 1f,   0.25f);
        private static readonly Color ObjectBoundColor     = new Color(1f,    0.85f, 0f,   0.85f);
        private static readonly Color ObjectFillColor      = new Color(1f,    0.85f, 0f,   0.12f);
        private static readonly Color SpawnCircleColor     = new Color(0.3f,  0.6f,  1f,   0.9f);

        // prefabId → sprite 缓存（null 表示已查过但无 Sprite）
        private static readonly Dictionary<string, Sprite> _spriteCache = new Dictionary<string, Sprite>();

        public static void ClearObjectSpriteCache() => _spriteCache.Clear();

        private static Sprite GetObjectSprite(string prefabId, MapEditorConfig config)
        {
            if (string.IsNullOrEmpty(prefabId)) return null;
            if (_spriteCache.TryGetValue(prefabId, out var cached)) return cached;

            string searchPath = config?.objectPalettePath ?? "Assets";
            string normalizedPrefabId = prefabId.Replace('\\', '/');

            if (normalizedPrefabId.Contains("/"))
            {
                string directPath = $"{searchPath.TrimEnd('/')}/{normalizedPrefabId}.prefab";
                var directGo = AssetDatabase.LoadAssetAtPath<GameObject>(directPath);
                if (directGo != null)
                {
                    var directSr = directGo.GetComponentInChildren<SpriteRenderer>();
                    _spriteCache[prefabId] = directSr?.sprite;
                    return _spriteCache[prefabId];
                }
            }

            string fileName = Path.GetFileNameWithoutExtension(normalizedPrefabId);
            string[] guids = AssetDatabase.FindAssets($"t:Prefab {fileName}", new[] { searchPath });
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(path) != fileName) continue;

                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var sr = go != null ? go.GetComponentInChildren<SpriteRenderer>() : null;
                _spriteCache[prefabId] = sr?.sprite;
                return _spriteCache[prefabId];
            }

            _spriteCache[prefabId] = null;
            return null;
        }

        public static Vector3 MapOrigin = Vector3.zero;

        // ── 主入口 ────────────────────────────────────────────────────────
        public static void DrawAll(MapEditorCore core, SceneView sv)
        {
            if (core?.MapData == null || core.Config == null) return;

            // Grid 线保留在 Handles（3D），在 GUI 层下方
            DrawGrid(core);

            // GUI 层：Sprite → 叠加色 → 对象/出生点 → 悬停/选中（依次覆盖）
            Handles.BeginGUI();
            DrawLayerTiles(core);
            DrawCellOverlays(core);
            DrawObjects(core);
            DrawSpawnPoints(core);
            DrawObjectGhost(core);
            DrawBrushGhost(core);
            DrawSelection(core);
            DrawHoveredCell(core);
            DrawSelectedCell(core);
            Handles.EndGUI();
        }

        // ── Grid 线（Handles）────────────────────────────────────────────
        private static void DrawGrid(MapEditorCore core)
        {
            var    data   = core.MapData;
            var    config = core.Config;
            float  cs     = data.cellSize;
            Color  col    = config.gridLineColor;
            int    w      = data.width;
            int    h      = data.height;
            Vector3 o     = MapOrigin;

            Handles.color = col;
            for (int y = 0; y <= h; y++)
                Handles.DrawLine(new Vector3(o.x, o.y + y * cs, o.z), new Vector3(o.x + w * cs, o.y + y * cs, o.z));
            for (int x = 0; x <= w; x++)
                Handles.DrawLine(new Vector3(o.x + x * cs, o.y, o.z), new Vector3(o.x + x * cs, o.y + h * cs, o.z));

            // 外边框加粗
            Color bold = new Color(col.r, col.g, col.b, Mathf.Clamp01(col.a * 2f));
            Handles.color = bold;
            float tw = w * cs, th = h * cs;
            Handles.DrawLine(o,                                          new Vector3(o.x + tw, o.y,      o.z));
            Handles.DrawLine(new Vector3(o.x + tw, o.y,      o.z),      new Vector3(o.x + tw, o.y + th, o.z));
            Handles.DrawLine(new Vector3(o.x + tw, o.y + th, o.z),      new Vector3(o.x,      o.y + th, o.z));
            Handles.DrawLine(new Vector3(o.x,      o.y + th, o.z),      o);
        }

        // ── 图层 Tile（GUI）──────────────────────────────────────────────
        private static void DrawLayerTiles(MapEditorCore core)
        {
            if (Event.current.type != EventType.Repaint) return;
            if (core.Config?.tileSets == null) return;

            var data = core.MapData;
            float cs = data.cellSize;

            var sortedLayers = new System.Collections.Generic.List<LayerDefinition>(core.Config.layers);
            sortedLayers.Sort((a, b) => a.sortOrder.CompareTo(b.sortOrder));

            foreach (var layerDef in sortedLayers)
            {
                var state = core.GetLayerState(layerDef.id);
                if (!state.visible) continue;

                var layer = data.layers?.Find(l => l.id == layerDef.id);
                if (layer?.tiles == null) continue;

                Color prevColor = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, state.opacity);

                for (int y = 0; y < data.height; y++)
                {
                    for (int x = 0; x < data.width; x++)
                    {
                        int tileId = layer.tiles[y * data.width + x];
                        if (tileId < 0) continue;

                        Rect rect = CellToGUIRect(x, y, cs);
                        if (rect.width < 1f || rect.height < 1f) continue;

                        // 全局查找：跨所有 TileSet 找 tileId
                        var entry = core.Config.FindTileEntry(tileId);
                        if (entry != null)
                        {
                            var sprite = ResolveVariantSprite(entry, x, y);
                            if (sprite != null)
                                DrawSprite(rect, sprite);
                            else
                            {
                                Color c = entry.fallbackColor;
                                EditorGUI.DrawRect(rect, new Color(c.r, c.g, c.b, c.a * state.opacity));
                            }
                        }
                        else
                        {
                            // tileId 找不到对应条目，用图层调试色兜底
                            Color dc = layerDef.debugColor;
                            EditorGUI.DrawRect(rect, new Color(dc.r, dc.g, dc.b, dc.a * state.opacity));
                        }
                    }
                }

                GUI.color = prevColor;
            }
        }

        private static void DrawSprite(Rect guiRect, Sprite sprite)
        {
            var tex     = sprite.texture;
            var texRect = sprite.textureRect;
            var uv = new Rect(
                texRect.x      / tex.width,
                texRect.y      / tex.height,
                texRect.width  / tex.width,
                texRect.height / tex.height);

            GUI.DrawTextureWithTexCoords(guiRect, tex, uv, true);
        }

        /// <summary>在给定矩形内保持 Sprite 纵横比，左下角对齐绘制（不拉伸变形）。</summary>
        private static void DrawSpriteAspect(Rect guiRect, Sprite sprite)
        {
            if (sprite?.texture == null) return;

            var tex     = sprite.texture;
            var texRect = sprite.textureRect;
            var uv = new Rect(
                texRect.x      / tex.width,
                texRect.y      / tex.height,
                texRect.width  / tex.width,
                texRect.height / tex.height);

            float spriteW = texRect.width;
            float spriteH = texRect.height;
            float aspect  = spriteW / spriteH;
            float containerAspect = guiRect.width / guiRect.height;

            float drawW, drawH;
            if (aspect > containerAspect)
            {
                drawW = guiRect.width;
                drawH = drawW / aspect;
            }
            else
            {
                drawH = guiRect.height;
                drawW = drawH * aspect;
            }

            var drawRect = new Rect(
                guiRect.x + (guiRect.width - drawW) * 0.5f,
                guiRect.yMax - drawH,
                drawW, drawH);

            GUI.DrawTextureWithTexCoords(drawRect, tex, uv, true);
        }

        private static Sprite ResolveVariantSprite(TileEntry entry, int x, int y)
            => entry.ResolveSprite(x, y);

        // ── 格子属性叠加色（GUI）────────────────────────────────────────
        private static void DrawCellOverlays(MapEditorCore core)
        {
            if (Event.current.type != EventType.Repaint) return;
            if (core.Config?.cellPropertySchema == null) return;

            var data = core.MapData;
            float cs = data.cellSize;

            foreach (var def in core.Config.cellPropertySchema)
            {
                if (!def.enableOverlay || !core.IsOverlayEnabled(def.key)) continue;

                bool isEnumMultiColor = def.valueType == PropType.Enum && def.enumOverlayColors != null && def.enumOverlayColors.Count > 0;

                for (int y = 0; y < data.height; y++)
                    for (int x = 0; x < data.width; x++)
                    {
                        string val = core.GetCellProp(x, y, def.key);

                        Color color;
                        if (isEnumMultiColor)
                        {
                            var mapped = def.GetEnumOverlayColor(val);
                            if (mapped == null) continue;
                            color = mapped.Value;
                        }
                        else
                        {
                            if (!string.Equals(val, def.overlayTrueValue, System.StringComparison.OrdinalIgnoreCase)) continue;
                            color = def.overlayColor;
                        }

                        Rect rect = CellToGUIRect(x, y, cs);
                        if (rect.width >= 1f && rect.height >= 1f)
                            EditorGUI.DrawRect(rect, color);
                    }
            }
        }

        // ── 对象占位框（GUI）────────────────────────────────────────────
        private static readonly Color ObjectSelectedColor = new Color(0.2f, 0.9f, 1f, 1f);

        private static void DrawObjects(MapEditorCore core)
        {
            if (Event.current.type != EventType.Repaint) return;
            if (core.MapData?.objects == null) return;

            float cs = core.MapData.cellSize;

            foreach (var obj in core.MapData.objects)
            {
                Rect r = MultiCellToGUIRect(obj.x, obj.y, obj.width, obj.height, cs);
                if (r.width < 1f || r.height < 1f) continue;

                bool isSelected = core.SelectedObject == obj;

                var sprite = GetObjectSprite(obj.prefabId, core.Config);
                if (sprite != null)
                {
                    Color prev = GUI.color;
                    GUI.color = isSelected ? new Color(0.7f, 1f, 1f, 1f) : Color.white;
                    DrawSpriteAspect(r, sprite);
                    GUI.color = prev;
                }
                else
                {
                    EditorGUI.DrawRect(r, isSelected ? new Color(0.2f, 0.8f, 1f, 0.25f) : ObjectFillColor);
                    GUI.Label(new Rect(r.x + 2, r.y + 2, r.width - 4, 16),
                        obj.prefabId, EditorStyles.miniLabel);
                }

                // 选中对象用亮蓝色粗框 + 角标
                if (isSelected)
                {
                    DrawGUIOutline(r, ObjectSelectedColor, 2.5f);
                    // 左上角小三角标
                    var marker = new Rect(r.x, r.y, 10, 10);
                    EditorGUI.DrawRect(marker, ObjectSelectedColor);
                }
                else
                {
                    DrawGUIOutline(r, ObjectBoundColor, 1.5f);
                }
            }
        }

        // ── 出生点（GUI）────────────────────────────────────────────────
        private static void DrawSpawnPoints(MapEditorCore core)
        {
            if (Event.current.type != EventType.Repaint) return;
            if (core.MapData?.spawnPoints == null || core.Config == null) return;

            float cs = core.MapData.cellSize;

            foreach (var sp in core.MapData.spawnPoints)
            {
                Color gizmoColor = SpawnCircleColor;
                foreach (var typeDef in core.Config.spawnPointTypes)
                    if (typeDef.id == sp.type) { gizmoColor = typeDef.gizmoColor; break; }

                Rect cellRect = CellToGUIRect(sp.x, sp.y, cs);
                if (cellRect.width < 4f || cellRect.height < 4f) continue;

                // 圆形近似：缩进 20% 的正方形
                float inset = cellRect.width * 0.2f;
                Rect inner = new Rect(cellRect.x + inset, cellRect.y + inset,
                    cellRect.width - inset * 2, cellRect.height - inset * 2);

                EditorGUI.DrawRect(inner, new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.3f));
                DrawGUIOutline(inner, gizmoColor, 1.5f);

                // 类型标签
                GUI.Label(new Rect(cellRect.x, cellRect.yMax - 14, cellRect.width, 14),
                    sp.type, EditorStyles.miniLabel);
            }
        }

        // ── 画笔预览（GUI，TilePaint 模式，多格画笔时显示）───────────────
        private static readonly Color BrushGhostFill   = new Color(0.4f, 0.8f, 0.4f, 0.2f);
        private static readonly Color BrushGhostBorder = new Color(0.4f, 0.8f, 0.4f, 0.8f);

        private static void DrawBrushGhost(MapEditorCore core)
        {
            if (core.EditMode != EditMode.TilePaint) return;
            if (core.BrushTiles == null || core.BrushTiles.Count <= 1) return;

            var hov = core.HoveredCell;
            if (hov.x < 0 || !core.IsValidCell(hov.x, hov.y)) return;
            if (Event.current.type != EventType.Repaint) return;

            float cs = core.MapData.cellSize;

            foreach (var bt in core.BrushTiles)
            {
                int x = hov.x + bt.dx;
                int y = hov.y + bt.dy;
                if (!core.IsValidCell(x, y)) continue;

                Rect rect = CellToGUIRect(x, y, cs);
                if (rect.width < 1f || rect.height < 1f) continue;

                var entry = core.Config.FindTileEntry(bt.tileId);
                if (entry?.sprite != null)
                {
                    Color prev = GUI.color;
                    GUI.color = new Color(1f, 1f, 1f, 0.6f);
                    DrawSprite(rect, entry.sprite);
                    GUI.color = prev;
                }
                else if (entry != null)
                {
                    Color c = entry.fallbackColor;
                    EditorGUI.DrawRect(rect, new Color(c.r, c.g, c.b, 0.4f));
                }

                EditorGUI.DrawRect(rect, BrushGhostFill);
                DrawGUIOutline(rect, BrushGhostBorder, 1.5f);
            }

            Rect labelRect = MultiCellToGUIRect(hov.x, hov.y, core.BrushW, core.BrushH, cs);
            GUI.Label(new Rect(labelRect.x, labelRect.y - 18f, 200f, 16f),
                $"画笔 {core.BrushW}×{core.BrushH}", EditorStyles.miniLabel);
        }

        // ── 对象放置预览（GUI，ObjectPlace 模式）─────────────────────────
        private static readonly Color GhostFill   = new Color(0.6f, 0.4f, 1f, 0.25f);
        private static readonly Color GhostBorder  = new Color(0.6f, 0.4f, 1f, 0.9f);
        private static readonly Color GhostBlocked = new Color(1f,   0.2f, 0.2f, 0.35f);

        private static void DrawObjectGhost(MapEditorCore core)
        {
            if (core.EditMode != EditMode.ObjectPlace) return;
            if (string.IsNullOrEmpty(core.SelectedPrefabId)) return;

            var hov = core.HoveredCell;
            if (hov.x < 0 || !core.IsValidCell(hov.x, hov.y)) return;
            if (Event.current.type != EventType.Repaint) return;

            float cs  = core.MapData.cellSize;
            int   w   = core.PlacementWidth;
            int   h   = core.PlacementHeight;
            int   fpW = core.PlacementFootprintW > 0 ? core.PlacementFootprintW : w;
            int   fpH = core.PlacementFootprintH > 0 ? core.PlacementFootprintH : h;

            bool  blocked     = !core.IsFootprintValid(hov.x, hov.y, fpW, fpH);
            Color borderColor = blocked ? new Color(1f, 0.2f, 0.2f, 1f) : GhostBorder;
            Rect  rect        = MultiCellToGUIRect(hov.x, hov.y, w, h, cs);

            var sprite = GetObjectSprite(core.SelectedPrefabId, core.Config);
            if (sprite != null)
            {
                Color prev = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, blocked ? 0.35f : 0.75f);
                DrawSpriteAspect(rect, sprite);
                GUI.color = prev;
                if (blocked)
                    EditorGUI.DrawRect(rect, GhostBlocked);
            }
            else
            {
                EditorGUI.DrawRect(rect, blocked ? GhostBlocked : GhostFill);
            }

            DrawGUIOutline(rect, borderColor, 2f);

            string label = blocked
                ? $"{core.SelectedPrefabId} ✗ 占用"
                : $"{core.SelectedPrefabId} ({w}×{h})";
            GUI.Label(new Rect(rect.x + 2, rect.y - 18f, 200f, 16f), label, EditorStyles.miniLabel);
        }

        // ── 框选区域（GUI，Select 模式）──────────────────────────────────
        private static readonly Color SelectionFill   = new Color(0.3f, 0.6f, 1f, 0.18f);
        private static readonly Color SelectionBorder = new Color(0.3f, 0.7f, 1f, 1f);

        private static void DrawSelection(MapEditorCore core)
        {
            if (core.EditMode != EditMode.Select) return;
            if (!core.HasSelection && !core.IsSelecting) return;
            if (Event.current.type != EventType.Repaint) return;

            float cs = core.MapData.cellSize;

            // 每格填色
            for (int y = core.SelectMinY; y <= core.SelectMaxY; y++)
            for (int x = core.SelectMinX; x <= core.SelectMaxX; x++)
            {
                if (!core.IsValidCell(x, y)) continue;
                EditorGUI.DrawRect(CellToGUIRect(x, y, cs), SelectionFill);
            }

            // 整体边框（2px）
            Rect selRect = MultiCellToGUIRect(
                core.SelectMinX, core.SelectMinY,
                core.SelectionW, core.SelectionH, cs);
            DrawGUIOutline(selRect, SelectionBorder, 2f);

            // 尺寸标签
            string label = core.HasSelection
                ? $"选区 {core.SelectionW}×{core.SelectionH}（{core.SelectionCount} 格）"
                : $"框选中… {core.SelectionW}×{core.SelectionH}";
            GUI.Label(new Rect(selRect.x, selRect.y - 18f, 220f, 16f),
                label, EditorStyles.miniLabel);
        }

        // ── 悬停格子（GUI）──────────────────────────────────────────────
        private static void DrawHoveredCell(MapEditorCore core)
        {
            // CellEdit 模式已有选中格子时不显示悬停
            if (core.EditMode == EditMode.CellEdit && core.SelectedCell.x >= 0) return;

            var hov = core.HoveredCell;
            if (hov.x < 0 || !core.IsValidCell(hov.x, hov.y)) return;
            if (Event.current.type != EventType.Repaint) return;

            Rect rect = CellToGUIRect(hov.x, hov.y, core.MapData.cellSize);
            EditorGUI.DrawRect(rect, HoverFillColor);
            DrawGUIOutline(rect, HoverOutlineColor, 1.5f);

            if (core.Config.showCellCoords)
                GUI.Label(new Rect(rect.x + 2, rect.y + 2, rect.width, 14),
                    $"{hov.x},{hov.y}", EditorStyles.miniLabel);
        }

        // ── 选中格子（GUI，CellEdit 专属）────────────────────────────────
        private static void DrawSelectedCell(MapEditorCore core)
        {
            if (core.EditMode != EditMode.CellEdit) return;

            var sel = core.SelectedCell;
            if (sel.x < 0 || !core.IsValidCell(sel.x, sel.y)) return;
            if (Event.current.type != EventType.Repaint) return;

            Rect rect = CellToGUIRect(sel.x, sel.y, core.MapData.cellSize);
            EditorGUI.DrawRect(rect, SelectedFillColor);
            DrawGUIOutline(rect, SelectedOutlineColor, 2f);

            // 四角标记
            float cLen = Mathf.Min(rect.width, rect.height) * 0.2f;
            float t    = 2f;
            Color col  = SelectedOutlineColor;
            // 左上
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, cLen, t), col);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, t, cLen), col);
            // 右上
            EditorGUI.DrawRect(new Rect(rect.xMax - cLen, rect.y, cLen, t), col);
            EditorGUI.DrawRect(new Rect(rect.xMax - t, rect.y, t, cLen), col);
            // 左下
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - t, cLen, t), col);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - cLen, t, cLen), col);
            // 右下
            EditorGUI.DrawRect(new Rect(rect.xMax - cLen, rect.yMax - t, cLen, t), col);
            EditorGUI.DrawRect(new Rect(rect.xMax - t, rect.yMax - cLen, t, cLen), col);

            // 坐标标签（居中）
            GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal    = { textColor = SelectedOutlineColor },
            };
            GUI.Label(rect, $"[{sel.x}, {sel.y}]", labelStyle);
        }

        // ── 工具方法 ─────────────────────────────────────────────────────

        /// <summary>将单格世界坐标转为 Scene View GUI Rect。</summary>
        private static Rect CellToGUIRect(int x, int y, float cs)
        {
            Vector3 o    = MapOrigin;
            var     guiBL = HandleUtility.WorldToGUIPoint(new Vector3(o.x +  x      * cs, o.y +  y      * cs, o.z));
            var     guiTR = HandleUtility.WorldToGUIPoint(new Vector3(o.x + (x + 1) * cs, o.y + (y + 1) * cs, o.z));
            // GUI Y 向下：guiBL.y > guiTR.y（screen 坐标 BL 比 TR 大）
            return new Rect(guiBL.x, guiTR.y, guiTR.x - guiBL.x, guiBL.y - guiTR.y);
        }

        /// <summary>将多格区域转为 GUI Rect。</summary>
        private static Rect MultiCellToGUIRect(int x, int y, int w, int h, float cs)
        {
            Vector3 o    = MapOrigin;
            var     guiBL = HandleUtility.WorldToGUIPoint(new Vector3(o.x +  x      * cs, o.y +  y      * cs, o.z));
            var     guiTR = HandleUtility.WorldToGUIPoint(new Vector3(o.x + (x + w) * cs, o.y + (y + h) * cs, o.z));
            return new Rect(guiBL.x, guiTR.y, guiTR.x - guiBL.x, guiBL.y - guiTR.y);
        }

        /// <summary>在 GUI 空间绘制矩形边框（4 条细边）。</summary>
        private static void DrawGUIOutline(Rect r, Color color, float thickness = 1f)
        {
            EditorGUI.DrawRect(new Rect(r.x,              r.y,              r.width,    thickness), color);
            EditorGUI.DrawRect(new Rect(r.x,              r.yMax - thickness, r.width,  thickness), color);
            EditorGUI.DrawRect(new Rect(r.x,              r.y,              thickness,  r.height),  color);
            EditorGUI.DrawRect(new Rect(r.xMax - thickness, r.y,            thickness,  r.height),  color);
        }

        /// <summary>世界格子中心点（Handles 空间，供 DrawGrid 等使用）。</summary>
        public static Vector3 CellCenter(int x, int y, float cs)
        {
            Vector3 o = MapOrigin;
            return new Vector3(o.x + x * cs + cs * 0.5f, o.y + y * cs + cs * 0.5f, o.z);
        }
    }
}
