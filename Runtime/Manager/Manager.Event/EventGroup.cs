//------------------------------
// ZEngine
// 作者: Chenyu
//------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;
using ZEngine.Reference;

namespace ZEngine.Manager.Event
{
    public class EventGroup
    {
        private readonly Dictionary<Type, List<Action<IEventMessage>>> _cachedListener = new Dictionary<Type, List<Action<IEventMessage>>>();

        /// <summary>
        /// 添加监听
        /// </summary>
        public void AddListener<T>(Action<IEventMessage> listener) where T : IEventMessage
        {
            Type type = typeof(T);
            if (!_cachedListener.ContainsKey(type))
                _cachedListener.Add(type, new List<Action<IEventMessage>>());

            if (!_cachedListener[type].Contains(listener))
            {
                _cachedListener[type].Add(listener);
                EventManager.Instance.AddListener(type, listener);
            }
            else
            {
                Debug.LogWarning($"{type}事件已经存在");
            }
        }

        /// <summary>
        /// 移除全部监听
        /// </summary>
        public void RemoveAllListener()
        {
            foreach (var type in _cachedListener.Keys)
            {
                var listeners = _cachedListener[type];
                for (int i = 0; i < listeners.Count; i++)
                {
                    EventManager.Instance.RemoveListener(type, listeners[i]);
                }
                _cachedListener[type].Clear();
            }
            _cachedListener.Clear();
        }
    }
}
