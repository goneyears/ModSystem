using ModSystem.Core.EventSystem;
using ModSystem.Core.Interfaces;
using ModSystem.Core.Lifecycle;
using System;

namespace ModSystem.Core.Runtime
{
    /// <summary>
    /// 模组基类 - V4版本，添加生命周期支持
    /// </summary>
    public abstract class ModBase : IModBehaviour, IModLifecycle
    {
        protected IEventBus EventBus { get; private set; }
        protected ILogger Logger { get; private set; }
        protected IUnityAccess UnityAccess { get; private set; }

        // V4新增：定时器系统
        private TimerSystem _timerSystem;
        private LifecycleManager _lifecycleManager;

        public abstract string ModId { get; }

        public void Initialize(ModContext context)
        {
            EventBus = context.EventBus;
            Logger = context.Logger;
            UnityAccess = context.UnityAccess;

            // V4新增：初始化定时器
            _timerSystem = new TimerSystem();

            // V4新增：注册到生命周期管理器（如果提供）
            _lifecycleManager = context.LifecycleManager;
            if (_lifecycleManager != null)
            {
                _lifecycleManager.RegisterMod(this);
            }

            OnInitialize();
        }

        public void Shutdown()
        {
            OnShutdown();

            // V4新增：清理定时器
            _timerSystem?.Clear();

            // V4新增：从生命周期管理器注销
            _lifecycleManager?.UnregisterMod(this);
        }

        // 原有的抽象方法
        protected abstract void OnInitialize();
        protected virtual void OnShutdown() { }

        // 原有的辅助方法
        protected void PublishEvent<T>(T eventData) where T : IModEvent
        {
            if (eventData is ModEventBase modEvent)
            {
                modEvent.SenderId = ModId;
                modEvent.Timestamp = DateTime.Now;
            }
            EventBus?.Publish(eventData);
        }

        protected void Subscribe<T>(Action<T> handler) where T : IModEvent
        {
            EventBus?.Subscribe(handler);
        }

        // ========== V4新增：生命周期方法（提供默认空实现） ==========

        /// <summary>
        /// 每帧调用（可重写）
        /// </summary>
        public virtual void OnUpdate(float deltaTime)
        {
            // 更新定时器系统
            _timerSystem?.Update(deltaTime);
        }

        /// <summary>
        /// 固定间隔调用（可重写）
        /// </summary>
        public virtual void OnFixedUpdate(float fixedDeltaTime) { }

        /// <summary>
        /// Update后调用（可重写）
        /// </summary>
        public virtual void OnLateUpdate(float deltaTime) { }

        // ========== V4新增：定时器API ==========

        /// <summary>
        /// 设置一次性定时器
        /// </summary>
        /// <param name="delay">延迟时间（秒）</param>
        /// <param name="callback">回调函数</param>
        /// <returns>定时器ID，用于取消</returns>
        protected int SetTimer(float delay, Action callback)
        {
            if (_timerSystem == null)
            {
                Logger?.LogWarning("Timer system not initialized");
                return -1;
            }
            return _timerSystem.SetTimer(delay, callback);
        }

        /// <summary>
        /// 设置重复定时器
        /// </summary>
        /// <param name="interval">间隔时间（秒）</param>
        /// <param name="callback">回调函数</param>
        /// <returns>定时器ID，用于取消</returns>
        protected int SetRepeatingTimer(float interval, Action callback)
        {
            if (_timerSystem == null)
            {
                Logger?.LogWarning("Timer system not initialized");
                return -1;
            }
            return _timerSystem.SetRepeatingTimer(interval, callback);
        }

        /// <summary>
        /// 取消定时器
        /// </summary>
        /// <param name="timerId">定时器ID</param>
        protected void CancelTimer(int timerId)
        {
            _timerSystem?.CancelTimer(timerId);
        }
    }
}