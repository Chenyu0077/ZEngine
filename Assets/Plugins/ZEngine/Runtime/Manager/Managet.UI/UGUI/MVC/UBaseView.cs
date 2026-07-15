using System;
using System.Collections.Generic;
using UnityEngine;
using YooAsset;
using ZEngine.Manager.Event;
using ZEngine.Manager.Pool;
using ZEngine.Manager.UI.UGUI;
using ZEngine.Manager.UI.UGUI.Components;

namespace ZEngine.Manager.UI
{
    /// <summary>
    /// UGUI View 基类（MonoBehaviour，挂载到 UI 面板预制体上）
    /// </summary>
    public abstract class UBaseView : MonoBehaviour, IUBaseData
    {
        /// <summary>
        /// 唯一 ID（GUID），同类型多实例时用于区分
        /// </summary>
        public string ID { get; internal set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// UI 面板所属层级（由 UIViewAttribute 初始化）
        /// </summary>
        public UUILayer LayerType { get; internal set; }

        /// <summary>
        /// 是否单例（同类型只允许一个实例）
        /// </summary>
        public bool IsSingleton { get; internal set; }

        /// <summary>
        /// 是否全屏窗口（用于全屏遮挡自动隐藏）
        /// </summary>
        public bool IsFullScreen { get; internal set; }

        /// <summary>
        /// 是否可移除（View 自管理生命周期，如 NPC 死亡时标记为 true）
        /// </summary>
        public bool CanRemoved { get; set; }

        /// <summary>
        /// 对象池句柄（异步加载路径使用，关闭时归还对象池）
        /// </summary>
        internal SpawnGameObject PoolHandle { get; set; }

        /// <summary>
        /// 同步加载的资源句柄（OpenViewSync 路径使用，关闭时 Release 归零引用计数，避免资源泄漏）
        /// </summary>
        internal AssetHandle SyncHandle { get; set; }

        /// <summary>
        /// 事件组（销毁时自动注销所有监听）
        /// </summary>
        protected EventGroup _eventGroup = new EventGroup();

        /// <summary>
        /// 数据模型
        /// </summary>
        private UBaseModel _data;
        public UBaseModel Data
        {
            get => _data;
            set
            {
                if (_data != value)
                {
                    _data = value;
                    OnChanged?.Invoke(_data);
                }
            }
        }
        public Action<UBaseModel> OnChanged { get; set; }


        /// <summary>
        /// Model 类型（子类覆盖指定具体 Model）
        /// </summary>
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

        /// <summary>
        /// Controller 类型（子类覆盖指定具体 Controller）
        /// </summary>
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


        // key = 相对于 View 根节点的路径（如 "Header/NameText"），value = 该节点上所有 Component
        private readonly Dictionary<string, Component[]> _childComponents = new Dictionary<string, Component[]>();

        // 子节点上所有 UIComponentBase 包装组件，BuildChildCache 时一次性收集，
        // 供 OnInit / OnRelease 批量通知，避免每次遍历全量 _childComponents。
        private readonly List<UIComponentBase> _uiComponents = new List<UIComponentBase>();

        /// <summary>
        /// 遍历所有子节点并缓存 Component，由框架在 Initialize() 前自动调用，无需手动调用。
        /// 依次：收集子节点组件 → 提取 UIComponentBase → [UIBind] 自动注入 → 批量调组件 OnInit。
        /// 使 Initialize / OnComplete 时绑定字段与组件均已就绪。公开以支持过程化构建与测试驱动；幂等可重复调用。
        /// </summary>
        public void BuildChildCache()
        {
            _childComponents.Clear();
            _uiComponents.Clear();
            CollectChildComponents(transform, string.Empty);
            GatherUIComponents();
            UIBinder.Bind(this);
            NotifyComponentsInit();
        }

        private void GatherUIComponents()
        {
            foreach (var comps in _childComponents.Values)
            {
                for (int i = 0; i < comps.Length; i++)
                {
                    if (comps[i] is UIComponentBase c)
                        _uiComponents.Add(c);
                }
            }
        }

        /// <summary>组件一次性初始化（绑定后、View.Initialize 前）。单个异常不影响其余组件。</summary>
        private void NotifyComponentsInit()
        {
            for (int i = 0; i < _uiComponents.Count; i++)
            {
                try { _uiComponents[i].OnInit(); }
                catch (Exception e) { Debug.LogException(e); }
            }
        }

        /// <summary>组件释放（View.OnRelease 中、清缓存前调用）。</summary>
        private void NotifyComponentsRelease()
        {
            for (int i = 0; i < _uiComponents.Count; i++)
            {
                try { _uiComponents[i].OnRelease(); }
                catch (Exception e) { Debug.LogException(e); }
            }
        }

        private void CollectChildComponents(Transform node, string parentPath)
        {
            for (int i = 0; i < node.childCount; i++)
            {
                Transform child = node.GetChild(i);
                string path = string.IsNullOrEmpty(parentPath) ? child.name : $"{parentPath}/{child.name}";
                if (!_childComponents.ContainsKey(path))
                    _childComponents[path] = child.GetComponents<Component>();
                CollectChildComponents(child, path);
            }
        }

        /// <summary>
        /// 按相对路径获取子节点上指定类型的组件（路径格式同 Transform.Find，如 "Header/NameText"）。
        /// 非泛型版本，供 UIBinder 反射注入使用；IsInstanceOfType 兼容父类查找。
        /// </summary>
        public Component GetChild(Type type, string relativePath)
        {
            if (_childComponents.TryGetValue(relativePath, out var comps))
            {
                for (int i = 0; i < comps.Length; i++)
                {
                    if (comps[i] != null && type.IsInstanceOfType(comps[i]))
                        return comps[i];
                }
            }
            return null;
        }

        /// <summary>
        /// 按相对路径获取子节点上指定类型的组件（路径格式同 Transform.Find，如 "Header/NameText"）
        /// </summary>
        public T GetChild<T>(string relativePath) where T : Component
        {
            return GetChild(typeof(T), relativePath) as T;
        }

        /// <summary>
        /// 按相对路径获取子节点的 GameObject
        /// </summary>
        public GameObject GetChildGO(string relativePath)
        {
            if (_childComponents.TryGetValue(relativePath, out var comps) && comps.Length > 0)
                return comps[0].gameObject;
            return null;
        }

        /// <summary>
        /// 初始化（资源实例化后、OnComplete 前调用，此时子节点缓存已就绪）
        /// </summary>
        public virtual void Initialize() { }

        /// <summary>
        /// 资源加载 / 绑定完成回调
        /// </summary>
        public virtual void OnComplete() { }

        /// <summary>
        /// 释放（销毁或归还对象池）
        /// </summary>
        public virtual void OnRelease()
        {
            _eventGroup.RemoveAllListener();
            OnChanged = null;
            _data = null;
            NotifyComponentsRelease();   // 先通知组件释放（清监听/句柄），再清缓存
            _uiComponents.Clear();
            _childComponents.Clear();

            if (PoolHandle != null)
            {
                PoolHandle.Restore();
                PoolHandle = null;
            }
            else
            {
                // 同步加载路径：View 即将销毁，释放资源句柄归零引用计数
                SyncHandle?.Release();
                SyncHandle = null;
                this.transform.SetParent(null);
                Destroy(this.gameObject);
            }
        }
    }
}
