//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using System;
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
                    throw new InvalidOperationException(
                        $"{typeof(T).Name} 尚未创建，请先调用 ZEngineMain.CreateManager<{typeof(T).Name}>()");
                return _instance;
            }
        }

        protected UnityEngine.GameObject _root;


        protected ManagerSingleton()
        {
            if (_instance != null)
                throw new InvalidOperationException($"{typeof(T).Name} 实例已存在，不允许重复创建");
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