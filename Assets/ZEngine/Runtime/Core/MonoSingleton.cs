using UnityEngine;

namespace ZEngine.Core
{
    /// <summary>
    /// Generic singleton base class for MonoBehaviour classes.
    /// </summary>
    /// <typeparam name="T">The type of the MonoBehaviour singleton.</typeparam>
    public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
    {
        private static T _instance;
        private static readonly object _lock = new object();
        private static bool _applicationIsQuitting = false;

        public static T Instance
        {
            get
            {
                if (_applicationIsQuitting)
                {
                    Debug.LogWarning($"[MonoSingleton] Instance of {typeof(T)} already destroyed on application quit. Returning null.");
                    return null;
                }

                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = FindObjectOfType<T>();

                        if (_instance == null)
                        {
                            var go = new GameObject($"[{typeof(T).Name}]");
                            _instance = go.AddComponent<T>();
                            DontDestroyOnLoad(go);
                        }
                    }
                    return _instance;
                }
            }
        }

        protected virtual void Awake()
        {
            if (_instance == null)
            {
                _instance = this as T;
                DontDestroyOnLoad(gameObject);
                OnInit();
            }
            else if (_instance != this)
            {
                Debug.LogWarning($"[MonoSingleton] Duplicate instance of {typeof(T)} detected. Destroying the duplicate.");
                Destroy(gameObject);
            }
        }

        protected virtual void OnApplicationQuit()
        {
            _applicationIsQuitting = true;
        }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        /// <summary>
        /// Called once when the singleton is initialized.
        /// </summary>
        protected virtual void OnInit() { }
    }
}
