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
    public class CircleCollider2D : BaseCollider2D
    {
        public override ColliderType ColliderType => ColliderType.Circle;
        public override Vector2 Position => CircleData.Center;

        [SerializeField, Title("偏移值")]
        private Vector2 _offset = Vector2.zero;
        [SerializeField, Title("半径"), MinValue(0.01f), MaxValue(25f)]
        private float _radius = 1f;

        public CircleColliderData CircleData => new CircleColliderData(transform.position.V3ToXY() + _offset, _radius);


        public override bool CheckCollision(ICollider2D other)
        {
            return CollisionUtils.CheckCollision(this, other);
        }

        public override void DrawGizmos(Color color)
        {
            Gizmos.color = color;
            GizmosUtility.DrawCircle(CircleData.Center, CircleData.Radius);
        }
    }
}
