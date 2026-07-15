//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------


using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace ZEngine.Module.Collider2D
{
    public abstract class BaseCollider2D : MonoBehaviour, ICollider2D
    {
        public abstract ColliderType ColliderType { get; }

        [Title("Tag类型")]
        public ColliderTag Tag;

        [Title("碰撞体中心世界坐标")]
        public virtual Vector2 Position { get; set; }

        [Title("碰撞体可视化颜色")]
        public Color GizmoColor = Color.green;


        public Action<BaseCollider2D> OnTriggerEnter;
        public Action<BaseCollider2D> OnTriggerStay;
        public Action<BaseCollider2D> OnTriggerExit;

        protected void OnActive()
        {
            if(ColliderManager.Instance != null)
                ColliderManager.Instance.Register(this);
        }

        protected void OnDeActive()
        {
            if(ColliderManager.Instance != null)
                ColliderManager.Instance.UnRegister(this);
        }

        public abstract bool CheckCollision(ICollider2D other);
        public abstract void DrawGizmos(Color color);

        public void OnDrawGizmos()
        {
            DrawGizmos(GizmoColor);
        }

        //当前存在碰撞的对象
        private readonly HashSet<BaseCollider2D> _currentCollisions = new HashSet<BaseCollider2D>();
        //当前帧检测存在碰撞的对象
        private readonly List<BaseCollider2D> _thisFrameCols = new List<BaseCollider2D>();
        
        /// <summary>
        /// 碰撞检测成功的通知调用
        /// </summary>
        /// <param name="other"></param>
        public void NotifyCollision(BaseCollider2D other)
        {
            _thisFrameCols.Add(other);

            if (!_currentCollisions.Contains(other))
            {
                OnTriggerEnter?.Invoke(other);
            }
            else
            {
                OnTriggerStay?.Invoke(other);
            }
        }

        protected virtual void OnEnable()
        {
            OnActive(); // 注册到碰撞管理器
        }

        protected virtual void OnDisable()
        {
            OnDeActive(); // 从碰撞管理器中注销
        }

        private void LateUpdate()
        {
            //找出不再碰撞的对象
            foreach(var other in _currentCollisions)
            {
                if (!_thisFrameCols.Contains(other))
                {
                    OnTriggerExit?.Invoke(other);
                }
            }

            //更新当前的碰撞状态
            _currentCollisions.Clear();
            foreach(var other in _thisFrameCols)
            {
                _currentCollisions.Add(other);
            }

            _thisFrameCols.Clear();
        }



        private void Awake()
        {
            OnTriggerEnter += TriggerEnter;
            OnTriggerStay += TriggerStay;
            OnTriggerExit += TriggerExit;
        }

        private void TriggerEnter(ICollider2D other)
        {
            GizmoColor = Color.red;
        }

        private void TriggerStay(ICollider2D other)
        {
            GizmoColor = Color.red;
        }

        private void TriggerExit(ICollider2D other)
        {
            GizmoColor = Color.green;
        }
    }
}
