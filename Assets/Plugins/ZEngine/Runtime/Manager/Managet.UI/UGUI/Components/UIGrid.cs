//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ZEngine.Manager.UI.UGUI.Components
{
    /// <summary>
    /// 数据驱动网格 UI 组件（非虚拟化）：容器挂 GridLayoutGroup，按数据实例化/复用 cell 并填充（待优化）。
    /// 适合条目数可控的场景；超大数据量（数千+）请用基于 LoopScrollRect 的 UIListView。
    /// 用法：预制体节点挂 GridLayoutGroup + 本组件，View 内 [UIBind("path")] UIGrid grid;，
    /// grid.SetData(list, cellPrefab, (i, item, t) => t.GetComponent&lt;MyCell&gt;().Set(item));
    /// </summary>
    [RequireComponent(typeof(GridLayoutGroup))]
    public class UIGrid : UIComponentBase
    {
        protected GridLayoutGroup _layout;
        protected GridLayoutGroup Layout => _layout != null ? _layout : (_layout = GetComponent<GridLayoutGroup>());

        private GameObject _cellPrefab;
        // 当前激活的 cell（按数据索引）
        private readonly List<Transform> _active = new List<Transform>();
        // 回收池（SetActive(false)，下次复用）
        private readonly Stack<Transform> _pool = new Stack<Transform>();

        public void SetCellPrefab(GameObject prefab)
        {
            _cellPrefab = prefab;
        }

        /// <summary>
        /// 设置数据并填充网格。复用已有 cell，不足则按 prefab 实例化，多余回收进池。
        /// binder(i, item, cellTransform) 负责把数据写进 cell。
        /// </summary>
        public void SetData<T>(IList<T> data, GameObject cellPrefab, Action<int, T, Transform> binder)
        {
            if (cellPrefab != null)
                _cellPrefab = cellPrefab;
            if (_cellPrefab == null)
            {
                Debug.LogWarning("[UIGrid] 未设置 cellPrefab，无法填充");
                return;
            }

            // 回收所有激活 cell 进池
            for (int i = 0; i < _active.Count; i++)
            {
                var t = _active[i];
                if (t == null) continue;
                t.SetParent(null, false);
                t.gameObject.SetActive(false);
                _pool.Push(t);
            }
            _active.Clear();

            if (data == null)
                return;

            for (int i = 0; i < data.Count; i++)
            {
                Transform t = _pool.Count > 0 ? _pool.Pop() : ((GameObject)Instantiate(_cellPrefab)).transform;
                t.SetParent(transform, false);
                t.gameObject.SetActive(true);
                _active.Add(t);
                if (binder != null)
                    binder(i, data[i], t);
            }
        }

        public int Count => _active.Count;

        public Transform GetItem(int index)
        {
            return (index >= 0 && index < _active.Count) ? _active[index] : null;
        }

        public override void OnRelease()
        {
            // 销毁激活 cell（非池化归还，因为宿主 View 正在释放）
            for (int i = 0; i < _active.Count; i++)
            {
                if (_active[i] != null)
                    Destroy(_active[i].gameObject);
            }
            _active.Clear();
            while (_pool.Count > 0)
            {
                var t = _pool.Pop();
                if (t != null)
                    Destroy(t.gameObject);
            }
            _cellPrefab = null;
        }
    }
}
