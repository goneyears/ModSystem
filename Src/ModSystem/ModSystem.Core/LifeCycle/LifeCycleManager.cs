using System;
using System.Collections.Generic;
using ModSystem.Core.Interfaces;

namespace ModSystem.Core.Lifecycle
{
    /// <summary>
    /// 生命周期管理器 - 管理所有模组的生命周期调用
    /// </summary>
    public class LifecycleManager
    {
        private readonly List<IModLifecycle> _lifecycleMods = new List<IModLifecycle>();
        private readonly ILogger _logger;

        public LifecycleManager(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 注册支持生命周期的模组
        /// </summary>
        public void RegisterMod(IModLifecycle mod)
        {
            if (mod != null && !_lifecycleMods.Contains(mod))
            {
                _lifecycleMods.Add(mod);
                _logger.Log($"Registered lifecycle mod: {mod.GetType().Name}");
            }
        }

        /// <summary>
        /// 注销模组
        /// </summary>
        public void UnregisterMod(IModLifecycle mod)
        {
            if (mod != null && _lifecycleMods.Remove(mod))
            {
                _logger.Log($"Unregistered lifecycle mod: {mod.GetType().Name}");
            }
        }

        /// <summary>
        /// 调用所有模组的Update
        /// </summary>
        public void UpdateAll(float deltaTime)
        {
            // 简单的列表遍历，不做复杂优化
            for (int i = 0; i < _lifecycleMods.Count; i++)
            {
                try
                {
                    _lifecycleMods[i].OnUpdate(deltaTime);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error in OnUpdate: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 调用所有模组的FixedUpdate
        /// </summary>
        public void FixedUpdateAll(float fixedDeltaTime)
        {
            for (int i = 0; i < _lifecycleMods.Count; i++)
            {
                try
                {
                    _lifecycleMods[i].OnFixedUpdate(fixedDeltaTime);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error in OnFixedUpdate: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 调用所有模组的LateUpdate
        /// </summary>
        public void LateUpdateAll(float deltaTime)
        {
            for (int i = 0; i < _lifecycleMods.Count; i++)
            {
                try
                {
                    _lifecycleMods[i].OnLateUpdate(deltaTime);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error in OnLateUpdate: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 清理所有注册的模组
        /// </summary>
        public void Clear()
        {
            _lifecycleMods.Clear();
        }

        /// <summary>
        /// 获取注册的模组数量
        /// </summary>
        public int GetRegisteredCount() => _lifecycleMods.Count;
    }
}