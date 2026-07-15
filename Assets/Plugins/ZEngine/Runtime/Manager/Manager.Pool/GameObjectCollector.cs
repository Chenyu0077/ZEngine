//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------


using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using YooAsset;
using ZEngine.Manager.Resource;

namespace ZEngine.Manager.Pool
{
    public class GameObjectCollector
    {
        private readonly Queue<GameObject> _cache;
        private readonly List<SpawnGameObject> _loadingSpawn = new List<SpawnGameObject>();//等待加载的游戏对象列表
        private readonly List<SpawnGameObject> _usingSpawn = new List<SpawnGameObject>();//正在使用的游戏对象列表(外部引用列表)
        private readonly Transform _root;
        private AssetHandle _handle;
        private float _lastRestoreRealTime = -1f;

        /// <summary>
		/// 资源定位地址
		/// </summary>
		public string Location { private set; get; }

        /// <summary>
        /// 对象池标签（并不唯一，只是代表一个身份，通常为一类事物）
        /// 初次创建对象池时可以设置（为预创建对象池且是第一次Spawn时可以设置）
        /// </summary>
        public string Tag { private set; get; }

        /// <summary>
        /// 资源常驻不销毁
        /// </summary>
        public bool DontDestroy { private set; get; }

        /// <summary>
        /// 对象池的初始容量
        /// </summary>
        public int InitCapacity { private set; get; }

        /// <summary>
        /// 对象池的最大容量
        /// </summary>
        public int MaxCapacity { private set; get; }

        /// <summary>
        /// 静默销毁时间
        /// </summary>
        public float DestroyTime { private set; get; }

        /// <summary>
        /// 是否加载完毕
        /// </summary>
        public bool IsDone
        {
            get
            {
                return _handle == null ? false : _handle.IsDone;
            }
        }

        /// <summary>
        /// 当前的加载状态
        /// </summary>
        public EOperationStatus States
        {
            get
            {
                return _handle.Status;
            }
        }

        /// <summary>
        /// 内部缓存总数
        /// </summary>
        public int CacheCount
        {
            get { return _cache.Count; }
        }

        /// <summary>
        /// 外部使用总数
        /// </summary>
        public int SpawnCount { private set; get; } = 0;


        public GameObjectCollector(Transform root, string location, string tag, bool dontDestroy, int initCapacity, int maxCapacity, float destroyTime)
        {
            _root = root;
            Location = location;
            Tag = tag;
            DontDestroy = dontDestroy;
            InitCapacity = initCapacity;
            MaxCapacity = maxCapacity;
            DestroyTime = destroyTime;

            // 创建缓存池
            _cache = new Queue<GameObject>(initCapacity);

            // 加载资源
            LoadGameObject(location).Forget();
        }

        private async UniTaskVoid LoadGameObject(string location)
        {
            _handle = await ResourceManager.Instance.LoadAssetAsync<GameObject>(location);
            _handle.Completed += Handle_Completed;
        }

        private void Handle_Completed(AssetHandle obj)
        {
            // 创建初始对象
            for (int i = 0; i < InitCapacity; i++)
            {
                GameObject cloneObj = InstantiateGameObject();
                SetRestoreCloneObject(cloneObj);
                _cache.Enqueue(cloneObj);
            }

            // 最后返回结果
            for (int i = 0; i < _loadingSpawn.Count; i++)
            {
                GameObject cloneObj = InstantiateGameObject();
                SpawnGameObject spawn = _loadingSpawn[i];
                spawn.Go = cloneObj;
                _usingSpawn.Add(spawn);

                // 注意Spawn对象的当前状态
                if (spawn.SpawnState == SpawnGameObject.ESpawnState.Restore)
                {
                    if (spawn.Go != null)
                        RestoreGameObject(spawn.Go);
                }
                else if (spawn.SpawnState == SpawnGameObject.ESpawnState.Discard)
                {
                    if (spawn.Go != null)
                        DiscardGameObject(spawn.Go);
                }
                else
                {
                    SetSpawnCloneObject(cloneObj);
                    spawn.UserCallback?.Invoke(spawn);
                }
            }
            _loadingSpawn.Clear();
        }

        private GameObject InstantiateGameObject()
        {
            var cloneObject = _handle.InstantiateSync();

            // 如果加载失败，创建临时对象
            if (cloneObject == null)
                cloneObject = new GameObject(Location);

            return cloneObject;
        }

        /// <summary>
		/// 查询静默时间内是否可以销毁
		/// </summary>
		public bool CanAutoDestroy()
        {
            if (DontDestroy)
                return false;

            if (DestroyTime < 0)
                return false;

            if (_lastRestoreRealTime > 0 && SpawnCount <= 0)
                return (Time.realtimeSinceStartup - _lastRestoreRealTime) > DestroyTime;
            else
                return false;
        }

        /// <summary>
		/// 获取游戏对象
		/// </summary>
		public SpawnGameObject Spawn(bool forceClone, params System.Object[] userDatas)
        {
            SpawnGameObject spawn;

            // 如果资源还未加载完毕
            if (IsDone == false)
            {
                spawn = new SpawnGameObject(this, userDatas);
                _loadingSpawn.Add(spawn);
            }
            else
            {
                if (forceClone == false && _cache.Count > 0)
                {
                    GameObject go = _cache.Dequeue();
                    spawn = new SpawnGameObject(this, go, userDatas);
                    SetSpawnCloneObject(go);
                }
                else
                {
                    GameObject go = InstantiateGameObject();
                    spawn = new SpawnGameObject(this, go, userDatas);
                    SetSpawnCloneObject(go);
                }
                _usingSpawn.Add(spawn);
            }

            SpawnCount++;
            return spawn;
        }

        /// <summary>
		/// 销毁对象池
		/// </summary>
		public void Destroy()
        {
            // 卸载资源对象
            _handle.Release();

            // 销毁游戏对象
            foreach (var go in _cache)
            {
                if (go != null)
                    GameObject.Destroy(go);
            }
            _cache.Clear();

            // 清空加载列表
            _loadingSpawn.Clear();
            SpawnCount = 0;
        }


        /// <summary>
        /// 获取本对象池中所有正在使用的游戏对象（供外部调用的接口，并不会改变对象池实际列表）
        /// </summary>
        /// <returns></returns>
        public List<SpawnGameObject> GetSpawnGameObjects()
        {
            List<SpawnGameObject> result = new List<SpawnGameObject>();
            result.AddRange(_usingSpawn);
            return result;
        }



        // 回收游戏对象
        internal void Restore(SpawnGameObject spawn)
        {
            SpawnCount--;
            if (SpawnCount <= 0)
            {
                _lastRestoreRealTime = Time.realtimeSinceStartup;
            }

            // 从使用列表中移除
            if (_usingSpawn.Contains(spawn))
                _usingSpawn.Remove(spawn);

            // 注意：资源有可能还未加载完毕
            if (spawn.Go != null)
                RestoreGameObject(spawn.Go);
        }
        private void RestoreGameObject(GameObject go)
        {
            SetRestoreCloneObject(go);
            if (_cache.Count < MaxCapacity)
                _cache.Enqueue(go);
            else
                GameObject.Destroy(go);
        }

        // 丢弃游戏对象
        internal void Discard(SpawnGameObject spawn)
        {
            SpawnCount--;
            if (SpawnCount <= 0)
            {
                _lastRestoreRealTime = Time.realtimeSinceStartup;
            }

            // 注意：资源有可能还未加载完毕
            if (spawn.Go != null)
                DiscardGameObject(spawn.Go);
        }
        private void DiscardGameObject(GameObject go)
        {
            GameObject.Destroy(go);
        }

        private void SetSpawnCloneObject(GameObject cloneObj)
        {
            cloneObj.SetActive(true);
            cloneObj.transform.SetParent(null);
            cloneObj.transform.localPosition = Vector3.zero;
        }
        private void SetRestoreCloneObject(GameObject cloneObj)
        {
            cloneObj.SetActive(false);
            cloneObj.transform.SetParent(_root);
            cloneObj.transform.localPosition = Vector3.zero;
        }
    }
}
