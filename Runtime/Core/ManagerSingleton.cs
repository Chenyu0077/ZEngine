//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using UnityEngine; 

namespace ZEngine.Core
{
    public abstract class ManagerSingleton<T> where T : class, IManager
    {
        private static T _instance;

        public static T Instance 
        {
            get
            {
                if (_instance == null)
                    Debug.Log($"{typeof(T)} is not create. {nameof(ZEngine)}.{nameof(ZEngine)} create");
                return _instance;
            }
        }

        protected GameObject _root;


        protected ManagerSingleton()
        {
            if (_instance != null)
                Debug.Log($"{typeof(T)} instance already created.");
            _instance = this as T;
        }

        protected void DestroySingleton()
        {
            _instance = null;
            if(_root != null)
            {
                GameObject.Destroy(_root);
            }
        }
    }
}