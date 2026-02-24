//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;
using ZEngine.Core;
using ZEngine.Manager.Resource;

namespace ZEngine.Manager.Pool
{
    public class ObjectPoolManager : ManagerSingleton<ObjectPoolManager>, IManager
    {
        /// <summary>
		/// 游戏模块创建参数
		/// </summary>
		public class CreateParameters
        {
            /// <summary>
            /// 是否启用惰性对象池
            /// </summary>
            public bool EnableLazyPool = false;

            /// <summary>
            /// 默认的初始容器值
            /// </summary>
            public int DefaultInitCapacity = 0;

            /// <summary>
            /// 默认的最大容器值
            /// </summary>
            public int DefaultMaxCapacity = int.MaxValue;

            /// <summary>
            /// 默认的静默销毁时间
            /// 注意：小于零代表不主动销毁
            /// 该参数即表示：超过该时间没有对对象池做变动，那么就删除该对象池
            /// </summary>
            public float DefaultDestroyTime = -1f;
        }

        private readonly Dictionary<string, GameObjectCollector> _collectors = new Dictionary<string, GameObjectCollector>(100);
        private readonly List<GameObjectCollector> _removeList = new List<GameObjectCollector>(100);
        private bool _enableLazyPool;
        private int _defaultInitCapacity;
        private int _defaultMaxCapacity;
        private float _defaultDestroyTime;

        public void OnInit(object param)
        {
            // 检测依赖模块
            if (ZEngineMain.Contains(typeof(ResourceManager)) == false)
                throw new Exception($"{nameof(ObjectPoolManager)}依赖于{nameof(ResourceManager)}");

            CreateParameters parameters = param as CreateParameters;
            if (param == null)
                throw new Exception($"{nameof(ObjectPoolManager)}无有效参数");
            if (parameters.DefaultMaxCapacity < parameters.DefaultInitCapacity)
                throw new Exception("最大容量一定是比初始容量更大的!");

            _enableLazyPool = parameters.EnableLazyPool;
            _defaultInitCapacity = parameters.DefaultInitCapacity;
            _defaultMaxCapacity = parameters.DefaultMaxCapacity;
            _defaultDestroyTime = parameters.DefaultDestroyTime;

            _root = new GameObject("[Z][PoolManager]");
            _root.transform.position = Vector3.zero;
            _root.transform.eulerAngles = Vector3.zero;
            UnityEngine.Object.DontDestroyOnLoad(_root);
        }

        public void OnUpdate()
        {
            _removeList.Clear();
            foreach (var valuePair in _collectors)
            {
                var collector = valuePair.Value;
                if (collector.CanAutoDestroy())
                    _removeList.Add(collector);
            }

            // 移除并销毁
            foreach (var collector in _removeList)
            {
                _collectors.Remove(collector.Location);
                collector.Destroy();
            }
        }

        public void OnDestroy()
        {
            DestroyAll();
            DestroySingleton();
        }

        public void OnGUI()
        {
            
        }


        /// <summary>
		/// 是否都已经加载完毕
		/// </summary>
		public bool IsAllDone()
        {
            foreach (var pair in _collectors)
            {
                if (pair.Value.IsDone == false)
                    return false;
            }
            return true;
        }

        /// <summary>
		/// 销毁所有对象池及其资源
		/// </summary>
		public void DestroyAll()
        {
            List<GameObjectCollector> removeList = new List<GameObjectCollector>();
            foreach (var pair in _collectors)
            {
                if (pair.Value.DontDestroy == false)
                    removeList.Add(pair.Value);
            }

            // 移除并销毁
            foreach (var collector in removeList)
            {
                _collectors.Remove(collector.Location);
                collector.Destroy();
            }

            ZEngineLog.Log("销毁全部对象池!");
        }


        /// <summary>
		/// 创建指定资源的游戏对象池
		/// </summary>
		/// <param name="location">资源定位地址</param>
        /// <param name="tag">对象池标签（初次创建对象池时可以设置）</param>
		/// <param name="dontDestroy">是否常驻不销毁</param>
		/// <param name="initCapacity">初始的容器值</param>
		/// <param name="maxCapacity">最大的容器值</param>
		/// <param name="destroyTime">静默销毁时间（注意：小于零代表不主动销毁）</param>
		public GameObjectCollector CreatePool(string location, string tag = "", bool dontDestroy = false, int initCapacity = 0, int maxCapacity = int.MaxValue, float destroyTime = -1f)
        {
            if (_collectors.ContainsKey(location))
            {
                ZEngineLog.Warning($"Asset is already existed : {location}");
                return _collectors[location];
            }
            return CreatePoolInternal(location, tag, dontDestroy, initCapacity, maxCapacity, destroyTime);
        }

        /// <summary>
        /// 获取游戏对象
        /// (因为资源加载用的是异步加载，所以如果没有预创建对象池的话，第一次Spawn会异步创建资源，需等待完成，
        /// 可通过“IsHasPool”判断是否提前创建过对象池)
        /// </summary>
        /// <param name="location">资源定位地址</param>
        /// <param name="tag">对象池标签(未提前创建对象池，那么只有第一次Spawn的时候才能设置；否则不能设置)</param>
        /// <param name="forceClone">强制克隆游戏对象，忽略缓存池里的对象(克隆是同步的)</param>
        /// <param name="userDatas">用户自定义数据</param>
        /// /// <returns></returns>

        public SpawnGameObject Spawn(string location, string tag = "", bool forceClone = false, params System.Object[] userDatas)
        {
            if (_collectors.ContainsKey(location))
            {
                return _collectors[location].Spawn(forceClone, userDatas);
            }
            else
            {
                // 如果不存在，创建游戏对象池
                GameObjectCollector pool = CreatePoolInternal(location, tag, false, _defaultInitCapacity, _defaultMaxCapacity, _defaultDestroyTime);
                return pool.Spawn(forceClone, userDatas);
            }
        }


        /// <summary>
        /// 判断是否存在指定资源的对象池
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        public bool IsHasPool(string location)
        {
            return _collectors.ContainsKey(location);
        }

        /// <summary>
        /// 通过标签获取所有正在使用的游戏对象
        /// </summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        public List<SpawnGameObject> GetSpawnGameObjectsByTag(string tag)
        {
            List<SpawnGameObject> result = new List<SpawnGameObject>();
            foreach (var pair in _collectors)
            {
                if (pair.Value.Tag == tag)
                {
                    result.AddRange(pair.Value.GetSpawnGameObjects());
                }
            }
            return result;
        }



        /// <summary>
        /// 创建指定资源的游戏对象池
        /// </summary>
        /// <param name="location">资源定位地址</param>
        /// <param name="dontDestroy">是否常驻不销毁</param>
        /// <param name="initCapacity">初始的容器值</param>
        /// <param name="maxCapacity">最大的容器值</param>
        /// <param name="destroyTime">静默销毁时间（注意：小于零代表不主动销毁）</param>
        /// <returns></returns>
        private GameObjectCollector CreatePoolInternal(string location, string tag, bool dontDestroy, int initCapacity, int maxCapacity, float destroyTime)
        {
            GameObjectCollector pool = new GameObjectCollector(_root.transform, location, tag, dontDestroy, initCapacity, maxCapacity, destroyTime);
            _collectors.Add(location, pool);
            return pool;
        }
    }
}
