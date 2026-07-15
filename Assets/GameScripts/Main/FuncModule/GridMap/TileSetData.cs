using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Main.FuncModule
{
    [Serializable]
    public class TileSpriteGroup
    {
        public string       groupName = "basic";
        public float        weight    = 1f;
        public List<Sprite> sprites   = new List<Sprite>();
    }

    [Serializable]
    public class TileEntry
    {
        public int     tileId;
        public string  tileName     = "Tile";
        public Sprite  sprite;
        public Color   fallbackColor = Color.white;
        public int     gridX;
        public int     gridY;

        public bool                  useRandom = false;
        public int                   seed      = 0;
        public List<TileSpriteGroup> groups    = new List<TileSpriteGroup>();

        public Sprite ResolveSprite(int x, int y)
        {
            if (!useRandom || groups == null || groups.Count == 0) return sprite;

            // 只统计有 sprite 且 weight > 0 的组
            float totalWeight = 0f;
            foreach (var g in groups)
                if (g.sprites != null && g.sprites.Count > 0 && g.weight > 0f)
                    totalWeight += g.weight;

            if (totalWeight <= 0f) return sprite;

            // 用格子坐标和 seed 初始化，保证同一格每次结果一致
            Random.InitState(seed ^ (y * 100003 + x));

            float pick = Random.Range(0f, totalWeight);  // [0, totalWeight)，不含上界
            float cumulative = 0f;
            foreach (var g in groups)
            {
                if (g.sprites == null || g.sprites.Count == 0 || g.weight <= 0f) continue;
                cumulative += g.weight;
                if (pick < cumulative)
                {
                    var s = g.sprites[Random.Range(0, g.sprites.Count)];
                    return s != null ? s : sprite;
                }
            }

            return sprite;
        }
    }

    [CreateAssetMenu(fileName = "TileSetData", menuName = "Map Editor/TileSet Data")]
    public class TileSetData : ScriptableObject
    {
        public int  gridWidth  = 16;
        public int  gridHeight = 16;
        public List<TileEntry> tiles = new List<TileEntry>();

        public TileEntry GetTile(int tileId)
        {
            foreach (var t in tiles)
                if (t.tileId == tileId) return t;
            return null;
        }

        public TileEntry GetTileAt(int gx, int gy)
        {
            foreach (var t in tiles)
                if (t.gridX == gx && t.gridY == gy) return t;
            return null;
        }

        public Color GetTileColor(int tileId)
        {
            var entry = GetTile(tileId);
            if (entry == null) return Color.magenta;
            if (entry.sprite != null)
            {
                var tex = entry.sprite.texture;
                var rect = entry.sprite.textureRect;
                return tex.GetPixel((int)(rect.x + rect.width * 0.5f), (int)(rect.y + rect.height * 0.5f));
            }
            return entry.fallbackColor;
        }

        /// <summary>移除指定网格位置的瓦片，返回是否成功。</summary>
        public bool RemoveTileAt(int gx, int gy)
        {
            for (int i = tiles.Count - 1; i >= 0; i--)
            {
                if (tiles[i].gridX == gx && tiles[i].gridY == gy)
                {
                    tiles.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }
    }
}