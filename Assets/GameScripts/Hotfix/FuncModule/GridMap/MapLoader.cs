using System.Collections.Generic;
using Main.Core;
using Main.FuncModule;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Tilemaps;
using ZEngine.Manager.Log;
using ZEngine.Manager.Resource;

namespace Hotfix.FuncModule
{
    /// <summary>
    /// 运行时地图加载器（纯 C# 单例）。
    /// 通过 MapLoader.Instance.Load(mapId) 加载地图。
    /// TileSetData 放置在 Resources/SO/TileSets/ 目录，Init() 时自动扫描。
    /// </summary>
    public class MapLoader : Singleton<MapLoader>
    {
        // ── 地图原点（替代 MonoBehaviour.transform.position）────────────────
        /// <summary>地图左下角世界坐标，在 Load() 前设置。默认 Vector3.zero。</summary>
        public Vector3 MapOrigin = Vector3.zero;

        // ── 事件 ─────────────────────────────────────────────────────────────
        public event System.Action<MapSaveData> OnMapLoaded;

        // ── 内部状态 ──────────────────────────────────────────────────────────
        private MapSaveData                    _mapData;
        private CellRuntime[,]                 _grid;
        private Grid                           _tilemapGrid;
        private readonly Dictionary<string, Tilemap> _layerTilemaps  = new Dictionary<string, Tilemap>();
        private readonly List<GameObject>            _objectInstances = new List<GameObject>();
        private readonly List<Tile>                      _runtimeTiles    = new List<Tile>();
        private          Dictionary<int, TileEntry>           _tileLookup       = new Dictionary<int, TileEntry>();
        private          TileSetData[]                        _tileSetDatas;
        private readonly Dictionary<MapObjectType, GameObject> _objectGroupRoots = new Dictionary<MapObjectType, GameObject>();
        private readonly Dictionary<string, ObjectRuntime>    _objectRuntimes   = new Dictionary<string, ObjectRuntime>();


        protected override void DestroySingleton()
        {
            UnloadMap();
            base.DestroySingleton();
        }
        
        /// <summary>
        /// 从 Resources/Configs/Maps/{id}.json 加载地图
        /// </summary>
        /// <param name="id"></param>
        public void Load(string id)
        {
            var handle = ResourceManager.Instance.LoadAssetSync<TextAsset>(HotfixAssetPaths.Config_Map + id);
            var asset = handle?.AssetObject as TextAsset;
            if (asset == null)
            {
                LogManager.Instance.Error($"[MapLoader] 找不到地图资源: {HotfixAssetPaths.Config_Map}{id}");
                return;
            }
            LoadFromJson(asset.text);
        }

        /// <summary>
        /// 从 JSON 字符串加载地图（适用于网络下发、热更等场景）
        /// </summary>
        /// <param name="json"></param>
        public void LoadFromJson(string json)
        {
            UnloadMap();     // 先清理上一次加载的资源
            _mapData = JsonConvert.DeserializeObject<MapSaveData>(json);
            if (_mapData == null)
            {
                LogManager.Instance.Error("[MapLoader] 地图 JSON 解析失败");
                return;
            }

            BuildGrid();
            BuildTilemaps();
            SpawnObjects();

            OnMapLoaded?.Invoke(_mapData);

            LogManager.Instance.Info($"[MapLoader] 地图 '{_mapData.mapId}' 加载完成 " +
                      $"({_mapData.width}×{_mapData.height}，" +
                      $"对象 {_mapData.objects.Count} 个，" +
                      $"出生点 {_mapData.spawnPoints.Count} 个)");
        }

        /// <summary>
        /// 卸载当前地图，释放运行时创建的资源
        /// </summary>
        public void UnloadMap()
        {
            // 清理 Tile 实例
            foreach (var t in _runtimeTiles)
                if (t != null) Object.Destroy(t);
            _runtimeTiles.Clear();

            // 清理 Tilemap 图层
            foreach (var tm in _layerTilemaps.Values)
                if (tm != null) Object.Destroy(tm.gameObject);
            _layerTilemaps.Clear();

            // 清理 MapGrid 容器
            if (_tilemapGrid != null)
            {
                Object.Destroy(_tilemapGrid.gameObject);
                _tilemapGrid = null;
            }

            // 清理对象分组根节点（子对象随父节点一并销毁）
            foreach (var root in _objectGroupRoots.Values)
                if (root != null) Object.Destroy(root);
            _objectGroupRoots.Clear();

            // 清理已实例化对象（分组根节点销毁后这些引用已无效，仅清空列表）
            _objectInstances.Clear();

            _tileLookup.Clear();
            _objectRuntimes.Clear();
            _tileSetDatas = null;
            _grid    = null;
            _mapData = null;
        }

        
        #region 内部构建

        private void BuildGrid()
        {
            int w = _mapData.width, h = _mapData.height;
            _grid = new CellRuntime[w, h];

            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                    _grid[x, y] = new CellRuntime(x, y, null, _mapData.schema);

            foreach (var cell in _mapData.cells)
            {
                if (cell.x < 0 || cell.x >= w || cell.y < 0 || cell.y >= h) continue;
                _grid[cell.x, cell.y] = new CellRuntime(cell.x, cell.y, cell.props, _mapData.schema);
            }
        }

        private void BuildTilemaps()
        {
            // 创建场景根节点 MapGrid 容器
            var mapRootHandle = ResourceManager.Instance.LoadAssetSync<GameObject>(HotfixAssetPaths.PrefabGridMapPath + "GridMap");
            var mapRootPrefab = mapRootHandle?.AssetObject as GameObject;
            GameObject gridGo;
            if(mapRootPrefab != null)
            {
                gridGo = GameObject.Instantiate(mapRootPrefab, Vector3.zero, Quaternion.identity);
                gridGo.AddComponent<MapSurfaceSpawner>();
            }
            else
            {
                gridGo = new GameObject("MapGrid");   
                gridGo.AddComponent<Grid>();
                gridGo.AddComponent<MapSurfaceSpawner>();
            }
            gridGo.transform.position = MapOrigin;
            _tilemapGrid = gridGo.GetComponent<Grid>();
            _tilemapGrid.cellSize = new Vector3(_mapData.cellSize, _mapData.cellSize, 0f);

            _tileLookup = BuildTileLookup();

            bool hasSchema = _mapData.schema?.layers != null && _mapData.schema.layers.Count > 0;
            if (hasSchema)
            {
                var sorted = new List<LayerSnapshot>(_mapData.schema.layers);
                sorted.Sort((a, b) => a.sortOrder.CompareTo(b.sortOrder));
                foreach (var sl in sorted)
                {
                    var ld = _mapData.layers?.Find(l => l.id == sl.id);
                    if (ld != null) CreateLayer(ld, sl.sortOrder, _tileLookup);
                }
            }
            else if (_mapData.layers != null)
            {
                for (int i = 0; i < _mapData.layers.Count; i++)
                    CreateLayer(_mapData.layers[i], i * 10, _tileLookup);
            }
        }

        private void CreateLayer(LayerTileData layerData, int sortingOrder, Dictionary<int, TileEntry> tileLookup)
        {
            var go       = new GameObject($"Layer_{layerData.id}");
            go.transform.SetParent(_tilemapGrid.transform, false);

            var tm       = go.AddComponent<Tilemap>();
            var renderer = go.AddComponent<TilemapRenderer>();
            renderer.sortingOrder        = sortingOrder;
            _layerTilemaps[layerData.id] = tm;

            if (layerData.tiles == null) return;

            for (int y = 0; y < _mapData.height; y++)
            for (int x = 0; x < _mapData.width;  x++)
            {
                int idx    = y * _mapData.width + x;
                if (idx >= layerData.tiles.Length) continue;
                int tileId = layerData.tiles[idx];
                if (tileId < 0) continue;

                if (!tileLookup.TryGetValue(tileId, out var entry)) continue;

                var sprite = ResolveSpriteWithSeed(entry, x, y);
                var tile   = ScriptableObject.CreateInstance<Tile>();
                tile.sprite       = sprite;
                tile.colliderType = Tile.ColliderType.None;
                _runtimeTiles.Add(tile);
                tm.SetTile(new Vector3Int(x, y, 0), tile);
            }
        }

        private Dictionary<int, TileEntry> BuildTileLookup()
        {
            EnsureTileSetDatas();
            var lookup = new Dictionary<int, TileEntry>();
            if (_tileSetDatas == null) return lookup;

            foreach (var tsd in _tileSetDatas)
            {
                if (tsd == null) continue;
                foreach (var entry in tsd.tiles)
                {
                    if (lookup.ContainsKey(entry.tileId)) continue;
                    if (entry.sprite == null && !entry.useRandom)
                    {
                        Debug.LogWarning($"[MapLoader] Tile '{entry.tileName}' (id={entry.tileId}) 没有 Sprite，运行时将不可见");
                        continue;
                    }
                    lookup[entry.tileId] = entry;
                }
            }
            return lookup;
        }

        private static Sprite ResolveSpriteWithSeed(TileEntry entry, int x, int y)
            => entry.ResolveSprite(x, y);

        private void SpawnObjects()
        {
            string basePath = GetObjectResourceBasePath();
            float cs = CellSize;
            Vector3 origin = MapOrigin;

            _objectRuntimes.Clear();
            foreach (var obj in _mapData.objects)
            {
                _objectRuntimes[obj.instanceId] = new ObjectRuntime(obj, _mapData.schema);
                string resourcePath = $"{basePath}/{obj.prefabId}";
                var prefabHandle = ResourceManager.Instance.LoadAssetSync<GameObject>(resourcePath);
                var prefab = prefabHandle?.AssetObject as GameObject;
                if (prefab == null)
                    prefab = FindPrefabByName(basePath, obj.prefabId);
                if (prefab == null)
                {
                    LogManager.Instance.Warning($"[MapLoader] 找不到对象 Prefab: {resourcePath}");
                    continue;
                }

                // 编辑器：obj.x/y 为左下角格子坐标，精灵在矩形内水平居中、底部对齐。
                // 运行时：GameObject 世界坐标 = 精灵 pivot 处。
                float worldX = origin.x + (obj.x + obj.width * 0.5f) * cs;
                float worldY = origin.y + obj.y * cs;
                var sr = prefab.GetComponentInChildren<SpriteRenderer>();
                if (sr?.sprite != null)
                {
                    float normalizedPivotY = sr.sprite.pivot.y / sr.sprite.rect.height;
                    worldY += normalizedPivotY * sr.sprite.bounds.size.y;
                }

                var worldPos = new Vector3(worldX, worldY, origin.z);
                var groupRoot = GetOrCreateObjectGroup(obj.objectType);
                var instance = Object.Instantiate(prefab, worldPos, Quaternion.Euler(0, 0, obj.rotation), groupRoot.transform);
                instance.name = $"Obj_{obj.instanceId}_{obj.prefabId}";
                _objectInstances.Add(instance);
            }
        }

        private GameObject GetOrCreateObjectGroup(MapObjectType type)
        {
            if (_objectGroupRoots.TryGetValue(type, out var root) && root != null)
                return root;

            root = new GameObject($"Objects_{type}");
            if (_tilemapGrid != null)
                root.transform.SetParent(_tilemapGrid.transform, false);
            _objectGroupRoots[type] = root;
            return root;
        }

        private string GetObjectResourceBasePath()
        {
            if (_mapData != null && !string.IsNullOrEmpty(_mapData.objectResourcePath))
                return _mapData.objectResourcePath.TrimEnd('/');
            return "Prefabs/MapObjects";
        }

        private static GameObject FindPrefabByName(string basePath, string prefabId)
        {
            string fileName = prefabId;
            int lastSlash = prefabId.LastIndexOf('/');
            if (lastSlash >= 0)
                fileName = prefabId.Substring(lastSlash + 1);

            var foundHandle = ResourceManager.Instance.LoadAssetSync<GameObject>($"{basePath}/{fileName}");
            GameObject found = foundHandle?.AssetObject as GameObject;
            if (found != null) return found;

            string[] subDirs = new[] { "Building", "Items", "Plant", "Decor", "Nature" };
            foreach (string dir in subDirs)
            {
                var subHandle = ResourceManager.Instance.LoadAssetSync<GameObject>($"{basePath}/{dir}/{fileName}");
                found = subHandle?.AssetObject as GameObject;
                if (found != null) return found;
            }

            return null;
        }

        /// <summary>懒加载 TileSetData：首次 BuildTileLookup 时按 HotfixAssetPaths.TileSetAssets 列表逐个加载。</summary>
        private void EnsureTileSetDatas()
        {
            if (_tileSetDatas != null) return;
            var list = new List<TileSetData>();
            foreach (var path in HotfixAssetPaths.TileSetAssets)
            {
                var handle = ResourceManager.Instance.LoadAssetSync<TileSetData>(path);
                var data = handle?.AssetObject as TileSetData;
                if (data != null) list.Add(data);
            }
            _tileSetDatas = list.ToArray();
            if (_tileSetDatas.Length == 0)
                LogManager.Instance.Warning("[MapLoader] 未找到任何 TileSetData，瓦片将无法渲染");
            else
                LogManager.Instance.Info($"[MapLoader] 加载到 {_tileSetDatas.Length} 个 TileSetData");
        }
        
        #endregion
        

        #region 格子相关 API

        public CellRuntime GetCell(int x, int y)
        {
            if (_grid == null || _mapData == null) return null;
            if (x < 0 || x >= _mapData.width || y < 0 || y >= _mapData.height) return null;
            return _grid[x, y];
        }

        public bool   IsValid    (int x, int y) => _mapData != null && x >= 0 && x < _mapData.width && y >= 0 && y < _mapData.height;
        public bool   IsWalkable (int x, int y) => GetCell(x, y)?.IsWalkable  ?? false;
        public bool   IsBuildable(int x, int y) => GetCell(x, y)?.IsBuildable ?? false;
        public float  PathWeight (int x, int y) => GetCell(x, y)?.PathWeight  ?? 1f;
        public string TerrainType(int x, int y) => GetCell(x, y)?.TerrainType ?? "";
        public string Zone       (int x, int y) => GetCell(x, y)?.Zone        ?? "";

        public List<Vector2Int> GetCellsByZone(string zone)
        {
            var result = new List<Vector2Int>();
            if (_grid == null || _mapData == null) return result;
            int w = _mapData.width, h = _mapData.height;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    var cell = _grid[x, y];
                    if (cell != null && cell.IsBuildable && cell.Zone == zone)
                        result.Add(new Vector2Int(x, y));
                }
            return result;
        }


        public void SetTile(string layerId, int x, int y, int tileId)
        {
            if (!IsLoaded) return;
            if (!_layerTilemaps.TryGetValue(layerId, out var tilemap)) return;

            var pos = new Vector3Int(x, y, 0);
            if (tileId < 0) { tilemap.SetTile(pos, null); return; }

            if (_tileLookup.TryGetValue(tileId, out var entry))
            {
                var tile = ScriptableObject.CreateInstance<Tile>();
                tile.sprite       = ResolveSpriteWithSeed(entry, x, y);
                tile.colliderType = Tile.ColliderType.None;
                _runtimeTiles.Add(tile);
                tilemap.SetTile(pos, tile);
            }
            else
                Debug.LogWarning($"[MapLoader] SetTile: tileId={tileId} 在 TileSetData 中找不到对应条目");
        }

        public void SetCellProp(int x, int y, string key, string value)
            => GetCell(x, y)?.Set(key, value);

        #endregion
        

        #region 坐标转换

        public Vector3 GridToWorld(int x, int y)
        {
            float cs = CellSize;
            return new Vector3(
                MapOrigin.x + x * cs + cs * 0.5f,
                MapOrigin.y + y * cs + cs * 0.5f,
                MapOrigin.z);
        }

        public Vector2Int WorldToGrid(Vector3 world)
        {
            float cs = CellSize;
            return new Vector2Int(
                Mathf.FloorToInt((world.x - MapOrigin.x) / cs),
                Mathf.FloorToInt((world.y - MapOrigin.y) / cs));
        }

        #endregion
        

        #region 寻路矩阵
        public bool[,] GetWalkabilityMatrix()
        {
            if (_grid == null) return new bool[0, 0];
            int w = _mapData.width, h = _mapData.height;
            var mat = new bool[w, h];
            for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                mat[x, y] = _grid[x, y]?.IsWalkable ?? false;
            return mat;
        }

        public float[,] GetPathWeightMatrix()
        {
            if (_grid == null) return new float[0, 0];
            int w = _mapData.width, h = _mapData.height;
            var mat = new float[w, h];
            for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                mat[x, y] = _grid[x, y]?.PathWeight ?? 1f;
            return mat;
        }
        #endregion

        
        #region 出生点 API
        
        public List<SpawnPointSaveData> GetSpawnPoints()
            => _mapData?.spawnPoints ?? new List<SpawnPointSaveData>();

        public List<SpawnPointSaveData> GetSpawnPointsByType(string type)
            => _mapData?.spawnPoints?.FindAll(sp => sp.type == type) ?? new List<SpawnPointSaveData>();

        public Vector3 GetSpawnWorldPos(SpawnPointSaveData sp) => GridToWorld(sp.x, sp.y);

        public static Quaternion FacingToRotation(string facing)
        {
            switch (facing)
            {
                case "up":    return Quaternion.Euler(0, 0,   0);
                case "down":  return Quaternion.Euler(0, 0, 180);
                case "left":  return Quaternion.Euler(0, 0,  90);
                case "right": return Quaternion.Euler(0, 0, -90);
                default:      return Quaternion.identity;
            }
        }

        #endregion

        
        #region 对象查询
        public List<ObjectSaveData> GetObjects()
            => _mapData?.objects ?? new List<ObjectSaveData>();

        public List<ObjectSaveData> GetObjectsByPrefab(string prefabId)
            => _mapData?.objects?.FindAll(o => o.prefabId == prefabId) ?? new List<ObjectSaveData>();

        public List<ObjectSaveData> GetObjectsByType(MapObjectType mapObjectType)
            => _mapData?.objects?.FindAll(o => o.objectType == mapObjectType) ?? new List<ObjectSaveData>();

        public ObjectRuntime GetObjectRuntime(string instanceId)
        {
            _objectRuntimes.TryGetValue(instanceId, out var rt);
            return rt;
        }

        public List<ObjectRuntime> GetObjectRuntimes()
        {
            var list = new List<ObjectRuntime>(_objectRuntimes.Count);
            foreach (var rt in _objectRuntimes.Values) list.Add(rt);
            return list;
        }

        public List<ObjectRuntime> GetObjectRuntimesByType(MapObjectType type)
        {
            var list = new List<ObjectRuntime>();
            foreach (var rt in _objectRuntimes.Values)
                if (rt.ObjectType == type) list.Add(rt);
            return list;
        }

        #endregion
        
        
        #region 元数据属性
        public MapSaveData GetMapData() => _mapData;
        public int         MapWidth     => _mapData?.width    ?? 0;
        public int         MapHeight    => _mapData?.height   ?? 0;
        public float       CellSize     => _mapData?.cellSize ?? 1f;
        public bool        IsLoaded     => _mapData != null && _grid != null;

        #endregion
    }
}
