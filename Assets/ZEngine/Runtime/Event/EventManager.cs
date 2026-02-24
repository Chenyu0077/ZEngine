using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZEngine.Event
{
    /// <summary>
    /// Central event manager for the ZEngine framework.
    /// Supports subscribe, unsubscribe, and dispatch of typed events.
    /// </summary>
    public class EventManager : Core.Singleton<EventManager>
    {
        private readonly Dictionary<int, List<Delegate>> _eventHandlers = new Dictionary<int, List<Delegate>>();

        /// <summary>
        /// Subscribe to an event with a specific event ID.
        /// </summary>
        public void Subscribe<T>(int eventId, Action<T> handler) where T : IEventData
        {
            if (handler == null) return;
            if (!_eventHandlers.TryGetValue(eventId, out var handlers))
            {
                handlers = new List<Delegate>();
                _eventHandlers[eventId] = handlers;
            }
            if (!handlers.Contains(handler))
            {
                handlers.Add(handler);
            }
        }

        /// <summary>
        /// Subscribe to an event with no data payload.
        /// </summary>
        public void Subscribe(int eventId, Action handler)
        {
            if (handler == null) return;
            if (!_eventHandlers.TryGetValue(eventId, out var handlers))
            {
                handlers = new List<Delegate>();
                _eventHandlers[eventId] = handlers;
            }
            if (!handlers.Contains(handler))
            {
                handlers.Add(handler);
            }
        }

        /// <summary>
        /// Unsubscribe from an event.
        /// </summary>
        public void Unsubscribe<T>(int eventId, Action<T> handler) where T : IEventData
        {
            if (handler == null) return;
            if (_eventHandlers.TryGetValue(eventId, out var handlers))
            {
                handlers.Remove(handler);
            }
        }

        /// <summary>
        /// Unsubscribe a no-data handler from an event.
        /// </summary>
        public void Unsubscribe(int eventId, Action handler)
        {
            if (handler == null) return;
            if (_eventHandlers.TryGetValue(eventId, out var handlers))
            {
                handlers.Remove(handler);
            }
        }

        /// <summary>
        /// Dispatch an event with a data payload.
        /// </summary>
        public void Dispatch<T>(int eventId, T eventData) where T : IEventData
        {
            if (!_eventHandlers.TryGetValue(eventId, out var handlers)) return;
            var snapshot = new List<Delegate>(handlers);
            foreach (var handler in snapshot)
            {
                try
                {
                    if (handler is Action<T> typedHandler)
                    {
                        typedHandler.Invoke(eventData);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[EventManager] Exception in event handler for eventId={eventId}: {e}");
                }
            }
        }

        /// <summary>
        /// Dispatch an event with no data payload.
        /// </summary>
        public void Dispatch(int eventId)
        {
            if (!_eventHandlers.TryGetValue(eventId, out var handlers)) return;
            var snapshot = new List<Delegate>(handlers);
            foreach (var handler in snapshot)
            {
                try
                {
                    if (handler is Action action)
                    {
                        action.Invoke();
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[EventManager] Exception in event handler for eventId={eventId}: {e}");
                }
            }
        }

        /// <summary>
        /// Remove all handlers for a given event ID.
        /// </summary>
        public void Clear(int eventId)
        {
            _eventHandlers.Remove(eventId);
        }

        /// <summary>
        /// Remove all registered event handlers.
        /// </summary>
        public void ClearAll()
        {
            _eventHandlers.Clear();
        }
    }
}
