////------------------------------
//// ZEngine
//// 作者: Chenyu
////------------------------------

//using System;
//using System.Collections.Generic;
//using UnityEngine;
//using ZEngine.Core;
//using ZEngine.Reference;
//using ZEngine.Manager.Resource;
//using Cysharp.Threading.Tasks;
//using YooAsset;

//namespace ZEngine.Manager.UI
//{
//    /// <summary>
//    /// UGUI管理器，管理3D世界里的UGUI（血条、头顶名字等）
//    /// </summary>
//    public class UGUIManager : ManagerSingleton<UGUIManager>, IManager
//    {
//        private Canvas _worldCanvas;

//        // 同一 prefab 类型可能有多个实例（比如多个怪物的血条）
//        private readonly Dictionary<string, UGUIView> _viewDic = new Dictionary<string, UGUIView>();
//        private Dictionary<string, BaseController> _controllerDic = new Dictionary<string, BaseController>();
//        private List<string> _removedViewIds = new List<string>();

//        public void OnInit(object param)
//        {
//            _root = new GameObject("[Z][UGUIManager]");
//            GameObject.DontDestroyOnLoad(_root);

//            // 添加世界 Canvas
//            _worldCanvas = new GameObject("WorldUICanvas").AddComponent<Canvas>();      
//            _worldCanvas.renderMode = RenderMode.WorldSpace;
//            _worldCanvas.worldCamera = Camera.main;
//            _worldCanvas.transform.SetParent(_root.transform, false);

//            // 添加CanvasScaler和GraphicRaycaster
//            var scaler = _worldCanvas.gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
//            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
//            scaler.referenceResolution = new Vector2(1920, 1080);
//            _worldCanvas.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
//        }

//        public void OnUpdate()
//        {
//            // 可选：遍历，处理 CanRemoved 自动销毁
//            foreach (var kv in _viewDic)
//            {
                
//            }
//        }

//        public void OnGUI() { }

//        public void OnDestroy()
//        {
//            // 释放所有资源句柄
//            foreach(var item in _viewDic)
//            {
//                item.Value.OnRelease();
//            }
//            DestroySingleton();
//        }

//        /// <summary>
//        /// 打开一个基于 UGUI 的世界 UI（通过 prefab）
//        /// prefabPath 是相对于 GameAssetPaths.AssetRoot 的路径
//        /// </summary>
//        public async UniTask<T> OpenViewAsync<T>(string prefabPath, BaseModel model = null)
//            where T : UGUIView
//        {
//            // 使用 ResourceManager 加载资源
//            var handle = await ResourceManager.Instance.LoadAssetAsync<GameObject>(prefabPath);
//            if (handle == null || handle.AssetObject == null)
//            {
//                Debug.LogError($"WorldUI prefab not found: {prefabPath}");
//                return null;
//            }

//            var prefab = handle.AssetObject as GameObject;
//            if (prefab == null)
//            {
//                Debug.LogError($"WorldUI prefab is not GameObject: {prefabPath}");
//                handle.Release();
//                return null;
//            }

//            var go = GameObject.Instantiate(prefab, _worldCanvas.transform);
//            var view = go.GetComponent<T>();
//            if (view == null)
//            {
//                Debug.LogError($"UGUIView component {typeof(T)} not found on prefab {prefabPath}");
//                GameObject.Destroy(go);
//                handle.Release();
//                return null;
//            }

//            // 保存资源句柄，用于后续释放
//            //view.ID = Guid.NewGuid().ToString();
//            _assetHandles[view.ID] = handle;

//            view.Initialize();
//            view.Data = model ?? (BaseModel)ReferencePool.Spawn(view.ModelType);
//            view.OnComplete();

//            var key = typeof(T).FullName;
//            if (!_viewDic.TryGetValue(key, out var list))
//            {
//                list = new List<UGUIView>();
//                _viewDic[key] = list;
//            }

//            if (view.IsSingleton)
//            {
//                // 已经有一个就直接返回
//                var existed = list.Find(v => v != null);
//                if (existed != null)
//                {
//                    GameObject.Destroy(go);
//                    handle.Release();
//                    _assetHandles.Remove(view.ID);
//                    return (T)existed;
//                }
//            }

//            list.Add(view);
//            return view;
//        }

//        public void CloseView<T>(T view) where T : UGUIView
//        {
//            if (view == null) return;
//            InternalCloseView(view);
//        }

//        public void CloseView(string viewID)
//        {
//            foreach (var kv in _viewDic)
//            {
//                var view = kv.Value.Find(v => v.ID == viewID);
//                if (view != null)
//                {
//                    InternalCloseView(view);
//                    return;
//                }
//            }
//        }

//        public void CloseAll()
//        {
//            foreach (var pair in _viewDic)
//            {
//                if (_controllerDic.TryGetValue(pair.Key, out var controller))
//                {
//                    ReferencePool.Release(controller);
//                    ReferencePool.Release(pair.Value.Data);                   
//                    ReferencePool.Release(pair.Value);
//                    _controllerDic.Remove(pair.Key);       
//                }
//            }
//        }


//        /// <summary>
//        /// 关闭指定View并执行回调
//        /// </summary>
//        private void InternalCloseView(UGUIView targetView, Action onViewClosed)
//        {
//            if (_controllerDic.TryGetValue(targetView.ID, out var controller))
//            {
//                ReferencePool.Release(controller);
//                ReferencePool.Release(targetView.Data);
//                ReferencePool.Release(targetView);
//                _controllerDic.Remove(targetView.ID);
//                _viewDic.Remove(targetView.ID);
//                onViewClosed?.Invoke();
//            }
//            else
//            {
//                Debug.LogError($"未找到[{targetView.GetType()}]对应的Controller");
//            }
//        }

//        /// <summary>
//        /// 获取世界Canvas，用于设置UI的世界坐标位置
//        /// </summary>
//        public Canvas GetWorldCanvas()
//        {
//            return _worldCanvas;
//        }
//    }
//}