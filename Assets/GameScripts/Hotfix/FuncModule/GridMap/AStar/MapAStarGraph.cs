using System.Collections.Generic;
using MotionFramework.AI;
using UnityEngine;

namespace Hotfix.FuncModule.GridMap
{
    /// <summary>
    /// 将 MapLoader 的网格地图封装为 IAStarGraph，供 AStarPathFinding.FindPath 使用。
    /// 四方向移动，直线移动代价 1，Heuristic 使用 Manhattan 距离。
    /// 每次地图加载后通过 RebuildFromMap() 重建节点缓存。
    /// </summary>
    public class MapAStarGraph : IAStarGraph
    {
        private MapAStarNode[,] _nodes;
        private int _width;
        private int _height;

        private static readonly Vector2Int[] Directions = new Vector2Int[]
        {
            new Vector2Int( 0,  1),  // 上
            new Vector2Int( 0, -1),  // 下
            new Vector2Int(-1,  0),  // 左
            new Vector2Int( 1,  0),  // 右
        };

        /// <summary>
        /// 从当前 MapLoader 状态重建所有节点缓存。地图加载/重载后调用一次即可。
        /// </summary>
        public void RebuildFromMap()
        {
            var map = MapLoader.Instance;
            if (!map.IsLoaded)
            {
                _nodes  = null;
                _width  = 0;
                _height = 0;
                return;
            }

            _width  = map.MapWidth;
            _height = map.MapHeight;
            _nodes  = new MapAStarNode[_width, _height];

            for (int x = 0; x < _width; x++)
            for (int y = 0; y < _height; y++)
            {
                var worldPos = map.GridToWorld(x, y);
                var isBlock  = !map.IsWalkable(x, y);
                _nodes[x, y] = new MapAStarNode(x, y, worldPos, isBlock);
            }
        }

        /// <summary>
        /// 获取最近的可走节点（用于将世界坐标对齐到图节点）。
        /// </summary>
        public MapAStarNode WorldToNode(Vector3 worldPos)
        {
            var grid = MapLoader.Instance.WorldToGrid(worldPos);
            return GetNode(grid.x, grid.y);
        }

        public MapAStarNode GetNode(int x, int y)
        {
            if (_nodes == null || x < 0 || x >= _width || y < 0 || y >= _height)
                return null;
            return _nodes[x, y];
        }

        public bool IsReady => _nodes != null;

        // ── IAStarGraph ──────────────────────────────────────────────────────

        public IEnumerable<AStarNode> Neighbors(AStarNode node)
        {
            var n = (MapAStarNode)node;
            foreach (var dir in Directions)
            {
                int nx = n.GridX + dir.x;
                int ny = n.GridY + dir.y;
                var neighbor = GetNode(nx, ny);
                if (neighbor != null)
                    yield return neighbor;
            }
        }

        public float MoveCost(AStarNode from, AStarNode to)
        {
            var t = (MapAStarNode)to;
            return MapLoader.Instance.PathWeight(t.GridX, t.GridY);
        }

        public float Heuristic(AStarNode from, AStarNode to) =>
            AStarHeuristic.ManhattanDist(from.Position, to.Position);
    }
}
