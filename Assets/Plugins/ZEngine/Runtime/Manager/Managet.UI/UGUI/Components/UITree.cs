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
    /// 树形 UI 组件：按树结构渲染可见行（扁平化），支持展开/折叠。
    /// 容器挂 VerticalLayoutGroup（行纵向堆叠）；行缩进由 binder 按 depth 自行处理。
    /// 节点须为引用类型（where T : class），展开态用引用相等判别。
    /// 用法：
    ///   tree.SetData(root, n => n.Children, rowPrefab, (depth, node, t) => {
    ///       t.Find("Label").GetComponent&lt;UIText&gt;().SetText(node.Name);
    ///       t.Find("Expand").GetComponent&lt;UIButton&gt;().OnClick += () => tree.Toggle(node);
    ///       t.Find("Expand").GetComponent&lt;UIText&gt;().SetText(tree.IsExpanded(node) ? "v" : "&gt;");
    ///   });
    /// </summary>
    [RequireComponent(typeof(VerticalLayoutGroup))]
    public class UITree : UIComponentBase
    {
        /// <summary>点击某行节点（由 binder 在行内按钮触发，可选）</summary>
        public event Action<object> OnNodeSelected;

        private object _root;
        private Func<object, IEnumerable<object>> _getChildren;
        private Action<int, object, Transform> _binder;
        private GameObject _rowPrefab;

        // 可见行扁平列表
        private readonly List<RowEntry> _rows = new List<RowEntry>();
        private readonly HashSet<object> _expanded = new HashSet<object>();
        // 当前渲染的行 Transform（与 _rows 一一对应）
        private readonly List<Transform> _active = new List<Transform>();
        private readonly Stack<Transform> _pool = new Stack<Transform>();

        private struct RowEntry
        {
            public readonly object Node;
            public readonly int Depth;
            public RowEntry(object node, int depth) { Node = node; Depth = depth; }
        }

        /// <summary>设置树数据并首次渲染。root 节点默认折叠。</summary>
        public void SetData<T>(T root, Func<T, IEnumerable<T>> getChildren, GameObject rowPrefab,
                               Action<int, T, Transform> binder) where T : class
        {
            _root = root;
            // 适配为非泛型内部委托（节点以 object 传递，调用时还原 T）
            _getChildren = n => getChildren((T)n);
            _binder = (depth, n, t) => binder(depth, (T)n, t);
            _rowPrefab = rowPrefab;
            _expanded.Clear();
            Rebuild();
        }

        /// <summary>展开/折叠指定节点并重渲染。</summary>
        public void Toggle(object node)
        {
            if (node == null) return;
            if (!_expanded.Add(node))
                _expanded.Remove(node);
            Rebuild();
        }

        public bool IsExpanded(object node) => node != null && _expanded.Contains(node);

        /// <summary>展开全部 / 折叠全部</summary>
        public void ExpandAll() { CollectAll(_root, _expanded); Rebuild(); }
        public void CollapseAll() { _expanded.Clear(); Rebuild(); }

        private void CollectAll(object node, HashSet<object> set)
        {
            if (node == null) return;
            set.Add(node);
            var kids = _getChildren?.Invoke(node);
            if (kids != null) foreach (var c in kids) CollectAll(c, set);
        }

        private void Rebuild()
        {
            // 回收当前渲染行进池
            for (int i = 0; i < _active.Count; i++)
            {
                var t = _active[i];
                if (t == null) continue;
                t.SetParent(null, false);
                t.gameObject.SetActive(false);
                _pool.Push(t);
            }
            _active.Clear();
            _rows.Clear();

            if (_root == null || _rowPrefab == null)
                return;

            Flatten(_root, 0);
            for (int i = 0; i < _rows.Count; i++)
            {
                Transform t = _pool.Count > 0 ? _pool.Pop() : ((GameObject)Instantiate(_rowPrefab)).transform;
                t.SetParent(transform, false);
                t.gameObject.SetActive(true);
                _active.Add(t);
                _binder?.Invoke(_rows[i].Depth, _rows[i].Node, t);
            }
        }

        private void Flatten(object node, int depth)
        {
            _rows.Add(new RowEntry(node, depth));
            if (_expanded.Contains(node))
            {
                var kids = _getChildren?.Invoke(node);
                if (kids != null)
                {
                    foreach (var c in kids)
                        Flatten(c, depth + 1);
                }
            }
        }

        public void Select(object node) => OnNodeSelected?.Invoke(node);

        public override void OnRelease()
        {
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
            _rows.Clear();
            _expanded.Clear();
            OnNodeSelected = null;
            _root = null;
            _getChildren = null;
            _binder = null;
            _rowPrefab = null;
        }
    }
}
