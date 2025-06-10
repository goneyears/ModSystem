// ===================================================================
// ModSystem.Core.cs - 完整的Core层实现
// 平台无关的模组系统核心
// ===================================================================

namespace ModSystem.Core
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Newtonsoft.Json;

    #region 接口定义

    /// <summary>
    /// 模组接口 - 所有模组必须实现此接口
    /// </summary>
    public interface IMod
    {
        void OnInitialize(IModContext context);
        void OnEnable();
        void OnDisable();
        void OnDestroy();
    }

    /// <summary>
    /// 模组上下文 - 提供模组运行环境
    /// </summary>
    public interface IModContext
    {
        string ModId { get; }
        IEventBus EventBus { get; }
        IUIFactory UI { get; }
        void Log(string message);
        void LogError(string message);
    }

    /// <summary>
    /// 事件总线接口
    /// </summary>
    public interface IEventBus
    {
        void Subscribe<T>(string eventId, Action<T> handler) where T : IModEvent;
        void Unsubscribe<T>(string eventId, Action<T> handler) where T : IModEvent;
        void UnsubscribeAll(object subscriber);
        void Publish<T>(T eventData) where T : IModEvent;
    }

    /// <summary>
    /// 模组事件基类
    /// </summary>
    public interface IModEvent
    {
        string EventId { get; }
        string SourceModId { get; }
        DateTime Timestamp { get; }
    }

    /// <summary>
    /// 日志接口
    /// </summary>
    public interface ILogger
    {
        void Log(string message);
        void LogWarning(string message);
        void LogError(string message);
    }

    /// <summary>
    /// 路径提供器接口
    /// </summary>
    public interface IPathProvider
    {
        string GetModsPath();
        string GetConfigPath();
    }

    /// <summary>
    /// 支持热重载的模组接口（可选）
    /// </summary>
    public interface IReloadable
    {
        void OnBeforeReload();
        void OnAfterReload();
    }

    /// <summary>
    /// UI工厂接口 - 简化的UI创建
    /// </summary>
    public interface IUIFactory
    {
        string CreateButton(string name, string text, Action onClick);
        string CreateText(string name, string text);
        string CreateImage(string name);
        string CreatePanel(string name);
        void SetProperty(string elementId, string property, object value);
        void DestroyElement(string elementId);
        T GetProperty<T>(string elementId, string property);
    }

    #endregion

    #region 核心实现

    /// <summary>
    /// 模组清单
    /// </summary>
    [Serializable]
    public class ModManifest
    {
        public string id { get; set; }
        public string name { get; set; }
        public string version { get; set; }
        public string author { get; set; }
        public string description { get; set; }
        public string main_class { get; set; }
        public string[] dependencies { get; set; }
        public Dictionary<string, object> metadata { get; set; }
    }

    /// <summary>
    /// 模组基类 - 提供默认实现
    /// </summary>
    public abstract class ModBase : IMod
    {
        protected IModContext Context { get; private set; }

        public virtual void OnInitialize(IModContext context)
        {
            Context = context;
            Context.Log($"Mod {Context.ModId} initialized");
        }

        public virtual void OnEnable()
        {
            Context.Log($"Mod {Context.ModId} enabled");
        }

        public virtual void OnDisable()
        {
            Context.Log($"Mod {Context.ModId} disabled");
        }

        public virtual void OnDestroy()
        {
            Context.Log($"Mod {Context.ModId} destroyed");
        }
    }

    /// <summary>
    /// 事件总线实现
    /// </summary>
    public class EventBus : IEventBus
    {
        private readonly Dictionary<string, List<Delegate>> handlers;
        private readonly Dictionary<object, List<(string eventId, Delegate handler)>> subscriberMap;
        private readonly ILogger logger;

        public EventBus(ILogger logger)
        {
            this.logger = logger;
            handlers = new Dictionary<string, List<Delegate>>();
            subscriberMap = new Dictionary<object, List<(string, Delegate)>>();
        }

        public void Subscribe<T>(string eventId, Action<T> handler) where T : IModEvent
        {
            if (!handlers.ContainsKey(eventId))
                handlers[eventId] = new List<Delegate>();

            handlers[eventId].Add(handler);

            // 记录订阅者
            var target = handler.Target;
            if (target != null)
            {
                if (!subscriberMap.ContainsKey(target))
                    subscriberMap[target] = new List<(string, Delegate)>();

                subscriberMap[target].Add((eventId, handler));
            }

            logger.Log($"Event subscribed: {eventId}");
        }

        public void Unsubscribe<T>(string eventId, Action<T> handler) where T : IModEvent
        {
            if (handlers.ContainsKey(eventId))
            {
                handlers[eventId].Remove(handler);
            }

            // 从订阅者映射中移除
            var target = handler.Target;
            if (target != null && subscriberMap.ContainsKey(target))
            {
                subscriberMap[target].RemoveAll(x => x.eventId == eventId && x.handler.Equals(handler));
            }
        }

        public void UnsubscribeAll(object subscriber)
        {
            if (subscriber == null) return;

            if (subscriberMap.TryGetValue(subscriber, out var subscriptions))
            {
                foreach (var (eventId, handler) in subscriptions)
                {
                    if (handlers.ContainsKey(eventId))
                    {
                        handlers[eventId].Remove(handler);
                    }
                }

                subscriberMap.Remove(subscriber);
                logger.Log($"Unsubscribed all events for {subscriber.GetType().Name}");
            }
        }

        public void Publish<T>(T eventData) where T : IModEvent
        {
            if (handlers.ContainsKey(eventData.EventId))
            {
                foreach (var handler in handlers[eventData.EventId].ToList())
                {
                    try
                    {
                        (handler as Action<T>)?.Invoke(eventData);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError($"Event handler error: {ex.Message}");
                    }
                }
            }
        }
    }

    /// <summary>
    /// 模组加载器 - 支持热重载
    /// </summary>
    public class ModLoader
    {
        private readonly ILogger logger;
        private readonly IPathProvider pathProvider;
        private readonly Dictionary<string, int> loadVersions;
        private readonly Dictionary<string, string> dllHashes;

        public ModLoader(ILogger logger, IPathProvider pathProvider)
        {
            this.logger = logger;
            this.pathProvider = pathProvider;
            this.loadVersions = new Dictionary<string, int>();
            this.dllHashes = new Dictionary<string, string>();
        }

        public async Task<LoadedMod> LoadModAsync(string modPath)
        {
            try
            {
                // 加载清单
                var manifestPath = Path.Combine(modPath, "manifest.json");
                if (!File.Exists(manifestPath))
                    throw new FileNotFoundException("Manifest not found");

                var manifestJson = await File.ReadAllTextAsync(manifestPath);
                var manifest = JsonConvert.DeserializeObject<ModManifest>(manifestJson);

                // 加载程序集（如果存在）
                Assembly assembly = null;
                var dllPath = Path.Combine(modPath, $"{manifest.id}.dll");

                if (File.Exists(dllPath))
                {
                    var dllBytes = await File.ReadAllBytesAsync(dllPath);

                    // 计算DLL哈希
                    var currentHash = ComputeHash(dllBytes);
                    bool hasChanged = !dllHashes.ContainsKey(manifest.id) || dllHashes[manifest.id] != currentHash;

                    if (hasChanged)
                    {
                        logger.Log($"[ModLoader] DLL changed for {manifest.id}, loading new version");
                        dllHashes[manifest.id] = currentHash;
                    }

                    // 尝试加载PDB文件
                    var pdbPath = Path.ChangeExtension(dllPath, ".pdb");
                    byte[] pdbBytes = null;
                    if (File.Exists(pdbPath))
                    {
                        pdbBytes = await File.ReadAllBytesAsync(pdbPath);
                    }

                    // 使用byte[]加载
                    if (pdbBytes != null)
                    {
                        assembly = Assembly.Load(dllBytes, pdbBytes);
                    }
                    else
                    {
                        assembly = Assembly.Load(dllBytes);
                    }

                    // 更新加载版本
                    if (!loadVersions.ContainsKey(manifest.id))
                        loadVersions[manifest.id] = 0;
                    loadVersions[manifest.id]++;

                    logger.Log($"[ModLoader] Loaded {manifest.id} v{manifest.version} (Load #{loadVersions[manifest.id]})");
                }

                return new LoadedMod
                {
                    Manifest = manifest,
                    Assembly = assembly,
                    RootPath = modPath,
                    LoadVersion = loadVersions.ContainsKey(manifest.id) ? loadVersions[manifest.id] : 0
                };
            }
            catch (Exception ex)
            {
                logger.LogError($"Failed to load mod from {modPath}: {ex.Message}");
                throw;
            }
        }

        private string ComputeHash(byte[] data)
        {
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                var hash = md5.ComputeHash(data);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }
    }

    /// <summary>
    /// 加载的模组数据
    /// </summary>
    public class LoadedMod
    {
        public ModManifest Manifest { get; set; }
        public Assembly Assembly { get; set; }
        public string RootPath { get; set; }
        public int LoadVersion { get; set; }
    }

    /// <summary>
    /// 模组管理器核心
    /// </summary>
    public class ModManagerCore
    {
        private readonly Dictionary<string, ModInstance> loadedMods;
        private readonly Dictionary<string, List<Assembly>> modAssemblyHistory;
        private readonly ModLoader loader;
        private readonly IEventBus eventBus;
        private readonly ILogger logger;

        public IReadOnlyDictionary<string, ModInstance> LoadedMods => loadedMods;
        public IEventBus EventBus => eventBus;

        public ModManagerCore(ILogger logger, IPathProvider pathProvider)
        {
            this.logger = logger;
            this.eventBus = new EventBus(logger);
            this.loader = new ModLoader(logger, pathProvider);
            this.loadedMods = new Dictionary<string, ModInstance>();
            this.modAssemblyHistory = new Dictionary<string, List<Assembly>>();
        }

        public async Task<ModInstance> LoadModAsync(string modPath)
        {
            var loadedMod = await loader.LoadModAsync(modPath);

            if (!modAssemblyHistory.ContainsKey(loadedMod.Manifest.id))
                modAssemblyHistory[loadedMod.Manifest.id] = new List<Assembly>();

            if (loadedMod.Assembly != null)
            {
                modAssemblyHistory[loadedMod.Manifest.id].Add(loadedMod.Assembly);
                logger.Log($"[ModManagerCore] Assembly history for {loadedMod.Manifest.id}: {modAssemblyHistory[loadedMod.Manifest.id].Count} versions");
            }

            var instance = new ModInstance
            {
                LoadedMod = loadedMod,
                State = ModState.Loaded,
                LoadTime = DateTime.Now
            };

            loadedMods[loadedMod.Manifest.id] = instance;
            logger.Log($"[ModManagerCore] Mod loaded: {loadedMod.Manifest.name} (v{loadedMod.LoadVersion})");

            return instance;
        }

        public async Task<ModInstance> ReloadModAsync(string modPath)
        {
            var loadedMod = await loader.LoadModAsync(modPath);
            var modId = loadedMod.Manifest.id;

            // 如果模组已加载，先通知它即将重载
            if (loadedMods.TryGetValue(modId, out var existingInstance))
            {
                if (existingInstance.ModBehaviour is IReloadable reloadable)
                {
                    try
                    {
                        reloadable.OnBeforeReload();
                    }
                    catch (Exception ex)
                    {
                        logger.LogError($"Error in OnBeforeReload for {modId}: {ex.Message}");
                    }
                }
            }

            if (!modAssemblyHistory.ContainsKey(modId))
                modAssemblyHistory[modId] = new List<Assembly>();

            if (loadedMod.Assembly != null)
            {
                modAssemblyHistory[modId].Add(loadedMod.Assembly);
                logger.Log($"[ModManagerCore] Assembly history for {modId}: {modAssemblyHistory[modId].Count} versions");
            }

            var instance = new ModInstance
            {
                LoadedMod = loadedMod,
                State = ModState.Loaded,
                LoadTime = DateTime.Now
            };

            loadedMods[modId] = instance;
            logger.Log($"[ModManagerCore] Mod reloaded: {loadedMod.Manifest.name} (v{loadedMod.LoadVersion})");

            return instance;
        }

        public void UnloadMod(string modId)
        {
            if (loadedMods.TryGetValue(modId, out var instance))
            {
                if (instance.ModBehaviour != null)
                {
                    try
                    {
                        instance.ModBehaviour.OnDisable();
                        instance.ModBehaviour.OnDestroy();
                    }
                    catch (Exception ex)
                    {
                        logger.LogError($"Error unloading mod {modId}: {ex.Message}");
                    }

                    eventBus.UnsubscribeAll(instance.ModBehaviour);
                }

                instance.State = ModState.NotLoaded;
                loadedMods.Remove(modId);

                logger.Log($"[ModManagerCore] Mod unloaded: {modId}");
            }
        }

        public void ClearMods()
        {
            foreach (var mod in loadedMods.Values)
            {
                mod.State = ModState.NotLoaded;
            }
            loadedMods.Clear();

            logger.Log($"[ModManagerCore] Mods cleared. Assembly history retained: {modAssemblyHistory.Count} mods");
        }

        public IMod CreateModBehaviour(ModInstance instance)
        {
            logger.Log($"[ModManagerCore] Creating behaviour for {instance.LoadedMod.Manifest.main_class}");

            var mainClass = instance.LoadedMod.Manifest.main_class;
            Type modType = null;

            if (instance.LoadedMod.Assembly != null)
            {
                logger.Log($"[ModManagerCore] Searching in assembly: {instance.LoadedMod.Assembly.FullName}");
                modType = instance.LoadedMod.Assembly.GetType(mainClass);

                if (modType == null)
                {
                    // 尝试不同的命名空间组合
                    var possibleNames = new[]
                    {
                        mainClass,
                        $"ExampleMods.{mainClass}",
                        $"ModSystem.{mainClass}",
                        mainClass.Contains('.') ? mainClass.Split('.').Last() : null
                    }.Where(n => n != null).Distinct();

                    foreach (var name in possibleNames)
                    {
                        modType = instance.LoadedMod.Assembly.GetType(name);
                        if (modType != null)
                        {
                            logger.Log($"[ModManagerCore] Found type with alternate name: {name}");
                            break;
                        }
                    }
                }
            }

            if (modType == null)
            {
                logger.LogError($"[ModManagerCore] Type {mainClass} not found");
                return null;
            }

            if (!typeof(IMod).IsAssignableFrom(modType))
            {
                logger.LogError($"[ModManagerCore] Type {mainClass} does not implement IMod");
                return null;
            }

            try
            {
                var mod = Activator.CreateInstance(modType) as IMod;
                instance.ModBehaviour = mod;
                logger.Log($"[ModManagerCore] Successfully created instance of {modType.FullName}");
                return mod;
            }
            catch (Exception ex)
            {
                logger.LogError($"[ModManagerCore] Failed to create instance: {ex.Message}");
                return null;
            }
        }
    }

    /// <summary>
    /// 模组实例
    /// </summary>
    public class ModInstance
    {
        public LoadedMod LoadedMod { get; set; }
        public ModState State { get; set; }
        public List<object> GameObjects { get; set; } = new List<object>();
        public DateTime LoadTime { get; set; }
        public IMod ModBehaviour { get; set; }
    }

    /// <summary>
    /// 模组状态
    /// </summary>
    public enum ModState
    {
        NotLoaded,
        Loading,
        Loaded,
        Active,
        Error
    }

    /// <summary>
    /// 模组上下文实现
    /// </summary>
    public class ModContext : IModContext
    {
        public string ModId { get; set; }
        public IEventBus EventBus { get; set; }
        public IUIFactory UI { get; set; }
        public ILogger Logger { get; set; }

        public void Log(string message)
        {
            Logger?.Log($"[{ModId}] {message}");
        }

        public void LogError(string message)
        {
            Logger?.LogError($"[{ModId}] {message}");
        }
    }

    /// <summary>
    /// 默认的空UI工厂（用于非Unity环境的测试）
    /// </summary>
    public class NullUIFactory : IUIFactory
    {
        public string CreateButton(string name, string text, Action onClick) => Guid.NewGuid().ToString();
        public string CreateText(string name, string text) => Guid.NewGuid().ToString();
        public string CreateImage(string name) => Guid.NewGuid().ToString();
        public string CreatePanel(string name) => Guid.NewGuid().ToString();
        public void SetProperty(string elementId, string property, object value) { }
        public void DestroyElement(string elementId) { }
        public T GetProperty<T>(string elementId, string property) => default(T);
    }

    #endregion

    #region UI辅助类

    /// <summary>
    /// UI属性构建器 - 提供流畅的API
    /// </summary>
    public static class UIProperties
    {
        public static object Size(float width, float height) => (width, height);
        public static object Position(float x, float y) => (x, y);
        public static object Color(float r, float g, float b, float a = 1f)
        {
            return new Dictionary<string, object>
            {
                { "r", r },
                { "g", g },
                { "b", b },
                { "a", a }
            };
        }
    }

    /// <summary>
    /// IUIFactory的扩展方法
    /// </summary>
    public static class UIFactoryExtensions
    {
        public static string CreateButton(this IUIFactory ui, string name, string text, Action onClick,
            float width = 160f, float height = 30f)
        {
            var buttonId = ui.CreateButton(name, text, onClick);
            ui.SetProperty(buttonId, "size", (width, height));
            return buttonId;
        }

        public static void SetProperties(this IUIFactory ui, string elementId, params (string property, object value)[] properties)
        {
            foreach (var (property, value) in properties)
            {
                ui.SetProperty(elementId, property, value);
            }
        }

        public static void SetTransform(this IUIFactory ui, string elementId, float x, float y, float width, float height)
        {
            ui.SetProperty(elementId, "position", (x, y));
            ui.SetProperty(elementId, "size", (width, height));
        }
    }

    #endregion
}