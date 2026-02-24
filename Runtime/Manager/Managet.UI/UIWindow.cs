//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using FairyGUI;
using YooAsset;
using System;
using ZEngine.Manager.Event;
using UnityEngine;

namespace ZEngine.Manager.UI
{
    public abstract class UIWindow : GComponent
    {
        private AssetHandle _handle;
        private Action<BaseView> _prepareCallback;
        private bool _isLoadAsset = false;

        /// <summary>
		/// 事件组
		/// 在窗口销毁的时候，自动移除注册的事件
		/// </summary>
		protected readonly EventGroup EventGrouper = new EventGroup();


        /// <summary>
		/// 窗口名称
		/// </summary>
		public string WindowName { private set; get; }

        /// <summary>
        /// 窗口层级
        /// </summary>
        public int WindowLayer { private set; get; }

        /// <summary>
        /// 是否是全屏窗口
        /// </summary>
        public bool FullScreen { private set; get; }

        /// <summary>
		/// 是否加载完毕
		/// </summary>
		public bool IsDone { get { return _handle.IsDone; } }

        /// <summary>
        /// 是否准备完毕
        /// </summary>
        public bool IsPrepare { private set; get; }

        /// <summary>
        /// 是否隐藏
        /// </summary>
        public bool IsHide { private set; get; }

        /// <summary>
        /// 数据
        /// </summary>
        //public IDataBase Data { private set; get; }

        /// <summary>
        /// UI面板等
        /// </summary>
        public GObject UIObject { private set; get; }


        internal void InternalInit()
        {
            UIInit();
        }

        internal void InternalUpdate()
        {
            UIUpdate();
        }

        internal void InternalDestroy()
        {
            if(UIObject != null)
            {
                UIDestroy();
                UIObject.Dispose();
                UIObject = null;
            }

            //卸载面板资源
            _handle.Release();

            //移除事件监听
            EventGrouper.RemoveAllListener();
        }

        public abstract void UIInit();
        public abstract void UIUpdate();
        public abstract void UIDestroy();
    }
}
