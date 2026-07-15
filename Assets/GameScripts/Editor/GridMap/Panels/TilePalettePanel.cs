using System.Collections.Generic;
using Main.FuncModule;
using UnityEditor;
using UnityEngine;


namespace GameScripts.Editor
{
    public class TilePalettePanel
    {
        private const int   BaseCellSize = 48;
        private const int   MinCellSize  = 16;
        private const int   MaxCellSize  = 128;
        private const int   CellPad       = 1;
private const int   HEADER_H     = 20;
        private const int   SelectBarH    = 18;

        private static readonly Color CheckerLight   = new Color(0.76f, 0.76f, 0.76f, 1f);
        private static readonly Color CheckerDark     = new Color(0.60f, 0.60f, 0.60f, 1f);
        private static readonly Color SelBorder       = new Color(0.26f, 0.75f, 1f, 1f);
        private static readonly Color SelFill         = new Color(0.26f, 0.75f, 1f, 0.22f);
        private static readonly Color BrushRectBorder = new Color(0.3f, 0.9f, 1f, 1f);
        private static readonly Color BrushRectFill    = new Color(0.3f, 0.9f, 1f, 0.08f);
        private static readonly Color PanelBg          = new Color(0.22f, 0.22f, 0.22f, 1f);
        private static readonly Color GridLineColor    = new Color(0.14f, 0.14f, 0.14f, 1f);
        private static readonly Color DropHighlight   = new Color(1f, 0.8f, 0.2f, 0.5f);
        private static readonly Color EmptyCellBg     = new Color(0.3f, 0.3f, 0.3f, 1f);

        private static Texture2D _checkerTex;
        private static Texture2D _emptyTex;
        private readonly Dictionary<Color, Texture2D> _colorTexCache = new Dictionary<Color, Texture2D>();
        private readonly Dictionary<string, bool>    _groupFoldout  = new Dictionary<string, bool>();

        private float _zoom = 1f;

        // 框选状态
        private bool _draggingSel;
        private int  _selGroupIdx = -1;
        private int  _selMinGX, _selMinGY, _selMaxGX, _selMaxGY;

        // 拖放目标格子
        private int _dropGX = -1, _dropGY = -1, _dropGroupIdx = -1;
        private List<SpriteDropInfo> _dropLayout = new List<SpriteDropInfo>();

        private struct SpriteDropInfo
        {
            public Sprite sprite;
            public int offsetX;
            public int offsetY;
        }

        // 网格内拖拽移动瓦片
        private bool   _isMovingTile;
        private int    _moveGroupIdx = -1;
        private int    _moveFromGX, _moveFromGY;
        private int    _moveToGX, _moveToGY;
        private static readonly Color MoveSourceColor = new Color(1f, 0.4f, 0.4f, 0.35f);
        private static readonly Color MoveTargetColor = new Color(0.2f, 1f, 0.4f, 0.45f);

        // 滚动偏移
        private Vector2 _scrollPos;

        public void Draw(MapEditorCore core) => Draw(core, 0);

        public void Draw(MapEditorCore core, int availableWidth)
        {
            if (core?.Config == null) return;

            int gridWidth = availableWidth > 0 ? availableWidth : 240;
            int cellSize   = Mathf.Clamp(Mathf.RoundToInt(BaseCellSize * _zoom), MinCellSize, MaxCellSize);

            DrawZoomBar();
            EditorGUILayout.Space(2);

            if (core.Config.tileSets.Count == 0)
            {
                EditorGUILayout.HelpBox("尚未配置 TileSet。\n请在 MapEditorConfig → TileSet 资产引用中添加。", MessageType.Info);
                DrawEraseTile(core);
                return;
            }

            DrawBrushInfo(core);
            DrawEraseTile(core);

            EnsureTextures();

            float contentW = CalcTotalWidth(core, cellSize);
            float totalH = CalcTotalHeight(core, gridWidth, cellSize);

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            {
                Rect panelRect = GUILayoutUtility.GetRect(contentW, totalH, GUILayout.ExpandWidth(true));

                if (Event.current.type == EventType.Repaint)
                {
                    EditorGUI.DrawRect(panelRect, PanelBg);
                    DrawAllGroups(core, cellSize, panelRect);
                }

                HandleInput(core, cellSize, panelRect);
            }
            EditorGUILayout.EndScrollView();
        }

        // ── 缩放 ────────────────────────────────────────────────────────
        private void DrawZoomBar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("缩放", EditorStyles.miniLabel, GUILayout.Width(28));
            if (GUILayout.Button("\u2212", EditorStyles.toolbarButton, GUILayout.Width(24)))
                _zoom = Mathf.Clamp(_zoom - 0.25f, 0.33f, 3f);
            GUILayout.Label($"{Mathf.RoundToInt(_zoom * 100)}%", EditorStyles.miniLabel, GUILayout.Width(36));
            if (GUILayout.Button("+", EditorStyles.toolbarButton, GUILayout.Width(24)))
                _zoom = Mathf.Clamp(_zoom + 0.25f, 0.33f, 3f);
            GUILayout.FlexibleSpace();
            GUILayout.Label("Ctrl+滚轮缩放", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }

        // ── 画笔信息 ─────────────────────────────────────────────────────
        private void DrawBrushInfo(MapEditorCore core)
        {
            if (core.BrushTiles == null || core.BrushTiles.Count == 0) return;
            EditorGUILayout.Space(2);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"画笔: {core.BrushW}\u00d7{core.BrushH}  ({core.BrushTiles.Count} 瓦片)", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
        }

        // ── 布局计算 ─────────────────────────────────────────────────────
        private static int GetGroupColCount(TileSetData ts)
        {
            if (ts == null) return 1;
            return Mathf.Max(1, ts.gridWidth);
        }

        private float CalcTotalWidth(MapEditorCore core, int cellSize)
        {
            float maxW = 0;
            for (int gi = 0; gi < core.Config.tileSets.Count; gi++)
            {
                var tsRef = core.Config.tileSets[gi];
                if (IsGroupFolded(tsRef.id)) continue;
                if (tsRef.tileSet == null) continue;
                int cols = Mathf.Max(1, tsRef.tileSet.gridWidth);
                float w = cols * (cellSize + CellPad) + CellPad;
                if (w > maxW) maxW = w;
            }
            return maxW > 0 ? maxW : 100;
        }

        private float CalcTotalHeight(MapEditorCore core, int availableWidth, int cellSize)
        {
            float h = 0;
            for (int gi = 0; gi < core.Config.tileSets.Count; gi++)
            {
                var tsRef = core.Config.tileSets[gi];
                bool folded = IsGroupFolded(tsRef.id);
                h += HEADER_H;

                if (!folded && tsRef.tileSet != null)
                {
                    int rows = Mathf.Max(1, tsRef.tileSet.gridHeight);
                    h += rows * (cellSize + CellPad) + CellPad;
                    h += SelectBarH;
                }
                else if (!folded)
                {
                    h += 22;
                }
                h += 4;
            }
            return h;
        }

        // ── 绘制所有分组 ────────────────────────────────────────────────
        private void DrawAllGroups(MapEditorCore core, int cellSize, Rect panelRect)
        {
            float y = panelRect.y;
            for (int gi = 0; gi < core.Config.tileSets.Count; gi++)
            {
                var tsRef = core.Config.tileSets[gi];
                bool folded = IsGroupFolded(tsRef.id);

                var headerRect = new Rect(panelRect.x, y, panelRect.width, HEADER_H);
                var foldRect   = new Rect(panelRect.x + 2, y + 2, 14, HEADER_H - 4);

                GUI.Label(foldRect, folded ? "\u25b8" : "\u25be", EditorStyles.miniLabel);
                GUI.Label(headerRect, $"  {tsRef.displayName}", EditorStyles.miniLabel);
                y += HEADER_H;

                if (!folded)
                {
                    if (tsRef.tileSet != null)
                    {
                        int cols = Mathf.Max(1, tsRef.tileSet.gridWidth);
                        int rows = Mathf.Max(1, tsRef.tileSet.gridHeight);
                        float gridW = cols * (cellSize + CellPad) + CellPad;
                        float gridH = rows * (cellSize + CellPad) + CellPad;
                        Rect gridRect = new Rect(panelRect.x, y, gridW, gridH);

                        DrawGrid(core, tsRef.tileSet, gi, cols, rows, cellSize, gridRect);

                        Rect layoutEntry = new Rect(panelRect.x, y + gridH, panelRect.width, SelectBarH);
                        DrawSelectInfo(core, gi, layoutEntry);

                        y += gridH + SelectBarH;
                    }
                    else
                    {
                        GUI.Label(new Rect(panelRect.x + 4, y, panelRect.width - 8, 18),
                            "(未分配 TileSetData)", EditorStyles.miniLabel);
                        y += 22;
                    }
                }
                y += 4;
            }
        }

        // ── 绘制网格 ────────────────────────────────────────────────────
        private void DrawGrid(MapEditorCore core, TileSetData ts, int groupIdx, int cols, int rows, int cellSize, Rect gridRect)
        {
            EditorGUI.DrawRect(gridRect, EmptyCellBg);

            for (int gy = 0; gy < rows; gy++)
            for (int gx = 0; gx < cols; gx++)
            {
                Rect cell = CellRect(gx, gy, cellSize, gridRect);
                DrawCheckerboard(cell);

                var entry = ts.GetTileAt(gx, gy);
                if (entry != null)
                    DrawTileSprite(cell, entry);
                else
                {
                    // 空格：再画一遍半透明棋盘格
                    EditorGUI.DrawRect(cell, new Color(0.3f, 0.3f, 0.3f, 0.4f));
                }

                // 框选高亮
                if (IsCellInBrush(groupIdx, gx, gy))
                {
                    EditorGUI.DrawRect(cell, SelFill);
                    float t = 2f;
                    EditorGUI.DrawRect(new Rect(cell.x, cell.y, cell.width, t), SelBorder);
                    EditorGUI.DrawRect(new Rect(cell.x, cell.yMax - t, cell.width, t), SelBorder);
                    EditorGUI.DrawRect(new Rect(cell.x, cell.y, t, cell.height), SelBorder);
                    EditorGUI.DrawRect(new Rect(cell.xMax - t, cell.y, t, cell.height), SelBorder);
                }

                // 网格线
                EditorGUI.DrawRect(new Rect(cell.x, cell.y, cell.width, 1), GridLineColor);
                EditorGUI.DrawRect(new Rect(cell.x, cell.y, 1, cell.height), GridLineColor);

                // 拖放高亮（支持多格子预览）
                if (_dropGroupIdx == groupIdx && _dropLayout.Count > 0)
                {
                    for (int di = 0; di < _dropLayout.Count; di++)
                    {
                        if (_dropGX + _dropLayout[di].offsetX == gx && _dropGY + _dropLayout[di].offsetY == gy)
                        {
                            EditorGUI.DrawRect(cell, DropHighlight);
                            break;
                        }
                    }
                }

                // Alt+拖拽移动：源格子高亮
                if (_isMovingTile && _moveGroupIdx == groupIdx && gx == _moveFromGX && gy == _moveFromGY)
                {
                    EditorGUI.DrawRect(cell, MoveSourceColor);
                    DrawRectOutline(cell, new Color(1f, 0.3f, 0.3f, 1f), 2f);
                }

                // Alt+拖拽移动：目标格子高亮
                if (_isMovingTile && _moveGroupIdx == groupIdx && gx == _moveToGX && gy == _moveToGY
                    && (_moveFromGX != _moveToGX || _moveFromGY != _moveToGY))
                {
                    EditorGUI.DrawRect(cell, MoveTargetColor);
                    DrawRectOutline(cell, new Color(0.1f, 0.8f, 0.3f, 1f), 2f);
                }
            }

            // 框选矩形边框
            if (_selGroupIdx == groupIdx && core.BrushTiles != null && core.BrushTiles.Count > 0)
            {
                float x0 = gridRect.x + CellPad + _selMinGX * (cellSize + CellPad);
                float y0 = gridRect.y + CellPad + _selMinGY * (cellSize + CellPad);
                float x1 = gridRect.x + CellPad + (_selMaxGX + 1) * (cellSize + CellPad);
                float y1 = gridRect.y + CellPad + (_selMaxGY + 1) * (cellSize + CellPad);
                Rect selRect = new Rect(x0, y0, x1 - x0, y1 - y0);
                EditorGUI.DrawRect(selRect, BrushRectFill);
                DrawRectOutline(selRect, BrushRectBorder, 1.5f);
            }
        }

        private void DrawSelectInfo(MapEditorCore core, int groupIdx, Rect rect)
        {
            if (_selGroupIdx != groupIdx || core.BrushTiles == null || core.BrushTiles.Count == 0)
                return;
            GUI.Label(rect, $"已选: {core.BrushW}\u00d7{core.BrushH} ({core.BrushTiles.Count})", EditorStyles.miniLabel);
        }

        private void DrawTileSprite(Rect cell, TileEntry entry)
        {
            if (entry.sprite != null)
            {
                Texture2D preview = AssetPreview.GetAssetPreview(entry.sprite);
                Texture2D src      = preview != null ? preview : entry.sprite.texture;
                if (src != null)
                {
                    Rect drawRect = FitSpriteInCell(cell, entry.sprite, src == preview);
                    Rect uv       = GetSpriteUV(entry.sprite, src == preview);
                    GUI.DrawTextureWithTexCoords(drawRect, src, uv);
                }
            }
            else
            {
                GUI.DrawTexture(cell, GetColorTex(entry.fallbackColor), ScaleMode.StretchToFill, false);
            }
        }

        // ── 输入处理 ─────────────────────────────────────────────────────
        private void HandleInput(MapEditorCore core, int cellSize, Rect panelRect)
        {
            Event e = Event.current;

            // ── 1. Ctrl+滚轮缩放（普通滚轮用于滚动内容）───────────────────
            if (e.type == EventType.ScrollWheel && e.control && panelRect.Contains(e.mousePosition))
            {
                float delta = e.delta.y > 0 ? -0.15f : 0.15f;
                _zoom = Mathf.Clamp(_zoom + delta, 0.33f, 3f);
                e.Use();
                RepaintPanel();
                return;
            }

            // ── 2. 拖放 Sprite（必须在其他输入之前处理）──────────────────
            if (HandleDragDrop(core, cellSize, panelRect))
                return;

            // ── 3. 折叠标题点击 ─────────────────────────────────────────
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                int foldIdx = FindGroupHeaderAt(core, e.mousePosition, cellSize, panelRect);
                if (foldIdx >= 0)
                {
                    string gid = core.Config.tileSets[foldIdx].id;
                    _groupFoldout[gid] = !IsGroupFolded(gid);
                    e.Use();
                    RepaintPanel();
                    return;
                }
            }

            // ── 4. 瓦片点击 / 框选 / Alt+拖拽移动瓦片 ──────────────────────
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                var hit = FindCellAt(core, e.mousePosition, cellSize, panelRect);
                if (hit != null)
                {
                    var ts = core.Config.tileSets[hit.Value.groupIdx].tileSet;
                    if (ts != null)
                    {
                        if (e.alt)
                        {
                            var tile = ts.GetTileAt(hit.Value.gx, hit.Value.gy);
                            if (tile != null)
                            {
                                _isMovingTile = true;
                                _moveGroupIdx = hit.Value.groupIdx;
                                _moveFromGX = hit.Value.gx;
                                _moveFromGY = hit.Value.gy;
                                _moveToGX = hit.Value.gx;
                                _moveToGY = hit.Value.gy;
                            }
                        }
                        else
                        {
                            _draggingSel = true;
                            _selGroupIdx = hit.Value.groupIdx;
                            _selMinGX = hit.Value.gx;
                            _selMinGY = hit.Value.gy;
                            _selMaxGX = hit.Value.gx;
                            _selMaxGY = hit.Value.gy;
                            ApplyBrushFromPalette(core, hit.Value.groupIdx);
                        }
                        e.Use();
                        RepaintPanel();
                    }
                }
            }

            if (e.type == EventType.MouseDrag && _isMovingTile)
            {
                var hit = FindCellAt(core, e.mousePosition, cellSize, panelRect);
                if (hit != null && hit.Value.groupIdx == _moveGroupIdx)
                {
                    _moveToGX = hit.Value.gx;
                    _moveToGY = hit.Value.gy;
                }
                else
                {
                    _moveToGX = _moveFromGX;
                    _moveToGY = _moveFromGY;
                }
                e.Use();
                RepaintPanel();
            }

            if (e.type == EventType.MouseDrag && _draggingSel && !_isMovingTile)
            {
                var hit = FindCellAt(core, e.mousePosition, cellSize, panelRect);
                if (hit != null && hit.Value.groupIdx == _selGroupIdx)
                {
                    var tsRef = core.Config.tileSets[hit.Value.groupIdx];
                    if (tsRef.tileSet != null)
                    {
                        int gx = Mathf.Clamp(hit.Value.gx, 0, tsRef.tileSet.gridWidth - 1);
                        int gy = Mathf.Clamp(hit.Value.gy, 0, tsRef.tileSet.gridHeight - 1);
                        _selMinGX = Mathf.Min(_selMinGX, gx);
                        _selMinGY = Mathf.Min(_selMinGY, gy);
                        _selMaxGX = Mathf.Max(_selMaxGX, gx);
                        _selMaxGY = Mathf.Max(_selMaxGY, gy);
                        ApplyBrushFromPalette(core, hit.Value.groupIdx);
                    }
                }
                e.Use();
                RepaintPanel();
            }

            if (e.type == EventType.MouseUp && (e.button == 0))
            {
                if (_isMovingTile)
                {
                    if (_moveFromGX != _moveToGX || _moveFromGY != _moveToGY)
                    {
                        if (_moveGroupIdx < 0 || _moveGroupIdx >= core.Config.tileSets.Count)
                        {
                            _isMovingTile = false;
                            _moveGroupIdx = -1;
                        }
                        else
                        {
                            var ts = core.Config.tileSets[_moveGroupIdx].tileSet;
                            if (ts != null)
                            {
                                var tile = ts.GetTileAt(_moveFromGX, _moveFromGY);
                                if (tile != null)
                                {
                                    Undo.RecordObject(ts, "Move Tile");
                                    var target = ts.GetTileAt(_moveToGX, _moveToGY);
                                    ts.RemoveTileAt(_moveFromGX, _moveFromGY);
                                    if (target != null)
                                    {
                                        ts.RemoveTileAt(_moveToGX, _moveToGY);
                                        target.gridX = _moveFromGX;
                                        target.gridY = _moveFromGY;
                                        ts.tiles.Add(target);
                                    }
                                    tile.gridX = _moveToGX;
                                    tile.gridY = _moveToGY;
                                    ts.tiles.Add(tile);
                                    EditorUtility.SetDirty(ts);
                                }
                            }
                            if (IsCellInBrush(_moveGroupIdx, _moveFromGX, _moveFromGY)
                                || IsCellInBrush(_moveGroupIdx, _moveToGX, _moveToGY))
                                InvalidateBrushIfGroup(core, _moveGroupIdx);
                        }
                    }
                    _isMovingTile = false;
                    _moveGroupIdx = -1;
                }
                _draggingSel = false;
                e.Use();
                RepaintPanel();
            }

            // ── 5. 右键清除格子（带确认）──────────────────────────────────
            if (e.type == EventType.MouseDown && e.button == 1)
            {
                var hit = FindCellAt(core, e.mousePosition, cellSize, panelRect);
                if (hit != null)
                {
                    var ts = core.Config.tileSets[hit.Value.groupIdx].tileSet;
                    if (ts != null)
                    {
                        if (_selGroupIdx == hit.Value.groupIdx && IsCellInBrush(_selGroupIdx, hit.Value.gx, hit.Value.gy))
                        {
                            int count = 0;
                            for (int gy = _selMinGY; gy <= _selMaxGY; gy++)
                            for (int gx = _selMinGX; gx <= _selMaxGX; gx++)
                                if (ts.GetTileAt(gx, gy) != null) count++;

                            if (count > 0 && EditorUtility.DisplayDialog(
                                "批量删除瓦片",
                                $"确定要删除选区内 {count} 个瓦片吗？",
                                "删除", "取消"))
                            {
                                Undo.RecordObject(ts, "Remove Tiles");
                                for (int gy = _selMinGY; gy <= _selMaxGY; gy++)
                                for (int gx = _selMinGX; gx <= _selMaxGX; gx++)
                                    ts.RemoveTileAt(gx, gy);
                                EditorUtility.SetDirty(ts);
                                InvalidateBrushIfGroup(core, _selGroupIdx);
                            }
                        }
                        else
                        {
                            var tile = ts.GetTileAt(hit.Value.gx, hit.Value.gy);
                            if (tile != null && EditorUtility.DisplayDialog(
                                "删除瓦片",
                                $"确定要删除 [{tile.tileName}] ({hit.Value.gx}, {hit.Value.gy}) 吗？",
                                "删除", "取消"))
                            {
                                Undo.RecordObject(ts, "Remove Tile");
                                ts.RemoveTileAt(hit.Value.gx, hit.Value.gy);
                                EditorUtility.SetDirty(ts);
                                InvalidateBrushIfGroup(core, hit.Value.groupIdx);
                            }
                        }
                    }
                    e.Use();
                    RepaintPanel();
                }
            }
        }

        // ── 拖放 Sprite（返回 true 表示事件已处理）─────────────────────
        private bool HandleDragDrop(MapEditorCore core, int cellSize, Rect panelRect)
        {
            Event e = Event.current;

            if (e.type == EventType.DragUpdated || e.type == EventType.DragPerform)
            {
                bool isPerform = e.type == EventType.DragPerform;

                _dropGroupIdx = -1;
                _dropGX = -1;
                _dropGY = -1;
                _dropLayout.Clear();

                if (HasSpriteInDrag() && panelRect.Contains(e.mousePosition))
                {
                    var hit = FindCellAt(core, e.mousePosition, cellSize, panelRect);
                    if (hit != null)
                    {
                        var ts = core.Config.tileSets[hit.Value.groupIdx].tileSet;
                        if (ts != null)
                        {
                            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                            var layout = ComputeDropLayout(GetSpritesFromDrag());

                            _dropGroupIdx = hit.Value.groupIdx;
                            _dropGX = hit.Value.gx;
                            _dropGY = hit.Value.gy;
                            _dropLayout = layout;

                            if (!isPerform)
                                RepaintPanel();

                            if (isPerform)
                            {
                                if (layout.Count > 0)
                                {
                                    int undoGroup = Undo.GetCurrentGroup();

                                    int maxOX = 0, maxOY = 0;
                                    for (int i = 0; i < layout.Count; i++)
                                    {
                                        if (layout[i].offsetX > maxOX) maxOX = layout[i].offsetX;
                                        if (layout[i].offsetY > maxOY) maxOY = layout[i].offsetY;
                                    }
                                    int neededW = hit.Value.gx + maxOX + 1;
                                    int neededH = hit.Value.gy + maxOY + 1;

                                    if (neededW > ts.gridWidth)
                                    {
                                        Undo.RecordObject(ts, "Expand Grid");
                                        ts.gridWidth = neededW;
                                    }
                                    if (neededH > ts.gridHeight)
                                    {
                                        Undo.RecordObject(ts, "Expand Grid");
                                        ts.gridHeight = neededH;
                                    }

                                    Undo.RecordObject(ts, "Place Tiles");
                                    for (int i = 0; i < layout.Count; i++)
                                    {
                                        PlaceSpriteAt(core, ts, layout[i].sprite,
                                            hit.Value.gx + layout[i].offsetX,
                                            hit.Value.gy + layout[i].offsetY);
                                    }
                                    Undo.CollapseUndoOperations(undoGroup);
                                    EditorUtility.SetDirty(ts);
                                    InvalidateBrushIfGroup(core, hit.Value.groupIdx);
                                }
                                DragAndDrop.AcceptDrag();
                            }
                        }
                    }
                    else
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                    }
                }

                e.Use();
                if (isPerform)
                {
                    _dropGroupIdx = -1;
                    _dropGX = -1;
                    _dropGY = -1;
                    _dropLayout.Clear();
                    RepaintPanel();
                }
                return true;
            }

            if (e.type == EventType.DragExited)
            {
                _dropGroupIdx = -1;
                _dropGX = -1;
                _dropGY = -1;
                _dropLayout.Clear();
                RepaintPanel();
            }

            return false;
        }

        private static bool HasSpriteInDrag()
        {
            if (DragAndDrop.objectReferences != null)
            {
                foreach (var obj in DragAndDrop.objectReferences)
                    if (obj is Sprite) return true;
            }
            // 拖入的是 Texture2D（含多个Sprite）时 objectReferences 中没有 Sprite
            // 检查 paths 中是否有 .png 等图片资源
            if (DragAndDrop.paths != null)
            {
                foreach (var path in DragAndDrop.paths)
                {
                    if (path.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase) ||
                        path.EndsWith(".jpg", System.StringComparison.OrdinalIgnoreCase) ||
                        path.EndsWith(".jpeg", System.StringComparison.OrdinalIgnoreCase) ||
                        path.EndsWith(".tga", System.StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
            return false;
        }

        private static List<Sprite> GetSpritesFromDrag()
        {
            var sprites = new List<Sprite>();
            if (DragAndDrop.objectReferences != null)
            {
                foreach (var obj in DragAndDrop.objectReferences)
                {
                    if (obj is Sprite sp) { sprites.Add(sp); continue; }
                    if (obj is Texture2D tex)
                    {
                        string assetPath = AssetDatabase.GetAssetPath(tex);
                        var allSprites = AssetDatabase.LoadAllAssetRepresentationsAtPath(assetPath);
                        if (allSprites != null && allSprites.Length > 0)
                        {
                            foreach (var s in allSprites)
                                if (s is Sprite) sprites.Add((Sprite)s);
                        }
                        else
                        {
                            sp = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                            if (sp != null) sprites.Add(sp);
                        }
                    }
                }
            }
            if (sprites.Count == 0 && DragAndDrop.paths != null)
            {
                foreach (var path in DragAndDrop.paths)
                {
                    if (path.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase) ||
                        path.EndsWith(".jpg", System.StringComparison.OrdinalIgnoreCase) ||
                        path.EndsWith(".jpeg", System.StringComparison.OrdinalIgnoreCase) ||
                        path.EndsWith(".tga", System.StringComparison.OrdinalIgnoreCase))
                    {
                        var allSprites = AssetDatabase.LoadAllAssetRepresentationsAtPath(path);
                        if (allSprites != null && allSprites.Length > 0)
                        {
                            foreach (var s in allSprites)
                                if (s is Sprite) sprites.Add((Sprite)s);
                        }
                        else
                        {
                            var sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                            if (sp != null) sprites.Add(sp);
                        }
                    }
                }
            }
            return sprites;
        }

        private static List<SpriteDropInfo> ComputeDropLayout(List<Sprite> sprites)
        {
            var result = new List<SpriteDropInfo>();
            if (sprites.Count == 0) return result;

            var byTexture = new Dictionary<Texture2D, List<int>>();
            var texOrder = new List<Texture2D>();

            for (int i = 0; i < sprites.Count; i++)
            {
                var tex = sprites[i].texture;
                if (tex == null)
                {
                    result.Add(new SpriteDropInfo { sprite = sprites[i], offsetX = result.Count, offsetY = 0 });
                    continue;
                }
                if (!byTexture.ContainsKey(tex))
                {
                    byTexture[tex] = new List<int>();
                    texOrder.Add(tex);
                }
                byTexture[tex].Add(i);
            }

            if (byTexture.Count == 0)
            {
                NormalizeOffsets(result);
                return result;
            }

            int groupCursorX = 0;

            foreach (var tex in texOrder)
            {
                var indices = byTexture[tex];

                if (indices.Count == 1)
                {
                    result.Add(new SpriteDropInfo { sprite = sprites[indices[0]], offsetX = groupCursorX, offsetY = 0 });
                    groupCursorX++;
                    continue;
                }

                float minX = float.MaxValue, minY = float.MaxValue;
                float cellW = float.MaxValue, cellH = float.MaxValue;

                foreach (int idx in indices)
                {
                    var r = sprites[idx].rect;
                    if (r.x < minX) minX = r.x;
                    if (r.y < minY) minY = r.y;
                    if (r.width < cellW) cellW = r.width;
                    if (r.height < cellH) cellH = r.height;
                }

                cellW = Mathf.Max(1f, cellW);
                cellH = Mathf.Max(1f, cellH);

                int maxIY = 0;
                foreach (int idx in indices)
                {
                    int iy = Mathf.RoundToInt((sprites[idx].rect.y - minY) / cellH);
                    if (iy > maxIY) maxIY = iy;
                }

                int maxOXInGroup = 0;
                foreach (int idx in indices)
                {
                    int ix = Mathf.RoundToInt((sprites[idx].rect.x - minX) / cellW);
                    int iy = Mathf.RoundToInt((sprites[idx].rect.y - minY) / cellH);
                    int oy = maxIY - iy;
                    result.Add(new SpriteDropInfo { sprite = sprites[idx], offsetX = groupCursorX + ix, offsetY = oy });
                    if (groupCursorX + ix > maxOXInGroup) maxOXInGroup = groupCursorX + ix;
                }

                groupCursorX = maxOXInGroup + 1;
            }

            NormalizeOffsets(result);
            return result;
        }

        private static void NormalizeOffsets(List<SpriteDropInfo> list)
        {
            if (list.Count == 0) return;
            int minOX = int.MaxValue, minOY = int.MaxValue;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].offsetX < minOX) minOX = list[i].offsetX;
                if (list[i].offsetY < minOY) minOY = list[i].offsetY;
            }
            for (int i = 0; i < list.Count; i++)
            {
                list[i] = new SpriteDropInfo
                {
                    sprite = list[i].sprite,
                    offsetX = list[i].offsetX - minOX,
                    offsetY = list[i].offsetY - minOY
                };
            }
        }

        private void PlaceSpriteAt(MapEditorCore core, TileSetData ts, Sprite sprite, int gx, int gy)
        {
            if (gx < 0 || gx >= ts.gridWidth || gy < 0 || gy >= ts.gridHeight) return;

            // 找全局唯一 tileId（当前最大 + 1）
            int maxId = -1;
            foreach (var tsRef in core.Config.tileSets)
            {
                if (tsRef.tileSet == null) continue;
                foreach (var t in tsRef.tileSet.tiles)
                    if (t.tileId > maxId) maxId = t.tileId;
            }

            // 移除该位置已有瓦片
            ts.RemoveTileAt(gx, gy);

            var entry = new TileEntry
            {
                tileId = maxId + 1,
                tileName = sprite.name,
                sprite = sprite,
                fallbackColor = Color.white,
                gridX = gx,
                gridY = gy,
            };
            ts.tiles.Add(entry);
        }

        // ── 画笔生成 ────────────────────────────────────────────────────
        private void ApplyBrushFromPalette(MapEditorCore core, int groupIdx)
        {
            if (groupIdx < 0 || groupIdx >= core.Config.tileSets.Count) return;
            var ts = core.Config.tileSets[groupIdx].tileSet;
            if (ts == null) return;

            core.BrushTiles.Clear();

            core.BrushW = _selMaxGX - _selMinGX + 1;
            core.BrushH = _selMaxGY - _selMinGY + 1;

            for (int gy = _selMinGY; gy <= _selMaxGY; gy++)
            for (int gx = _selMinGX; gx <= _selMaxGX; gx++)
            {
                var entry = ts.GetTileAt(gx, gy);
                core.BrushTiles.Add(new BrushTile
                {
                    dx = gx - _selMinGX,
                    dy = gy - _selMinGY,
                    tileId = entry != null ? entry.tileId : -1,
                });
            }

            // 设置 SelectedTileId 为选区左上角瓦片
            var topLeft = ts.GetTileAt(_selMinGX, _selMinGY);
            if (topLeft != null)
                core.SelectedTileId = topLeft.tileId;

            core.EditMode = EditMode.TilePaint;
        }

        // ── 查找 ─────────────────────────────────────────────────────────
        private struct CellHit { public int groupIdx; public int gx; public int gy; }

        private CellHit? FindCellAt(MapEditorCore core, Vector2 mouse, int cellSize, Rect panelRect)
        {
            float y = panelRect.y;
            for (int gi = 0; gi < core.Config.tileSets.Count; gi++)
            {
                var tsRef = core.Config.tileSets[gi];
                bool folded = IsGroupFolded(tsRef.id);
                y += HEADER_H;

                if (folded) { y += 4; continue; }

                if (tsRef.tileSet == null) { y += 22 + 4; continue; }

                int cols = Mathf.Max(1, tsRef.tileSet.gridWidth);
                int rows = Mathf.Max(1, tsRef.tileSet.gridHeight);
                float gridW = cols * (cellSize + CellPad) + CellPad;
                float gridH = rows * (cellSize + CellPad) + CellPad;

                Rect gridRect = new Rect(panelRect.x, y, gridW, gridH);
                if (gridRect.Contains(mouse))
                {
                    int gx = Mathf.FloorToInt((mouse.x - gridRect.x - CellPad) / (cellSize + CellPad));
                    int gy = Mathf.FloorToInt((mouse.y - gridRect.y - CellPad) / (cellSize + CellPad));
                    if (gx >= 0 && gx < cols && gy >= 0 && gy < rows)
                        return new CellHit { groupIdx = gi, gx = gx, gy = gy };
                }

                y += gridH + SelectBarH + 4;
            }
            return null;
        }

        private int FindGroupHeaderAt(MapEditorCore core, Vector2 mouse, int cellSize, Rect panelRect)
        {
            float y = panelRect.y;
            for (int gi = 0; gi < core.Config.tileSets.Count; gi++)
            {
                var tsRef = core.Config.tileSets[gi];
                Rect headerRect = new Rect(panelRect.x, y, panelRect.width, HEADER_H);
                if (headerRect.Contains(mouse))
                    return gi;

                bool folded = IsGroupFolded(tsRef.id);
                y += HEADER_H;
                if (!folded)
                {
                    if (tsRef.tileSet != null)
                    {
                        int cols = Mathf.Max(1, tsRef.tileSet.gridWidth);
                        int rows = Mathf.Max(1, tsRef.tileSet.gridHeight);
                        y += rows * (cellSize + CellPad) + CellPad + SelectBarH;
                    }
                    else
                    {
                        y += 22;
                    }
                }
                y += 4;
            }
            return -1;
        }

        private bool IsCellInBrush(int groupIdx, int gx, int gy)
        {
            if (_selGroupIdx < 0) return false;
            return groupIdx == _selGroupIdx && gx >= _selMinGX && gx <= _selMaxGX && gy >= _selMinGY && gy <= _selMaxGY;
        }

        private void InvalidateBrushIfGroup(MapEditorCore core, int groupIdx)
        {
            if (_selGroupIdx == groupIdx)
            {
                _selGroupIdx = -1;
                core.BrushTiles?.Clear();
            }
        }

        private bool IsGroupFolded(string id)
        {
            if (_groupFoldout.TryGetValue(id, out bool f)) return f;
            return false;
        }

        // ── 矩形辅助 ────────────────────────────────────────────────────
        private static Rect CellRect(int gx, int gy, int cellSize, Rect gridRect)
        {
            return new Rect(
                gridRect.x + CellPad + gx * (cellSize + CellPad),
                gridRect.y + CellPad + gy * (cellSize + CellPad),
                cellSize, cellSize);
        }

        private void DrawCheckerboard(Rect cell)
        {
            int checkSize = Mathf.Max(4, Mathf.RoundToInt(cell.width / 8));
            var uv = new Rect(0, 0, cell.width / checkSize, cell.height / checkSize);
            GUI.DrawTextureWithTexCoords(cell, _checkerTex, uv);
        }

        private Rect FitSpriteInCell(Rect cell, Sprite sprite, bool isPreview)
        {
            float cellAspect = cell.width / cell.height;
            float spriteAspect;

            if (isPreview || sprite.texture == null)
                spriteAspect = 1f;
            else
            {
                var tr = sprite.textureRect;
                spriteAspect = tr.width / tr.height;
            }

            float w, h;
            if (spriteAspect > cellAspect) { w = cell.width; h = w / spriteAspect; }
            else { h = cell.height; w = h * spriteAspect; }

            return new Rect(cell.x + (cell.width - w) * 0.5f, cell.y + (cell.height - h) * 0.5f, w, h);
        }

        private Rect GetSpriteUV(Sprite sprite, bool isPreview)
        {
            if (isPreview)
                return new Rect(0, 0, 1, 1);

            var tex     = sprite.texture;
            var texRect = sprite.textureRect;
            return new Rect(
                texRect.x / tex.width, texRect.y / tex.height,
                texRect.width / tex.width, texRect.height / tex.height);
        }

        private static void DrawRectOutline(Rect r, Color color, float t)
        {
            EditorGUI.DrawRect(new Rect(r.x, r.y, r.width, t), color);
            EditorGUI.DrawRect(new Rect(r.x, r.yMax - t, r.width, t), color);
            EditorGUI.DrawRect(new Rect(r.x, r.y, t, r.height), color);
            EditorGUI.DrawRect(new Rect(r.xMax - t, r.y, t, r.height), color);
        }

        // ── 橡皮 / 油漆桶 ────────────────────────────────────────────────
        private void DrawEraseTile(MapEditorCore core)
        {
            EditorGUILayout.Space(4);
            Color bgOld = GUI.backgroundColor;

            bool isErasing = core.EditMode == EditMode.TileErase;
            GUI.backgroundColor = isErasing ? new Color(1f, 0.5f, 0.5f) : bgOld;
            if (GUILayout.Button("橡皮擦（清除 Tile）", GUILayout.Height(24)))
                core.EditMode = EditMode.TileErase;

            bool isFilling = core.EditMode == EditMode.TileFill;
            GUI.backgroundColor = isFilling ? new Color(1f, 0.8f, 0.3f) : bgOld;
            if (GUILayout.Button("油漆桶（填充区域）", GUILayout.Height(24)))
                core.EditMode = EditMode.TileFill;

            GUI.backgroundColor = bgOld;
        }

        // ── 纹理缓存 ────────────────────────────────────────────────────
        private Texture2D GetColorTex(Color c)
        {
            if (_colorTexCache.TryGetValue(c, out var tex)) return tex;

            tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, c);
            tex.Apply();
            tex.hideFlags = HideFlags.HideAndDontSave;
            _colorTexCache[c] = tex;
            return tex;
        }

        private void EnsureTextures()
        {
            if (_checkerTex == null)
            {
                int size = 16;
                _checkerTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                _checkerTex.hideFlags = HideFlags.HideAndDontSave;
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        bool light = (x % 8 < 4) ^ (y % 8 < 4);
                        _checkerTex.SetPixel(x, y, light ? CheckerLight : CheckerDark);
                    }
                _checkerTex.Apply();
                _checkerTex.wrapMode = TextureWrapMode.Repeat;
            }

            if (_emptyTex == null)
            {
                _emptyTex = new Texture2D(1, 1);
                _emptyTex.SetPixel(0, 0, EmptyCellBg);
                _emptyTex.Apply();
                _emptyTex.hideFlags = HideFlags.HideAndDontSave;
            }
        }

        private static void RepaintPanel()
        {
            SceneView.RepaintAll();
            var win = EditorWindow.focusedWindow;
            if (win != null) win.Repaint();
        }
    }
}