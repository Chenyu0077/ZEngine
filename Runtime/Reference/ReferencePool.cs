//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using System;
using System.Collections.Generic;

namespace ZEngine.Reference
{
    /// <summary>
    /// 引用池
    /// </summary>
    public class ReferencePool
    {
        private static readonly Dictionary<Type, ReferenceCollector> _collectors = new Dictionary<Type, ReferenceCollector>();

        /// <summary>
        /// 引用池初始容量
        /// </summary>
        public static int InitCapacity { get; set; } = 100;

        /// <summary>
        /// 引用池数量
        /// </summary>
        public static int Count
        {
            get
            {
                return _collectors.Count;
            }
        }

        /// <summary>
        /// 申请引用对象
        /// </summary>
        public static IReference Spawn(Type type)
        {
            if (!_collectors.ContainsKey(type))
            {
                _collectors.Add(type, new ReferenceCollector(type, InitCapacity));
            }
            return _collectors[type].Spawn();
        }

        /// <summary>
        /// 申请引用对象
        /// </summary>
        public static T Spawn<T>() where T : class, IReference, new()
        {
            Type type = typeof(T);
            return Spawn(type) as T;
        }

        /// <summary>
        /// 回收引用对象
        /// </summary>
        public static void Release(IReference item)
        {
            Type type = item.GetType();
            if (!_collectors.ContainsKey(type))
            {
                _collectors.Add(type, new ReferenceCollector(type, InitCapacity));
            }
            _collectors[type].Release(item);
        }

        /// <summary>
        /// 批量回收列表集合引用对象
        /// </summary>
        public static void Release<T>(List<T> items) where T : class, IReference, new()
        {
            Type type = typeof(T);
            if (!_collectors.ContainsKey(type))
            {
                _collectors.Add(type, new ReferenceCollector(type, InitCapacity));
            }

            for(int i = 0; i < items.Count; i++)
            {
                _collectors[type].Release(items[i]);
            }
        }
    }
}

