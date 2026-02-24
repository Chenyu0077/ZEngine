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
    public class SectorCollider2D : BaseCollider2D
    {
        public override ColliderType ColliderType => ColliderType.Sector;

        public override Vector2 Position => SectorData.Center;

        [SerializeField, Title("扇心")]
        private Vector2 _offset = Vector2.zero;
        [SerializeField, Title("半径"), MinValue(0.01f), MaxValue(25f)]
        private float _radius = 1f;
        [SerializeField, Title("角度"), MinValue(1f), MaxValue(360f)]
        private float _angle = 90f;
        [SerializeField, Title("扇形起始角度")]
        private float _startAngle = 0;


        public SectorColliderData SectorData => new SectorColliderData(transform.position.V3ToXY() + _offset, _radius, _angle, _startAngle);


        public override bool CheckCollision(ICollider2D other)
        {
            return CollisionUtils.CheckCollision(this, other);
        }

        public override void DrawGizmos(Color color)
        {
            Gizmos.color = color;
            GizmosUtility.DrawSector(SectorData.Center, SectorData.Radius, SectorData.Angle, SectorData.StartAngle);
        }
    }
}
