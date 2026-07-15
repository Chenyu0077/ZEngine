//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using System;
using UnityEngine;

namespace Main.Core
{
    /// <summary>
    /// Behaviour单例基类
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class BehaviourSingleton<T> : MonoBehaviour where T : BehaviourSingleton<T>
    {
        private static T _instance;

        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    // 在场景里找寻第一个该组件的对象
                    var found = GameObject.FindObjectOfType<T>();
                    if (found != null) return found;

                    Type type = typeof(T);
                    string name = type.Name;
                    var go = GameObject.Find(name);
                    if (go == null)
                        go = GameObject.Find("GameManager");

                    if(go != null)
                    {
                        _instance = go.GetComponent<T>();
                        if(_instance == null)
                            _instance = go.AddComponent<T>();
                    }
                    else
                    {
                        GameObject singletonObject = new GameObject(name);
                        _instance = singletonObject.AddComponent<T>();
                        DontDestroyOnLoad(singletonObject);
                    }

                    if(_instance == null)
                        Debug.LogError($"Failed to create BehaviourSingleton instance of type {type}.");
                    else
                        _instance.Initialize();
                }
                     
                return _instance;
            }
        }

        protected virtual void Initialize()
        {
            
        }

        protected virtual void OnDestroy()
        {
            DestroySingleton();
        }

        protected virtual void DestroySingleton()
        {
            if(_instance != null)
            {
                if( _instance.gameObject != null)
                {
                    var name = typeof(T).Name;
                    if (_instance.gameObject.name.Equals(name))
                        Destroy(_instance.gameObject);
                }
            }
            _instance = null;
        }
    }
}
