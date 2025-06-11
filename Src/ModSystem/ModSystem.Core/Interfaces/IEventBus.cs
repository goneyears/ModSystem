using System;

namespace ModSystem.Core
{
    /// <summary>
    /// 事件总线接口
    /// 提供事件的发布和订阅功能
    /// </summary>
    public interface IEventBus
    {
        /// <summary>
        /// 订阅指定类型的事件
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="handler">事件处理器</param>
        void Subscribe<T>(Action<T> handler) where T : IModEvent;
        
        /// <summary>
        /// 订阅指定类型的事件（带过滤条件）
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="handler">事件处理器</param>
        /// <param name="filter">过滤条件</param>
        void Subscribe<T>(Action<T> handler, Predicate<T> filter) where T : IModEvent;
        
        /// <summary>
        /// 发布事件
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="eventData">事件数据</param>
        void Publish<T>(T eventData) where T : IModEvent;
        
        /// <summary>
        /// 取消订阅事件
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="handler">要取消的事件处理器</param>
        void Unsubscribe<T>(Action<T> handler) where T : IModEvent;
        
        /// <summary>
        /// 取消指定订阅者的所有事件订阅
        /// </summary>
        /// <param name="subscriber">订阅者对象</param>
        void UnsubscribeAll(object subscriber);
    }
} 