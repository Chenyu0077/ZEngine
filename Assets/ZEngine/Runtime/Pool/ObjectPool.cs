using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZEngine.Pool
{
    /// <summary>
    /// A generic object pool for any reference type.
    /// </summary>
    /// <typeparam name="T">The type of object to pool.</typeparam>
    public class ObjectPool<T> where T : class
    {
        private readonly Stack<T> _pool;
        private readonly Func<T> _createFunc;
        private readonly Action<T> _onGet;
        private readonly Action<T> _onRelease;
        private readonly Action<T> _onDestroy;
        private readonly int _maxSize;

        public int CountAll { get; private set; }
        public int CountInactive => _pool.Count;
        public int CountActive => CountAll - CountInactive;

        /// <param name="createFunc">Function called to create a new instance.</param>
        /// <param name="onGet">Called when an object is retrieved from the pool.</param>
        /// <param name="onRelease">Called when an object is returned to the pool.</param>
        /// <param name="onDestroy">Called when an object is destroyed (pool exceeds max size).</param>
        /// <param name="defaultCapacity">Initial capacity of the pool.</param>
        /// <param name="maxSize">Maximum number of inactive objects retained.</param>
        public ObjectPool(
            Func<T> createFunc,
            Action<T> onGet = null,
            Action<T> onRelease = null,
            Action<T> onDestroy = null,
            int defaultCapacity = 10,
            int maxSize = 100)
        {
            _createFunc = createFunc ?? throw new ArgumentNullException(nameof(createFunc));
            _onGet = onGet;
            _onRelease = onRelease;
            _onDestroy = onDestroy;
            _maxSize = maxSize;
            _pool = new Stack<T>(defaultCapacity);
        }

        /// <summary>
        /// Retrieve an object from the pool (creates a new one if empty).
        /// </summary>
        public T Get()
        {
            T item;
            if (_pool.Count > 0)
            {
                item = _pool.Pop();
            }
            else
            {
                item = _createFunc();
                CountAll++;
            }
            _onGet?.Invoke(item);
            return item;
        }

        /// <summary>
        /// Return an object to the pool.
        /// </summary>
        public void Release(T item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            _onRelease?.Invoke(item);
            if (_pool.Count < _maxSize)
            {
                _pool.Push(item);
            }
            else
            {
                _onDestroy?.Invoke(item);
                CountAll--;
            }
        }

        /// <summary>
        /// Remove and destroy all inactive objects in the pool.
        /// </summary>
        public void Clear()
        {
            while (_pool.Count > 0)
            {
                var item = _pool.Pop();
                _onDestroy?.Invoke(item);
                CountAll--;
            }
        }
    }
}
