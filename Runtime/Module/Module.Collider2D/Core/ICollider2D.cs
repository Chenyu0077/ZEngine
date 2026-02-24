//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using UnityEngine;

namespace ZEngine.Module.Collider2D
{
    public interface ICollider2D
    {
        /// <summary>
        /// 碰撞体类型
        /// </summary>
        ColliderType ColliderType { get; }

        /// <summary>
        /// 检测碰撞
        /// </summary>
        bool CheckCollision(ICollider2D other);

        /// <summary>
        /// 绘制碰撞体轮廓线
        /// </summary>
        void DrawGizmos(Color color);
    }
}
