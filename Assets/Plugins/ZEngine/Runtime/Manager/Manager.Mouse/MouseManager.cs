using System;
using System.Collections.Generic;
using UnityEngine;
using ZEngine.Core;
using ZEngine.Manager.Log;

namespace ZEngine.Manager.Mouse
{
    public class MouseManager : ManagerSingleton<MouseManager>, IManager
    {
        private MouseEventHandler _mouseHandler;
        
        #region 生命周期
        public void OnInit(object param)
        {
            //检测依赖模块
            if (ZEngineMain.Contains(typeof(LogManager)) == false)
                throw new Exception($"{nameof(MouseManager)}依赖于{nameof(LogManager)}");
            _root = new GameObject("[Z][MouseManager]");
            GameObject.DontDestroyOnLoad(_root);

            _mouseHandler = new MouseEventHandler();
        }

        public void OnUpdate()
        {
            _mouseHandler.Update();
        }

        public void OnDestroy()
        {
            _mouseHandler.ClearAllListeners();
            _mouseHandler = null;
            DestroySingleton();
        }

        public void OnGUI()
        {
            
        }
        
        #endregion


        #region 添加事件 

        /// <summary>
        /// 添加左键单击事件
        /// </summary>
        /// <param name="handler"></param>
        public void AddLeftClickEvent(Action<Vector2> handler)
        {
            _mouseHandler.OnLeftClick += handler;
        }
        
        /// <summary>
        /// 添加左键双击事件
        /// </summary>
        /// <param name="handler"></param>
        public void AddLeftDoubleClickEvent(Action<Vector2> handler)
        {
            _mouseHandler.OnLeftDoubleClick += handler;
        }

        /// <summary>
        /// 添加右键单击事件
        /// </summary>
        /// <param name="handler"></param>
        public void AddRightClickEvent(Action<Vector2> handler)
        {
            _mouseHandler.OnRightClick += handler;
        }

        /// <summary>
        /// 添加右键双击事件
        /// </summary>
        /// <param name="handler"></param>
        public void AddRightDoubleClickEvent(Action<Vector2> handler)
        {
            _mouseHandler.OnRightDoubleClick += handler;
        }

        /// <summary>
        /// 添加滑轮滚动事件
        /// </summary>
        /// <param name="handler"></param>
        public void AddScrollEvent(Action<MouseScrollEventArgs> handler)
        {
            _mouseHandler.OnScroll += handler;
        }

        /// <summary>
        /// 添加左键拖拽开始事件
        /// </summary>
        /// <param name="handler"></param>
        public void AddLeftDragBeginEvent(Action<Vector2> handler)
        {
            _mouseHandler.OnLeftDragBegin += handler;
        }

        /// <summary>
        /// 添加左键拖拽中事件
        /// </summary>
        /// <param name="handler"></param>
        public void AddLeftDraggingEvent(Action<MouseDragEventArgs> handler)
        {
            _mouseHandler.OnLeftDragging += handler;
        }

        /// <summary>
        /// 添加左键拖拽结束事件
        /// </summary>
        /// <param name="handler"></param>
        public void AddLeftDragEndEvent(Action<MouseDragEventArgs> handler)
        {
            _mouseHandler.OnLeftDragEnd += handler;
        }

        #endregion


        #region 移除事件

        /// <summary>
        /// 添加左键单击事件
        /// </summary>
        /// <param name="handler"></param>
        public void RemoveLeftClickEvent(Action<Vector2> handler)
        {
            _mouseHandler.OnLeftClick -= handler;
        }
        
        /// <summary>
        /// 添加左键双击事件
        /// </summary>
        /// <param name="handler"></param>
        public void RemoveLeftDoubleClickEvent(Action<Vector2> handler)
        {
            _mouseHandler.OnLeftDoubleClick -= handler;
        }

        /// <summary>
        /// 添加右键单击事件
        /// </summary>
        /// <param name="handler"></param>
        public void RemoveRightClickEvent(Action<Vector2> handler)
        {
            _mouseHandler.OnRightClick -= handler;
        }

        /// <summary>
        /// 添加右键双击事件
        /// </summary>
        /// <param name="handler"></param>
        public void RemoveRightDoubleClickEvent(Action<Vector2> handler)
        {
            _mouseHandler.OnRightDoubleClick -= handler;
        }

        /// <summary>
        /// 添加滑轮滚动事件
        /// </summary>
        /// <param name="handler"></param>
        public void RemoveScrollEvent(Action<MouseScrollEventArgs> handler)
        {
            _mouseHandler.OnScroll -= handler;
        }

        /// <summary>
        /// 添加左键拖拽开始事件
        /// </summary>
        /// <param name="handler"></param>
        public void RemoveLeftDragBeginEvent(Action<Vector2> handler)
        {
            _mouseHandler.OnLeftDragBegin -= handler;
        }

        /// <summary>
        /// 添加左键拖拽中事件
        /// </summary>
        /// <param name="handler"></param>
        public void RemoveLeftDraggingEvent(Action<MouseDragEventArgs> handler)
        {
            _mouseHandler.OnLeftDragging -= handler;
        }

        /// <summary>
        /// 添加左键拖拽结束事件
        /// </summary>
        /// <param name="handler"></param>
        public void RemoveLeftDragEndEvent(Action<MouseDragEventArgs> handler)
        {
            _mouseHandler.OnLeftDragEnd -= handler;
        }

        #endregion
    }   
}
