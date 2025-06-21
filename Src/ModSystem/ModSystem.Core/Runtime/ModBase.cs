using ModSystem.Core.EventSystem;
using ModSystem.Core.Interfaces;
using ModSystem.Core.Lifecycle;
using ModSystem.Core.Configuration;
using System;
using System.IO;

namespace ModSystem.Core.Runtime
{
    /// <summary>
    /// 模组基类 - V5版本，添加配置支持
    /// </summary>
    public abstract class ModBase : IModBehaviour, IModLifecycle
    {
        protected IEventBus EventBus { get; private set; }
        protected ILogger Logger { get; private set; }
        protected IUnityAccess UnityAccess { get; private set; }

        // V4添加：定时器系统
        private TimerSystem _timerSystem;
        private LifecycleManager _lifecycleManager;

        // V5添加：配置路径
        private string _configPath;

        public abstract string ModId { get; }

        public void Initialize(ModContext context)
        {
            EventBus = context.EventBus;
            Logger = context.Logger;
            UnityAccess = context.UnityAccess;

            // V4添加：初始化定时器
            _timerSystem = new TimerSystem();

            // V4添加：注册到生命周期管理器
            _lifecycleManager = context.LifecycleManager;
            if (_lifecycleManager != null)
            {
                _lifecycleManager.RegisterMod(this);
            }

            // V5添加：设置配置路径（从context获取，如果没有则使用默认值）
            _configPath = context.ConfigPath ?? Path.Combine("ModConfigs");

            OnInitialize();
        }

        public void Shutdown()
        {
            OnShutdown();

            // V4添加：清理定时器
            _timerSystem?.Clear();

            // V4添加：从生命周期管理器注销
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

        // ========== V4添加：生命周期方法 ==========

        public virtual void OnUpdate(float deltaTime)
        {
            _timerSystem?.Update(deltaTime);
        }

        public virtual void OnFixedUpdate(float fixedDeltaTime) { }

        public virtual void OnLateUpdate(float deltaTime) { }

        // ========== V4添加：定时器API ==========

        protected int SetTimer(float delay, Action callback)
        {
            if (_timerSystem == null)
            {
                Logger?.LogWarning("Timer system not initialized");
                return -1;
            }
            return _timerSystem.SetTimer(delay, callback);
        }

        protected int SetRepeatingTimer(float interval, Action callback)
        {
            if (_timerSystem == null)
            {
                Logger?.LogWarning("Timer system not initialized");
                return -1;
            }
            return _timerSystem.SetRepeatingTimer(interval, callback);
        }

        protected void CancelTimer(int timerId)
        {
            _timerSystem?.CancelTimer(timerId);
        }

        // ========== V5新增：配置API ==========

        /// <summary>
        /// 加载模组配置
        /// </summary>
        /// <typeparam name="T">配置类型</typeparam>
        /// <returns>配置对象，如果加载失败返回新实例</returns>
        protected T LoadConfig<T>() where T : new()
        {
            return ConfigLoader.LoadConfig<T>(ModId, _configPath, Logger);
        }

        /// <summary>
        /// 保存配置
        /// </summary>
        /// <typeparam name="T">配置类型</typeparam>
        /// <param name="config">要保存的配置对象</param>
        /// <returns>是否保存成功</returns>
        protected bool SaveConfig<T>(T config)
        {
            return ConfigLoader.SaveConfig(ModId, _configPath, config, Logger);
        }

        /// <summary>
        /// 重新加载配置
        /// </summary>
        /// <typeparam name="T">配置类型</typeparam>
        /// <returns>新的配置对象</returns>
        protected T ReloadConfig<T>() where T : new()
        {
            Logger?.Log($"Reloading config for {ModId}");
            return LoadConfig<T>();
        }
    }
}