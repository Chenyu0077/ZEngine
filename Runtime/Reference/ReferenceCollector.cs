//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZEngine.Reference
{
    public class ReferenceCollector
    {
        private readonly Stack<IReference> _collector;

        /// <summary>
        /// 引用类型
        /// </summary>
        public Type ClassType { private set; get; }

        /// <summary>
        /// 内部缓冲池数量
        /// </summary>
        public int Count
        {
            get
            {
                return _collector.Count;
            }
        }

        /// <summary>
        /// 外部引用数量
        /// </summary>
        public int SpawnCount { private set; get; }

        public ReferenceCollector(Type type, int capacity)
        {
            ClassType = type;

            //初始化缓存池
            _collector = new Stack<IReference>(capacity);

            //检测是否继承了IReference接口
            Type temp = type.GetInterface(nameof(IReference));
            if (temp == null)
                throw new Exception($"{type.Name}没有继承{nameof(IReference)}");
        }

        /// <summary>
        /// 申请对象
        /// </summary>
        public IReference Spawn()
        {
            IReference item;
            if(_collector.Count > 0)
            {
                item = _collector.Pop();
            }
            else
            {
                item = Activator.CreateInstance(ClassType) as IReference;
            }
            SpawnCount++;
            return item;
        }

        /// <summary>
        /// 回收引用对象
        /// </summary>
        public void Release(IReference item)
        {
            if (item == null)
                return;

            if (item.GetType() != ClassType)
                throw new Exception($"{item.GetType().Name}类型不是{ClassType.Name}");

            SpawnCount--;
            item.OnRelease();
            _collector.Push(item);
        }

        /// <summary>
        /// 清空引用池
        /// </summary>
        public void Clear()
        {
            _collector.Clear();
            SpawnCount = 0;
        }
    }
}
