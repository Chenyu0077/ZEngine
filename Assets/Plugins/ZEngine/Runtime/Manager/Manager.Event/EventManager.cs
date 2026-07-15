//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;
using ZEngine.Core;
using ZEngine.Manager.Log;
using ZEngine.Reference;

namespace ZEngine.Manager.Event
{
    public class EventManager : ManagerSingleton<EventManager>, IManager
    {
        //延时事件包装
        private class EventWrapper : IReference
        {
            public int DelayFrame;
            public IEventMessage Message;

            public void OnRelease()
            {
                DelayFrame = 0;
                Message = null;
            }
        }

        // 事件监听列表
        private readonly Dictionary<Type, LinkedList<Action<IEventMessage>>> _listeners = new Dictionary<Type, LinkedList<Action<IEventMessage>>>();
        // 延时事件列表
        private readonly List<EventWrapper> _eventWrappers = new List<EventWrapper>(1000);

        public void OnInit(object param)
        {
            //检测依赖模块
            if (ZEngineMain.Contains(typeof(LogManager)) == false)
                throw new Exception($"{nameof(EventManager)}依赖于{nameof(LogManager)}");

            _root = new GameObject("[Z][EventManager]");
            GameObject.DontDestroyOnLoad(_root);
        }

        public void OnUpdate()
        {
            for(int i = _eventWrappers.Count - 1; i >= 0; i--)
            {
                var wrapper = _eventWrappers[i];
                if(UnityEngine.Time.frameCount > wrapper.DelayFrame)
                {
                    SendMessage(wrapper.Message);
                    _eventWrappers.RemoveAt(i);
                    ReferencePool.Release(wrapper);
                }
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
        /// 添加监听
        /// </summary>
        public void AddListener(Type type, Action<IEventMessage> listener)
        {
            if (!_listeners.ContainsKey(type))
                _listeners.Add(type, new LinkedList<Action<IEventMessage>>());
            if (!_listeners[type].Contains(listener))
                _listeners[type].AddLast(listener);
        }

        /// <summary>
        /// 添加监听
        /// </summary>
        public void AddListener<T>(Action<IEventMessage> listener) where T : IEventMessage
        {
            AddListener(typeof(T), listener);
        }

        /// <summary>
        /// 移除监听
        /// </summary>
        public void RemoveListener(Type type, Action<IEventMessage> listener)
        {
            if (_listeners.ContainsKey(type))
                _listeners[type].Remove(listener);
        }

        /// <summary>
        /// 移除监听
        /// </summary>
        public void RemoveListener<T>(Action<IEventMessage> listener) where T : IEventMessage
        {
            RemoveListener(typeof(T), listener);
        }

        /// <summary>
        /// 实时广播事件
        /// </summary>
        public void SendMessage(IEventMessage message)
        {
            Type type = message.GetType();
            if (!_listeners.ContainsKey(type))
                return;

            LinkedList<Action<IEventMessage>> listeners = _listeners[type];
            if (listeners.Count > 0)
            {
                // 正向遍历：按注册顺序执行，与观察者模式惯例一致
                var currentNode = listeners.First;
                while (currentNode != null)
                {
                    var next = currentNode.Next;
                    currentNode.Value.Invoke(message);
                    currentNode = next;
                }
            }

            //回收引用对象
            IReference refClass = message as IReference;
            if (refClass != null)
                ReferencePool.Release(refClass);
        }

        /// <summary>
        /// 延迟广播事件(一般可用于线程安全)
        /// </summary>
        public void DelayMessage(IEventMessage message, int delayFrame = 1)
        {
            var wrapper = ReferencePool.Spawn<EventWrapper>();
            wrapper.DelayFrame = UnityEngine.Time.frameCount + delayFrame;
            wrapper.Message = message;
            _eventWrappers.Add(wrapper);
        }

        /// <summary>
        /// 清空所有监听
        /// </summary>
        public void ClearListener()
        {
            foreach(var type in _listeners.Keys)
            {
                _listeners[type].Clear();
            }
            _listeners.Clear();
        }

        #region 测试用
        /// <summary>
        /// 获取监听事件总数
        /// </summary>
        /// <returns></returns>
        private int GetAllListenerCount()
        {
            int count = 0;
            foreach(var list in _listeners)
            {
                count += list.Value.Count;
            }
            return count;
        }
        #endregion
    }
}
