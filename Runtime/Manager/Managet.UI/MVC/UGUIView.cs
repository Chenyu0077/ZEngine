//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using YooAsset;
using ZEngine.Config;
using ZEngine.Manager.Event;

namespace ZEngine.Manager.UI
{
    /// <summary>
    /// 只负责 UGUI 的 View，走 MonoBehaviour
    /// </summary>
    public abstract class UGUIView : MonoBehaviour, IBaseData
    {
        /// <summary>
        /// 唯一 ID，便于和当前框架保持一致
        /// </summary>
        public string ID { get; protected set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 是否单例（同类型 UI 只允许一个实例）
        /// </summary>
        public virtual bool IsSingleton { get; protected set; } = true;

        /// <summary>
        /// 是否可移除（比如世界 UI 自己判断角色死亡）
        /// </summary>
        public bool CanRemoved { get; set; }

        /// <summary>
        /// 事件组
        /// </summary>
        protected EventGroup _eventGroup = new EventGroup();

        /// <summary>
        /// 数据模型
        /// </summary>
        private BaseModel _data;
        public BaseModel Data
        {
            get => _data;
            set
            {
                if (_data != value)
                {
                    _data = value;
                    OnChanged?.Invoke();
                }
            }
        }
        public Action OnChanged { get; set; }

        private Type _modelType;
        public virtual Type ModelType
        {
            get
            {
                if (_modelType == null)
                    _modelType = typeof(BaseModel);
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
                    _controllerType = typeof(BaseController);
                return _controllerType;
            }
            protected set
            {
                _controllerType = value;
            }
        }

        /// <summary>
        /// 根 Transform（方便 Manager 做层级管理）
        /// </summary>
        public Transform RootTransform => this.transform;

        #region 资源相关
        // 资源句柄列表
        private AssetHandle _handle;                            //自身资源
        private string _path = GameAssetPaths.UIPath;           //UI资源路径
        private string _assetName = string.Empty;               //资源名称
        #endregion

        /// <summary>
        /// 初始化
        /// </summary>
        public virtual void Initialize() { }

        /// <summary>
        /// 资源加载/绑定完成
        /// </summary>
        public virtual void OnComplete() { }

        /// <summary>
        /// 释放（销毁 GameObject + 事件解绑等）
        /// </summary>
        public virtual void OnRelease()
        {
            _handle?.Release();
            _eventGroup.RemoveAllListener();
            this.transform.SetParent(null);
            Destroy(this.gameObject);
        }
    }
}
