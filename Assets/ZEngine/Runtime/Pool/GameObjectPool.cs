using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZEngine.Pool
{
    /// <summary>
    /// A pool for Unity GameObjects.
    /// </summary>
    public class GameObjectPool
    {
        private readonly GameObject _prefab;
        private readonly Transform _container;
        private readonly Stack<GameObject> _inactive = new Stack<GameObject>();
        private readonly List<GameObject> _active = new List<GameObject>();
        private readonly int _maxSize;

        public int CountActive => _active.Count;
        public int CountInactive => _inactive.Count;

        /// <param name="prefab">The prefab to instantiate.</param>
        /// <param name="container">Parent transform for pooled objects.</param>
        /// <param name="initialSize">Number of instances to pre-warm the pool with.</param>
        /// <param name="maxSize">Maximum number of inactive instances retained.</param>
        public GameObjectPool(GameObject prefab, Transform container = null, int initialSize = 0, int maxSize = 50)
        {
            _prefab = prefab ?? throw new ArgumentNullException(nameof(prefab));
            _container = container;
            _maxSize = maxSize;

            for (int i = 0; i < initialSize; i++)
            {
                var go = CreateNew();
                go.SetActive(false);
                _inactive.Push(go);
            }
        }

        /// <summary>
        /// Get an instance from the pool, activating it at the given position and rotation.
        /// </summary>
        public GameObject Get(Vector3 position = default, Quaternion rotation = default)
        {
            GameObject go;
            if (_inactive.Count > 0)
            {
                go = _inactive.Pop();
            }
            else
            {
                go = CreateNew();
            }

            go.transform.SetPositionAndRotation(position, rotation);
            go.SetActive(true);
            _active.Add(go);
            return go;
        }

        /// <summary>
        /// Return a GameObject to the pool.
        /// </summary>
        public void Release(GameObject go)
        {
            if (go == null) return;
            if (!_active.Remove(go))
            {
                Debug.LogWarning($"[GameObjectPool] Releasing object that wasn't tracked as active: {go.name}");
            }
            go.SetActive(false);

            if (_inactive.Count < _maxSize)
            {
                if (_container != null)
                    go.transform.SetParent(_container);
                _inactive.Push(go);
            }
            else
            {
                UnityEngine.Object.Destroy(go);
            }
        }

        /// <summary>
        /// Return all active objects to the pool.
        /// </summary>
        public void ReleaseAll()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                Release(_active[i]);
            }
        }

        /// <summary>
        /// Destroy all pooled objects (active and inactive).
        /// </summary>
        public void Clear()
        {
            foreach (var go in _active)
            {
                if (go != null) UnityEngine.Object.Destroy(go);
            }
            _active.Clear();
            while (_inactive.Count > 0)
            {
                var go = _inactive.Pop();
                if (go != null) UnityEngine.Object.Destroy(go);
            }
        }

        private GameObject CreateNew()
        {
            var go = UnityEngine.Object.Instantiate(_prefab, _container);
            go.name = _prefab.name;
            return go;
        }
    }
}
