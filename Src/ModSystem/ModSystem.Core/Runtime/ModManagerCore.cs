using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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

        public ModManagerCore(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 获取日志接口（供模组使用）
        /// </summary>
        public ILogger Logger => _logger;

        /// <summary>
        /// 从目录加载所有模组
        /// </summary>
        public void LoadModsFromDirectory(string directory)
        {
            if (!Directory.Exists(directory))
            {
                _logger.LogWarning($"Mods directory not found: {directory}");
                return;
            }

            _logger.Log($"Loading mods from: {directory}");

            // 查找所有DLL文件
            var dllFiles = Directory.GetFiles(directory, "*.dll", SearchOption.AllDirectories);

            foreach (var dllFile in dllFiles)
            {
                try
                {
                    LoadModFromAssembly(dllFile);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to load mod from {dllFile}: {ex.Message}");
                }
            }

            _logger.Log($"Loaded {_loadedMods.Count} mods");
        }

        /// <summary>
        /// 从程序集加载模组
        /// </summary>
        private void LoadModFromAssembly(string assemblyPath)
        {
            _logger.Log($"Loading assembly: {assemblyPath}");

            var assembly = Assembly.LoadFrom(assemblyPath);

            // 查找实现IModBehaviour的类型
            var modTypes = assembly.GetTypes()
                .Where(t => typeof(IModBehaviour).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                .ToList();

            foreach (var modType in modTypes)
            {
                try
                {
                    // 尝试使用带ILogger参数的构造函数
                    var loggerConstructor = modType.GetConstructor(new[] { typeof(ILogger) });
                    IModBehaviour modInstance;

                    if (loggerConstructor != null)
                    {
                        modInstance = Activator.CreateInstance(modType, _logger) as IModBehaviour;
                    }
                    else
                    {
                        modInstance = Activator.CreateInstance(modType) as IModBehaviour;

                        // 如果是ModBase的子类，设置Logger
                        if (modInstance is ModBase modBase)
                        {
                            modBase.SetLogger(_logger);
                        }
                    }

                    if (modInstance != null)
                    {
                        RegisterMod(modInstance);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to create instance of {modType.Name}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 注册模组
        /// </summary>
        private void RegisterMod(IModBehaviour mod)
        {
            if (_loadedMods.ContainsKey(mod.ModId))
            {
                _logger.LogWarning($"Mod with ID '{mod.ModId}' already loaded, skipping");
                return;
            }

            _loadedMods[mod.ModId] = mod;
            _logger.Log($"Registered mod: {mod.ModId}");

            // 初始化模组
            try
            {
                mod.Initialize();
                _logger.Log($"Initialized mod: {mod.ModId}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to initialize mod {mod.ModId}: {ex.Message}");
                _loadedMods.Remove(mod.ModId);
            }
        }

        /// <summary>
        /// 关闭所有模组
        /// </summary>
        public void ShutdownAllMods()
        {
            _logger.Log("Shutting down all mods...");

            foreach (var mod in _loadedMods.Values)
            {
                try
                {
                    mod.Shutdown();
                    _logger.Log($"Shutdown mod: {mod.ModId}");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error shutting down mod {mod.ModId}: {ex.Message}");
                }
            }

            _loadedMods.Clear();
        }

        /// <summary>
        /// 获取已加载的模组数量
        /// </summary>
        public int GetLoadedModCount() => _loadedMods.Count;

        /// <summary>
        /// 获取所有已加载的模组ID
        /// </summary>
        public IEnumerable<string> GetLoadedModIds() => _loadedMods.Keys;
    }
}