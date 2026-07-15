//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------
using UnityEngine;

namespace MotionFramework.AI
{
    public abstract class AStarNode
    {
        /// <summary>
        /// 节点在世界空间中的位置，用于启发式距离计算。
        /// </summary>
        public abstract Vector3 Position { get; }

        /// <summary>
        /// 是否为阻挡节点
        /// </summary>
        public abstract bool IsBlock();

        /// <summary>
        /// 总的代价值 F = G + H
        /// </summary>
        internal float Cost => G + H;

        /// <summary>
        /// 从起点移动到该节点的实际代价
        /// </summary>
        internal float G { set; get; }

        /// <summary>
        /// 从该节点到终点的启发式估算代价
        /// </summary>
        internal float H { set; get; }

        /// <summary>
        /// 路径回溯父节点
        /// </summary>
        internal AStarNode Parent { set; get; }

        /// <summary>
        /// 清空临时数据
        /// </summary>
        public void ClearTemper()
        {
            G = 0;
            H = 0;
            Parent = null;
        }

        // 封闭相等性方法，保证 HashSet<AStarNode> 始终使用引用相等。
        // 子类若覆写 Equals/GetHashCode 会导致 openSet/closedSet 行为异常。
        public sealed override bool Equals(object obj) => ReferenceEquals(this, obj);
        public sealed override int GetHashCode() =>
            System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this);
    }
}
