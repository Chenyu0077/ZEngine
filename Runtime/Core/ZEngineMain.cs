//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ZEngine.Core
{
    public class ManagerWrapper
    {
        public int Priority { private set; get; }
        public IManager Manager { private set; get; }

        public ManagerWrapper(IManager manager, int priority)
        {
            Manager = manager;
            Priority = priority;
        }
    }

    public static class ZEngineMain
    {
        private static List<ManagerWrapper> _wrappers = new List<ManagerWrapper>(100);
        private static MonoBehaviour _behaviour;
        private static bool _isDirty = false;
        private static long _manager = 0;

        /// <summary>
        /// 初始化管理器
        /// </summary>
        public static void Initialize(MonoBehaviour behaviour)
        {
            if (behaviour == null)
                throw new Exception("ZEngine的behaviour为null");
            if (_behaviour != null)
                throw new Exception($"{nameof(ZEngine)}已经被初始化");

            _behaviour = behaviour;
            GameObject.DontDestroyOnLoad(behaviour.gameObject);

            ZEngineLog.RegisterCallback((logLevel, log) =>
            {
                if (logLevel == ELogLevel.Log)
                {
                    UnityEngine.Debug.Log(log);
                }
                else if (logLevel == ELogLevel.Error)
                {
                    UnityEngine.Debug.LogError(log);
                }
                else if (logLevel == ELogLevel.Warning)
                {
                    UnityEngine.Debug.LogWarning(log);
                }
                else if (logLevel == ELogLevel.Exception)
                {
                    UnityEngine.Debug.LogError(log);
                }
                else
                {
                    throw new NotImplementedException($"{logLevel}");
                }
            });

            behaviour.StartCoroutine(CheckManager());
        }

        /// <summary>
        /// 检测ZEngine是否更新
        /// </summary>
        private static IEnumerator CheckManager()
        {
            var waitTime = new WaitForSeconds(1f);
            yield return waitTime;

            if (_manager == 0)
                throw new Exception("ZEngine初始化失败");
        }

        public static T CreateManager<T>(int priority = 0) where T : class, IManager
        {
            return CreateManager<T>(null, priority);
        }

        /// <summary>
		/// 创建游戏模块
		/// </summary>
		/// <typeparam name="T">模块类</typeparam>
		/// <param name="createParam">创建参数</param>
		/// <param name="priority">运行时的优先级，优先级越大越早执行。如果没有设置优先级，那么会按照添加顺序执行</param>
        public static T CreateManager<T>(object param, int priority = 0) where T : class, IManager
        {
            if (priority < 0)
                throw new Exception("参数priority不能是负数");

            bool isExist = false;
            for(int i = 0; i < _wrappers.Count; i++)
            {
                if (_wrappers[i].Manager.GetType() == typeof(T))
                    isExist = true;
            }
            if (isExist)
                throw new Exception($"{typeof(T)}类型管理器已经存在");

            ZEngineLog.Log($"创建游戏管理器 : {typeof(T)}");
            T manager = Activator.CreateInstance<T>();
            ManagerWrapper wrapper = new ManagerWrapper(manager, priority);
            wrapper.Manager.OnInit(param);
            _wrappers.Add(wrapper);
            _isDirty = true;
            return manager;
        }

        /// <summary>
        /// 更新各管理器
        /// </summary>
        public static void Update()
        {
            _manager++;

            //有新模块则需要重新排序
            if (_isDirty)
            {
                _isDirty = false;
                _wrappers.Sort((left, right) =>
                {
                    if (left.Priority > right.Priority)
                        return -1;
                    else if (left.Priority < right.Priority)
                        return 1;
                    else
                        return 0;
                });
            }

            for(int i = 0; i < _wrappers.Count; i++)
            {
                _wrappers[i].Manager.OnUpdate();
            }
        }

        /// <summary>
        /// 销毁各管理器
        /// </summary>
        public static void Destory()
        {
            for (int i = 0; i < _wrappers.Count; i++)
            {
                _wrappers[i].Manager.OnDestroy();
            }
            _wrappers.Clear();
        }

        /// <summary>
        /// 绘制各管理器GUI
        /// </summary>
        public static void DrawGUI()
        {
            for(int i = 0; i < _wrappers.Count; i++)
            {
                _wrappers[i].Manager.OnGUI();
            }
        }

        /// <summary>
        /// 查询游戏模块是否存在
        /// </summary>
        public static bool Contains<T>() where T : class, IManager
        {
            Type type = typeof(T);
            return Contains(type);
        }

        /// <summary>
        /// 查询游戏模块是否存在
        /// </summary>
        public static bool Contains(Type moduleType)
        {
            for(int i = 0; i < _wrappers.Count; i++)
            {
                if (_wrappers[i].Manager.GetType() == moduleType)
                    return true;
            }
            return false;
        }


        #region 协程相关
        /// <summary>
        /// 开启一个协程
        /// </summary>
        public static Coroutine StartCoroutine(IEnumerator coroutine)
        {
            if (_behaviour == null)
                throw new Exception($"{nameof(ZEngineMain)}未初始化");
            return _behaviour.StartCoroutine(coroutine);
        }

        /// <summary>
        /// 停止一个协程
        /// </summary>
        public static void StopCoroutine(Coroutine coroutine)
        {
            if (_behaviour == null)
                throw new Exception($"{nameof(ZEngineMain)}未初始化");
            _behaviour.StopCoroutine(coroutine);
        }


        /// <summary>
        /// 开启一个协程
        /// </summary>
        public static void StartCoroutine(string methodName)
        {
            if (_behaviour == null)
                throw new Exception($"{nameof(ZEngineMain)}未初始化");
            _behaviour.StartCoroutine(methodName);
        }

        /// <summary>
        /// 停止一个协程
        /// </summary>
        public static void StopCoroutine(string methodName)
        {
            if (_behaviour == null)
                throw new Exception($"{nameof(ZEngineMain)}未初始化");
            _behaviour.StopCoroutine(methodName);
        }


        /// <summary>
        /// 停止所有协程
        /// </summary>
        public static void StopAllCoroutines()
        {
            if (_behaviour == null)
                throw new Exception($"{nameof(ZEngineMain)}未初始化");
            _behaviour.StopAllCoroutines();
        }
        #endregion
    }
}
