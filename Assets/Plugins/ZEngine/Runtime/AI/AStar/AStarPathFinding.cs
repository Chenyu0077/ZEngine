//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------
using System;
using System.Collections.Generic;

namespace MotionFramework.AI
{
    // 注意：静态字段使 FindPath 非线程安全，仅供 Unity 主线程调用。
    public static class AStarPathFinding
    {
        private static readonly AStarMinHeap       _openHeap  = new AStarMinHeap();
        private static readonly HashSet<AStarNode> _openSet   = new HashSet<AStarNode>();
        private static readonly HashSet<AStarNode> _closedSet = new HashSet<AStarNode>();
        private static readonly List<AStarNode>    _visited   = new List<AStarNode>(256);

        /// <summary>
        /// 获取一条路径。
        /// </summary>
        /// <param name="graph">节点关系图，不可为 null</param>
        /// <param name="from">起点，不可为 null，不可为阻挡节点，不含于返回路径中</param>
        /// <param name="to">终点，不可为 null</param>
        /// <returns>
        /// 路径节点列表（不含 from，含 to）；
        /// from == to 时返回空列表；
        /// 无可达路径时返回 null。
        /// </returns>
        public static List<AStarNode> FindPath(IAStarGraph graph, AStarNode from, AStarNode to)
        {
            if (graph == null) throw new ArgumentNullException(nameof(graph));
            if (from  == null) throw new ArgumentNullException(nameof(from));
            if (to    == null) throw new ArgumentNullException(nameof(to));
            if (from.IsBlock()) return null;
            if (from == to)     return new List<AStarNode>();

            // 只清理上次实际访问的节点，避免全图遍历
            foreach (AStarNode node in _visited)
                node.ClearTemper();
            _visited.Clear();
            _openHeap.Clear();
            _openSet.Clear();
            _closedSet.Clear();

            from.G = 0;
            from.H = graph.Heuristic(from, to);
            _openHeap.Push(from, from.Cost);
            _openSet.Add(from);
            _visited.Add(from);

            while (_openHeap.Count > 0)
            {
                AStarNode current = _openHeap.Pop();

                // 懒惰删除：跳过因 G 值更新而遗留的旧堆条目
                if (_closedSet.Contains(current))
                    continue;

                _openSet.Remove(current);
                _closedSet.Add(current);

                if (current == to)
                    return RetracePath(from, to);

                foreach (AStarNode neighbor in graph.Neighbors(current))
                {
                    if (neighbor == null || neighbor.IsBlock() || _closedSet.Contains(neighbor))
                        continue;

                    float newG = current.G + graph.MoveCost(current, neighbor);

                    if (!_openSet.Contains(neighbor))
                    {
                        neighbor.G      = newG;
                        neighbor.H      = graph.Heuristic(neighbor, to);
                        neighbor.Parent = current;
                        _openHeap.Push(neighbor, neighbor.Cost);
                        _openSet.Add(neighbor);
                        _visited.Add(neighbor);
                    }
                    else if (newG < neighbor.G)
                    {
                        neighbor.G      = newG;
                        neighbor.Parent = current;
                        // H 不变，无需重算；重推新条目，旧条目经 _closedSet 跳过
                        _openHeap.Push(neighbor, neighbor.Cost);
                    }
                }
            }

            return null;
        }

        private static List<AStarNode> RetracePath(AStarNode from, AStarNode to)
        {
            var path = new List<AStarNode>();
            AStarNode current = to;
            while (current != from)
            {
                path.Add(current);
                current = current.Parent;
            }
            path.Reverse();
            return path;
        }
    }
}
