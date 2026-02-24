//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using System;
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;
using YooAsset;
using ZEngine.Core;
using ZEngine.Manager.Resource;

namespace ZEngine.Manager.Scene
{
    /// <summary>
    /// 场景资源类
    /// </summary>
    public class AssetScene
    {
        private SceneHandle _handle;
        private Action<SceneHandle> _finishedCallback;
        private Action<int> _progressCallback;
        private LocalPhysicsMode _physicsMode;//场景物理模式
        private int _lastProgressValue = 0;

        /// <summary>
        /// 场景地址
        /// </summary>
        public string Location { private set; get; }

        /// <summary>
        /// 场景加载进度（0-100）
        /// </summary>
        public int Progress
        {
            get
            {
                if (_handle == null)
                    return 0;
                return (int)(_handle.Progress * 100f);
            }
        }

        /// <summary>
        /// 场景是否加载完毕
        /// </summary>
        public bool IsDone
        {
            get
            {
                if (_handle == null)
                    return false;
                return _handle.IsDone;
            }
        }

        public AssetScene(string location, LocalPhysicsMode physicsMode)
        {
            Location = location;
            _physicsMode = physicsMode;
        }

        /// <summary>
        /// 异步加载场景
        /// </summary>
        /// <param name="isAdditive">是否是附加场景</param>
        /// <param name="suspendLoad">场景加载到90%自动挂起</param>
        /// <param name="finishedCallback">场景加载完后的回调函数</param>
        /// <param name="progressCallback">进度回调</param>
        public async UniTask Load(bool isAdditive, bool suspendLoad, Action<SceneHandle> finishedCallback, Action<int> progressCallback)
        {
            if (_handle != null)
                return;

            var _sceneMode = isAdditive ? LoadSceneMode.Additive : LoadSceneMode.Single;

            ZEngineLog.Log($"开始加载场景: {Location}");
            _finishedCallback = finishedCallback;
            _progressCallback = progressCallback;
            _handle = await ResourceManager.Instance.LoadSceneAsync(Location, _sceneMode, _physicsMode, suspendLoad);
            _handle.Completed += Handle_Completed;
        }

        private void Handle_Completed(SceneHandle handle)
        {
            _finishedCallback?.Invoke(handle);
        }

        /// <summary>
        /// 卸载场景
        /// </summary>
        public void UnLoad()
        {
            if(_handle != null)
            {
                ZEngineLog.Log($"开始卸载场景: {Location}");
                _finishedCallback = null;
                _progressCallback = null;

                _handle.UnloadAsync();
                _handle = null;
            }
        }

        public void Update()
        {
            if(_handle != null)
            {
                if(_lastProgressValue != Progress)
                {
                    _lastProgressValue = Progress;
                    _progressCallback?.Invoke(_lastProgressValue);
                }
            }
        }
    }
}
