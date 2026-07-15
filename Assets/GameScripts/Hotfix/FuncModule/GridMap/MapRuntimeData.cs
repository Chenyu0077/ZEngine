using System;
using System.Collections.Generic;
using Main.FuncModule;
using Newtonsoft.Json;

namespace Hotfix.FuncModule
{
    // ─── Schema 快照（嵌入 JSON，使 MapData 自描述）─────────────────────────

    public class CellPropertySnapshot
    {
        public string key;
        public string type;
        public string defaultValue;
    }

    public class LayerSnapshot
    {
        public string id;
        public int    sortOrder;
    }

    public class MapSchema
    {
        public List<CellPropertySnapshot> cellProperties   = new List<CellPropertySnapshot>();
        public List<CellPropertySnapshot> objectProperties = new List<CellPropertySnapshot>();
        public List<string>               terrainTypes     = new List<string>();
        public List<string>               zoneTypes        = new List<string>();
        public List<LayerSnapshot>        layers           = new List<LayerSnapshot>();
    }

    // ─── 格子数据（稀疏，仅存非默认值）─────────────────────────────────────

    public class CellSaveData
    {
        public int x;
        public int y;
        public Dictionary<string, string> props = new Dictionary<string, string>();
    }

    // ─── 图层 Tile 数据 ────────────────────────────────────────────────────

    public class LayerTileData
    {
        public string id;
        public int[]  tiles;  // 一维展开：tiles[y * width + x]，-1 表示空
    }

    // ─── 大型对象 ──────────────────────────────────────────────────────────
    // 注：MapObjectType 枚举已移至 Main/FuncModule/GridMap/MapObjectType.cs
    //     （Main、Hotfix、Editor 三方共享，须放依赖链底层）

    public class ObjectSaveData
    {
        public string        instanceId;
        public string        prefabId;
        public MapObjectType objectType = MapObjectType.Other;
        public int           x, y;
        public int           width  = 1;
        public int           height = 1;
        // 实际碰撞占地（可小于 width/height，0 表示与 width/height 相同）
        public int           footprintW = 0;
        public int           footprintH = 0;
        public int           rotation;
        public Dictionary<string, string> props = new Dictionary<string, string>();

        public int ActualFootprintW => footprintW > 0 ? footprintW : width;
        public int ActualFootprintH => footprintH > 0 ? footprintH : height;
    }

    // ─── 出生点 ────────────────────────────────────────────────────────────

    public class SpawnPointSaveData
    {
        public string id;
        public int    x, y;
        public string type   = "npc";
        public string npcId  = "";
        public string facing = "down";
    }

    // ─── 顶层 MapData ──────────────────────────────────────────────────────

    public class MapSaveData
    {
        public string mapId         = "map_01";
        public string mapName       = "新地图";
        public int    width         = 40;
        public int    height        = 40;
        public float  cellSize      = 1.0f;
        public string schemaVersion = "1.0";
        public string configPath    = "";
        public string objectResourcePath = "Prefabs/MapObjects";

        public MapSchema                schema      = new MapSchema();
        public List<LayerTileData>      layers      = new List<LayerTileData>();
        public List<CellSaveData>       cells       = new List<CellSaveData>();
        public List<ObjectSaveData>     objects     = new List<ObjectSaveData>();
        public List<SpawnPointSaveData> spawnPoints = new List<SpawnPointSaveData>();
    }

    // ─── Runtime 对象访问封装 ─────────────────────────────────────────────

    public class ObjectRuntime
    {
        private readonly ObjectSaveData _data;
        private readonly MapSchema      _schema;

        public string        InstanceId => _data.instanceId;
        public string        PrefabId   => _data.prefabId;
        public MapObjectType ObjectType => _data.objectType;
        public int           X          => _data.x;
        public int           Y          => _data.y;
        public int           Width      => _data.width;
        public int           Height     => _data.height;
        public int           Rotation   => _data.rotation;

        public ObjectRuntime(ObjectSaveData data, MapSchema schema)
        {
            _data   = data;
            _schema = schema;
        }

        public string GetString(string key)
        {
            if (_data.props != null && _data.props.TryGetValue(key, out string v)) return v;
            return GetDefault(key);
        }

        public bool  GetBool (string key) => string.Equals(GetString(key), "true", StringComparison.OrdinalIgnoreCase);
        public int   GetInt  (string key) => int.TryParse  (GetString(key), out int   r) ? r : 0;
        public float GetFloat(string key) => float.TryParse(GetString(key), out float r) ? r : 0f;

        public void Set(string key, string value)
        {
            if (_data.props == null) _data.props = new Dictionary<string, string>();
            _data.props[key] = value;
        }

        public bool HasProp(string key) => _data.props != null && _data.props.ContainsKey(key);

        public bool IsDefault(string key)
            => string.Equals(GetString(key), GetDefault(key), StringComparison.OrdinalIgnoreCase);

        private string GetDefault(string key)
        {
            if (_schema?.objectProperties == null) return "";
            foreach (var def in _schema.objectProperties)
                if (def.key == key) return def.defaultValue ?? "";
            return "";
        }

        public ObjectSaveData RawData => _data;
        
        public int   EntryGridX  => GetInt("entryGridX");
        public int   EntryGridY => GetInt("entryGridY");
        public string  Type  => GetString("type");
        public int Capacity => GetInt("capacity");
    }

    // ─── Runtime 格子访问封装 ─────────────────────────────────────────────

    public class CellRuntime
    {
        private readonly Dictionary<string, string> _props;
        private readonly MapSchema                  _schema;

        public int X { get; }
        public int Y { get; }

        public CellRuntime(int x, int y, Dictionary<string, string> props, MapSchema schema)
        {
            X       = x;
            Y       = y;
            _props  = props ?? new Dictionary<string, string>();
            _schema = schema;
        }

        public string GetString(string key)
        {
            if (_props.TryGetValue(key, out string v)) return v;
            return GetDefault(key);
        }

        public bool GetBool(string key)
        {
            return string.Equals(GetString(key), "true", StringComparison.OrdinalIgnoreCase);
        }

        public float GetFloat(string key)
        {
            return float.TryParse(GetString(key), out float r) ? r : 0f;
        }

        public int GetInt(string key)
        {
            return int.TryParse(GetString(key), out int r) ? r : 0;
        }

        public void Set(string key, string value) => _props[key] = value;

        public bool IsDefault(string key, string value)
        {
            return string.Equals(GetDefault(key), value, StringComparison.OrdinalIgnoreCase);
        }

        private string GetDefault(string key)
        {
            if (_schema?.cellProperties == null) return "";
            foreach (var def in _schema.cellProperties)
                if (def.key == key) return def.defaultValue ?? "";
            return "";
        }

        public bool   IsWalkable  => GetBool("walkable");
        public bool   IsBuildable => GetBool("buildable");
        public float  PathWeight  => GetFloat("pathWeight");
        public string TerrainType => GetString("terrainType");
        public string Zone        => GetString("zone");
    }
}
