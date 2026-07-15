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
    public class BoxCollider2D : BaseCollider2D
    {
        public override ColliderType ColliderType => ColliderType.Box;

        public override Vector2 Position => BoxData.Center;

        [SerializeField, Title("偏移值")]
        private Vector2 _offset = Vector2.zero;
        [SerializeField, Title("大小"), MinValue(0.01f), MaxValue(25f)]
        private Vector2 _size = Vector2.one;

        public BoxColliderData BoxData => new BoxColliderData(transform.position.V3ToXY() + _offset, _size);


        public override bool CheckCollision(ICollider2D other)
        {
            return CollisionUtils.CheckCollision(this, other);
        }

        

        public override void DrawGizmos(Color color)
        {
            Gizmos.color = color;
            GizmosUtility.DrawRect(BoxData.Center, BoxData.Size);
        }
    }
}
