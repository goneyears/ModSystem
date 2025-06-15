using System;
using System.Collections.Generic;
using System.Linq;
using ModSystem.Core.Interfaces;

namespace ModSystem.Core.EventSystem
{
    /// <summary>
    /// 事件总线实现
    /// </summary>
    public class EventBus : IEventBus
    {
        private readonly Dictionary<Type, List<Delegate>> _handlers = new Dictionary<Type, List<Delegate>>();
        private readonly object _lock = new object();

        public void Publish<T>(T eventData) where T : IModEvent
        {
            if (eventData == null) return;

            List<Delegate> handlers;
            lock (_lock)
            {
                if (!_handlers.TryGetValue(typeof(T), out handlers))
                    return;

                handlers = handlers.ToList(); // 复制以避免迭代时修改
            }

            foreach (var handler in handlers)
            {
                try
                {
                    (handler as Action<T>)?.Invoke(eventData);
                }
                catch
                {
                    // 静默处理异常，避免影响其他处理器
                }
            }
        }

        public void Subscribe<T>(Action<T> handler) where T : IModEvent
        {
            if (handler == null) return;

            lock (_lock)
            {
                if (!_handlers.TryGetValue(typeof(T), out var list))
                {
                    list = new List<Delegate>();
                    _handlers[typeof(T)] = list;
                }

                if (!list.Contains(handler))
                {
                    list.Add(handler);
                }
            }
        }

        public void Unsubscribe<T>(Action<T> handler) where T : IModEvent
        {
            if (handler == null) return;

            lock (_lock)
            {
                if (_handlers.TryGetValue(typeof(T), out var list))
                {
                    list.Remove(handler);
                    if (list.Count == 0)
                    {
                        _handlers.Remove(typeof(T));
                    }
                }
            }
        }
    }
}