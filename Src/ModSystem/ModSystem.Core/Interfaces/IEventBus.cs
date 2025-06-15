using System;

namespace ModSystem.Core.Interfaces
{
    /// <summary>
    /// 事件总线接口
    /// </summary>
    public interface IEventBus
    {
        /// <summary>
        /// 发布事件
        /// </summary>
        void Publish<T>(T eventData) where T : IModEvent;

        /// <summary>
        /// 订阅事件
        /// </summary>
        void Subscribe<T>(Action<T> handler) where T : IModEvent;

        /// <summary>
        /// 取消订阅
        /// </summary>
        void Unsubscribe<T>(Action<T> handler) where T : IModEvent;
    }
}