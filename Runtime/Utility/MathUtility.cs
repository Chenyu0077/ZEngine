//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using UnityEngine;

namespace ZEngine.Utility
{
    public static class MathUtility
    {
        /// <summary>
        /// 计算点到某条线段的最短距离
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public static float CalcDistanceFromPointToSegment(Vector2 start, Vector2 end, Vector2 point)
        {
            Vector2 ab = end - start;
            Vector2 ac = point - start;
            Vector2 bc = point - end;

            //点投影在线段左端点外侧
            float proj = Vector2.Dot(ac, ab);
            if (proj <= 0)
                return ac.magnitude;

            //点投影在线段右端点外侧
            float abSqr = ab.sqrMagnitude;
            if (proj >= abSqr)
                return bc.magnitude;

            //点投影在线段内部
            float t = proj / abSqr;
            Vector2 closestPoint = start + t * ab;
            return (point - closestPoint).magnitude;
        }
    }
}
