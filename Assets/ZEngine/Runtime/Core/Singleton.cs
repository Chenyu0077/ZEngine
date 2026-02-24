namespace ZEngine.Core
{
    /// <summary>
    /// Generic singleton base class for non-MonoBehaviour classes.
    /// </summary>
    /// <typeparam name="T">The type of the singleton.</typeparam>
    public abstract class Singleton<T> where T : Singleton<T>, new()
    {
        private static T _instance;
        private static readonly object _lock = new object();

        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new T();
                            _instance.OnInit();
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Called once when the singleton is first created.
        /// </summary>
        protected virtual void OnInit() { }

        /// <summary>
        /// Release the singleton instance.
        /// </summary>
        public virtual void Dispose()
        {
            _instance = null;
        }
    }
}
