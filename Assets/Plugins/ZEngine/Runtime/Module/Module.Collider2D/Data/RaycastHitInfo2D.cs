//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------


using UnityEngine;

namespace ZEngine.Module.Collider2D
{
    public struct RaycastHitInfo2D
    {
        public Vector2 Point;   //命中点
        public Vector2 Normal;  //法线
        public float Distance;  //距离
        public ICollider2D Collider;    //命中的碰撞体对象
    }
}
