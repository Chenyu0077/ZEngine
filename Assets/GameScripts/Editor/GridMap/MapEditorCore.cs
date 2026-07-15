using System.Collections.Generic;
using Hotfix.FuncModule;
using Main.FuncModule;
using Newtonsoft.Json;
using UnityEngine;

namespace GameScripts.Editor
{
    public enum EditMode
    {
        None,
        TilePaint,
        TileErase,
        TileFill,
        ObjectPlace,
        ObjectErase,
        ObjectSelect,
        CellEdit,
        SpawnEdit,
        Select,
    }

    public class LayerState
    {
        public string id;
        public bool   visible  = true;
        public bool   locked   = false;
        public float  opacity  = 1f;
    }

    public class SpawnPointOverride
    {
        public SpawnPointSaveData data;
        public bool               selected;
    }

    public struct BrushTile
    {
        public int dx;
        public int dy;
        public int tileId;
    }

    /// <summary>
    /// 编辑器运行时状态 + 当前地图数据持有。
    /// </summary>
    public class MapEditorCore
    {
        // ── 当前地图数据 ────────────────────────────────────────────────
        public MapSaveData  MapData     { get; private set; }
        public bool         IsDirty     { get; set; }
        public string       CurrentPath { get; set; }

        // ── 编辑状态 ─────────────────────────────────────────────────────
        public EditMode  EditMode        { get; set; } = EditMode.TilePaint;
        public string    ActiveLayerId   { get; set; } = "ground";
        public int       SelectedTileId  { get; set; } = 0;
        public string        SelectedPrefabId     { get; set; } = "";
        public MapObjectType PlacementObjectType  { get; set; } = MapObjectType.Other;
        public int           PlacementWidth       { get; set; } = 1;
        public int           PlacementHeight      { get; set; } = 1;
        // 当前选中 prefab 的实际占地（碰撞用），0 = 与 PlacementWidth/Height 相同
        public int           PlacementFootprintW  { get; set; } = 0;
        public int           PlacementFootprintH  { get; set; } = 0;

        // ── 画笔状态（瓦片调色板框选后生成）───────────────────────────────
        public List<BrushTile> BrushTiles { get; set; } = new List<BrushTile>();
        public int             BrushW    { get; set; } = 1;
        public int             BrushH    { get; set; } = 1;

        // 悬停/选中格子坐标
        public Vector2Int    HoveredCell    { get; set; } = new Vector2Int(-1, -1);
        public Vector2Int    SelectedCell   { get; set; } = new Vector2Int(-1, -1);

        // 选中的放置对象
        public ObjectSaveData SelectedObject { get; set; }

        // 出生点拖拽中的新点
        public SpawnPointSaveData PendingSpawnPoint { get; set; }

        // ── 框选状态 ─────────────────────────────────────────────────────
        private int  _selX0, _selY0, _selX1, _selY1;
        public bool  HasSelection  { get; private set; }
        public bool  IsSelecting   { get; private set; }
        public int   SelectMinX    => Mathf.Min(_selX0, _selX1);
        public int   SelectMinY    => Mathf.Min(_selY0, _selY1);
        public int   SelectMaxX    => Mathf.Max(_selX0, _selX1);
        public int   SelectMaxY    => Mathf.Max(_selY0, _selY1);
        public int   SelectionW    => SelectMaxX - SelectMinX + 1;
        public int   SelectionH    => SelectMaxY - SelectMinY + 1;
        public int   SelectionCount => SelectionW * SelectionH;

        public void StartSelection(int x, int y)
        {
            _selX0 = _selX1 = Mathf.Clamp(x, 0, MapData != null ? MapData.width  - 1 : 0);
            _selY0 = _selY1 = Mathf.Clamp(y, 0, MapData != null ? MapData.height - 1 : 0);
            IsSelecting  = true;
            HasSelection = false;
        }

        public void UpdateSelection(int x, int y)
        {
            _selX1 = Mathf.Clamp(x, 0, MapData != null ? MapData.width  - 1 : 0);
            _selY1 = Mathf.Clamp(y, 0, MapData != null ? MapData.height - 1 : 0);
        }

        public void EndSelection()
        {
            IsSelecting  = false;
            HasSelection = true;
        }

        public void ClearSelection()
        {
            HasSelection = false;
            IsSelecting  = false;
        }

        public bool IsCellInSelection(int x, int y)
            => (HasSelection || IsSelecting)
               && x >= SelectMinX && x <= SelectMaxX
               && y >= SelectMinY && y <= SelectMaxY;

        // ── 图层运行时状态 ───────────────────────────────────────────────
        private readonly Dictionary<string, LayerState> _layerStates = new Dictionary<string, LayerState>();

        // ── 撤销栈（轻量：存每次操作前的 JSON 快照）──────────────────────
        private readonly Stack<string> _undoStack = new Stack<string>();
        private readonly Stack<string> _redoStack = new Stack<string>();
        private const int MaxUndoSteps = 30;

        // ── 叠加层开关（key = CellPropertyDef.key）──────────────────────
        private readonly Dictionary<string, bool> _overlayEnabled = new Dictionary<string, bool>();

        // ── 配置引用（编辑器会在 OnEnable 时注入）───────────────────────
        public MapEditorConfig Config { get; set; }

        // ────────────────────────────────────────────────────────────────
        public void NewMap(MapEditorConfig config)
        {
            Config = config;
            MapData = new MapSaveData
            {
                mapId    = "map_01",
                mapName  = "新地图",
                width    = config.defaultWidth,
                height   = config.defaultHeight,
                cellSize = config.defaultCellSize,
            };

            // 从 Config 生成 schema 快照
            RefreshSchema();
            // 初始化所有图层的 tile 数组
            RebuildLayerArrays();
            SyncLayerStates();

            IsDirty    = false;
            CurrentPath = null;
            ClearUndoRedo();
        }

        public void LoadMapData(MapSaveData data, MapEditorConfig config)
        {
            Config  = config;
            MapData = data;
            RefreshSchema();
            // 补全 Config 中新增但 JSON 里没有的图层
            RebuildLayerArrays();
            SyncLayerStates();
            IsDirty = false;
            ClearUndoRedo();
        }

        // ── Schema 快照同步 ─────────────────────────────────────────────
        public void RefreshSchema()
        {
            if (MapData == null || Config == null) return;
            var schema = new MapSchema();
            foreach (var def in Config.cellPropertySchema)
                schema.cellProperties.Add(new CellPropertySnapshot { key = def.key, type = def.valueType.ToString(), defaultValue = def.defaultValue });
            foreach (var t in Config.terrainTypes)  schema.terrainTypes.Add(t.id);
            foreach (var z in Config.zoneTypes)     schema.zoneTypes.Add(z.id);
            foreach (var l in Config.layers)        schema.layers.Add(new LayerSnapshot { id = l.id, sortOrder = l.sortOrder });
            MapData.schema = schema;
        }

        // ── 图层数组管理 ────────────────────────────────────────────────
        public void RebuildLayerArrays()
        {
            if (MapData == null || Config == null) return;
            int tileCount = MapData.width * MapData.height;

            // 确保每个 Config 图层在 MapData.layers 中都有对应条目
            foreach (var layerDef in Config.layers)
            {
                if (!MapData.layers.Exists(l => l.id == layerDef.id))
                {
                    var newLayer = new LayerTileData { id = layerDef.id, tiles = new int[tileCount] };
                    for (int i = 0; i < tileCount; i++) newLayer.tiles[i] = -1;
                    MapData.layers.Add(newLayer);
                }
                else
                {
                    // 尺寸变化时调整数组大小
                    var existing = MapData.layers.Find(l => l.id == layerDef.id);
                    if (existing.tiles == null || existing.tiles.Length != tileCount)
                    {
                        var newTiles = new int[tileCount];
                        for (int i = 0; i < tileCount; i++) newTiles[i] = -1;
                        if (existing.tiles != null)
                            System.Array.Copy(existing.tiles, newTiles, Mathf.Min(existing.tiles.Length, tileCount));
                        existing.tiles = newTiles;
                    }
                }
            }
        }

        private void SyncLayerStates()
        {
            if (Config == null) return;
            foreach (var def in Config.layers)
            {
                if (!_layerStates.ContainsKey(def.id))
                    _layerStates[def.id] = new LayerState { id = def.id, visible = def.defaultVisible, locked = def.defaultLocked, opacity = def.defaultOpacity };
            }
        }

        // ── 地图尺寸 / 格子大小变更 ─────────────────────────────────────
        public void ResizeMap(int newWidth, int newHeight, float newCellSize)
        {
            if (MapData == null) return;

            PushUndo();

            int oldW = MapData.width;
            int oldH = MapData.height;
            int copyW = Mathf.Min(oldW, newWidth);
            int copyH = Mathf.Min(oldH, newHeight);

            // 调整各图层 tile 数组
            foreach (var layer in MapData.layers)
            {
                var newTiles = new int[newWidth * newHeight];
                for (int i = 0; i < newTiles.Length; i++) newTiles[i] = -1;

                if (layer.tiles != null)
                {
                    for (int y = 0; y < copyH; y++)
                        for (int x = 0; x < copyW; x++)
                        {
                            int src = y * oldW    + x;
                            int dst = y * newWidth + x;
                            if (src < layer.tiles.Length)
                                newTiles[dst] = layer.tiles[src];
                        }
                }
                layer.tiles = newTiles;
            }

            // 移除超出新边界的稀疏格子数据
            MapData.cells.RemoveAll(c => c.x >= newWidth || c.y >= newHeight);
            // 移除超出范围的出生点
            MapData.spawnPoints.RemoveAll(sp => sp.x >= newWidth || sp.y >= newHeight);
            // 移除超出范围的对象（起点超出即移除）
            MapData.objects.RemoveAll(o => o.x >= newWidth || o.y >= newHeight);

            MapData.width    = newWidth;
            MapData.height   = newHeight;
            MapData.cellSize = newCellSize;

            IsDirty = true;
        }

        // ── 图层状态访问 ────────────────────────────────────────────────
        public LayerState GetLayerState(string layerId)
        {
            if (!_layerStates.TryGetValue(layerId, out var state))
            {
                state = new LayerState { id = layerId };
                _layerStates[layerId] = state;
            }
            return state;
        }

        // ── Tile 读写 ────────────────────────────────────────────────────
        public int GetTile(string layerId, int x, int y)
        {
            var layer = GetLayer(layerId);
            if (layer?.tiles == null) return -1;
            int idx = y * MapData.width + x;
            if (idx < 0 || idx >= layer.tiles.Length) return -1;
            return layer.tiles[idx];
        }

        public void SetTile(string layerId, int x, int y, int tileId)
        {
            if (!IsValidCell(x, y)) return;
            var state = GetLayerState(layerId);
            if (state.locked) return;

            var layer = GetLayer(layerId);
            if (layer == null) return;

            int idx = y * MapData.width + x;
            if (layer.tiles[idx] == tileId) return;

            PushUndo();
            layer.tiles[idx] = tileId;
            IsDirty = true;
        }

        public void FloodFill(string layerId, int x, int y, int newTileId)
        {
            if (!IsValidCell(x, y)) return;
            int oldTileId = GetTile(layerId, x, y);
            if (oldTileId == newTileId) return;

            PushUndo();
            FloodFillInternal(layerId, x, y, oldTileId, newTileId);
            IsDirty = true;
        }

        private void FloodFillInternal(string layerId, int x, int y, int oldId, int newId)
        {
            var layer = GetLayer(layerId);
            if (layer?.tiles == null) return;

            int w = MapData.width, h = MapData.height;
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(new Vector2Int(x, y));

            while (queue.Count > 0)
            {
                var c = queue.Dequeue();
                int cx = c.x, cy = c.y;
                if (cx < 0 || cx >= w || cy < 0 || cy >= h) continue;
                int idx = cy * w + cx;
                if (layer.tiles[idx] != oldId) continue;
                layer.tiles[idx] = newId;
                queue.Enqueue(new Vector2Int(cx + 1, cy));
                queue.Enqueue(new Vector2Int(cx - 1, cy));
                queue.Enqueue(new Vector2Int(cx, cy + 1));
                queue.Enqueue(new Vector2Int(cx, cy - 1));
            }
        }

        // ── 画笔批量盖印 ────────────────────────────────────────────────
        public void StampBrush(string layerId, int originX, int originY)
        {
            if (BrushTiles == null || BrushTiles.Count == 0) return;
            var state = GetLayerState(layerId);
            if (state.locked) return;
            var layer = GetLayer(layerId);
            if (layer?.tiles == null) return;

            PushUndo();
            foreach (var bt in BrushTiles)
            {
                int x = originX + bt.dx;
                int y = originY + bt.dy;
                if (!IsValidCell(x, y)) continue;
                int idx = y * MapData.width + x;
                layer.tiles[idx] = bt.tileId;
            }
            IsDirty = true;
        }

        // ── 格子属性读写 ────────────────────────────────────────────────
        public CellSaveData GetCellData(int x, int y)
        {
            if (MapData == null) return null;
            return MapData.cells.Find(c => c.x == x && c.y == y);
        }

        public string GetCellProp(int x, int y, string key)
        {
            var cell = GetCellData(x, y);
            if (cell != null && cell.props.TryGetValue(key, out string v))
                return v;
            return GetDefaultProp(key);
        }

        public void SetCellProp(int x, int y, string key, string value)
        {
            if (!IsValidCell(x, y)) return;
            PushUndo();

            string defaultVal = GetDefaultProp(key);
            var cell = GetCellData(x, y);

            if (string.Equals(value, defaultVal, System.StringComparison.OrdinalIgnoreCase))
            {
                cell?.props.Remove(key);
                if (cell != null && cell.props.Count == 0)
                    MapData.cells.Remove(cell);
            }
            else
            {
                if (cell == null)
                {
                    cell = new CellSaveData { x = x, y = y };
                    MapData.cells.Add(cell);
                }
                cell.props[key] = value;
            }
            IsDirty = true;
        }

        private string GetDefaultProp(string key)
        {
            if (Config == null) return "";
            foreach (var def in Config.cellPropertySchema)
                if (def.key == key) return def.defaultValue ?? "";
            return "";
        }

        // ── 对象属性 ─────────────────────────────────────────────────────

        public string GetObjProp(ObjectSaveData obj, string key)
        {
            if (obj?.props != null && obj.props.TryGetValue(key, out string v)) return v;
            return GetDefaultObjProp(key);
        }

        public void SetObjProp(ObjectSaveData obj, string key, string value)
        {
            if (obj == null) return;
            PushUndo();

            string defaultVal = GetDefaultObjProp(key);
            if (string.Equals(value, defaultVal, System.StringComparison.OrdinalIgnoreCase))
                obj.props?.Remove(key);
            else
            {
                if (obj.props == null) obj.props = new System.Collections.Generic.Dictionary<string, string>();
                obj.props[key] = value;
            }
            IsDirty = true;
        }

        private string GetDefaultObjProp(string key)
        {
            if (Config == null) return "";
            foreach (var def in Config.objectPropertySchema)
                if (def.key == key) return def.defaultValue ?? "";
            return "";
        }

        // ── 选区填充瓦片 ──────────────────────────────────────────────
        public void FillSelectionWithTile(string layerId, int tileId)
        {
            if (!HasSelection) return;
            var state = GetLayerState(layerId);
            if (state.locked) return;

            var layer = GetLayer(layerId);
            if (layer?.tiles == null) return;

            PushUndo();

            for (int y = SelectMinY; y <= SelectMaxY; y++)
                for (int x = SelectMinX; x <= SelectMaxX; x++)
                {
                    int idx = y * MapData.width + x;
                    layer.tiles[idx] = tileId;
                }

            IsDirty = true;
        }

        // ── 批量修改（选区内所有格子）────────────────────────────────────
        public void BatchSetCellProp(string key, string value)
        {
            if (!HasSelection) return;
            PushUndo();

            string defaultVal = GetDefaultProp(key);

            for (int y = SelectMinY; y <= SelectMaxY; y++)
            for (int x = SelectMinX; x <= SelectMaxX; x++)
            {
                if (!IsValidCell(x, y)) continue;
                var cell = GetCellData(x, y);

                if (string.Equals(value, defaultVal, System.StringComparison.OrdinalIgnoreCase))
                {
                    cell?.props.Remove(key);
                    if (cell != null && cell.props.Count == 0)
                        MapData.cells.Remove(cell);
                }
                else
                {
                    if (cell == null)
                    {
                        cell = new CellSaveData { x = x, y = y };
                        MapData.cells.Add(cell);
                    }
                    cell.props[key] = value;
                }
            }
            IsDirty = true;
        }

        // ── 出生点管理 ──────────────────────────────────────────────────
        public void AddSpawnPoint(SpawnPointSaveData sp)
        {
            PushUndo();
            MapData.spawnPoints.Add(sp);
            IsDirty = true;
        }

        public void RemoveSpawnPoint(SpawnPointSaveData sp)
        {
            PushUndo();
            MapData.spawnPoints.Remove(sp);
            IsDirty = true;
        }

        // ── 对象管理 ─────────────────────────────────────────────────────
        public void AddObject(ObjectSaveData obj)
        {
            PushUndo();
            MapData.objects.Add(obj);
            IsDirty = true;
        }

        public void RemoveObject(ObjectSaveData obj)
        {
            PushUndo();
            MapData.objects.Remove(obj);
            IsDirty = true;
        }

        /// <summary>单格是否被任意对象占用。</summary>
        public bool IsCellOccupiedByObject(int x, int y)
        {
            foreach (var obj in MapData.objects)
            {
                if (x >= obj.x && x < obj.x + obj.width &&
                    y >= obj.y && y < obj.y + obj.height)
                    return true;
            }
            return false;
        }

        /// <summary>判断指定格子是否为空（所有图层 tileId 均为 -1）。</summary>
        public bool IsEmptyCell(int x, int y)
        {
            if (!IsValidCell(x, y)) return false;
            foreach (var layer in MapData.layers)
            {
                int idx = y * MapData.width + x;
                if (layer.tiles != null && idx >= 0 && idx < layer.tiles.Length && layer.tiles[idx] >= 0)
                    return false;
            }
            return true;
        }

        // ── 空格子批量属性修改 ─────────────────────────────────────────
        public void BatchSetEmptyCellProp(string key, string value)
        {
            if (MapData == null) return;
            PushUndo();

            string defaultVal = GetDefaultProp(key);

            for (int y = 0; y < MapData.height; y++)
            for (int x = 0; x < MapData.width; x++)
            {
                if (!IsEmptyCell(x, y)) continue;

                if (string.Equals(value, defaultVal, System.StringComparison.OrdinalIgnoreCase))
                {
                    var cell = GetCellData(x, y);
                    cell?.props.Remove(key);
                    if (cell != null && cell.props.Count == 0)
                        MapData.cells.Remove(cell);
                }
                else
                {
                    var cell = GetCellData(x, y);
                    if (cell == null)
                    {
                        cell = new CellSaveData { x = x, y = y };
                        MapData.cells.Add(cell);
                    }
                    cell.props[key] = value;
                }
            }
            IsDirty = true;
        }

        /// <summary>统计空格子数量。</summary>
        public int CountEmptyCells()
        {
            if (MapData == null) return 0;
            int count = 0;
            for (int y = 0; y < MapData.height; y++)
            for (int x = 0; x < MapData.width; x++)
                if (IsEmptyCell(x, y)) count++;
            return count;
        }

        /// <summary>
        /// 检查以 (x,y) 为左下角、尺寸为 (w×h) 的占位矩形是否可以放置：
        ///   1. 完整在地图边界内
        ///   2. 与所有已有对象无重叠
        /// </summary>
        public bool IsFootprintValid(int x, int y, int w, int h)
        {
            if (MapData == null) return false;

            // 越界检查
            if (x < 0 || y < 0 || x + w > MapData.width || y + h > MapData.height)
                return false;

            // 与已有对象的占地碰撞检测（用 ActualFootprintW/H，忽略视觉超出部分）
            foreach (var obj in MapData.objects)
            {
                bool overlapX = x < obj.x + obj.ActualFootprintW && x + w > obj.x;
                bool overlapY = y < obj.y + obj.ActualFootprintH && y + h > obj.y;
                if (overlapX && overlapY) return false;
            }

            return true;
        }

        // ── 叠加层开关 ──────────────────────────────────────────────────
        public bool IsOverlayEnabled(string key)
        {
            _overlayEnabled.TryGetValue(key, out bool v);
            return v;
        }
        public void SetOverlayEnabled(string key, bool enabled) => _overlayEnabled[key] = enabled;

        // ── 撤销/重做 ────────────────────────────────────────────────────
        public void PushUndo()
        {
            if (MapData == null) return;

            // 使用 Newtonsoft.Json，正确序列化 Dictionary<string,string>
            string json = JsonConvert.SerializeObject(MapData);
            _undoStack.Push(json);

            // 超出最大步数时从栈底移除最旧的快照
            if (_undoStack.Count > MaxUndoSteps)
            {
                var tmp = new Stack<string>();
                int keep = MaxUndoSteps;
                foreach (var s in _undoStack)
                {
                    if (keep-- > 0) tmp.Push(s);
                }
                _undoStack.Clear();
                foreach (var s in tmp) _undoStack.Push(s);
            }

            _redoStack.Clear();
        }

        public void Undo()
        {
            if (_undoStack.Count == 0) return;
            _redoStack.Push(JsonConvert.SerializeObject(MapData));
            MapData = JsonConvert.DeserializeObject<MapSaveData>(_undoStack.Pop());
            RebuildLayerArrays();
            IsDirty = true;
        }

        public void Redo()
        {
            if (_redoStack.Count == 0) return;
            _undoStack.Push(JsonConvert.SerializeObject(MapData));
            MapData = JsonConvert.DeserializeObject<MapSaveData>(_redoStack.Pop());
            RebuildLayerArrays();
            IsDirty = true;
        }

        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;

        private void ClearUndoRedo() { _undoStack.Clear(); _redoStack.Clear(); }

        // ── 工具方法 ─────────────────────────────────────────────────────
        public bool IsValidCell(int x, int y)
            => MapData != null && x >= 0 && x < MapData.width && y >= 0 && y < MapData.height;

        private LayerTileData GetLayer(string id)
            => MapData?.layers?.Find(l => l.id == id);

        public Vector3 GridToWorld(int x, int y, Vector3 origin)
        {
            float cs = MapData?.cellSize ?? 1f;
            return new Vector3(origin.x + x * cs + cs * 0.5f, origin.y + y * cs + cs * 0.5f, origin.z);
        }

        public Vector2Int WorldToGrid(Vector3 world, Vector3 origin)
        {
            float cs = MapData?.cellSize ?? 1f;
            return new Vector2Int(
                Mathf.FloorToInt((world.x - origin.x) / cs),
                Mathf.FloorToInt((world.y - origin.y) / cs));
        }
    }
}
