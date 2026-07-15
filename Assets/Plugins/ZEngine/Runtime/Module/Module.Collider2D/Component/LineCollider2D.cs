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
    [RequireComponent(typeof(Transform))]
    public class LineCollider2D : BaseCollider2D
    {
        public override ColliderType ColliderType => ColliderType.Line;
        public override Vector2 Position => LineData.Center;

        [SerializeField, Title("偏移值")]
        private Vector2 _offset = Vector2.zero;
        [SerializeField, Title("左端点")]
        private Vector2 _v1 = new Vector2(-1f, 0);
        [SerializeField, Title("右端点")]
        private Vector2 _v2 = new Vector2(1f, 0);

        public LineColliderData LineData => new LineColliderData(transform.position.V3ToXY() + _offset, _v1, _v2);


        public override bool CheckCollision(ICollider2D other)
        {
            return CollisionUtils.CheckCollision(this, other);
        }

        public override void DrawGizmos(Color color)
        {
            Gizmos.color = color;
            GizmosUtility.DrawLine(LineData.Center, LineData.V1, LineData.V2);
        }
    }
}
