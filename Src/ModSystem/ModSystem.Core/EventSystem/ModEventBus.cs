using System;
using System.Collections.Generic;
using System.Linq;

namespace ModSystem.Core
{
    /// <summary>
    /// 模组事件总线实现
    /// 提供线程安全的事件发布和订阅功能
    /// </summary>
    public class ModEventBus : IEventBus
    {
        private readonly Dictionary<Type, List<EventHandler>> handlers;
        private readonly object lockObject = new object();
        private readonly IEventLogger logger;
        
        /// <summary>
        /// 事件发布时触发的事件
        /// </summary>
        public event Action<IModEvent> OnEventPublished;
        
        /// <summary>
        /// 内部事件处理器包装类
        /// </summary>
        private class EventHandler
        {
            public Delegate Handler { get; set; }
            public Predicate<IModEvent> Filter { get; set; }
            public string SubscriberId { get; set; }
            public WeakReference TargetRef { get; set; }
        }
        
        /// <summary>
        /// 创建事件总线实例
        /// </summary>
        /// <param name="logger">可选的事件日志记录器</param>
        public ModEventBus(IEventLogger logger = null)
        {
            handlers = new Dictionary<Type, List<EventHandler>>();
            this.logger = logger;
        }
        
        /// <summary>
        /// 订阅事件
        /// </summary>
        public void Subscribe<T>(Action<T> handler) where T : IModEvent
        {
            Subscribe(handler, null);
        }
        
        /// <summary>
        /// 订阅事件（带过滤器）
        /// </summary>
        public void Subscribe<T>(Action<T> handler, Predicate<T> filter) where T : IModEvent
        {
            lock (lockObject)
            {
                var eventType = typeof(T);
                if (!handlers.ContainsKey(eventType))
                {
                    handlers[eventType] = new List<EventHandler>();
                }
                
                handlers[eventType].Add(new EventHandler
                {
                    Handler = handler,
                    Filter = filter != null ? e => filter((T)e) : (Predicate<IModEvent>)null,
                    SubscriberId = handler.Target?.GetType().Name ?? "Anonymous",
                    TargetRef = handler.Target != null ? new WeakReference(handler.Target) : null
                });
                
                logger?.LogSubscription(eventType.Name, handler.Target?.GetType().Name);
            }
        }
        
        /// <summary>
        /// 发布事件
        /// </summary>
        public void Publish<T>(T eventData) where T : IModEvent
        {
            if (eventData == null) return;
            
            eventData.Timestamp = DateTime.Now;
            logger?.LogEvent(eventData);
            OnEventPublished?.Invoke(eventData);
            
            List<EventHandler> eventHandlers;
            lock (lockObject)
            {
                var eventType = typeof(T);
                if (!handlers.ContainsKey(eventType))
                    return;
                
                // 清理已释放的处理器
                handlers[eventType].RemoveAll(h => 
                    h.TargetRef != null && !h.TargetRef.IsAlive);
                
                eventHandlers = handlers[eventType].ToList();
            }
            
            // 在锁外执行处理器，避免死锁
            foreach (var handler in eventHandlers)
            {
                try
                {
                    if (handler.Filter != null && !handler.Filter(eventData))
                        continue;
                    
                    ((Action<T>)handler.Handler)?.Invoke(eventData);
                }
                catch (Exception ex)
                {
                    logger?.LogError($"Handler error for {typeof(T).Name}: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// 取消订阅
        /// </summary>
        public void Unsubscribe<T>(Action<T> handler) where T : IModEvent
        {
            lock (lockObject)
            {
                var eventType = typeof(T);
                if (!handlers.ContainsKey(eventType))
                    return;
                
                handlers[eventType].RemoveAll(h => h.Handler.Equals(handler));
                
                if (handlers[eventType].Count == 0)
                    handlers.Remove(eventType);
            }
        }
        
        /// <summary>
        /// 取消所有订阅
        /// </summary>
        public void UnsubscribeAll(object subscriber)
        {
            lock (lockObject)
            {
                foreach (var handlerList in handlers.Values)
                {
                    handlerList.RemoveAll(h => 
                        h.TargetRef != null && 
                        h.TargetRef.IsAlive && 
                        h.TargetRef.Target == subscriber);
                }
                
                // 清理空列表
                var emptyKeys = handlers.Where(kvp => kvp.Value.Count == 0)
                    .Select(kvp => kvp.Key).ToList();
                foreach (var key in emptyKeys)
                {
                    handlers.Remove(key);
                }
            }
        }
        
        /// <summary>
        /// 获取事件统计信息
        /// </summary>
        public Dictionary<string, int> GetEventStatistics()
        {
            lock (lockObject)
            {
                return handlers.ToDictionary(
                    kvp => kvp.Key.Name,
                    kvp => kvp.Value.Count
                );
            }
        }
    }
} 