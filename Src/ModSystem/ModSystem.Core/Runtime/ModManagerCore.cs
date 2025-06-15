using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ModSystem.Core.EventSystem;
using ModSystem.Core.Events;
using ModSystem.Core.Interfaces;

namespace ModSystem.Core.Runtime
{
    /// <summary>
    /// 核心模组管理器
    /// </summary>
    public class ModManagerCore
    {
        private readonly Dictionary<string, IModBehaviour> _loadedMods = new Dictionary<string, IModBehaviour>();
        private readonly ILogger _logger;
        private readonly IEventBus _eventBus;
        private readonly IUnityAccess _unityAccess;

        public ModManagerCore(ILogger logger, IUnityAccess unityAccess = null)
        {
            _logger = logger;
            _eventBus = new EventBus();
            _unityAccess = unityAccess;
        }

        public IEventBus EventBus => _eventBus;

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

            // 发布系统就绪事件
            _eventBus.Publish(new Events.SystemReadyEvent());
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
                        var context = new ModContext(_eventBus, _logger, _unityAccess);
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
        }

        public int GetLoadedModCount() => _loadedMods.Count;
    }
}