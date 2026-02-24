//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------


using System.Collections.Generic;
using UnityEngine;
using ZEngine.Utility;

namespace ZEngine.Module.Collider2D
{
    public static class CollisionUtils
    {
        #region 普通一对一碰撞检测算法
        public static bool LineIntersectsLine(LineColliderData line1, LineColliderData line2)
        {
            var a = line1.worldV1;
            var b = line1.worldV2;
            var c = line2.worldV1;
            var d = line2.worldV2;
            //return Vector2.Dot(c - a, d - a) * Vector2.Dot(c - b, d - b) <= 0 && Vector2.Dot(a - c, b - c) * Vector2.Dot(a - d, b - d) <= 0; //相交
            float cross1 = Cross(b - a, c - a);
            float cross2 = Cross(b - a, d - a);
            float cross3 = Cross(d - c, a - c);
            float cross4 = Cross(d - c, b - c);

            // 一般情况：两线段跨立相交
            if ((cross1 * cross2 < 0) && (cross3 * cross4 < 0))
                return true;

            // 特殊情况：共线重叠
            if (IsLineContainsPoint(a, c, d) || IsLineContainsPoint(b, c, d) ||
                IsLineContainsPoint(c, a, b) || IsLineContainsPoint(d, a, b))
                return true;

            return false;
        }

        public static bool LineIntersectsBox(LineColliderData line, BoxColliderData box)
        {
            //判断线段两端点是否在矩形内
            if (IsBoxContainsPoint(box, line.worldV1) || IsBoxContainsPoint(box, line.worldV2))
                return true;

            //判断线段是否与矩形两对角线相交
            var a1 = new Vector2(-box.Size.x / 2, -box.Size.y / 2);
            var b1 = new Vector2(box.Size.x / 2, box.Size.y / 2);
            LineColliderData line1 = new LineColliderData() { Center = box.Center, V1 = a1, V2 = b1 };
            bool isIntersect1 = LineIntersectsLine(line, line1);

            var a2 = new Vector2(-box.Size.x / 2, box.Size.y / 2);
            var b2 = new Vector2(box.Size.x / 2, -box.Size.y / 2);
            LineColliderData line2 = new LineColliderData() { Center = box.Center, V1 = a2, V2 = b2 };
            bool isIntersect2 = LineIntersectsLine(line, line2);

            return isIntersect1 || isIntersect2;
        }

        public static bool LineIntersectsCircle(LineColliderData line, CircleColliderData circle)
        {
            Vector2 ab = line.worldV2 - line.worldV1;
            Vector2 ac = circle.Center - line.worldV1;
            Vector2 bc = circle.Center - line.worldV2;

            //圆心投影在线段左端点外侧
            float proj = Vector2.Dot(ac, ab);
            if (proj <= 0)
                return ac.sqrMagnitude <= circle.Radius * circle.Radius;

            //圆形投影在线段右端点外侧
            float abSqr = ab.sqrMagnitude;
            if (proj >= abSqr)
                return bc.sqrMagnitude <= circle.Radius * circle.Radius;

            //圆心投影在线段内部
            float t = proj / abSqr;
            Vector2 closestPoint = line.worldV1 + t * ab;
            return (circle.Center - closestPoint).sqrMagnitude <= circle.Radius * circle.Radius;
        }

        public static bool LineIntersectsPolygon(LineColliderData line, PolygonColliderData polygon)
        {
            //1.判断两端点是否在凸多边形内部
            if (IsPolygonContainsPoint(polygon, line.worldV1) || IsPolygonContainsPoint(polygon, line.worldV2))
                return true;

            //2.判断线段是否与凸多边形的边相交
            var vertexs = polygon.Points;
            for(int i = 0; i < vertexs.Length; i++)
            {
                Vector2 v1 = vertexs[i];
                Vector2 v2 = vertexs[(i + 1) % vertexs.Length];
                LineColliderData newLine = new LineColliderData(polygon.Center, v1, v2);
                if (LineIntersectsLine(line, newLine))
                    return true;
            }

            return false;
        }

        public static bool LineIntersectsSector(LineColliderData line, SectorColliderData sector)
        {
            //1.判断线段端点是否在扇形内
            if (IsSectorContainsPoint(sector, line.worldV1) || IsSectorContainsPoint(sector, line.worldV2))
                return true;

            //2.判断线段是否与扇形两半径边相交
            float angle = sector.Angle * Mathf.PI / 180;
            float angleStart = sector.StartAngle * Mathf.PI / 180;
            var startPoint = new Vector2(Mathf.Cos(angleStart), Mathf.Sin(angleStart)) * sector.Radius;
            var endPoint = new Vector2(Mathf.Cos(angleStart + angle), Mathf.Sin(angleStart + angle)) * sector.Radius;
            LineColliderData line1 = new LineColliderData() { Center = sector.Center, V1 = Vector2.zero, V2 = startPoint };
            LineColliderData line2 = new LineColliderData() { Center = sector.Center, V1 = Vector2.zero, V2 = endPoint };
            if (LineIntersectsLine(line, line1) || LineIntersectsLine(line, line2))
                return true;

            //2.判断线段是否与扇形圆弧相交
            var intersects = GetLineIntersectsCircleVecs(line.worldV1, line.worldV2, sector.Center, sector.Radius);
            foreach ( var intersect in intersects)
            {
                if (IsSectorContainsPoint(sector, intersect))
                    return true;
            }
            return false;
        }

        public static bool LineIntersectsChain(LineColliderData line, ChainColliderData chain)
        {
            var points = chain.Points;
            for(int i = 0; i < points.Length - 1; i++)
            {
                var tempLine = new LineColliderData() { Center = chain.Center, V1 = points[i], V2 = points[i + 1] };
                if (LineIntersectsLine(line, tempLine))
                    return true;
            }

            return false;
        }

        public static bool BoxIntersectsBox(BoxColliderData box1, BoxColliderData box2)
        {
            if (box1.xMin > box2.xMax || box1.xMax < box2.xMin)
                return false;
            if (box1.yMin > box2.yMax || box1.yMax < box2.yMin)
                return false;
            return true;
        }

        public static bool BoxIntersectsCircle(BoxColliderData box, CircleColliderData circle)
        {
            var p = circle.Center - box.Center;
            var v = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y));
            var h = new Vector2(box.xMax, box.yMax) - box.Center;
            var u = v - h;
            float l = 0;
            if (u.x > 0 && u.y > 0)
                l = u.magnitude;
            else if (u.x <= 0 && u.y > 0)
                l = u.y;
            else if (u.x > 0 && u.y <= 0)
                l = u.x;
            else
                l = 0;
            if (l <= circle.Radius)
                return true;
            return false;
        }

        public static bool BoxIntersectsPolygon(BoxColliderData box, PolygonColliderData polygon)
        {
            if (ShapeIntersectsShape(box.GetWorldVectors(), polygon.GetWorldVectors()))
                return true;
            return false;
        }

        public static bool BoxIntersectsSector(BoxColliderData box, SectorColliderData sector)
        {
            if (ShapeIntersectsShape(box.GetWorldVectors(), sector.GetWorldVectors()))
                return true;
            return false;
        }

        public static bool BoxIntersectsChain(BoxColliderData box, ChainColliderData chain)
        {
            var points = chain.Points;
            for (int i = 0; i < points.Length - 1; i++)
            {
                var tempLine = new LineColliderData() { Center = chain.Center, V1 = points[i], V2 = points[i + 1] };
                if (LineIntersectsBox(tempLine, box))
                    return true;
            }

            return false;
        }

        public static bool CircleIntersectsCircle(CircleColliderData circle1, CircleColliderData circle2)
        {
            if (Vector2.Distance(circle1.Center, circle2.Center) <= circle1.Radius + circle2.Radius)
                return true;
            return false;
        }

        public static bool CircleIntersectsPolygon(CircleColliderData circle, PolygonColliderData polygon)
        {
            //1.判断圆心是否在多边形内部
            if (IsPolygonContainsPoint(polygon, circle.Center))
                return true;

            //2.判断圆心到任意一条边的距离是否小于等于半径
            var vertexs = polygon.worldPoints;
            for(int i = 0; i < vertexs.Length; i++)
            {
                Vector2 start = vertexs[i];
                Vector2 end = vertexs[(i + 1) % vertexs.Length];
                if (MathUtility.CalcDistanceFromPointToSegment(start, end, circle.Center) <= circle.Radius)
                    return true;
            }
            return false;
        }

        public static bool CircleIntersectsSector(CircleColliderData circle, SectorColliderData sector)
        {
            //转换成带π角度
            float angle = sector.Angle * Mathf.PI / 180;
            float angleStart = sector.StartAngle * Mathf.PI / 180;

            //1.判断圆心与扇心之间的距离是否小于两半径之和
            if (Vector2.Distance(circle.Center, sector.Center) > circle.Radius + sector.Radius)
                return false;

            //2.判断圆心是否在扇形角度之间
            var centerDir = circle.Center - sector.Center;
            var middleDir = new Vector2(Mathf.Cos(angleStart + angle / 2f), Mathf.Sin(angleStart + angle / 2f)) * sector.Radius;
            if (Vector2.Angle(centerDir, middleDir) < sector.Angle / 2f)
                return true;

            //3.判断圆是否与扇形的两条边有交叉
            var startPoint = sector.Center + new Vector2(Mathf.Cos(angleStart), Mathf.Sin(angleStart)) * sector.Radius;
            var endPoint = sector.Center + new Vector2(Mathf.Cos(angleStart + angle), Mathf.Sin(angleStart + angle)) * sector.Radius;
            LineColliderData line1 = new LineColliderData() { Center = Vector2.zero, V1 = sector.Center, V2 = startPoint };
            LineColliderData line2 = new LineColliderData() { Center = Vector2.zero, V1 = sector.Center, V2 = endPoint };
            if (LineIntersectsCircle(line1, circle) || LineIntersectsCircle(line2, circle))
                return true;
            return false;
        }

        public static bool CircleIntersectsChain(CircleColliderData circle, ChainColliderData chain)
        {
            var points = chain.Points;
            for (int i = 0; i < points.Length - 1; i++)
            {
                var tempLine = new LineColliderData() { Center = chain.Center, V1 = points[i], V2 = points[i + 1] };
                if (LineIntersectsCircle(tempLine, circle))
                    return true;
            }

            return false;
        }

        public static bool PolygonIntersectsPolygon(PolygonColliderData polygon1, PolygonColliderData polygon2)
        {
            if (ShapeIntersectsShape(polygon1.GetWorldVectors(), polygon2.GetWorldVectors()))
                return true;
            return false;
        }

        public static bool PolygonIntersectsChain(PolygonColliderData polygon, ChainColliderData chain)
        {
            var points = chain.Points;
            for (int i = 0; i < points.Length - 1; i++)
            {
                var tempLine = new LineColliderData() { Center = chain.Center, V1 = points[i], V2 = points[i + 1] };
                if (LineIntersectsPolygon(tempLine, polygon))
                    return true;
            }

            return false;
        }

        public static bool SectorIntersectsSector(SectorColliderData sector1, SectorColliderData sector2)
        {
            if (ShapeIntersectsShape(sector1.GetWorldVectors(), sector2.GetWorldVectors()))
                return true;
            return false;
        }

        public static bool SectorIntersectsPolygon(SectorColliderData sector, PolygonColliderData polygon)
        {
            if (ShapeIntersectsShape(sector.GetWorldVectors(), polygon.GetWorldVectors()))
                return true;
            return false;
        }

        public static bool SectorIntersectsChain(SectorColliderData sector, ChainColliderData chain)
        {
            var points = chain.Points;
            for (int i = 0; i < points.Length - 1; i++)
            {
                var tempLine = new LineColliderData() { Center = chain.Center, V1 = points[i], V2 = points[i + 1] };
                if (LineIntersectsSector(tempLine, sector))
                    return true;
            }

            return false;
        }

        public static bool ChainIntersectsChain(ChainColliderData chain1, ChainColliderData chain2)
        {
            var points1 = chain1.Points;
            var points2 = chain2.Points;
            for (int i = 0; i < points1.Length - 1; i++)
            {
                var tempLine1 = new LineColliderData() { Center = chain1.Center, V1 = points1[i], V2 = points1[i + 1] };
                for(int j = 0; j < points2.Length - 1; j++)
                {
                    var tempLine2 = new LineColliderData() { Center = chain2.Center, V1 = points2[j], V2 = points2[j + 1] };
                    if (LineIntersectsLine(tempLine1, tempLine2))
                        return true;
                }
            }

            return false;
        }
        #endregion

        /// <summary>
        /// 检测碰撞体是否发生碰撞
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool CheckCollision(ICollider2D a, ICollider2D b)
        {
            ColliderType typeA = a.ColliderType;
            ColliderType typeB = b.ColliderType;

            if ((int)typeA > (int)typeB)
            {
                (a, b) = (b, a);
                (typeA, typeB) = (typeB, typeA);
            }

            return (typeA, typeB) switch
            {
                (ColliderType.Line, ColliderType.Line) => LineIntersectsLine((a as LineCollider2D).LineData, (b as LineCollider2D).LineData),
                (ColliderType.Line, ColliderType.Box) => LineIntersectsBox((a as LineCollider2D).LineData, (b as BoxCollider2D).BoxData),
                (ColliderType.Line, ColliderType.Circle) => LineIntersectsCircle((a as LineCollider2D).LineData, (b as CircleCollider2D).CircleData),
                (ColliderType.Line, ColliderType.Sector) => LineIntersectsSector((a as LineCollider2D).LineData, (b as SectorCollider2D).SectorData),
                (ColliderType.Line, ColliderType.Polygon) => LineIntersectsPolygon((a as LineCollider2D).LineData, (b as PolygonCollider2D).PolygonData),
                (ColliderType.Line, ColliderType.Chain) => LineIntersectsChain((a as LineCollider2D).LineData, (b as ChainCollider2D).ChainData),
                (ColliderType.Box, ColliderType.Box) => BoxIntersectsBox((a as BoxCollider2D).BoxData, (b as BoxCollider2D).BoxData),
                (ColliderType.Box, ColliderType.Circle) => BoxIntersectsCircle((a as BoxCollider2D).BoxData, (b as CircleCollider2D).CircleData),
                (ColliderType.Box, ColliderType.Sector) => BoxIntersectsSector((a as BoxCollider2D).BoxData, (b as SectorCollider2D).SectorData),
                (ColliderType.Box, ColliderType.Polygon) => BoxIntersectsPolygon((a as BoxCollider2D).BoxData, (b as PolygonCollider2D).PolygonData),
                (ColliderType.Box, ColliderType.Chain) => BoxIntersectsChain((a as BoxCollider2D).BoxData, (b as ChainCollider2D).ChainData),
                (ColliderType.Circle, ColliderType.Circle) => CircleIntersectsCircle((a as CircleCollider2D).CircleData, (b as CircleCollider2D).CircleData),
                (ColliderType.Circle, ColliderType.Sector) => CircleIntersectsSector((a as CircleCollider2D).CircleData, (b as SectorCollider2D).SectorData),
                (ColliderType.Circle, ColliderType.Polygon) => CircleIntersectsPolygon((a as CircleCollider2D).CircleData, (b as PolygonCollider2D).PolygonData),
                (ColliderType.Circle, ColliderType.Chain) => CircleIntersectsChain((a as CircleCollider2D).CircleData, (b as ChainCollider2D).ChainData),
                (ColliderType.Sector, ColliderType.Sector) => SectorIntersectsSector((a as SectorCollider2D).SectorData, (b as SectorCollider2D).SectorData),
                (ColliderType.Sector, ColliderType.Polygon) => SectorIntersectsPolygon((a as SectorCollider2D).SectorData, (b as PolygonCollider2D).PolygonData),
                (ColliderType.Sector, ColliderType.Chain) => SectorIntersectsChain((a as SectorCollider2D).SectorData, (b as ChainCollider2D).ChainData),
                (ColliderType.Polygon, ColliderType.Polygon) => PolygonIntersectsPolygon((a as PolygonCollider2D).PolygonData, (b as PolygonCollider2D).PolygonData),
                (ColliderType.Polygon, ColliderType.Chain) => PolygonIntersectsChain((a as PolygonCollider2D).PolygonData, (b as ChainCollider2D).ChainData),
                (ColliderType.Chain, ColliderType.Chain) => ChainIntersectsChain((a as ChainCollider2D).ChainData, (b as ChainCollider2D).ChainData),
                _ => false,
            };
        }


        #region 射线检测
        /// <summary>
        /// 通用射线碰撞检测（返回一定距离内检测到的最近距离的碰撞体）
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="direction"></param>
        /// <param name="maxDistance"></param>
        /// <param name="hitInfo"></param>
        /// <returns></returns>
        public static bool Raycast(Vector2 origin, Vector2 direction, float maxDistance, out RaycastHitInfo2D hitInfo)
        {
            hitInfo = new RaycastHitInfo2D();
            List<RaycastHitInfo2D> hitInfos = null;
            int count = RaycastAll(origin, direction, maxDistance, out hitInfos);
            if(count > 0)
            {
                hitInfo = hitInfos[0];
                return true;
            }
            return false;
        }

        /// <summary>
        /// 获取一定距离内的所有碰撞体
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="direction"></param>
        /// <param name="maxDistance"></param>
        /// <param name="hitInfos"></param>
        /// <returns></returns>
        public static int RaycastAll(Vector2 origin, Vector2 direction, float maxDistance, out List<RaycastHitInfo2D> hitInfos)
        {
            var dir = direction.normalized;
            hitInfos = new List<RaycastHitInfo2D>();

            LineColliderData lineData = new LineColliderData() { Center = Vector2.zero, V1 = origin, V2 = origin + dir * maxDistance };
            foreach (var collider in ColliderManager.Instance.AllColliders)
            {
                if (collider == null || !collider.enabled)
                    continue;

                if (!TryHit(collider, lineData, out Vector2 point))
                    continue;

                float dist = Vector2.Distance(origin, point);

                var hitInfo = new RaycastHitInfo2D()
                {
                    Point = point,
                    Normal = (point - collider.Position).normalized,
                    Distance = dist,
                    Collider = collider,
                };
                hitInfos.Add(hitInfo);
            }

            //按照距离由近及远的顺序排序
            hitInfos.Sort((a, b) => a.Distance.CompareTo(b.Distance));
            return hitInfos.Count;
        }

        /// <summary>
        /// 通用射线碰撞检测（返回一定距离内检测到的最近距离的某Tag碰撞体）
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="direction"></param>
        /// <param name="maxDistance"></param>
        /// <param name="tag"></param>
        /// <param name="hitInfo"></param>
        /// <returns></returns>
        public static bool Raycast(Vector2 origin, Vector2 direction, float maxDistance, ColliderTag tag, out RaycastHitInfo2D hitInfo)
        {
            hitInfo = new RaycastHitInfo2D();
            List<RaycastHitInfo2D> hitInfos = null;
            int count = RaycastAll(origin, direction, maxDistance, tag, out hitInfos);
            if (count > 0)
            {
                hitInfo = hitInfos[0];
                return true;
            }
            return false;
        }

        /// <summary>
        /// 获取一定距离内某Tag的全部碰撞体
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="direction"></param>
        /// <param name="maxDistance"></param>
        /// <param name="hitInfos"></param>
        /// <returns></returns>
        public static int RaycastAll(Vector2 origin, Vector2 direction, float maxDistance, ColliderTag tag, out List<RaycastHitInfo2D> hitInfos)
        {
            var dir = direction.normalized;
            hitInfos = new List<RaycastHitInfo2D>();

            LineColliderData lineData = new LineColliderData() { Center = Vector2.zero, V1 = origin, V2 = origin + dir * maxDistance };
            foreach (var collider in ColliderManager.Instance.AllColliders)
            {
                if (collider == null || !collider.enabled)
                    continue;

                if (collider.Tag != tag)
                    continue;

                if (!TryHit(collider, lineData, out Vector2 point))
                    continue;

                float dist = Vector2.Distance(origin, point);

                var hitInfo = new RaycastHitInfo2D()
                {
                    Point = point,
                    Normal = (point - collider.Position).normalized,
                    Distance = dist,
                    Collider = collider,
                };
                hitInfos.Add(hitInfo);
            }

            //按照距离由近及远的顺序排序
            hitInfos.Sort((a, b) => a.Distance.CompareTo(b.Distance));
            return hitInfos.Count;
        }


        private static bool TryHit(ICollider2D collider, LineColliderData ray, out Vector2 point)
        {
            point = Vector2.zero;
            switch (collider.ColliderType)
            {
                case ColliderType.Line:
                    var lineCol = collider as LineCollider2D;
                    if(LineIntersectsLine(ray, lineCol.LineData))
                    {
                        GetClosedPoint(lineCol.LineData.GetWorldVectors(), ray, out point);
                        return true;
                    }
                    break;

                case ColliderType.Box:
                    var boxCol = collider as BoxCollider2D;
                    if (LineIntersectsBox(ray, boxCol.BoxData))
                    {
                        GetClosedPoint(boxCol.BoxData.GetWorldVectors(), ray, out point);
                        return true;
                    }
                    break;

                case ColliderType.Circle:
                    var circleCol = collider as CircleCollider2D;
                    if (LineIntersectsCircle(ray, circleCol.CircleData))
                    {
                        GetClosedPoint(circleCol.CircleData.GetWorldVectors(), ray, out point);
                        return true;
                    }
                    break;

                case ColliderType.Sector:
                    var sectorCol = collider as SectorCollider2D;
                    if (LineIntersectsSector(ray, sectorCol.SectorData))
                    {
                        GetClosedPoint(sectorCol.SectorData.GetWorldVectors(), ray, out point);
                        return true;
                    }
                    break;

                case ColliderType.Polygon:
                    var polygonCol = collider as PolygonCollider2D;
                    if (LineIntersectsPolygon(ray, polygonCol.PolygonData))
                    {
                        GetClosedPoint(polygonCol.PolygonData.GetWorldVectors(), ray, out point);
                        return true;
                    }
                    break;

                case ColliderType.Chain:
                    var chainCol = collider as ChainCollider2D;
                    if (LineIntersectsChain(ray, chainCol.ChainData))
                    {
                        GetClosedPoint(chainCol.ChainData.GetWorldVectors(), ray, out point);
                        return true;
                    }
                    break;
            }

            return false;
        }

        /// <summary>
        /// 获取图形上离碰撞点最近的点（粗略获取碰撞点位置）
        /// </summary>
        /// <param name="points"></param>
        /// <param name="origin"></param>
        /// <param name="point"></param>
        private static void GetClosedPoint(Vector2[] points, LineColliderData ray , out Vector2 point)
        {
            //与射线的夹角尽量小，到射线的距离尽量近
            point = new Vector2();
            float minAngle = float.MaxValue;
            float minProjDis = float.MaxValue;
            for(int i = 0; i < points.Length; i++)
            {
                var dir = points[i] - ray.worldV1;
                float tempAngle = Vector2.Angle(dir, ray.worldV2 - ray.worldV1);
                float tempProjDis = GetProjectDistance(points[i], ray.worldV1, ray.worldV2);
                if(tempAngle < minAngle && tempProjDis < minProjDis)
                {
                    point = points[i];
                    minAngle = tempAngle;
                    minProjDis = tempProjDis;
                }
            }
        }
        #endregion

        #region 判断图形是否包含点
        /// <summary>
        /// 叉乘
        /// </summary>
        private static float Cross(Vector2 a, Vector2 b)
        {
            return a.x * b.y - a.y * b.x;
        }

        /// <summary>
        /// 获取点到线段的投影距离
        /// </summary>
        /// <param name="point"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private static float GetProjectDistance(Vector2 point, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            Vector2 ap = point - a;
            //ap在ab上的投影长度
            return Vector2.Dot(ap, ab.normalized);
        }

        /// <summary>
        /// 判断线段是否包含点
        /// </summary>
        private static bool IsLineContainsPoint(Vector2 p, Vector2 a, Vector2 b)
        {
            return Mathf.Abs(Cross(b - a, p - a)) < Mathf.Epsilon &&
                   Vector2.Dot(p - a, p - b) <= 0;
        }

        /// <summary>
        /// 矩形是否包含点
        /// </summary>
        private static bool IsBoxContainsPoint(BoxColliderData box, Vector2 point)
        {
            if (point.x >= box.xMin && point.x <= box.xMax && point.y >= box.yMin && point.y <= box.yMax)
                return true;
            return false;
        }

        /// <summary>
        /// 圆是否包含点
        /// </summary>
        private static bool IsCircleContainsPoint(CircleColliderData circle, Vector2 point)
        {
            if (Vector2.Distance(circle.Center, point) <= circle.Radius)
                return true;
            return false;
        }

        /// <summary>
        /// 多边形是否包含点
        /// </summary>
        private static bool IsPolygonContainsPoint(PolygonColliderData polygon, Vector2 point)
        {
            if (!polygon.IsValid())
                return false;

            var vertexs = polygon.worldPoints;
            float lastCross = 0f;
            for(int i = 0; i < vertexs.Length; i++)
            {
                Vector2 a = vertexs[i];
                Vector2 b = vertexs[(i + 1) % vertexs.Length];
                Vector2 dir1 = b - a;
                Vector2 dir2 = point - a;
                float cross = dir1.x * dir2.y - dir2.x * dir1.y;

                if (i == 0)
                    lastCross = cross;
                else
                {
                    if (cross * lastCross < 0)
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 扇形是否包含点
        /// </summary>
        private static bool IsSectorContainsPoint(SectorColliderData sector, Vector2 point)
        {
            Vector2 toPoint = point - sector.Center;

            if (toPoint.sqrMagnitude > sector.Radius * sector.Radius)
                return false;

            //转换成带π角度
            float angle = sector.Angle * Mathf.PI / 180;
            float angleStart = sector.StartAngle * Mathf.PI / 180;
            var middleDir = new Vector2(Mathf.Cos(angleStart + angle / 2f), Mathf.Sin(angleStart + angle / 2f)) * sector.Radius;
            float angleToPoint = Vector2.Angle(middleDir, toPoint);

            return angleToPoint <= sector.Angle * 0.5f;
        }
        
        /// <summary>
        /// 获取线段与圆的所有交点
        /// </summary>
        private static List<Vector2> GetLineIntersectsCircleVecs(Vector2 p1, Vector2 p2, Vector2 center, float radius)
        {
            List<Vector2> result = new List<Vector2>();
            Vector2 d = p2 - p1;
            Vector2 f = p1 - center;

            float a = Vector2.Dot(d, d);
            float b = 2 * Vector2.Dot(f, d);
            float c = Vector2.Dot(f, f) - radius * radius;

            float discriminant = b * b - 4 * a * c;
            if (discriminant < 0)
                return result;

            discriminant = Mathf.Sqrt(discriminant);
            float t1 = (-b - discriminant) / (2 * a);
            float t2 = (-b + discriminant) / (2 * a);

            if (t1 >= 0 && t1 <= 1)
                result.Add(p1 + t1 * d);
            if (t2 >= 0 && t2 <= 1)
                result.Add(p1 + t2 * d);

            return result;
        }
        #endregion

        #region SAT
        public static bool ShapeIntersectsShape(Vector2[] shapeVecs1, Vector2[] shapeVecs2)
        {
            // 收集所有需要测试的分离轴（两者的边的法线）
            List<Vector2> axes = new();

            for (int i = 0; i < shapeVecs1.Length; i++)
            {
                Vector2 edge = shapeVecs1[(i + 1) % shapeVecs1.Length] - shapeVecs1[i];
                Vector2 normal = new Vector2(-edge.y, edge.x).normalized;
                axes.Add(normal);
            }

            for (int i = 0; i < shapeVecs2.Length; i++)
            {
                Vector2 edge = shapeVecs2[(i + 1) % shapeVecs2.Length] - shapeVecs2[i];
                Vector2 normal = new Vector2(-edge.y, edge.x).normalized;
                axes.Add(normal);
            }

            // 对每条轴进行投影检测
            foreach (var axis in axes)
            {
                if (IsSeparatingAxis(shapeVecs1, shapeVecs2, axis))
                    return false; // 有分离轴 ⇒ 不相交
            }

            return true; // 所有轴都有重叠 ⇒ 相交
        }

        private static bool IsSeparatingAxis(Vector2[] shapeA, Vector2[] shapeB, Vector2 axis)
        {
            float minA = float.MaxValue, maxA = float.MinValue;
            float minB = float.MaxValue, maxB = float.MinValue;

            foreach (var v in shapeA)
            {
                float proj = Vector2.Dot(v, axis);
                minA = Mathf.Min(minA, proj);
                maxA = Mathf.Max(maxA, proj);
            }

            foreach (var v in shapeB)
            {
                float proj = Vector2.Dot(v, axis);
                minB = Mathf.Min(minB, proj);
                maxB = Mathf.Max(maxB, proj);
            }

            // 判断是否有间隙
            return maxA < minB || maxB < minA;
        }
        #endregion
    }
}
