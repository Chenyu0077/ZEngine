//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using YooAsset;
using ZEngine.Core;
using ZEngine.Manager.Resource;

namespace ZEngine.Manager.Scene
{
    public class SceneManager : ManagerSingleton<SceneManager>, IManager
    {
        private readonly List<AssetScene> _additionScenes = new List<AssetScene>();
        private AssetScene _mainScene;

        public void OnInit(object param)
        {
            //检测依赖模块
            if (ZEngineMain.Contains(typeof(ResourceManager)) == false)
                throw new Exception($"{nameof(SceneManager)}依赖于{nameof(ResourceManager)}");
            _root = new GameObject("[Z][SceneManager]");
            GameObject.DontDestroyOnLoad(_root);
        }

        public void OnUpdate()
        {
            if (_mainScene != null)
                _mainScene.Update();

            foreach(var additionScene in _additionScenes)
            {
                if (additionScene != null)
                    additionScene.Update();
            }
        }

        public void OnGUI()
        {
            
        }

        public void OnDestroy()
        {
            DestroySingleton();
        }

        /// <summary>
        /// 切换主场景，之前的主场景以及附加场景将会被卸载
        /// </summary>
        /// <param name="location">场景资源地址</param>
        /// <param name="physicsMode">场景物理模式</param>
        /// <param name="finishedCallback"></param>
        /// <param name="progressCallback"></param>
        public void ChangeMainScene(string location, bool suspendLoad = false, LocalPhysicsMode physicsMode = LocalPhysicsMode.None, Action<SceneHandle> finishedCallback = null, Action<int> progressCallback = null)
        {
            if (_mainScene != null && _mainScene.IsDone == false)
                ZEngineLog.Warning($"当前主场景{_mainScene.Location}还在加载!");

            _mainScene = new AssetScene(location, physicsMode);
            _mainScene.Load(false, suspendLoad, finishedCallback, progressCallback).Forget();
        }

        /// <summary>
        /// 在当前主场景上加载附加场景
        /// </summary>
        /// <param name="location">场景资源地址</param>
        /// <param name="suspendLoad">场景加载到90%自动挂起</param>
        /// <param name="physicsMode">场景物理模式</param>
        /// <param name="finishedCallback">场景加载完成后的回调函数</param>
        /// <param name="progressCallback"></param>
        public void LoadAdditionScene(string location, bool suspendLoad, LocalPhysicsMode physicsMode = LocalPhysicsMode.None, Action<SceneHandle> finishedCallback = null, Action<int> progressCallback = null)
        {
            AssetScene scene = TryGetAdditionScene(location);
            if(scene != null)
            {
                ZEngineLog.Warning("这个附加场景{location}已经被加载了!");
                return;
            }

            AssetScene newScene = new AssetScene(location, physicsMode);
            _additionScenes.Add(newScene);
            newScene.Load(true, suspendLoad, finishedCallback, progressCallback).Forget();
        }

        /// <summary>
        /// 卸载当前主场景的附加场景
        /// </summary>
        /// <param name="location">场景资源地址</param>
        public void UnLoadAdditionScene(string location)
        {
            for(int i=_additionScenes.Count - 1; i >= 0; i--)
            {
                if (_additionScenes[i].Location == location)
                {
                    _additionScenes[i].UnLoad();
                    _additionScenes.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// 获取场景当前的加载进度， 如果场景不存在返回0
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        public int GetSceneLoadProgress(string location)
        {
            if(_mainScene != null)
            {
                if (_mainScene.Location == location)
                    return _mainScene.Progress;
            }

            AssetScene scene = TryGetAdditionScene(location);
            if (scene != null)
                return scene.Progress;

            ZEngineLog.Warning($"未发现场景{location}");
            return 0;
        }

        /// <summary>
        /// 检测场景是否加载完毕，如果场景不存在返回false
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        public bool CheckSceneIsDone(string location)
        {
            if(_mainScene != null)
            {
                if (_mainScene.Location == location)
                    return _mainScene.IsDone;
            }

            AssetScene scene = TryGetAdditionScene(location);
            if (scene != null)
                return scene.IsDone;

            ZEngineLog.Warning($"未发现场景{location}");
            return false;
        }

        public void ActivateScene()
        {
            
        }

        #region Private Function
        /// <summary>
        /// 尝试获取一个附加场景， 如果不存在返回null
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        private AssetScene TryGetAdditionScene(string location)
        {
            for(int i = 0; i < _additionScenes.Count; i++)
            {
                if (_additionScenes[i].Location == location)
                    return _additionScenes[i];
            }
            return null;
        }
        #endregion
    }
}
