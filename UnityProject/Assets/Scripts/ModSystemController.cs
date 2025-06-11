// ModSystem.Unity/ModSystemController.cs
using UnityEngine;
using ModSystem.Core;
using System.IO;
using System.Threading.Tasks;
using System;

namespace ModSystem.Unity
{
    /// <summary>
    /// Unity模组系统主控制器
    /// 负责初始化和管理整个模组系统，作为Unity和Core层之间的桥梁
    /// </summary>
    [AddComponentMenu("ModSystem/Mod System Controller")]
    [DisallowMultipleComponent]
    public class ModSystemController : MonoBehaviour
    {
        #region Singleton
        private static ModSystemController instance;
        
        /// <summary>
        /// 获取ModSystemController单例实例
        /// </summary>
        public static ModSystemController Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<ModSystemController>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("ModSystemController");
                        instance = go.AddComponent<ModSystemController>();
                        DontDestroyOnLoad(go);
                    }
                }
                return instance;
            }
        }
        #endregion

        #region Core Components
        private ModManagerCore modManagerCore;
        private ModEventBus eventBus;
        private ModServiceRegistry serviceRegistry;
        private CommunicationRouter router;
        private UnityLogger logger;
        private UnityPathProvider pathProvider;
        private RequestResponseManager requestResponseManager;
        #endregion

        #region Properties
        /// <summary>
        /// 获取事件总线实例
        /// </summary>
        public IEventBus EventBus => eventBus;
        
        /// <summary>
        /// 获取服务注册表实例
        /// </summary>
        public IServiceRegistry ServiceRegistry => serviceRegistry;
        
        /// <summary>
        /// 获取模组管理器核心实例
        /// </summary>
        public ModManagerCore ModManagerCore => modManagerCore;
        
        /// <summary>
        /// 获取请求响应管理器
        /// </summary>
        public IRequestResponseManager RequestResponseManager => requestResponseManager;
        #endregion

        #region Unity Lifecycle
        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            instance = this;
            DontDestroyOnLoad(gameObject);
            
            InitializeSystem();
        }
        
        void Start()
        {
            // 异步加载模组
            LoadModsAsync();
        }
        
        void Update()
        {
            // 更新所有活动的模组
            if (modManagerCore != null)
            {
                modManagerCore.UpdateMods(Time.deltaTime);
            }
        }
        
        void OnDestroy()
        {
            CleanupSystem();
            
            if (instance == this)
            {
                instance = null;
            }
        }
        
        void OnApplicationPause(bool pauseStatus)
        {
            // 处理应用暂停/恢复
            if (pauseStatus)
            {
                PauseAllMods();
            }
            else
            {
                ResumeAllMods();
            }
        }
        
        void OnApplicationQuit()
        {
            // 保存状态并清理资源
            SaveModStates();
            UnloadAllMods();
        }
        #endregion

        #region Initialization
        /// <summary>
        /// 初始化模组系统
        /// </summary>
        private void InitializeSystem()
        {
            try
            {
                // 创建Unity实现
                logger = new UnityLogger("[ModSystem]");
                pathProvider = new UnityPathProvider();
                
                // 初始化核心组件
                var eventLogger = new UnityEventLogger();
                eventBus = new ModEventBus(eventLogger);
                serviceRegistry = new ModServiceRegistry(eventBus, logger);
                requestResponseManager = new RequestResponseManager(eventBus);
                
                // 创建核心模组管理器
                modManagerCore = new ModManagerCore(logger, pathProvider, eventBus, serviceRegistry);
                
                // 创建Unity包装器
                var modManager = gameObject.AddComponent<ModManager>();
                modManager.Initialize(modManagerCore);
                
                // 加载配置
                LoadConfigurations();
                
                logger.Log("ModSystem initialized successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to initialize ModSystem: {ex}");
                enabled = false;
            }
        }
        
        /// <summary>
        /// 加载系统配置
        /// </summary>
        private void LoadConfigurations()
        {
            // 加载通信配置
            LoadCommunicationConfig();
            
            // 加载安全配置
            LoadSecurityConfig();
            
            // 加载系统设置
            LoadSystemSettings();
        }
        
        /// <summary>
        /// 加载通信配置
        /// </summary>
        private void LoadCommunicationConfig()
        {
            string configPath = Path.Combine(pathProvider.GetConfigPath(), "communication_config.json");
            
            if (File.Exists(configPath))
            {
                try
                {
                    string configJson = File.ReadAllText(configPath);
                    router = new CommunicationRouter(eventBus, configJson, logger);
                    logger.Log("Communication config loaded");
                }
                catch (Exception ex)
                {
                    logger.LogError($"Failed to load communication config: {ex.Message}");
                }
            }
            else
            {
                logger.LogWarning("Communication config not found, using defaults");
            }
        }
        
        /// <summary>
        /// 加载安全配置
        /// </summary>
        private void LoadSecurityConfig()
        {
            string configPath = Path.Combine(pathProvider.GetConfigPath(), "security_config.json");
            
            if (!File.Exists(configPath))
            {
                // 创建默认配置
                CreateDefaultSecurityConfig(configPath);
            }
        }
        
        /// <summary>
        /// 加载系统设置
        /// </summary>
        private void LoadSystemSettings()
        {
            // 从PlayerPrefs或配置文件加载设置
            // 例如：调试模式、性能设置等
        }
        #endregion

        #region Mod Loading
        /// <summary>
        /// 异步加载所有模组
        /// </summary>
        private async void LoadModsAsync()
        {
            try
            {
                logger.Log("Starting mod loading...");
                
                // 加载内置模组
                await LoadBuiltInMods();
                
                // 加载开发中的模组
                await modManagerCore.LoadModsFromDirectory(pathProvider.GetModsPath());
                
                // 加载外部模组包
                string packagePath = Path.Combine(Application.streamingAssetsPath, "ModPackages");
                await modManagerCore.LoadModPackagesFromDirectory(packagePath);
                
                logger.Log("Mod loading completed");
                
                // 发布系统就绪事件
                eventBus.Publish(new SystemReadyEvent
                {
                    SenderId = "ModSystem",
                    LoadedModCount = modManagerCore.GetLoadedMods().Count()
                });
            }
            catch (Exception ex)
            {
                logger.LogError($"Error during mod loading: {ex}");
            }
        }
        
        /// <summary>
        /// 加载内置模组
        /// </summary>
        private async Task LoadBuiltInMods()
        {
            // 这里可以加载一些系统必需的内置模组
            await Task.CompletedTask;
        }
        #endregion

        #region Mod Management
        /// <summary>
        /// 暂停所有模组
        /// </summary>
        private void PauseAllMods()
        {
            foreach (var modInstance in modManagerCore.GetLoadedMods())
            {
                if (modInstance.State == ModState.Active)
                {
                    modInstance.State = ModState.Paused;
                }
            }
            
            eventBus.Publish(new ModSystemPausedEvent { SenderId = "ModSystem" });
        }
        
        /// <summary>
        /// 恢复所有模组
        /// </summary>
        private void ResumeAllMods()
        {
            foreach (var modInstance in modManagerCore.GetLoadedMods())
            {
                if (modInstance.State == ModState.Paused)
                {
                    modInstance.State = ModState.Active;
                }
            }
            
            eventBus.Publish(new ModSystemResumedEvent { SenderId = "ModSystem" });
        }
        
        /// <summary>
        /// 卸载所有模组
        /// </summary>
        private void UnloadAllMods()
        {
            var modIds = modManagerCore.GetLoadedMods()
                .Select(m => m.LoadedMod.Manifest.id)
                .ToList();
            
            foreach (var modId in modIds)
            {
                modManagerCore.UnloadMod(modId);
            }
        }
        
        /// <summary>
        /// 保存模组状态
        /// </summary>
        private void SaveModStates()
        {
            // 实现模组状态持久化
            try
            {
                var states = new ModSystemState
                {
                    SaveTime = DateTime.Now,
                    LoadedMods = modManagerCore.GetLoadedMods()
                        .Select(m => new ModStateInfo
                        {
                            ModId = m.LoadedMod.Manifest.id,
                            Version = m.LoadedMod.Manifest.version,
                            State = m.State
                        }).ToList()
                };
                
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(states, Newtonsoft.Json.Formatting.Indented);
                string savePath = Path.Combine(pathProvider.GetPersistentDataPath(), "mod_states.json");
                File.WriteAllText(savePath, json);
                
                logger.Log("Mod states saved");
            }
            catch (Exception ex)
            {
                logger.LogError($"Failed to save mod states: {ex.Message}");
            }
        }
        #endregion

        #region Cleanup
        /// <summary>
        /// 清理系统资源
        /// </summary>
        private void CleanupSystem()
        {
            // 清理事件订阅
            if (requestResponseManager != null)
            {
                requestResponseManager.Dispose();
            }
            
            // 清理临时文件
            CleanupTempFiles();
            
            logger?.Log("ModSystem cleaned up");
        }
        
        /// <summary>
        /// 清理临时文件
        /// </summary>
        private void CleanupTempFiles()
        {
            try
            {
                string tempPath = pathProvider.GetTempPath();
                if (Directory.Exists(tempPath))
                {
                    Directory.Delete(tempPath, true);
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"Failed to cleanup temp files: {ex.Message}");
            }
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// 创建默认安全配置
        /// </summary>
        private void CreateDefaultSecurityConfig(string path)
        {
            var defaultConfig = new SecurityConfig
            {
                RequireSignedMods = false, // 开发环境默认不需要签名
                AllowedModPaths = new List<string> { pathProvider.GetModsPath() }
            };
            
            try
            {
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(defaultConfig, Newtonsoft.Json.Formatting.Indented);
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                logger.LogError($"Failed to create default security config: {ex.Message}");
            }
        }
        #endregion
    }

    #region Event Definitions
    /// <summary>
    /// 系统就绪事件
    /// </summary>
    public class SystemReadyEvent : IModEvent
    {
        public string EventId => "system_ready";
        public string SenderId { get; set; }
        public DateTime Timestamp { get; set; }
        public int LoadedModCount { get; set; }
    }
    
    /// <summary>
    /// 模组系统暂停事件
    /// </summary>
    public class ModSystemPausedEvent : IModEvent
    {
        public string EventId => "modsystem_paused";
        public string SenderId { get; set; }
        public DateTime Timestamp { get; set; }
    }
    
    /// <summary>
    /// 模组系统恢复事件
    /// </summary>
    public class ModSystemResumedEvent : IModEvent
    {
        public string EventId => "modsystem_resumed";
        public string SenderId { get; set; }
        public DateTime Timestamp { get; set; }
    }
    #endregion

    #region Data Structures
    /// <summary>
    /// 模组系统状态
    /// </summary>
    [Serializable]
    public class ModSystemState
    {
        public DateTime SaveTime { get; set; }
        public List<ModStateInfo> LoadedMods { get; set; }
    }
    
    /// <summary>
    /// 模组状态信息
    /// </summary>
    [Serializable]
    public class ModStateInfo
    {
        public string ModId { get; set; }
        public string Version { get; set; }
        public ModState State { get; set; }
    }
    #endregion
} 