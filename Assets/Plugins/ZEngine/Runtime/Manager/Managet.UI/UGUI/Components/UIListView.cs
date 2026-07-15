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
    /// 虚拟化无限滚动列表：基于 LoopScrollRect（LoopVerticalScrollRect / LoopHorizontalScrollRect）封装，
    /// 自带 PrefabSource（cell 实例化+对象池）与 DataSource 适配器，业务侧只需 SetData + binder。
    /// 注意：LoopScrollRect 的 RefillCells/ClearCells 仅在 PlayMode 生效（!isPlaying 早返回），
    /// 故填充验证需进 PlayMode 且节点已配好（Content 挂对应 LayoutGroup、pivot/anchor 满足方向约定、cell 预制体）。
    /// 用法：预制体节点挂 LoopVerticalScrollRect + 本组件，View 内 [UIBind("path")] UIListView list;，
    ///   list.SetData(itemList, cellPrefab, (i, item, t) => t.GetComponent&lt;MyCell&gt;().Set(item));
    /// </summary>
    [RequireComponent(typeof(LoopScrollRect))]
    public class UIListView : UIComponentBase
    {
        protected LoopScrollRect _scroll;
        protected LoopScrollRect Scroll => _scroll != null ? _scroll : (_scroll = GetComponent<LoopScrollRect>());

        private readonly PrefabSource _prefabSource = new PrefabSource();
        private DataSourceAdapter _dataSource;

        /// <summary>设置 cell 预制体（若 SetData 时已传则可省）。</summary>
        public void SetCellPrefab(GameObject prefab) => _prefabSource.prefab = prefab;

        /// <summary>
        /// 设置数据并刷新列表。binder(i, item, cellTransform) 负责把数据写进 cell，
        /// cell 复用时会被反复调用，binder 内不要缓存跨复用的状态。
        /// </summary>
        public void SetData<T>(IList<T> data, GameObject cellPrefab, Action<int, T, Transform> binder)
        {
            var s = Scroll;
            if (s == null)
            {
                Debug.LogWarning("[UIListView] 未找到 LoopScrollRect 组件");
                return;
            }
            if (cellPrefab != null)
                _prefabSource.prefab = cellPrefab;
            if (_prefabSource.prefab == null)
            {
                Debug.LogWarning("[UIListView] 未设置 cellPrefab，无法填充");
                return;
            }

            // 适配为非泛型 ProvideData（闭包捕获 data/binder，按 idx 取 item）
            _dataSource = new DataSourceAdapter((idx, tr) =>
            {
                if (data != null && idx >= 0 && idx < data.Count)
                    binder?.Invoke(idx, data[idx], tr);
            });

            s.prefabSource = _prefabSource;
            s.dataSource = _dataSource;
            s.totalCount = data != null ? data.Count : 0;
            s.ClearCells();
            s.RefillCells();
        }

        /// <summary>清空列表（totalCount=0 + ClearCells）。</summary>
        public void Clear()
        {
            var s = Scroll;
            if (s == null) return;
            s.ClearCells();
            s.totalCount = 0;
        }

        /// <summary>刷新当前可见 cell 的数据（不重建结构）。</summary>
        public void Refresh()
        {
            Scroll?.RefreshCells();
        }

        /// <summary>滚动定位模式（本地枚举，避免业务侧耦合 LoopScrollRect 程序集）</summary>
        public enum UIScrollMode { ToStart, ToCenter, JustAppear }

        /// <summary>滚动到指定索引 cell。</summary>
        public void ScrollToCell(int index, float speed, float offset = 0, UIScrollMode mode = UIScrollMode.ToStart)
        {
            var s = Scroll;
            if (s == null) return;
            LoopScrollRectBase.ScrollMode m;
            switch (mode)
            {
                case UIScrollMode.ToCenter: m = LoopScrollRectBase.ScrollMode.ToCenter; break;
                case UIScrollMode.JustAppear: m = LoopScrollRectBase.ScrollMode.JustAppear; break;
                default: m = LoopScrollRectBase.ScrollMode.ToStart; break;
            }
            s.ScrollToCell(index, speed, offset, m);
        }

        public int TotalCount => Scroll != null ? Scroll.totalCount : 0;

        // ---- cell 实例化 + 池化 ----
        private class PrefabSource : LoopScrollPrefabSource
        {
            public GameObject prefab;
            private readonly Stack<Transform> _pool = new Stack<Transform>();

            public GameObject GetObject(int index)
            {
                if (prefab == null) return null;
                Transform t = _pool.Count > 0 ? _pool.Pop() : null;
                return t != null ? t.gameObject : (GameObject)UnityEngine.Object.Instantiate(prefab);
            }

            public void ReturnObject(Transform trans)
            {
                if (trans == null) return;
                trans.SetParent(null, false);
                trans.gameObject.SetActive(false);
                _pool.Push(trans);
            }
        }

        // ---- 数据 -> ProvideData 适配（非泛型，避免 MonoBehaviour 泛型） ----
        private class DataSourceAdapter : LoopScrollDataSource
        {
            private readonly Action<int, Transform> _provide;
            public DataSourceAdapter(Action<int, Transform> provide) { _provide = provide; }
            public void ProvideData(Transform transform, int idx) => _provide?.Invoke(idx, transform);
        }

        public override void OnRelease()
        {
            _dataSource = null;
            _prefabSource.prefab = null;
        }
    }
}
