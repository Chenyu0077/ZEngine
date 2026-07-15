//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using Sirenix.OdinInspector;
using UnityEngine;
using ZEngine.Extension;
using ZEngine.Utility;

namespace ZEngine.Module.Collider2D
{
    public class PolygonCollider2D : BaseCollider2D
    {
        public override ColliderType ColliderType => ColliderType.Polygon;
        public override Vector2 Position => PolygonData.Center;

        [SerializeField, Title("偏移值")]
        private Vector2 _offset = Vector2.zero;
        [SerializeField, Title("顶点")]
        private Vector2[] _points = new Vector2[]
        {
            new Vector2(-0.5f, -0.5f),
            new Vector2(-0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, -0.5f),
        };
        public PolygonColliderData PolygonData => new PolygonColliderData(transform.position.V3ToXY() + _offset, _points);


        public override bool CheckCollision(ICollider2D other)
        {
            return CollisionUtils.CheckCollision(this, other);
        }

        public override void DrawGizmos(Color color)
        {
            Gizmos.color = color;
            GizmosUtility.DrawPolygon(PolygonData.Center, PolygonData.Points);
        }
    }
}
