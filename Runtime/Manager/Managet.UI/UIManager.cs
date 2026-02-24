//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using UnityEngine;
using ZEngine.Core;
using ZEngine.Manager.UI;
using FairyGUI;
using System.Collections.Generic;
using System;
using ZEngine.Extension;
using ZEngine.Reference;
using Cysharp.Threading.Tasks;
using System.Linq;

namespace ZEngine.Manager.UI
{
    public class UIManager : ManagerSingleton<UIManager>, IManager
    {

        //存储当前打开的View, 每个层级有多个View实例，用View.ID作为唯一Key
        private Dictionary<EUILayer, Dictionary<string, BaseView>> _viewDic = new Dictionary<EUILayer, Dictionary<string, BaseView>>();
        private Dictionary<string, BaseController> _controllerDic = new Dictionary<string, BaseController>();
        private List<string> _removedViewIds = new List<string>();

        public void OnInit(object param)
        {
            _root = new GameObject("[Z][UIManager]");
            GameObject.DontDestroyOnLoad(_root);

            //初始化各层级UI容器
            UILayer.Initialize();

            var layerTypes = EnumExtension.GetValues<EUILayer>();
            foreach (var layerType in layerTypes)
            {
                _viewDic.Add(layerType, new Dictionary<string, BaseView>());
            }

            GRoot.inst.SetContentScaleFactor(640, 360, UIContentScaler.ScreenMatchMode.MatchWidthOrHeight);
            GRoot.inst.pivotAsAnchor = true;
        }

        public void OnUpdate()
        {
            foreach(var item in _controllerDic)
            {
                if (item.Value != null)
                {
                    item.Value.OnUpdate();
                }
            }

            //由UI内部逻辑决定是否需要关闭View
            foreach (var layer in _viewDic.Keys)
            {
                var views = _viewDic[layer].Values.ToList();
                foreach (var view in views)
                {
                    if (view != null && view.CanRemoved)
                    {
                        _removedViewIds.Add(view.ID);
                    }
                }
            }

            foreach(var id in _removedViewIds)
            {
                CloseView(id, null);
            }
            _removedViewIds.Clear();
        }

        public void OnGUI()
        {

        }

        public void OnDestroy()
        {
            CloseAll();
            DestroySingleton();
        }


        /// <summary>
        /// 获取对应层级的父容器
        /// </summary>
        public GComponent GetLayer(EUILayer layer)
        {
            if (UILayer.LayerDic.TryGetValue(layer, out var gCompt))
                return gCompt;
            else
                throw new System.Exception($"未找到层级为[{layer}]的父容器");
        }

        /// <summary>
        /// 获取View对应的层级数
        /// </summary>
        public int GetLayerOrder(BaseView view)
        {
            var list = _viewDic[view.LayerType].Values.ToList();
            int index = list.IndexOf(view);
            if (index < 0)
            {
                throw new System.Exception($"未找到[{view.GetType()}]对应的层级");
            }
            return index;
        }

        /// <summary>
        /// 打开View（同步）
        /// </summary>
        public T OpenViewSync<T>(BaseModel model = null, Action<BaseView> onViewOpend = null) where T : BaseView, new()
        {
            Type type = typeof(T);
            var view = (T)ReferencePool.Spawn(type);
            view.Initialize();
            var layerType = view.LayerType;

            if (view.IsSingleton)
            {
                var existedView = _viewDic[layerType].Values.ToList().Find(v => v.GetType() == typeof(T));
                // 如果窗口已经存在
                if (existedView != null)
                {
                    ReferencePool.Release(view);
                    //如果已存在，将其置顶
                    BringToFront(existedView);
                    return (T)existedView;
                }
            }

            view.InternalLoadSync();
            GetLayer(layerType).AddChild(view.GetView());
            _viewDic[layerType].Add(view.ID, view);
            SortLayer(layerType);

            //创建Controller实例
            var controller = (BaseController)ReferencePool.Spawn(view.ControllerType);
            _controllerDic.Add(view.ID, controller);
            view.Data = model != null ? model : (BaseModel)ReferencePool.Spawn(view.ModelType);
            controller.SetView(view);
            controller.Initialize();
            //如果有回调函数，则执行
            onViewOpend?.Invoke(view);
            return view;
        }

        /// <summary>
        /// 打开View（异步）
        /// </summary>
        public async UniTask<T> OpenViewAsync<T>(BaseModel model = null, Action<BaseView> onViewOpend = null) where T : BaseView, new()
        {
            Type type = typeof(T);
            var view = (T)ReferencePool.Spawn(type);
            view.Initialize();
            var layerType = view.LayerType;

            if (view.IsSingleton)
            {
                var existedView = _viewDic[layerType].Values.ToList().Find(v => v.GetType() == typeof(T));
                // 如果窗口已经存在
                if (existedView != null)
                {
                    ReferencePool.Release(view);
                    //如果已存在，将其置顶
                    BringToFront(existedView);
                    return (T)existedView;
                }
            }

            await view.InternalLoadAsync();

            if (model != null)
            {
                view.Data = model;
            }
            GetLayer(layerType).AddChild(view.GetView());
            _viewDic[layerType].Add(view.ID, view);
            SortLayer(layerType);

            //创建Controller实例
            var controller = (BaseController)ReferencePool.Spawn(view.ControllerType);
            _controllerDic.Add(view.ID, controller);
            view.Data = model != null ? model : (BaseModel)ReferencePool.Spawn(view.ModelType);
            controller.SetView(view);
            controller.Initialize();

            //如果有回调函数，则执行
            onViewOpend?.Invoke(view);
            return view;
        }

        /// <summary>
        /// 关闭View
        /// </summary>
        public void CloseView<T>(string viewID = "", Action onViewClosed = null) where T : BaseView
        {
            var layerTypes = EnumExtension.GetValues<EUILayer>();
            foreach (var layer in layerTypes)
            {
                BaseView targetView = null;

                // 1. 如果提供了 viewID，优先精确查找
                if (!string.IsNullOrEmpty(viewID))
                {
                    _viewDic[layer].TryGetValue(viewID, out targetView);
                }

                // 2. 如果没有传 viewID 或 viewID 未找到，按类型查找
                if (targetView == null)
                {
                    targetView = _viewDic[layer].Values.ToList().Find(v => v.GetType() == typeof(T));
                }

                if (targetView != null)
                {
                    InternalCloseView(layer, targetView, onViewClosed);
                    return;
                }
            }
        }

        /// <summary>
        /// 根据View的ID关闭UI
        /// </summary>
        /// <param name="viewID"></param>
        /// <param name="onViewClosed"></param>
        public void CloseView(string viewID = "", Action onViewClosed = null)
        {
            var layerTypes = EnumExtension.GetValues<EUILayer>();
            foreach (var layer in layerTypes)
            {
                BaseView targetView = null;

                if (!string.IsNullOrEmpty(viewID))
                {
                    _viewDic[layer].TryGetValue(viewID, out targetView);
                }

                if (targetView != null)
                {
                    InternalCloseView(layer, targetView, onViewClosed);
                    return;
                }
            }

            Debug.LogError($"试图关闭一个ID为[{viewID}]的View，View不存在");
        }

        /// <summary>
        /// 关闭所有窗口
        /// </summary>
        public void CloseAll()
        {
            foreach (var layer in _viewDic.Keys)
            {
                foreach (var view in _viewDic[layer].Values.ToList())
                {
                    if (_controllerDic.TryGetValue(view.ID, out var controller))
                    {
                        ReferencePool.Release(controller);
                        ReferencePool.Release(view.Data);
                        ReferencePool.Release(view);
                        _controllerDic.Remove(view.ID);
                    }
                }
                _viewDic[layer].Clear();
            }
        }


        /// <summary>
        /// 指定View置顶
        /// </summary>
        /// <param name="view"></param>
        private void BringToFront(BaseView view)
        {
            var layer = view.LayerType;
            if (_viewDic[layer].Remove(view.ID))
            {
                _viewDic[layer].Add(view.ID, view);
                SortLayer(layer);
            }
        }

        /// <summary>
        /// 根据View列表刷新该层的显示顺序
        /// </summary>
        /// <param name="layer"></param>
        private void SortLayer(EUILayer layer)
        {
            var parent = GetLayer(layer);
            var list = _viewDic[layer].Values.ToList();
            for (int i = 0; i < list.Count; i++)
            {
                parent.SetChildIndex(list[i].GetView(), i);
            }
        }

        /// <summary>
        /// 关闭指定View并执行回调
        /// </summary>
        private void InternalCloseView(EUILayer layer, BaseView targetView, Action onViewClosed)
        {
            if (_controllerDic.TryGetValue(targetView.ID, out var controller))
            {
                ReferencePool.Release(controller);
                ReferencePool.Release(targetView.Data);
                ReferencePool.Release(targetView);
                _controllerDic.Remove(targetView.ID);
                _viewDic[layer].Remove(targetView.ID);
                onViewClosed?.Invoke();
            }
            else
            {
                Debug.LogError($"未找到[{targetView.GetType()}]对应的Controller");
            }
        }
    }
}
