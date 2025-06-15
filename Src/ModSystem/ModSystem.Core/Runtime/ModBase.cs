using ModSystem.Core.EventSystem;
using ModSystem.Core.Interfaces;
using System;

namespace ModSystem.Core.Runtime
{
    /// <summary>
    /// 模组基类
    /// </summary>
    public abstract class ModBase : IModBehaviour
    {
        protected IEventBus EventBus { get; private set; }
        protected ILogger Logger { get; private set; }

        public abstract string ModId { get; }

        public void Initialize(ModContext context)
        {
            EventBus = context.EventBus;
            Logger = context.Logger;
            OnInitialize();
        }

        public void Shutdown()
        {
            OnShutdown();
        }

        protected abstract void OnInitialize();
        protected virtual void OnShutdown() { }

        /// <summary>
        /// 发布事件（自动设置SenderId）
        /// </summary>
        protected void PublishEvent<T>(T eventData) where T : IModEvent
        {
            if (eventData is ModEventBase modEvent)
            {
                modEvent.SenderId = ModId;
                modEvent.Timestamp = DateTime.Now;
            }
            EventBus?.Publish(eventData);
        }

        /// <summary>
        /// 订阅事件
        /// </summary>
        protected void Subscribe<T>(Action<T> handler) where T : IModEvent
        {
            EventBus?.Subscribe(handler);
        }
    }
}