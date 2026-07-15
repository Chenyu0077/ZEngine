using System.IO;
using Hotfix.FuncModule;
using Main.FuncModule;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace GameScripts.Editor
{
    public static class MapDataSerializer
    {
        private static readonly JsonSerializerSettings _settings = new JsonSerializerSettings
        {
            Formatting           = Formatting.Indented,
            NullValueHandling    = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Include,
        };

        public static void Export(MapSaveData mapData, MapEditorConfig config, string path)
        {
            // 刷新 schema 快照
            var schema = new MapSchema();
            foreach (var def in config.cellPropertySchema)
                schema.cellProperties.Add(new CellPropertySnapshot { key = def.key, type = def.valueType.ToString(), defaultValue = def.defaultValue });
            foreach (var def in config.objectPropertySchema)
                schema.objectProperties.Add(new CellPropertySnapshot { key = def.key, type = def.valueType.ToString(), defaultValue = def.defaultValue });
            foreach (var t in config.terrainTypes)  schema.terrainTypes.Add(t.id);
            foreach (var z in config.zoneTypes)     schema.zoneTypes.Add(z.id);
            foreach (var l in config.layers)        schema.layers.Add(new LayerSnapshot { id = l.id, sortOrder = l.sortOrder });
            mapData.schema = schema;

            // 从 config.objectPalettePath 推算 Resources 的相对路径
            // 例: "Assets/Resources/Prefabs/MapObjects" → "Prefabs/MapObjects"
            if (!string.IsNullOrEmpty(config.objectPalettePath))
            {
                string normalized = config.objectPalettePath.Replace('\\', '/');
                int resIdx = normalized.IndexOf("/Resources/", System.StringComparison.OrdinalIgnoreCase);
                if (resIdx >= 0)
                    mapData.objectResourcePath = normalized.Substring(resIdx + "/Resources/".Length);
                else
                    mapData.objectResourcePath = normalized;
            }

            string json = JsonConvert.SerializeObject(mapData, _settings);
            string dir  = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir!);

            File.WriteAllText(path, json, System.Text.Encoding.UTF8);
            AssetDatabase.Refresh();
            Debug.Log($"[MapEditor] 导出成功: {path}");
        }

        public static MapSaveData Import(string path, MapEditorConfig config)
        {
            if (!File.Exists(path))
            {
                Debug.LogError($"[MapEditor] 找不到文件: {path}");
                return null;
            }

            string json    = File.ReadAllText(path, System.Text.Encoding.UTF8);
            var    mapData = JsonConvert.DeserializeObject<MapSaveData>(json);
            if (mapData == null)
            {
                Debug.LogError("[MapEditor] JSON 解析失败");
                return null;
            }

            // 兼容性：补全 Config 中新增但 JSON 缺失的图层
            int tileCount = mapData.width * mapData.height;
            foreach (var layerDef in config.layers)
            {
                if (!mapData.layers.Exists(l => l.id == layerDef.id))
                {
                    var newLayer = new LayerTileData { id = layerDef.id, tiles = new int[tileCount] };
                    for (int i = 0; i < tileCount; i++) newLayer.tiles[i] = -1;
                    mapData.layers.Add(newLayer);
                }
            }

            Debug.Log($"[MapEditor] 导入成功: {path}  ({mapData.width}x{mapData.height})");
            return mapData;
        }
    }
}
