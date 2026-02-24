//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using System.Collections.Generic;
using Sirenix.Utilities;
using UnityEngine;
using ZEngine.Core;

namespace ZEngine.Module.Collider2D
{
    public class ColliderManager : ManagerSingleton<ColliderManager>, IManager
    {
        #region 设置：可手动修改
        //空间划分的单位大小(可调试)
        private const float _cellSize = 20f;
        //Tag的允许碰撞对象
        private readonly Dictionary<ColliderTag, HashSet<ColliderTag>> _tagCollisionMatrix = new Dictionary<ColliderTag, HashSet<ColliderTag>>()
        {
            {ColliderTag.Player, new HashSet<ColliderTag>(){ColliderTag.Enemy, ColliderTag.Floor, ColliderTag.Player} },
            { ColliderTag.Enemy, new HashSet<ColliderTag> { ColliderTag.Player, ColliderTag.Floor, ColliderTag.Enemy } },
            { ColliderTag.Floor, new HashSet<ColliderTag> { ColliderTag.Player, ColliderTag.Enemy } },
            { ColliderTag.None, new HashSet<ColliderTag>() },
        };
        #endregion


        private readonly Dictionary<Vector2Int, List<BaseCollider2D>> _grid = new Dictionary<Vector2Int, List<BaseCollider2D>>(); //网格对应的碰撞体列表
        private readonly HashSet<BaseCollider2D> _allColliders = new HashSet<BaseCollider2D>();
        private readonly Dictionary<BaseCollider2D, Vector2Int> _lastCell = new Dictionary<BaseCollider2D, Vector2Int>();   //碰撞体最后所在网格坐标状态

        public HashSet<BaseCollider2D> AllColliders => _allColliders;//供外部获取使用
 
        #region 注册与卸载
        public void Register(BaseCollider2D collider)
        {
            if(!_allColliders.Contains(collider))
                _allColliders.Add(collider);
        }

        public void UnRegister(BaseCollider2D collider)
        {
            if(_allColliders.Contains(collider))
                _allColliders.Remove(collider);
        }
        #endregion


        #region 生命周期函数
        public void OnInit(object param)
        {
            _root = new GameObject("[Z][ColliderManager]");
            GameObject.DontDestroyOnLoad(_root);

            var objs = GameObject.FindObjectsOfType<BaseCollider2D>();
            objs.ForEach(x => Register(x));
        }

        public void OnUpdate()
        {
            UpdateCollisions();
        }

        public void OnGUI()
        {
            
        }

        public void OnDestroy()
        {
            DestroySingleton();
        }
        #endregion


        #region 碰撞体检测
        /// <summary>
        /// 更新碰撞体检测
        /// </summary>
        private void UpdateCollisions()
        {
            foreach(var collider in _allColliders)
            {
                UpdateColliderCell(collider);
            }

            foreach(var col in _allColliders)
            {
                var cell = WorldToCell(col.Position);
                var nearbyCols = GetNearbyColliders(cell);

                foreach(var other in nearbyCols)
                {
                    if(col == other) continue;

                    //Tag过滤
                    if (!IsTagCollisionAllowed(col.Tag, other.Tag))
                        continue;

                    //防止双向重复检测
                    if(col.GetInstanceID() < other.GetInstanceID())
                    {
                        if (col.CheckCollision(other))
                        {
                            col.NotifyCollision(other);
                            other.NotifyCollision(col);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 更新碰撞体位置信息（空间划分）
        /// </summary>
        /// <param name="col"></param>
        private void UpdateColliderCell(BaseCollider2D col)
        {
            var cell = WorldToCell(col.Position);
            if(_lastCell.TryGetValue(col, out var oldCell))
            {
                if (oldCell == cell)
                    return;
                if (_grid.TryGetValue(oldCell, out var oldList))
                    oldList.Remove(col);
            }
            _lastCell[col] = cell;

            if(!_grid.TryGetValue(cell, out var list))
                _grid[cell] = list = new List<BaseCollider2D>();
            list.Add(col);
        }
        #endregion


        #region 其他
        /// <summary>
        /// 将当前世界坐标点转化为所建立网格的坐标点
        /// </summary>
        private Vector2Int WorldToCell(Vector2 worldPos)
        {
            return new Vector2Int(Mathf.FloorToInt(worldPos.x / _cellSize), Mathf.FloorToInt(worldPos.y / _cellSize));
        }

        /// <summary>
        /// 获取附近所有的碰撞体（规则是：传入网格坐标点周围一圈的坐标）
        /// </summary>
        private List<BaseCollider2D> GetNearbyColliders(Vector2Int vector)
        {
            List<BaseCollider2D> colliders = new List<BaseCollider2D>();
            for(int dx = -1; dx <= 1; dx++)
            {
                for(int dy = -1; dy <= 1; dy++)
                {
                    Vector2Int cell = new Vector2Int(vector.x + dx, vector.y + dy);
                    if(_grid.TryGetValue(cell, out var list))
                    {
                        colliders.AddRange(list);
                    }
                }
            }
            return colliders;
        }

        /// <summary>
        /// 检测两碰撞体是否可检测
        /// </summary>
        /// <param name="tagA"></param>
        /// <param name="tagB"></param>
        /// <returns></returns>
        private bool IsTagCollisionAllowed(ColliderTag tagA, ColliderTag tagB)
        {
            if(_tagCollisionMatrix.TryGetValue(tagA, out var tags))
            {
                if (tags.Contains(tagB))
                    return true;
            }
            return false;
        }
        #endregion
    }
}
