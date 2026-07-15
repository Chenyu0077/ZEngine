//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using UnityEngine;
using ZEngine.Core;

namespace Main.Core
{
    /// <summary>
    /// 普通单例基类
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Singleton<T> where T : class, new()
    {
        private static T _instance;

        public static T Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new T();
                return _instance;
            }
        }

        protected Singleton()
        {
            if (_instance != null)
                Debug.Log($"{typeof(T)} instance already created.");
            _instance = this as T;
            // 注册销毁回调，确保单例在ZEngine销毁时被正确清理
            ZEngineMain.RegisterDestroyAction(DestroySingleton);
        }

        protected virtual void DestroySingleton()
        {
            _instance = null;
        }
    }
}
