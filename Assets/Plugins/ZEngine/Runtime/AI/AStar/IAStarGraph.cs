//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------
using System.Collections.Generic;

namespace MotionFramework.AI
{
    public interface IAStarGraph
    {
        /// <summary>
        /// 获取邻居节点
        /// </summary>
        IEnumerable<AStarNode> Neighbors(AStarNode node);

        /// <summary>
        /// 实际移动代价（含地形权重），用于计算 G 值。
        /// 不要求可接受性，可根据地形赋予不同权重。
        /// </summary>
        float MoveCost(AStarNode from, AStarNode to);

        /// <summary>
        /// 启发式估算代价，用于计算 H 值。
        /// 必须满足可接受性（不高估实际代价），以保证找到最优路径。
        /// 推荐使用 AStarHeuristic 中对应移动模式的距离函数。
        /// </summary>
        float Heuristic(AStarNode from, AStarNode to);
    }
}
