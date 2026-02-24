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
    public class ChainCollider2D : BaseCollider2D
    {
        public override ColliderType ColliderType => ColliderType.Chain;

        public override Vector2 Position => ChainData.Center;

        [SerializeField, Title("偏移值")]
        private Vector2 _offset = Vector2.zero;
        [SerializeField, Title("顶点")]
        private Vector2[] _points = new Vector2[]
        {
            new Vector2(-1f, -0.5f),
            new Vector2(0f, 0.5f),
            new Vector2(1f, -0.5f),
        };

        public ChainColliderData ChainData => new ChainColliderData(transform.position.V3ToXY() + _offset, _points);


        public override bool CheckCollision(ICollider2D other)
        {
            return CollisionUtils.CheckCollision(this, other);
        }

        public override void DrawGizmos(Color color)
        {
            Gizmos.color = color;
            GizmosUtility.DrawChain(ChainData.Center, ChainData.Points);
        }
    }
}

