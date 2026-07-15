using System;
using System.Collections.Generic;
using UnityEngine;

namespace Main.FuncModule
{
    public enum PropType { Bool, Int, Float, String, Enum }

    [Serializable]
    public class LayerDefinition
    {
        public string id           = "layer";
        public string displayName  = "新图层";
        public int    sortOrder    = 0;
        public Color  debugColor   = new Color(1f, 1f, 1f, 0.3f);
        public bool   defaultVisible = true;
        public bool   defaultLocked  = false;
        [Range(0f, 1f)]
        public float  defaultOpacity = 1f;
    }

    [Serializable]
    public class EnumOverlayColorEntry
    {
        public string enumId = "";
        public Color  color   = new Color(1f, 1f, 1f, 0.3f);
    }

    [Serializable]
    public class CellPropertyDef
    {
        public string   key              = "property";
        public string   displayName      = "属性";
        public PropType valueType        = PropType.Bool;
        public string   defaultValue     = "false";
        public string   enumOptionsRef   = "";
        public bool     enableOverlay    = false;
        public string   overlayTrueValue = "true";
        public Color    overlayColor     = new Color(1f, 0.2f, 0.2f, 0.45f);
        [Tooltip("Enum 多色叠加：每个枚举值对应不同颜色，启用后忽略 overlayTrueValue/overlayColor")]
        public List<EnumOverlayColorEntry> enumOverlayColors = new List<EnumOverlayColorEntry>();

        public Color? GetEnumOverlayColor(string enumValue)
        {
            if (enumOverlayColors == null) return null;
            foreach (var entry in enumOverlayColors)
                if (entry.enumId == enumValue) return entry.color;
            return null;
        }
    }
    
    [Serializable]
    public class ObjectPropertyDef
    {
        public string   key              = "property";
        public string   displayName      = "属性";
        public PropType valueType        = PropType.Bool;
        public string   defaultValue     = "false";
        public string   enumOptionsRef   = "";
        public bool     enableOverlay    = false;
        public string   overlayTrueValue = "true";
    }

    [Serializable]
    public class TerrainTypeDef
    {
        public string id                  = "grass";
        public string displayName         = "草地";
        public Color  mapColor            = Color.green;
        public float  defaultPathWeight   = 1.0f;
    }

    [Serializable]
    public class ZoneTypeDef
    {
        public string id          = "none";
        public string displayName = "无区域";
        public Color  debugColor  = Color.clear;
    }

    [Serializable]
    public class SpawnPointTypeDef
    {
        public string id          = "npc";
        public string displayName = "NPC";
        public Color  gizmoColor  = Color.blue;
    }
    
    [Serializable]
    public class POITypeDef
    {
        public string id          = "house";
        public string displayName = "家";
    }

    [Serializable]
    public class TileSetReference
    {
        public string      id          = "tileset";
        public string      displayName = "瓦片集";
        public TileSetData tileSet;
        // tileId 在所有 TileSet 中全局唯一，不再绑定具体图层
    }

    [CreateAssetMenu(fileName = "MapEditorConfig", menuName = "Map Editor/Config")]
    public class MapEditorConfig : ScriptableObject
    {
        /// <summary>
        /// Inspector 修改配置后触发，MapEditorWindow 订阅此事件自动刷新 MapData。
        /// </summary>
        public static event System.Action<MapEditorConfig> OnConfigChanged;

        private void OnValidate()
        {
            MigrateZoneOverlay();
            OnConfigChanged?.Invoke(this);
        }

        private void MigrateZoneOverlay()
        {
            var zoneDef = cellPropertySchema?.Find(p => p.key == "zone");
            if (zoneDef == null) return;
            if (zoneDef.enableOverlay && zoneDef.enumOverlayColors != null && zoneDef.enumOverlayColors.Count > 0) return;

            zoneDef.enableOverlay = true;

            if (zoneDef.enumOverlayColors == null || zoneDef.enumOverlayColors.Count == 0)
            {
                zoneDef.enumOverlayColors = new List<EnumOverlayColorEntry>
                {
                    new EnumOverlayColorEntry { enumId = "residential", color = new Color(0.2f, 0.6f, 1f,   0.3f) },
                    new EnumOverlayColorEntry { enumId = "commercial",  color = new Color(1f,  0.85f,0.2f, 0.3f) },
                    new EnumOverlayColorEntry { enumId = "farm",        color = new Color(0.4f, 0.9f, 0.3f, 0.3f) },
                    new EnumOverlayColorEntry { enumId = "nature",      color = new Color(0.3f, 0.8f, 0.5f, 0.3f) },
                };
            }

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        [Header("图层配置（可增删）")]
        public List<LayerDefinition>    layers            = new List<LayerDefinition>();

        [Header("格子属性 Schema（可增删）")]
        public List<CellPropertyDef>    cellPropertySchema   = new List<CellPropertyDef>();

        [Header("对象属性 Schema（可增删）")]
        public List<ObjectPropertyDef>    objectPropertySchema = new List<ObjectPropertyDef>();

        [Header("类型枚举表（可增删）")]
        public List<TerrainTypeDef>     terrainTypes      = new List<TerrainTypeDef>();
        public List<ZoneTypeDef>        zoneTypes         = new List<ZoneTypeDef>();
        public List<SpawnPointTypeDef>  spawnPointTypes   = new List<SpawnPointTypeDef>();
        public List<POITypeDef>  poiTypes   = new List<POITypeDef>();

        [Header("资产引用")]
        public List<TileSetReference>   tileSets          = new List<TileSetReference>();
        public string objectPalettePath = "Assets/Resources/Prefabs/MapObjects";

        [Header("地图默认参数")]
        public int    defaultWidth    = 40;
        public int    defaultHeight   = 40;
        public float  defaultCellSize = 1.0f;
        public string exportPath      = "Assets/Resources/Maps";

        [Header("编辑器视觉偏好")]
        public Color gridLineColor  = new Color(1f, 1f, 1f, 0.2f);
        public float gridLineWidth  = 1f;
        public bool  showCellCoords = false;

        public void InitializeDefaults()
        {
            layers = new List<LayerDefinition>
            {
                new LayerDefinition { id = "ground",     displayName = "地面", sortOrder = 0,  debugColor = new Color(0.4f, 0.75f, 0.4f, 0.5f) },
                new LayerDefinition { id = "decoration", displayName = "装饰", sortOrder = 10, debugColor = new Color(0.4f, 0.4f,  0.8f, 0.5f) },
                new LayerDefinition { id = "overlay",    displayName = "顶层", sortOrder = 20, debugColor = new Color(0.8f, 0.75f, 0.4f, 0.5f) },
            };

            cellPropertySchema = new List<CellPropertyDef>
            {
                new CellPropertyDef { key = "walkable",     displayName = "可行走", valueType = PropType.Bool,  defaultValue = "true",  enableOverlay = true,  overlayTrueValue = "false", overlayColor = new Color(1f,   0.2f, 0.2f, 0.45f) },
                new CellPropertyDef { key = "buildable",    displayName = "可建造", valueType = PropType.Bool,  defaultValue = "true",  enableOverlay = true,  overlayTrueValue = "false", overlayColor = new Color(1f,   0.6f, 0.1f, 0.45f) },
                new CellPropertyDef { key = "farmable",     displayName = "可耕种", valueType = PropType.Bool,  defaultValue = "false", enableOverlay = true,  overlayTrueValue = "true",  overlayColor = new Color(0.2f, 0.85f,0.2f, 0.45f) },
                new CellPropertyDef { key = "terrainType",  displayName = "地形类型",valueType = PropType.Enum,  defaultValue = "grass", enumOptionsRef = "terrainTypes" },
                new CellPropertyDef { key = "zone",         displayName = "区域",   valueType = PropType.Enum,  defaultValue = "none",  enumOptionsRef = "zoneTypes", enableOverlay = true, enumOverlayColors = new List<EnumOverlayColorEntry>
                {
                    new EnumOverlayColorEntry { enumId = "residential", color = new Color(0.2f, 0.6f, 1f,   0.3f) },
                    new EnumOverlayColorEntry { enumId = "commercial",  color = new Color(1f,  0.85f,0.2f, 0.3f) },
                    new EnumOverlayColorEntry { enumId = "farm",        color = new Color(0.4f, 0.9f, 0.3f, 0.3f) },
                    new EnumOverlayColorEntry { enumId = "nature",      color = new Color(0.3f, 0.8f, 0.5f, 0.3f) },
                } },
                new CellPropertyDef { key = "pathWeight",   displayName = "寻路权重",valueType = PropType.Float, defaultValue = "1.0"   },
            };

            terrainTypes = new List<TerrainTypeDef>
            {
                new TerrainTypeDef { id = "grass",  displayName = "草地", mapColor = new Color(0.3f, 0.7f, 0.3f), defaultPathWeight = 1.0f  },
                new TerrainTypeDef { id = "water",  displayName = "水域", mapColor = new Color(0.2f, 0.4f, 0.9f), defaultPathWeight = 99f   },
                new TerrainTypeDef { id = "road",   displayName = "道路", mapColor = new Color(0.7f, 0.6f, 0.4f), defaultPathWeight = 0.5f  },
                new TerrainTypeDef { id = "sand",   displayName = "沙地", mapColor = new Color(0.9f, 0.8f, 0.5f), defaultPathWeight = 1.5f  },
                new TerrainTypeDef { id = "forest", displayName = "森林", mapColor = new Color(0.1f, 0.5f, 0.1f), defaultPathWeight = 1.8f  },
            };

            zoneTypes = new List<ZoneTypeDef>
            {
                new ZoneTypeDef { id = "none",        displayName = "无区域" },
                new ZoneTypeDef { id = "residential", displayName = "住宅区", debugColor = new Color(0.8f, 0.8f, 0.2f, 0.2f) },
                new ZoneTypeDef { id = "commercial",  displayName = "商业区", debugColor = new Color(0.2f, 0.8f, 0.8f, 0.2f) },
                new ZoneTypeDef { id = "nature",      displayName = "自然区", debugColor = new Color(0.2f, 0.8f, 0.2f, 0.2f) },
                new ZoneTypeDef { id = "farm",        displayName = "农田区", debugColor = new Color(0.8f, 0.6f, 0.2f, 0.2f) },
            };

            spawnPointTypes = new List<SpawnPointTypeDef>
            {
                new SpawnPointTypeDef { id = "npc",    displayName = "NPC",  gizmoColor = Color.blue   },
                new SpawnPointTypeDef { id = "player", displayName = "玩家", gizmoColor = Color.green  },
                new SpawnPointTypeDef { id = "item",   displayName = "物品", gizmoColor = Color.yellow },
            };
        }

        /// <summary>在所有 TileSet 中查找 tileId 对应的条目（全局唯一 id 方案）。</summary>
        public TileEntry FindTileEntry(int tileId)
        {
            foreach (var tsRef in tileSets)
            {
                if (tsRef.tileSet == null) continue;
                var entry = tsRef.tileSet.GetTile(tileId);
                if (entry != null) return entry;
            }
            return null;
        }

        public string[] GetEnumOptions(string enumOptionsRef)
        {
            if (enumOptionsRef == "terrainTypes")
            {
                var list = new string[terrainTypes.Count];
                for (int i = 0; i < terrainTypes.Count; i++) list[i] = terrainTypes[i].id;
                return list;
            }
            if (enumOptionsRef == "zoneTypes")
            {
                var list = new string[zoneTypes.Count];
                for (int i = 0; i < zoneTypes.Count; i++) list[i] = zoneTypes[i].id;
                return list;
            }
            if (enumOptionsRef == "poiTypes")
            {
                var list = new string[poiTypes.Count];
                for (int i = 0; i < poiTypes.Count; i++) list[i] = poiTypes[i].id;
                return list;
            }
            return new string[0];
        }

        public string[] GetEnumDisplayNames(string enumOptionsRef)
        {
            if (enumOptionsRef == "terrainTypes")
            {
                var list = new string[terrainTypes.Count];
                for (int i = 0; i < terrainTypes.Count; i++) list[i] = terrainTypes[i].displayName;
                return list;
            }
            if (enumOptionsRef == "zoneTypes")
            {
                var list = new string[zoneTypes.Count];
                for (int i = 0; i < zoneTypes.Count; i++) list[i] = zoneTypes[i].displayName;
                return list;
            }
            if (enumOptionsRef == "poiTypes")
            {
                var list = new string[poiTypes.Count];
                for (int i = 0; i < poiTypes.Count; i++) list[i] = poiTypes[i].displayName;
                return list;
            }
            return new string[0];
        }
    }
}
