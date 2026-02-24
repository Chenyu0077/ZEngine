//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using System;
using System.Collections.Generic;
using FairyGUI;
using ZEngine.Manager.Event;
using YooAsset;
using ZEngine.Config;
using UnityEngine;
using Cysharp.Threading.Tasks;
using ZEngine.Manager.Resource;

namespace ZEngine.Manager.UI
{
    public abstract class BaseView : IBaseData
    {
        /// <summary>
        /// 窗口ID，唯一标识，用于同类型UI多实例管理
        /// </summary>
        public string ID { get; protected set; } = Guid.NewGuid().ToString();
        /// <summary>
        /// UI面板
        /// </summary>
        protected GComponent _view;
        /// <summary>
        /// 事件组
        /// </summary>
        protected EventGroup _eventGroup = new EventGroup();
        /// <summary>
        /// UI面板所属层类型
        /// </summary>
        public virtual EUILayer LayerType { get; protected set; } = EUILayer.Bottom_Layer;
        /// <summary>
        /// UI面板对应层级index
        /// </summary>
        public int LayerOrder;
        /// <summary>
        /// 是否是单例（即同类型UI只允许存在一个实例）
        /// </summary>
        public virtual bool IsSingleton { get; protected set; } = true;
        /// <summary>
        /// 能否被移除（由UI内部逻辑决定是否需要关闭UI）
        /// </summary>
        public bool CanRemoved { get; set; } = false;
        /// <summary>
        /// UI数据
        /// </summary>
        private BaseModel _data;
        public BaseModel Data
        {
            get
            {
                return _data;
            }
            set
            {
                if(_data != value)
                {
                    _data = value;
                    OnChanged?.Invoke();
                }
            }
        }
        public Action OnChanged {  get; set; }

        private Type _modelType;
        public virtual Type ModelType
        {
            get
            {
                if(_modelType == null)
                {
                    _modelType = typeof(BaseModel);
                }
                return _modelType;
            }
            protected set
            {
                _modelType = value;
            }
        }

        private Type _controllerType;
        public virtual Type ControllerType
        {
            get
            {
                if (_controllerType == null)
                {
                    _controllerType = typeof(BaseController);
                }
                return _controllerType;
            }
            protected set
            {
                _controllerType = value;
            }
        }

        #region 资源相关
        // 资源句柄列表
        private List<AssetHandle> _handles = new List<AssetHandle>(100);//依赖资源
        private AssetHandle _handle;                                    //自身资源
        protected string _pkgName;                                      //UI对应包名
        protected string _resName;                                      //UI对应组件名
        private string _path = GameAssetPaths.UIPath;          //UI资源路径
        #endregion


        public GComponent GetView()
        {
            return _view;
        }

        public virtual void Initialize() 
        { 
            //初始化包名等
        }

        public virtual void OnComplete()
        {
            //view加载完成后调用
        }

        public virtual void OnRelease()
        {
            // 卸载自身资源包 卸载依赖资源包
            _handle?.Release();
            _handles?.ForEach(handle => { handle.Release(); });
            _handles?.Clear();

            // 移除所有缓存的事件监听
            _eventGroup.RemoveAllListener();
            _view.parent.RemoveChild(_view, true);
            _view.Dispose();
        }


        #region 资源加载
        /// <summary>
        /// 执行窗口的同步加载逻辑，加载完成时调用 Handle_Completed
        /// </summary>
        internal void InternalLoadSync()
        {
            //获取依赖包 并加载
            List<string> dependencies = PackageUtils.GetDependencies(_pkgName);
            if (dependencies != null)
            {
                foreach (var item in dependencies)
                {
                    UIPackage.AddPackage(item, DependencyLoadFunc);
                }
            }
            //加载正式包
            var alreadyLoaded = UIPackage.GetPackages().Exists(p => p.assetPath == _pkgName);
            if (!alreadyLoaded)
            {
                UIPackage package = UIPackage.AddPackage(_pkgName, LoadFunc);
                _handle.Completed += Handle_Completed;
            }
            else
            {
                string name = _pkgName + "_fui";
                string extension = ".bytes";
                string location = $"{_path}{name}{extension}";
                var package = YooAssets.GetPackage("DefaultPackage");
                _handle = ResourceManager.Instance.LoadAssetSync(typeof(TextAsset), location);
                _handle.Completed += Handle_Completed;
            }
        }

        /// <summary>
        /// 执行窗口的异步加载逻辑，加载完成时调用 Handle_Completed
        /// </summary>
        internal async UniTask InternalLoadAsync(Action onLoaded = null)
        {
            //获取依赖包 并加载
            List<string> dependencies = PackageUtils.GetDependencies(_pkgName);
            if (dependencies != null)
            {
                foreach (var item in dependencies)
                {
                    UIPackage.AddPackage(item, DependencyLoadFunc);
                }
            }
            //加载正式包
            var alreadyLoaded = UIPackage.GetPackages().Exists(p => p.assetPath == _pkgName);
            if (!alreadyLoaded)
            {
                UIPackage package = UIPackage.AddPackage(_pkgName, LoadFunc);
                await _handle.ToUniTask();
                _handle.Completed += Handle_Completed;
                onLoaded?.Invoke();
            }
            else
            {
                string name = _pkgName + "_fui";
                string extension = ".bytes";
                string location = $"{_path}{name}{extension}";
                var package = YooAssets.GetPackage("DefaultPackage");
                _handle = ResourceManager.Instance.LoadAssetSync(typeof(TextAsset), location);
                await _handle.ToUniTask();
                _handle.Completed += Handle_Completed;
            }            
        }


        //=========================================资源加载相关=========================================
        private object DependencyLoadFunc(string name, string extension, System.Type type, out DestroyMethod method)
        {
            method = DestroyMethod.None; //注意：这里一定要设置为None
            string location = $"{_path}{name}{extension}";
            var package = YooAssets.GetPackage("DefaultPackage");
            var handle = ResourceManager.Instance.LoadAssetSync(type, location);
            _handles.Add(handle);
            return handle.AssetObject;
        }

        private object LoadFunc(string name, string extension, System.Type type, out DestroyMethod method)
        {
            method = DestroyMethod.None; //注意：这里一定要设置为None
            string location = $"{_path}{name}{extension}";
            var package = YooAssets.GetPackage("DefaultPackage");
            _handle = ResourceManager.Instance.LoadAssetSync(type, location);
            return _handle.AssetObject;
        }

        private void Handle_Completed(AssetHandle obj)
        {
            if (_handle.AssetObject == null)
                return;

            //实例化对象
            _view = UIPackage.CreateObject(_pkgName, _resName).asCom;

            OnComplete();
        }
        #endregion
    }
}
