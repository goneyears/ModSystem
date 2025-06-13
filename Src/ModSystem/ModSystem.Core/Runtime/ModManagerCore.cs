using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Reflection;

namespace ModSystem.Core
{
    /// <summary>
    /// 平台无关的模组管理器核心
    /// 负责模组的加载、管理和生命周期控制
    /// </summary>
    public class ModManagerCore
    {
        private readonly ILogger logger;
        private readonly IPathProvider pathProvider;
        private readonly IEventBus eventBus;
        private readonly IServiceRegistry serviceRegistry;
        private readonly ModLoader modLoader;
        private readonly Dictionary<string, ModInstance> modInstances;
        private readonly SecurityManager securityManager;
        // 新增字段
        private readonly Dictionary<string, List<Assembly>> modAssemblyHistory;

        /// <summary>
        /// 最后的错误信息
        /// </summary>
        public string LastError { get; private set; }

        /// <summary>
        /// 验证错误列表
        /// </summary>
        public List<string> ValidationErrors { get; private set; } = new List<string>();

        /// <summary>
        /// 创建模组管理器核心
        /// </summary>
        public ModManagerCore(
            ILogger logger,
            IPathProvider pathProvider,
            IEventBus eventBus,
            IServiceRegistry serviceRegistry)
        {
            this.logger = logger;
            this.pathProvider = pathProvider;
            this.eventBus = eventBus;
            this.serviceRegistry = serviceRegistry;

            var securityConfig = LoadSecurityConfig();
            securityManager = new SecurityManager(securityConfig, logger);

            modLoader = new ModLoader(logger, pathProvider, securityManager);
            modInstances = new Dictionary<string, ModInstance>();
            this.modAssemblyHistory = new Dictionary<string, List<Assembly>>();


        }

        /// <summary>
        /// 加载模组
        /// </summary>
        /// <param name="modPath">模组路径</param>
        /// <returns>是否成功加载</returns>
        public async Task<bool> LoadMod(string modPath)
        {
            try
            {
                ValidationErrors.Clear();

                // 加载模组
                var loadedMod = await modLoader.LoadModAsync(modPath);

                // 创建模组实例
                var instance = CreateModInstance(loadedMod);
                modInstances[loadedMod.Manifest.id] = instance;

                // 初始化行为
                foreach (var behaviour in loadedMod.Behaviours)
                {
                    var context = CreateModContext(loadedMod, instance);
                    behaviour.OnInitialize(context);
                }

                // 更新状态
                instance.State = ModState.Active;

                // 发布模组加载事件
                eventBus.Publish(new ModLoadedEvent
                {
                    ModId = loadedMod.Manifest.id,
                    ModName = loadedMod.Manifest.name,
                    Version = loadedMod.Manifest.version
                });

                logger.Log($"Mod loaded: {loadedMod.Manifest.name} v{loadedMod.Manifest.version}");
                return true;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                logger.LogError($"Failed to load mod: {ex}");
                return false;
            }
        }

        /// <summary>
        /// 从目录加载所有模组
        /// </summary>
        public async Task LoadModsFromDirectory(string directory)
        {
            if (!Directory.Exists(directory))
            {
                logger.LogWarning($"Mods directory not found: {directory}");
                return;
            }

            foreach (var modDir in Directory.GetDirectories(directory))
            {
                await LoadMod(modDir);
            }
        }

        /// <summary>
        /// 加载模组包文件
        /// </summary>
        public async Task<bool> LoadModPackage(string packagePath)
        {
            try
            {
                var tempPath = Path.Combine(pathProvider.GetTempPath(), Path.GetFileNameWithoutExtension(packagePath));

                // 解压包文件
                System.IO.Compression.ZipFile.ExtractToDirectory(packagePath, tempPath);

                // 加载模组
                var result = await LoadMod(tempPath);

                if (result)
                {
                    // 标记为临时模组
                    var manifest = await LoadManifest(tempPath);
                    if (modInstances.TryGetValue(manifest.id, out var instance))
                    {
                        instance.LoadedMod.IsTemporary = true;
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                logger.LogError($"Failed to load mod package: {ex}");
                return false;
            }
        }

        /// <summary>
        /// 从包目录加载所有模组包
        /// </summary>
        public async Task LoadModPackagesFromDirectory(string directory)
        {
            if (!Directory.Exists(directory))
            {
                logger.LogWarning($"Mod packages directory not found: {directory}");
                return;
            }

            foreach (var packageFile in Directory.GetFiles(directory, "*.modpack"))
            {
                await LoadModPackage(packageFile);
            }
        }


        /// <summary>
        /// 加载模组（支持热重载）
        /// </summary>
        public async Task<bool> LoadModWithHotReload(string modPath)
        {
            try
            {
                ValidationErrors.Clear();

                // 如果模组已加载，先通知它即将重载
                var modManifest = await LoadManifest(modPath);
                if (modInstances.TryGetValue(modManifest.id, out var existingInstance))
                {
                    NotifyBeforeReload(existingInstance);
                }

                // 使用支持热重载的加载方法
                var loadedMod = await modLoader.LoadModWithHotReloadAsync(modPath);

                // 记录程序集历史
                if (!modAssemblyHistory.ContainsKey(loadedMod.Manifest.id))
                    modAssemblyHistory[loadedMod.Manifest.id] = new List<Assembly>();

                if (loadedMod.Assembly != null)
                {
                    modAssemblyHistory[loadedMod.Manifest.id].Add(loadedMod.Assembly);
                    logger.Log($"[ModManagerCore] Assembly history for {loadedMod.Manifest.id}: {modAssemblyHistory[loadedMod.Manifest.id].Count} versions");
                }

                // 创建模组实例
                var instance = CreateModInstance(loadedMod);
                modInstances[loadedMod.Manifest.id] = instance;

                // 初始化模组（支持IMod接口）
                if (loadedMod.ModInstance != null)
                {
                    var context = CreateModContext(loadedMod, instance);
                    loadedMod.ModInstance.OnInitialize(context);
                    loadedMod.ModInstance.OnEnable();

                    // 如果支持热重载，调用OnAfterReload
                    if (loadedMod.ModInstance is IReloadable reloadable)
                    {
                        reloadable.OnAfterReload();
                    }
                }

                // 初始化行为（支持IModBehaviour接口）
                foreach (var behaviour in loadedMod.Behaviours)
                {
                    var context = CreateModContext(loadedMod, instance);
                    behaviour.OnInitialize(context);
                }

                // 更新状态
                instance.State = ModState.Active;

                // 发布模组加载事件
                eventBus.Publish(new ModLoadedEvent
                {
                    ModId = loadedMod.Manifest.id,
                    ModName = loadedMod.Manifest.name,
                    Version = loadedMod.Manifest.version
                });

                logger.Log($"Mod loaded: {loadedMod.Manifest.name} v{loadedMod.Manifest.version}");
                return true;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                logger.LogError($"Failed to load mod: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 通知模组即将重载
        /// </summary>
        private void NotifyBeforeReload(ModInstance instance)
        {
            if (instance.LoadedMod.ModInstance is IReloadable reloadable)
            {
                try
                {
                    reloadable.OnBeforeReload();
                }
                catch (Exception ex)
                {
                    logger.LogError($"Error in OnBeforeReload for {instance.LoadedMod.Manifest.id}: {ex.Message}");
                }
            }
        }


        /// <summary>
        /// 卸载模组（更新以支持IMod）
        /// </summary>
        public new async Task<bool> UnloadMod(string modId)
        {
            if (!modInstances.TryGetValue(modId, out var instance))
                return false;

            try
            {
                // 禁用和销毁IMod实例
                if (instance.LoadedMod.ModInstance != null)
                {
                    instance.LoadedMod.ModInstance.OnDisable();
                    instance.LoadedMod.ModInstance.OnDestroy();

                    // 取消订阅事件
                    eventBus.UnsubscribeAll(instance.LoadedMod.ModInstance);
                }

                // 禁用和销毁IModBehaviour实例
                foreach (var behaviour in instance.LoadedMod.Behaviours)
                {
                    behaviour.OnDestroy();
                    eventBus.UnsubscribeAll(behaviour);
                }

                // 清理资源
                instance.State = ModState.NotLoaded;
                modInstances.Remove(modId);

                // 发布卸载事件
                eventBus.Publish(new ModUnloadedEvent
                {
                    ModId = modId
                });

                logger.Log($"Mod unloaded: {modId}");
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError($"Failed to unload mod {modId}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取模组实例
        /// </summary>
        public ModInstance GetModInstance(string modId)
        {
            return modInstances.TryGetValue(modId, out var instance) ? instance : null;
        }

        /// <summary>
        /// 获取所有已加载的模组
        /// </summary>
        public IEnumerable<ModInstance> GetLoadedMods()
        {
            return modInstances.Values;
        }

        /// <summary>
        /// 更新所有模组
        /// </summary>
        public void UpdateMods(float deltaTime)
        {
            foreach (var instance in modInstances.Values.Where(i => i.State == ModState.Active))
            {
                foreach (var behaviour in instance.LoadedMod.Behaviours)
                {
                    try
                    {
                        behaviour.OnUpdate(deltaTime);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError($"Error updating behaviour {behaviour.BehaviourId}: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// 创建模组实例
        /// </summary>
        private ModInstance CreateModInstance(LoadedMod loadedMod)
        {
            return new ModInstance
            {
                LoadedMod = loadedMod,
                State = ModState.Loaded,
                LoadTime = DateTime.Now
            };
        }

        /// <summary>
        /// 创建模组上下文
        /// </summary>
        private IModContext CreateModContext(LoadedMod loadedMod, ModInstance instance)
        {
            var securityContext = securityManager.CreateContext(
                loadedMod.Manifest.id,
                loadedMod.Manifest.permissions
            );

            return new ModContext
            {
                ModId = loadedMod.Manifest.id,
                EventBus = eventBus,
                Services = serviceRegistry,
                Logger = logger,
                SecurityContext = securityContext,
                API = CreateModAPI(loadedMod)
            };
        }

        /// <summary>
        /// 创建模组API
        /// </summary>
        private IModAPI CreateModAPI(LoadedMod loadedMod)
        {
            return new ModAPI
            {
                RequestResponse = new RequestResponseManager(eventBus),
                ObjectFactory = null, // 需要在Unity层实现
                Utilities = new ModUtilities()
            };
        }


        /// <summary>
        /// 创建模组行为（支持IMod）
        /// </summary>
        public IMod CreateModBehaviour(ModInstance instance)
        {
            logger.Log($"[ModManagerCore] Creating behaviour for {instance.LoadedMod.Manifest.main_class}");

            if (instance.LoadedMod.ModInstance != null)
            {
                return instance.LoadedMod.ModInstance;
            }

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
                instance.LoadedMod.ModInstance = mod;
                logger.Log($"[ModManagerCore] Successfully created instance of {modType.FullName}");
                return mod;
            }
            catch (Exception ex)
            {
                logger.LogError($"[ModManagerCore] Failed to create instance: {ex.Message}");
                return null;
            }
        }



        /// <summary>
        /// 加载安全配置
        /// </summary>
        private SecurityConfig LoadSecurityConfig()
        {
            var configPath = Path.Combine(pathProvider.GetConfigPath(), "security_config.json");

            if (File.Exists(configPath))
            {
                try
                {
                    var json = File.ReadAllText(configPath);
                    return Newtonsoft.Json.JsonConvert.DeserializeObject<SecurityConfig>(json);
                }
                catch (Exception ex)
                {
                    logger.LogError($"Failed to load security config: {ex.Message}");
                }
            }

            // 返回默认配置
            return new SecurityConfig();
        }

        /// <summary>
        /// 加载清单文件
        /// </summary>
        private async Task<ModManifest> LoadManifest(string modPath)
        {
            var manifestPath = Path.Combine(modPath, "manifest.json");
            var json = await File.ReadAllTextAsync(manifestPath);
            return Newtonsoft.Json.JsonConvert.DeserializeObject<ModManifest>(json);
        }

        /// <summary>
        /// 重新加载模组（支持热重载）
        /// </summary>
        public async Task<ModInstance> ReloadModAsync(string modPath)
        {
            try
            {
                ValidationErrors.Clear();

                // 获取模组清单以确定ID
                var manifest = await LoadManifest(modPath);
                var modId = manifest.id;

                // 如果模组已加载，先通知它即将重载
                if (modInstances.TryGetValue(modId, out var existingInstance))
                {
                    NotifyBeforeReload(existingInstance);
                }

                // 使用支持热重载的加载方法
                var loadedMod = await modLoader.LoadModWithHotReloadAsync(modPath);

                // 记录程序集历史
                if (!modAssemblyHistory.ContainsKey(loadedMod.Manifest.id))
                    modAssemblyHistory[loadedMod.Manifest.id] = new List<Assembly>();

                if (loadedMod.Assembly != null)
                {
                    modAssemblyHistory[loadedMod.Manifest.id].Add(loadedMod.Assembly);
                    logger.Log($"[ModManagerCore] Assembly history for {loadedMod.Manifest.id}: {modAssemblyHistory[loadedMod.Manifest.id].Count} versions");
                }

                // 创建模组实例
                var instance = CreateModInstance(loadedMod);
                modInstances[loadedMod.Manifest.id] = instance;

                // 初始化模组（支持IMod接口）
                if (loadedMod.ModInstance != null)
                {
                    var context = CreateModContext(loadedMod, instance);
                    loadedMod.ModInstance.OnInitialize(context);
                    loadedMod.ModInstance.OnEnable();

                    // 如果支持热重载，调用OnAfterReload
                    if (loadedMod.ModInstance is IReloadable reloadable)
                    {
                        reloadable.OnAfterReload();
                    }

                    instance.ModBehaviour = loadedMod.ModInstance;
                }

                // 初始化行为（支持IModBehaviour接口）
                foreach (var behaviour in loadedMod.Behaviours)
                {
                    var context = CreateModContext(loadedMod, instance);
                    behaviour.OnInitialize(context);
                }

                // 更新状态
                instance.State = ModState.Active;

                // 发布模组加载事件
                eventBus.Publish(new ModLoadedEvent
                {
                    ModId = loadedMod.Manifest.id,
                    ModName = loadedMod.Manifest.name,
                    Version = loadedMod.Manifest.version
                });

                logger.Log($"Mod reloaded: {loadedMod.Manifest.name} v{loadedMod.Manifest.version} (Load #{loadedMod.LoadVersion})");

                return instance;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                logger.LogError($"Failed to reload mod: {ex.Message}");
                throw;
            }
        }
    }
}