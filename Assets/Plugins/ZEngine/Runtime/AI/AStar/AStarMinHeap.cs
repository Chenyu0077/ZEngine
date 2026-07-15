using System.Collections.Generic;

namespace MotionFramework.AI
{
    /// <summary>
    /// 用于 A* 开放列表的二叉最小堆。
    /// 支持含重复条目的懒惰删除（lazy deletion）模式：
    /// 对同一节点更新 G 值时只需 Push 新条目，旧条目在 Pop 时由调用方通过 closedSet 判断跳过。
    /// </summary>
    internal sealed class AStarMinHeap
    {
        private struct Entry
        {
            internal float Cost;
            internal AStarNode Node;
        }

        private readonly List<Entry> _data = new List<Entry>(256);

        internal int Count => _data.Count;

        internal void Push(AStarNode node, float cost)
        {
            _data.Add(new Entry { Cost = cost, Node = node });
            BubbleUp(_data.Count - 1);
        }

        internal AStarNode Pop()
        {
            AStarNode result = _data[0].Node;
            int last = _data.Count - 1;
            _data[0] = _data[last];
            _data.RemoveAt(last);
            if (_data.Count > 0)
                SiftDown(0);
            return result;
        }

        internal void Clear() => _data.Clear();

        private void BubbleUp(int i)
        {
            while (i > 0)
            {
                int parent = (i - 1) >> 1;
                if (_data[parent].Cost <= _data[i].Cost) break;
                Swap(i, parent);
                i = parent;
            }
        }

        private void SiftDown(int i)
        {
            int count = _data.Count;
            while (true)
            {
                int left  = (i << 1) + 1;
                int right = left + 1;
                int min   = i;

                if (left  < count && _data[left].Cost  < _data[min].Cost) min = left;
                if (right < count && _data[right].Cost < _data[min].Cost) min = right;
                if (min == i) break;

                Swap(i, min);
                i = min;
            }
        }

        private void Swap(int a, int b)
        {
            Entry tmp = _data[a];
            _data[a]  = _data[b];
            _data[b]  = tmp;
        }
    }
}
