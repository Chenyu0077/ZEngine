//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using UnityEngine;

namespace ZEngine.Utility
{
    public static class GizmosUtility
    {
        /// <summary>
        /// 绘制线段
        /// </summary>
        public static void DrawLine(Vector2 center, Vector2 v1, Vector2 v2)
        {
            Gizmos.DrawLine(center + v1, center + v2);
        }

        public static void DrawChain(Vector2 center, Vector2[] points)
        {
            for(int i = 0; i < points.Length - 1; i++)
            {
                var v1 = center + points[i];
                var v2 = center + points[i + 1];
                Gizmos.DrawLine(v1, v2);
            }
        }

        /// <summary>
        /// 绘制矩形
        /// </summary>
        public static void DrawRect(Vector2 center, Vector2 size)
        {
            float halfX = size.x / 2;
            float halfY = size.y / 2;
            Vector2 v1 = new Vector2(center.x - halfX, center.y - halfY);
            Vector2 v2 = new Vector2(center.x - halfX, center.y + halfY);
            Vector2 v3 = new Vector2(center.x + halfX, center.y + halfY);
            Vector2 v4 = new Vector2(center.x + halfX, center.y - halfY);
            Gizmos.DrawLine(v1, v2);
            Gizmos.DrawLine(v2, v3);
            Gizmos.DrawLine(v3, v4);
            Gizmos.DrawLine(v4, v1);
        }

        /// <summary>
        /// 绘制圆形
        /// </summary>
        public static void DrawCircle(Vector2 center, float radius, int segments = 30)
        {
            float angleUnit = 2 * Mathf.PI / segments;
            Vector2 prePoint = center + new Vector2(Mathf.Cos(0), Mathf.Sin(0)) * radius;
            for (int i = 1; i <= segments; i++)
            {
                float angle = angleUnit * i;
                Vector2 nextPoint = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                Gizmos.DrawLine(prePoint, nextPoint);
                prePoint = nextPoint;
            }
        }

        /// <summary>
        /// 绘制正多边形
        /// </summary>
        public static void DrawRegularPolygon(Vector2 center, float radius, int sides)
        {
            if (sides < 3) return;

            float angleUnit = 2 * Mathf.PI / sides;
            Vector2 prePoint = center + new Vector2(Mathf.Cos(0), Mathf.Sin(0)) * radius;
            for (int i = 1; i <= sides; i++)
            {
                float angle = angleUnit * i;
                Vector2 nextPoint = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                Gizmos.DrawLine(prePoint, nextPoint);
                prePoint = nextPoint;
            }
        }

        /// <summary>
        /// 绘制多边形
        /// </summary>
        public static void DrawPolygon(Vector2 center, Vector2[] points)
        {
            if (points.Length < 3) return;

            for(int i = 0; i < points.Length; i++)
            {
                Gizmos.DrawLine(center + points[i], center + points[(i + 1) % points.Length]);
            }
        }

        /// <summary>
        /// 绘制扇形
        /// </summary>
        /// <param name="center"></param>
        /// <param name="radius"></param>
        /// <param name="angle"></param>
        /// <param name="centerAngle">扇形角平分线对应的角度</param>
        /// <param name="segments"></param>
        public static void DrawSector(Vector2 center, float radius, float angle, float startAngle, int segments = 30)
        {
            float angleUnit = (angle * Mathf.PI) / (180 * segments);//分割弧线对应的带π角度
            float angleStart = startAngle * Mathf.PI / 180;//开始角度对应的带π角度
            Vector2 prePoint = center + new Vector2(Mathf.Cos(angleStart), Mathf.Sin(angleStart)) * radius;
            Gizmos.DrawLine(prePoint, center);
            for (int i = 1; i <= segments; i++)
            {
                float tempAngle = angleStart + angleUnit * i;
                Vector2 nextPoint = center + new Vector2(Mathf.Cos(tempAngle), Mathf.Sin(tempAngle)) * radius;
                Gizmos.DrawLine(prePoint, nextPoint);
                prePoint = nextPoint;
            }
            Gizmos.DrawLine(prePoint, center);
        }

        ///// <summary>
        ///// 绘制扇形
        ///// </summary>
        ///// <param name="center"></param>
        ///// <param name="radius"></param>
        ///// <param name="angle"></param>
        ///// <param name="centerAngle">扇形角平分线对应的角度</param>
        ///// <param name="segments"></param>
        //public static void DrawSector(Vector2 center, float radius, float angle, float centerAngle, int segments = 30)
        //{
        //    float angleUnit = (angle * Mathf.PI) / (180 * segments);
        //    Vector2 prePoint = center + new Vector2(Mathf.Cos(centerAngle), Mathf.Sin(centerAngle)) * radius;
        //    for (int i = 1; i <= segments / 2; i++)
        //    {
        //        float tempAngle = centerAngle + angleUnit * i;
        //        Vector2 nextPoint = center + new Vector2(Mathf.Cos(tempAngle), Mathf.Sin(tempAngle)) * radius;
        //        Gizmos.DrawLine(prePoint, nextPoint);
        //        prePoint = nextPoint;
        //    }
        //    Gizmos.DrawLine(prePoint, center);
        //    prePoint = center + new Vector2(Mathf.Cos(centerAngle), Mathf.Sin(centerAngle)) * radius;
        //    for (int i = 1; i <= segments / 2; i++)
        //    {
        //        float tempAngle = centerAngle - angleUnit * i;
        //        Vector2 nextPoint = center + new Vector2(Mathf.Cos(tempAngle), Mathf.Sin(tempAngle)) * radius;
        //        Gizmos.DrawLine(prePoint, nextPoint);
        //        prePoint = nextPoint;
        //    }
        //    Gizmos.DrawLine(prePoint, center);
        //}
    }
}
