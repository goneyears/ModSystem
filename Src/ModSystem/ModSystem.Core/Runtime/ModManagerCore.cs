using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ModSystem.Core.EventSystem;
using ModSystem.Core.Events;
using ModSystem.Core.Interfaces;
using ModSystem.Core.Lifecycle;

namespace ModSystem.Core.Runtime
{
    /// <summary>
    /// 核心模组管理器 - V4版本，添加生命周期管理
    /// </summary>
    public class ModManagerCore
    {
        private readonly Dictionary<string, IModBehaviour> _loadedMods = new Dictionary<string, IModBehaviour>();
        private readonly ILogger _logger;
        private readonly IEventBus _eventBus;
        private readonly IUnityAccess _unityAccess;

        // V4新增：生命周期管理器
        private readonly LifecycleManager _lifecycleManager;

        public ModManagerCore(ILogger logger, IUnityAccess unityAccess = null)
        {
            _logger = logger;
            _eventBus = new EventBus();
            _unityAccess = unityAccess;

            // V4新增：创建生命周期管理器
            _lifecycleManager = new LifecycleManager(logger);
        }

        public IEventBus EventBus => _eventBus;

        // V4新增：暴露生命周期管理器供Unity层调用
        public LifecycleManager LifecycleManager => _lifecycleManager;

        public void LoadModsFromDirectory(string directory)
        {
            if (!Directory.Exists(directory))
            {
                _logger.LogWarning($"Mods directory not found: {directory}");
                return;
            }

            var dllFiles = Directory.GetFiles(directory, "*.dll", SearchOption.AllDirectories);

            foreach (var dllFile in dllFiles)
            {
                try
                {
                    LoadModFromAssembly(dllFile);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to load {Path.GetFileName(dllFile)}: {ex.Message}");
                }
            }

            _logger.Log($"Loaded {_loadedMods.Count} mods");

            // V4新增：报告生命周期模组数量
            _logger.Log($"Lifecycle mods: {_lifecycleManager.GetRegisteredCount()}");

            // 发布系统就绪事件
            _eventBus.Publish(new SystemReadyEvent());
        }

        private void LoadModFromAssembly(string assemblyPath)
        {
            var assembly = Assembly.LoadFrom(assemblyPath);
            var modTypes = assembly.GetTypes()
                .Where(t => typeof(IModBehaviour).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                .ToList();

            foreach (var modType in modTypes)
            {
                try
                {
                    var mod = Activator.CreateInstance(modType) as IModBehaviour;
                    if (mod != null && !_loadedMods.ContainsKey(mod.ModId))
                    {
                        _loadedMods[mod.ModId] = mod;

                        // V4修改：传递生命周期管理器到context
                        var context = new ModContext(_eventBus, _logger, _unityAccess, _lifecycleManager);
                        mod.Initialize(context);

                        _logger.Log($"Loaded: {mod.ModId}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to initialize {modType.Name}: {ex.Message}");
                }
            }
        }

        public void ShutdownAllMods()
        {
            foreach (var mod in _loadedMods.Values)
            {
                try
                {
                    mod.Shutdown();
                }
                catch
                {
                    // 静默处理关闭异常
                }
            }
            _loadedMods.Clear();

            // V4新增：清理生命周期管理器
            _lifecycleManager.Clear();
        }

        public int GetLoadedModCount() => _loadedMods.Count;
    }
}