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
                    OnChanged?.Invoke(_data);
                }
            }
        }
        public Action<BaseModel> OnChanged {  get; set; }

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
        private string _path = GameAssetPaths.FGUIPath;                 //UI资源路径
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
            _view?.parent?.RemoveChild(_view, true);     
            _view?.Dispose();
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


        #region 设置View位置

        /// <summary>
        /// 设置面板到屏幕的指定角落
        /// </summary>
        /// <param name="corner">角落位置</param>
        public void SetToCorner(ScreenCorner corner, float marginX, float marginY)
        {                                                                                                                                      
             if (_view == null) return;                                                                                                                                                                                                                                         
                                                                                                                                                                                                                                        
             float screenWidth = GRoot.inst.width;                                                                                                                                                                             
             float screenHeight = GRoot.inst.height;                                                                                                                                                                                               
             float halfViewWidth = _view.width / 2;                                                                                                                                                                                                                             
             float halfViewHeight = _view.height / 2;                                                                                                                                                                                                       
                                                                                                                                                                                                                                                          
             float posX, posY;                                                                                                                                                                                                                                                  
                                                                                                                                                                                                                                                                                
             switch (corner)                                                                                                                                                                                                                                                    
             {                                                                                                                                                                                                                                                                  
                 case ScreenCorner.TopLeft:                                                                                                                                                                                                                                     
                     posX = halfViewWidth + marginX;                                                                                                                                                                                                         
                     posY = halfViewHeight + marginY;                                                                                                                                                                                                       
                     break;                                                                                                                                                                                                                                                     
                 case ScreenCorner.TopRight:                                                                                                                                                                                                                                    
                     posX = screenWidth - halfViewWidth - marginX;                                                                                                                                                                                                          
                     posY = halfViewHeight + marginY;                                                                                                                                                                                                       
                     break;                                                                                                                                                                                                                                                     
                 case ScreenCorner.BottomLeft:                                                                                                                                                                                                                                  
                     posX = halfViewWidth + marginX;                                                                                                                                                                                                         
                     posY = screenHeight - halfViewHeight - marginY;                                                                                                                                                                                                        
                     break;                                                                                                                                                                                                                                                     
                 case ScreenCorner.BottomRight:                                                                                                                                                                                                                                 
                     posX = screenWidth - halfViewWidth - marginX;                                                                                                                                                                                                          
                     posY = screenHeight - halfViewHeight - marginY;         
                     break;                                                                                                                                                                                                                                                     
                 default:                                                                                                                                                                                                                                                       
                     posX = 0;                                                                                                                                                                                                                                                  
                     posY = 0;                                                                                                                                                                                                                                                  
                     break;                                                                                                                                                                                                                                                     
             }                                                                                                                                                                                                                                                                  
                                                                                                                                                                                                                                                                                
             _view.SetXY(posX, posY);                                                                                                                                                                                                     
        }    

        public enum ScreenCorner
        {
            TopLeft,
            TopRight,
            BottomLeft,
            BottomRight
        }

        /// <summary>
        /// 设置为屏幕中心
        /// </summary>
        public void SetToCenter()
        {
            if (_view == null) return;
            _view.SetXY(GRoot.inst.width / 2, GRoot.inst.height / 2);
        }

        public enum ScreenEdge
        {
            TopCenter,
            BottomCenter,
            LeftCenter,
            RightCenter,
        }

        /// <summary>
        /// 将面板对齐到屏幕上/下/左/右的中间位置，offset 为距边缘的偏移距离（正值向内）
        /// </summary>
        public void SetToEdgeCenter(ScreenEdge edge, float offset = 0f)
        {
            if (_view == null) return;

            float sw = GRoot.inst.width;
            float sh = GRoot.inst.height;
            float hw = _view.width  * 0.5f;
            float hh = _view.height * 0.5f;

            float x, y;
            switch (edge)
            {
                case ScreenEdge.TopCenter:
                    x = sw * 0.5f;
                    y = offset;
                    break;
                case ScreenEdge.BottomCenter:
                    x = sw * 0.5f;
                    y = sh - offset;
                    break;
                case ScreenEdge.LeftCenter:
                    x = offset;
                    y = sh * 0.5f;
                    break;
                case ScreenEdge.RightCenter:
                    x = sw - offset;
                    y = sh * 0.5f;
                    break;
                default:
                    x = sw * 0.5f;
                    y = sh * 0.5f;
                    break;
            }

            _view.SetXY(x, y);
        }

        /// <summary>
        /// 计算UI位置
        /// </summary>
        /// <param name="corner"></param>
        /// <param name="marginX"></param>
        /// <param name="marginY"></param>
        /// <returns></returns>
        public Vector2 GetPosition(ScreenCorner corner, float marginX, float marginY)
        {
            if (_view == null) return Vector2.zero;                                                                                                                                                                                                                                         
                                                                                                                                                                                                                                        
             float screenWidth = GRoot.inst.width;                                                                                                                                                                             
             float screenHeight = GRoot.inst.height;                                                                                                                                                                                               
             float halfViewWidth = _view.width / 2;                                                                                                                                                                                                                             
             float halfViewHeight = _view.height / 2;                                                                                                                                                                                                       
                                                                                                                                                                                                                                                          
             float posX, posY;                                                                                                                                                                                                                                                  
                                                                                                                                                                                                                                                                                
             switch (corner)                                                                                                                                                                                                                                                    
             {                                                                                                                                                                                                                                                                  
                 case ScreenCorner.TopLeft:                                                                                                                                                                                                                                     
                     posX = halfViewWidth + marginX;                                                                                                                                                                                                         
                     posY = halfViewHeight + marginY;                                                                                                                                                                                                       
                     break;                                                                                                                                                                                                                                                     
                 case ScreenCorner.TopRight:                                                                                                                                                                                                                                    
                     posX = screenWidth - halfViewWidth - marginX;                                                                                                                                                                                                          
                     posY = halfViewHeight + marginY;                                                                                                                                                                                                       
                     break;                                                                                                                                                                                                                                                     
                 case ScreenCorner.BottomLeft:                                                                                                                                                                                                                                  
                     posX = halfViewWidth + marginX;                                                                                                                                                                                                         
                     posY = screenHeight - halfViewHeight - marginY;                                                                                                                                                                                                        
                     break;                                                                                                                                                                                                                                                     
                 case ScreenCorner.BottomRight:                                                                                                                                                                                                                                 
                     posX = screenWidth - halfViewWidth - marginX;                                                                                                                                                                                                          
                     posY = screenHeight - halfViewHeight - marginY;         
                     break;                                                                                                                                                                                                                                                     
                 default:                                                                                                                                                                                                                                                       
                     posX = 0;                                                                                                                                                                                                                                                  
                     posY = 0;                                                                                                                                                                                                                                                  
                     break;                                                                                                                                                                                                                                                     
             }           
             return new Vector2(posX, posY);
        }
        #endregion
    }
}
