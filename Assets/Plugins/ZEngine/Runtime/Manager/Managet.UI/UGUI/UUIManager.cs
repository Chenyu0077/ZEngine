using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;
using ZEngine.Core;
using ZEngine.Manager.Log;
using ZEngine.Manager.Pool;
using ZEngine.Manager.Resource;
using ZEngine.Reference;

namespace ZEngine.Manager.UI.UGUI
{
    /// <summary>
    /// UGUI 管理器
    /// </summary>
    public class UUIManager : ManagerSingleton<UUIManager>, IManager
    {
        // 用 List 保证插入顺序，末尾元素 = 最高 sibling index = 最顶层
        private Dictionary<UUILayer, List<UBaseView>> _viewDic = new Dictionary<UUILayer, List<UBaseView>>();
        private Dictionary<string, UBaseController> _controllerDic = new Dictionary<string, UBaseController>();

        private List<UUILayer> _layerTypes;
        private readonly List<string> _removedViewIds = new List<string>();
        private readonly List<KeyValuePair<string, UBaseController>> _tempControllers = new List<KeyValuePair<string, UBaseController>>();

        public void OnInit(object param)
        {
            _root = new GameObject("[Z][UUIManager]");
            GameObject.DontDestroyOnLoad(_root);
            UILayer.Initialize(_root);

            _layerTypes = new List<UUILayer>();
            foreach (UUILayer layer in Enum.GetValues(typeof(UUILayer)))
            {
                _layerTypes.Add(layer);
                _viewDic.Add(layer, new List<UBaseView>());
            }
        }

        public void OnUpdate()
        {
            // Controller Update
            _tempControllers.Clear();
            foreach (var kvp in _controllerDic)
                _tempControllers.Add(kvp);

            for (int i = 0; i < _tempControllers.Count; i++)
                _tempControllers[i].Value?.OnUpdate();

            // CanRemoved 自动关闭检测
            for (int i = 0; i < _layerTypes.Count; i++)
            {
                var list = _viewDic[_layerTypes[i]];
                for (int j = 0; j < list.Count; j++)
                {
                    var view = list[j];
                    if (view != null && view.CanRemoved)
                        _removedViewIds.Add(view.ID);
                }
            }

            for (int i = 0; i < _removedViewIds.Count; i++)
                CloseView(_removedViewIds[i], null);
            _removedViewIds.Clear();
        }

        public void OnGUI() { }

        public void OnDestroy()
        {
            CloseAll();
            DestroySingleton();
        }

        #region 公共方法

        /// <summary>
        /// 获取对应层级的父容器
        /// </summary>
        public GameObject GetLayer(UUILayer layer)
        {
            if (UILayer.LayerDic.TryGetValue(layer, out var obj))
                return obj;
            throw new Exception($"未找到层级为[{layer}]的父容器");
        }

        /// <summary>
        /// 同步打开 View
        /// </summary>
        public T OpenViewSync<T>(UBaseModel model = null, Action<UBaseView> onViewOpened = null) where T : UBaseView
        {
            Type type = typeof(T);
            var attr = GetViewAttribute(type);

            if (attr.IsSingleton)
            {
                var existed = FindViewByType(type, attr.Layer);
                if (existed != null)
                {
                    BringToFront(existed);
                    onViewOpened?.Invoke(existed);
                    return existed as T;
                }
            }

            var handle = ResourceManager.Instance.LoadAssetSync<GameObject>(attr.Location);
            if (handle.AssetObject == null)
            {
                // 加载失败也要释放句柄，避免引用计数残留
                handle.Release();
                LogManager.Instance.Error($"无法加载 View [{type.Name}]，路径: {attr.Location}");
                return null;
            }

            var obj = GameObject.Instantiate(handle.AssetObject as GameObject);
            var view = obj.GetComponent<T>();
            if (view == null)
                view = obj.AddComponent<T>();

            // 同步路径持有句柄，View 关闭时由 OnRelease 释放（归零引用计数）
            view.SyncHandle = handle;
            InitializeView(view, attr, model, onViewOpened);
            return view;
        }

        /// <summary>
        /// 异步打开 View（回调模式，使用对象池）
        /// </summary>
        public void OpenViewAsync<T>(UBaseModel model = null, Action<UBaseView> onViewOpened = null) where T : UBaseView
        {
            Type type = typeof(T);
            var attr = GetViewAttribute(type);

            if (attr.IsSingleton)
            {
                var existed = FindViewByType(type, attr.Layer);
                if (existed != null)
                {
                    BringToFront(existed);
                    onViewOpened?.Invoke(existed);
                    return;
                }
            }

            var spawnObj = ObjectPoolManager.Instance.Spawn(attr.Location);
            spawnObj.Completed += (spawn) =>
            {
                if (spawn.Go == null)
                {
                    LogManager.Instance.Error($"无法加载 View [{type.Name}]，路径: {attr.Location}");
                    onViewOpened?.Invoke(null); // 加载失败必须回调，否则 OpenViewAsyncAwait 的 tcs 永远不 resolve，导致 await 死锁
                    return;
                }

                // 异步加载期间的二次单例检查
                if (attr.IsSingleton)
                {
                    var existed = FindViewByType(type, attr.Layer);
                    if (existed != null)
                    {
                        spawn.Restore();
                        BringToFront(existed);
                        onViewOpened?.Invoke(existed);
                        return;
                    }
                }

                var view = spawn.Go.GetComponent<T>();
                if (view == null)
                    view = spawn.Go.AddComponent<T>();

                view.PoolHandle = spawn;
                InitializeView(view, attr, model, onViewOpened);
            };
        }

        /// <summary>
        /// 关闭指定类型的 View
        /// </summary>
        public void CloseView<T>(Action onViewClosed = null) where T : UBaseView
        {
            Type type = typeof(T);
            for (int i = 0; i < _layerTypes.Count; i++)
            {
                var layer = _layerTypes[i];
                var list = _viewDic[layer];
                UBaseView target = null;
                for (int j = list.Count - 1; j >= 0; j--)
                {
                    var view = list[j];
                    if (view != null && view.GetType() == type)
                    {
                        target = view;
                        break; // 找到第一个即停，避免意外关闭错误实例
                    }
                }

                if (target != null)
                {
                    InternalCloseView(layer, target, onViewClosed);
                    return;
                }
            }
        }

        /// <summary>
        /// 根据 View ID 关闭
        /// </summary>
        public void CloseView(string viewID, Action onViewClosed = null)
        {
            if (string.IsNullOrEmpty(viewID))
                return;

            for (int i = 0; i < _layerTypes.Count; i++)
            {
                var layer = _layerTypes[i];
                var list = _viewDic[layer];
                for (int j = 0; j < list.Count; j++)
                {
                    var view = list[j];
                    if (view != null && view.ID == viewID)
                    {
                        InternalCloseView(layer, view, onViewClosed);
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// 关闭所有窗口
        /// </summary>
        public void CloseAll()
        {
            for (int i = 0; i < _layerTypes.Count; i++)
            {
                var layer = _layerTypes[i];
                var list = _viewDic[layer];
                for (int j = 0; j < list.Count; j++)
                {
                    var view = list[j];
                    if (view == null)
                        continue;
                    if (_controllerDic.TryGetValue(view.ID, out var controller))
                    {
                        ReferencePool.Release(controller);
                        if (view.Data != null)
                            ReferencePool.Release(view.Data);
                    }
                    view.OnRelease();
                }
                // 移除被外部 Destroy 的空位，保持列表干净
                list.RemoveAll(static v => v == null);
            }
            _controllerDic.Clear();
            // CloseAll 后刷新可见性，保证状态干净
            RefreshVisibility();
        }

        /// <summary>
        /// 查询窗口是否存在
        /// </summary>
        public bool HasView<T>() where T : UBaseView
        {
            return FindViewByType(typeof(T)) != null;
        }

        #endregion

        #region 内部方法

        private UIViewAttribute GetViewAttribute(Type type)
        {
            var attr = Attribute.GetCustomAttribute(type, typeof(UIViewAttribute)) as UIViewAttribute;
            if (attr == null)
                throw new Exception($"View [{type.Name}] 缺少 [UIViewAttribute]，请在类上添加该特性");
            return attr;
        }

        private void InitializeView(UBaseView view, UIViewAttribute attr, UBaseModel model, Action<UBaseView> onViewOpened)
        {
            view.LayerType = attr.Layer;
            view.IsSingleton = attr.IsSingleton;
            view.IsFullScreen = attr.IsFullScreen;
            view.CanRemoved = false; // 对象池复用时 CanRemoved 可能为 true，必须重置否则 View 打开即被自动关闭

            view.BuildChildCache();
            view.Initialize();
            view.OnComplete();

            view.gameObject.transform.SetParent(GetLayer(attr.Layer).transform, false);
            _viewDic[attr.Layer].Add(view); // 直接 Add 到 List 末尾
            SortLayer(attr.Layer);

            var controller = (UBaseController)ReferencePool.Spawn(view.ControllerType);
            _controllerDic.Add(view.ID, controller);
            view.Data = model ?? (UBaseModel)ReferencePool.Spawn(view.ModelType);
            controller.SetView(view);
            controller.Initialize();

            RefreshVisibility();
            onViewOpened?.Invoke(view);
        }

        private void InternalCloseView(UUILayer layer, UBaseView targetView, Action onViewClosed)
        {
            if (_controllerDic.TryGetValue(targetView.ID, out var controller))
            {
                ReferencePool.Release(controller);
                if (targetView.Data != null)
                    ReferencePool.Release(targetView.Data);
                _controllerDic.Remove(targetView.ID);
            }

            _viewDic[layer].Remove(targetView); // List.Remove 按引用相等移除
            targetView.OnRelease();
            RefreshVisibility();
            onViewClosed?.Invoke();
        }

        private UBaseView FindViewByType(Type type, UUILayer? specificLayer = null)
        {
            if (specificLayer.HasValue)
            {
                var list = _viewDic[specificLayer.Value];
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i] != null && list[i].GetType() == type)
                        return list[i];
                }
                return null;
            }

            for (int i = 0; i < _layerTypes.Count; i++)
            {
                var list = _viewDic[_layerTypes[i]];
                for (int j = 0; j < list.Count; j++)
                {
                    if (list[j] != null && list[j].GetType() == type)
                        return list[j];
                }
            }
            return null;
        }

        private void BringToFront(UBaseView view)
        {
            var layer = view.LayerType;
            var list = _viewDic[layer];
            if (list.Remove(view)) // Remove + Add 到末尾，顺序明确
            {
                list.Add(view);
                SortLayer(layer);
                RefreshVisibility();
            }
        }

        private void SortLayer(UUILayer layer)
        {
            var list = _viewDic[layer];
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null)
                    list[i].gameObject.transform.SetSiblingIndex(i);
            }
        }

        /// <summary>
        /// 全屏遮挡自动隐藏：从最上层往下扫描，遇到全屏 View 后隐藏其下所有 View
        /// </summary>
        private void RefreshVisibility()
        {
            bool hideNext = false;

            for (int i = _layerTypes.Count - 1; i >= 0; i--)
            {
                var list = _viewDic[_layerTypes[i]];
                // List 末尾 = 最高 sibling index = 最顶层，从后往前扫
                for (int j = list.Count - 1; j >= 0; j--)
                {
                    var view = list[j];
                    if (view == null)
                        continue;
                    if (!hideNext)
                    {
                        view.gameObject.SetActive(true);
                        if (view.IsFullScreen)
                            hideNext = true;
                    }
                    else
                    {
                        view.gameObject.SetActive(false);
                    }
                }
            }
        }

        #endregion
    }
}
