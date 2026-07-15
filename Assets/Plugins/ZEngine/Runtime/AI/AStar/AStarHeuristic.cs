//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------
using UnityEngine;

namespace MotionFramework.AI
{
    /// <summary>
    /// 启发式距离算法（均基于 XY 平面，忽略 Z 轴）
    /// </summary>
    public static class AStarHeuristic
    {
        private static readonly float Sqrt2 = 1.41421356f;

        /// <summary>
        /// 曼哈顿距离，适合四方向（上下左右）移动
        /// </summary>
        public static float ManhattanDist(Vector3 a, Vector3 b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }

        /// <summary>
        /// 切比雪夫距离，适合八方向（含斜对角）移动
        /// </summary>
        public static float ChebyshevDist(Vector3 a, Vector3 b)
        {
            float dx = Mathf.Abs(a.x - b.x);
            float dy = Mathf.Abs(a.y - b.y);
            return Mathf.Max(dx, dy);
        }

        /// <summary>
        /// Octile 距离，适合八方向（含斜对角）移动，比切比雪夫更精确
        /// </summary>
        public static float OctileDist(Vector3 a, Vector3 b)
        {
            float dx = Mathf.Abs(a.x - b.x);
            float dy = Mathf.Abs(a.y - b.y);
            return Mathf.Max(dx, dy) + (Sqrt2 - 1f) * Mathf.Min(dx, dy);
        }

        /// <summary>
        /// 欧式距离，适合任意方向移动
        /// </summary>
        public static float EuclideanDist(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dy = a.y - b.y;
            return Mathf.Sqrt(dx * dx + dy * dy);
        }
    }
}
