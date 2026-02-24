//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using System;
using ZEngine.Core;
using YooAsset;
using UnityEngine.SceneManagement;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace ZEngine.Manager.Resource
{
    public class ResourceManager : ManagerSingleton<ResourceManager>, IManager
    {
        private InitializeParameters _initParameters;
        private ResourcePackage _defaultPackage;
        private string _locationRoot;

        public void OnInit(object param)
        {
            _initParameters = param as InitializeParameters;
            if (_initParameters == null)
                throw new Exception($"{nameof(ResourceManager)}模块创建时缺少参数");
            _root = new GameObject("[Z][ResourceManager]");
            GameObject.DontDestroyOnLoad(_root);
        }

        public void OnUpdate()
        {
            
        }

        public void OnGUI()
        {
            
        }

        public void OnDestroy()
        {
            ForceUnloadAllAssets().Forget();
            DestroySingleton();
        }

        /// <summary>
        /// 异步初始化
        /// </summary>
        /// <param name="locationRoot"></param>
        /// <param name="packageName"></param>
        /// <returns></returns>
        public InitializationOperation InitializeAsync(out ResourcePackage defaultPackage, string locationRoot, string packageName = "DefaultPackage")
        {
            _locationRoot = locationRoot;
            _defaultPackage = YooAssets.CreatePackage(packageName);
            YooAssets.SetDefaultPackage(_defaultPackage);
            defaultPackage = _defaultPackage;
            return _defaultPackage.InitializeAsync(_initParameters);
        }


        #region 资源下载
        /// <summary>
        /// 获取默认资源包版本号
        /// </summary>
        /// <returns></returns>
        public async UniTask<string> RequestPackageVersion()
        {
            if(_defaultPackage == null)
                _defaultPackage = YooAssets.GetPackage("DefaultPackage");

            var operation = _defaultPackage.RequestPackageVersionAsync();
            await operation.ToUniTask();

            if (operation.Status == EOperationStatus.Succeed)
            {
                Debug.Log($"请求资源包版本号: {operation.PackageVersion}");
                return operation.PackageVersion;
            }
            else
            {
                Debug.LogError(operation.Error);
                return null;
            }
        }

        /// <summary>
        /// 更新资源（补丁）清单
        /// </summary>
        /// <param name="packageVersion"></param>
        /// <returns></returns>
        public async UniTask UpdatePackageManifestAsync(string packageVersion)
        {
            if (_defaultPackage == null)
                _defaultPackage = YooAssets.GetPackage("DefaultPackage");

            var opearation = _defaultPackage.UpdatePackageManifestAsync(packageVersion);
            await opearation.ToUniTask();

            if(opearation.Status == EOperationStatus.Succeed)
            {
                Debug.Log("更新清单成功!");
            }
            else
            {
                Debug.LogError(opearation.Error);
            }
        }

        /// <summary>
        /// 下载资源文件
        /// </summary>
        /// <returns></returns>
        public async UniTask DownLoadPackageFiles()
        {
            //创建资源下载器
            Debug.Log("创建资源下载器!");
            int downloadingMaxNum = 10;
            int failedTryAgain = 3;
            var downloader = _defaultPackage.CreateResourceDownloader(downloadingMaxNum, failedTryAgain);

            if(downloader.TotalDownloadCount == 0)
            {
                Debug.Log("没有需要下载的资源文件");
                return;
            }

            //需要下载的文件总数和总大小
            int totalDownloadCount = downloader.TotalDownloadCount;
            long totalDownloadBytes = downloader.TotalDownloadBytes;

            //注册回调方法
            downloader.DownloadErrorCallback = OnDownloadErrorFunction;
            downloader.DownloadUpdateCallback = OnDownloadProgressUpdateFunction;
            downloader.DownloadFinishCallback = OnDownloadOverFunction;
            downloader.DownloadFileBeginCallback = OnStartDownloadFileFunction;

            //开启下载
            downloader.BeginDownload();
            await downloader;

            //检测下载结果
            if (downloader.Status == EOperationStatus.Succeed)
            {
                //下载成功
                Debug.Log("更新完成");
            }
            else
            {
                Debug.LogError(downloader.Error);
            }
        }

        #region 下载注册的回调方法
        /// <summary>
        /// 开始下载
        /// </summary>
        private void OnStartDownloadFileFunction(DownloadFileData downloadFileData)
        {
            Debug.Log($"开始下载：文件名：{downloadFileData.FileName}，文件大小：{downloadFileData.FileSize}");
        }

        /// <summary>
        /// 下载完成
        /// </summary>
        private void OnDownloadOverFunction(DownloaderFinishData downloaderFinishData)
        {
            Debug.Log("下载" + (downloaderFinishData.Succeed ? "成功" : "失败"));
        }

        /// <summary>
        /// 更新中
        /// </summary>
        private void OnDownloadProgressUpdateFunction(DownloadUpdateData downloadUpdateData)
        {
            Debug.Log($"文件总数：{downloadUpdateData.TotalDownloadCount}，已下载文件数：{downloadUpdateData.CurrentDownloadCount}，下载总大小：{downloadUpdateData.TotalDownloadBytes}，已下载大小{downloadUpdateData.CurrentDownloadBytes}");
        }

        /// <summary>
        /// 下载出错
        /// </summary>
        /// <param name="errorData"></param>
        private void OnDownloadErrorFunction(DownloadErrorData errorData)
        {
            Debug.LogError($"下载出错：包名:{errorData.PackageName} 文件名：{errorData.FileName}，错误信息：{errorData.ErrorInfo}");
        }
        #endregion

        /// <summary>
        /// 是否需要从远端下载
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        public bool IsNeedDownLoadFromRemote(string location)
        {
            return YooAssets.IsNeedDownloadFromRemote(location);
        }

        /// <summary>
        /// 获取资源对象信息列表
        /// </summary>
        /// <param name="tags"></param>
        /// <returns></returns>
        public AssetInfo[] GetBoundleInfos(string[] tags)
        {
            return YooAssets.GetAssetInfos(tags);
        }
        #endregion

        /// <summary>
		/// 资源回收（卸载引用计数为零的资源）
		/// </summary>
		public async UniTask UnloadUnusedAssets()
        {
            await _defaultPackage.UnloadUnusedAssetsAsync();
        }

        /// <summary>
		/// 强制回收所有资源
		/// </summary>
		public async UniTask ForceUnloadAllAssets()
        {
            await _defaultPackage.UnloadAllAssetsAsync();
        }

        /// <summary>
		/// 释放资源对象
		/// </summary>
		public void Release(AssetHandle handle)
        {
            handle.Dispose();
        }

        #region 场景加载
        /// <summary>
        /// 异步加载场景
        /// </summary>
        /// <param name="location">场景的定位地址</param>
        /// <param name="sceneMode">场景加载模式</param>
        /// <param name="physicsMode">场景物理模式</param>
        /// <param name="suspendLoad">场景加载到90%自动挂起</param>
        /// <param name="priority">加载的优先级</param>
        public async UniTask<SceneHandle> LoadSceneAsync(string location, LoadSceneMode sceneMode = LoadSceneMode.Single, LocalPhysicsMode physicsMode = LocalPhysicsMode.None, bool suspendLoad = true, uint priority = 0)
        {
            location = _locationRoot + location;
            var handle = _defaultPackage.LoadSceneAsync(location, sceneMode, physicsMode, suspendLoad, priority);
            await handle.ToUniTask();
            return handle;
        }

        /// <summary>
        /// 同步加载场景
        /// </summary>
        /// <param name="location">场景的定位地址</param>
        /// <param name="sceneMode">场景加载模式</param>
        /// <param name="physicsMode">场景物理模式</param>
        /// <returns></returns>
        public SceneHandle LoadSceneSync(string location, LoadSceneMode sceneMode = LoadSceneMode.Single, LocalPhysicsMode physicsMode = LocalPhysicsMode.None)
        {
            var handle = _defaultPackage.LoadSceneSync(location, sceneMode, physicsMode);
            return handle;
        }
        #endregion

        #region 资源加载
        /// <summary>
		/// 异步加载资源对象
		/// </summary>
		/// <param name="location">资源对象相对路径</param>
		public async UniTask<AssetHandle> LoadAssetAsync<T>(string location) where T : UnityEngine.Object
        {
            location = _locationRoot + location;
            var handle = YooAssets.LoadAssetAsync<T>(location);
            await handle.ToUniTask();
            return handle;
        }
        public async UniTask<AssetHandle> LoadAssetAsync(System.Type type, string location)
        {
            location = _locationRoot + location;
            var handle = YooAssets.LoadAssetAsync(location, type);
            await handle.ToUniTask();
            return handle;
        }

        /// <summary>
		/// 同步加载资源对象
		/// </summary>
		/// <param name="location">资源对象相对路径</param>
		public AssetHandle LoadAssetSync<T>(string location) where T : UnityEngine.Object
        {
            location = _locationRoot + location;
            return YooAssets.LoadAssetSync<T>(location);
        }
        public AssetHandle LoadAssetSync(System.Type type, string location)
        {
            location = _locationRoot + location;
            return YooAssets.LoadAssetSync(location, type);
        }

        /// <summary>
		/// 异步加载子资源对象集合
		/// </summary>
		/// <param name="location">资源对象相对路径</param>
		public async UniTask<SubAssetsHandle> LoadSubAssetsAsync<T>(string location) where T : UnityEngine.Object
        {
            location = _locationRoot + location;
            var handle = YooAssets.LoadSubAssetsAsync<T>(location);
            await handle.ToUniTask();
            return handle;
        }
        public async UniTask<SubAssetsHandle> LoadSubAssetsAsync(System.Type type, string location)
        {
            location = _locationRoot + location;
            var handle = YooAssets.LoadSubAssetsAsync(location, type);
            await handle.ToUniTask();
            return handle;
        }

        /// <summary>
		/// 同步加载子资源对象集合
		/// </summary>
		/// <param name="location">资源对象相对路径</param>
		public SubAssetsHandle LoadSubAssetsSync<T>(string location) where T : UnityEngine.Object
        {
            location = _locationRoot + location;
            return YooAssets.LoadSubAssetsSync<T>(location);
        }
        public SubAssetsHandle LoadSubAssetsSync(System.Type type, string location)
        {
            location = _locationRoot + location;
            return YooAssets.LoadSubAssetsSync(location, type);
        }
        #endregion
    }
}
