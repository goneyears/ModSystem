// UnityProject/Assets/Scripts/ModSystemController.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using ModSystem.Core;

namespace ModSystem.Unity
{
    /// <summary>
    /// 模组系统控制器
    /// Unity中模组系统的主入口点
    /// </summary>
    [AddComponentMenu("ModSystem/Mod System Controller")]
    public class ModSystemController : MonoBehaviour
    {
        #region Singleton
        private static ModSystemController instance;
        
        /// <summary>
        /// 获取控制器单例
        /// </summary>
        public static ModSystemController Instance => instance;
        #endregion

        #region Inspector Fields
        [Header("Configuration")]
        [SerializeField] private string modsPath = "Mods";
        [SerializeField] private string configPath = "ModConfigs";
        [SerializeField] private bool autoLoadMods = true;
        [SerializeField] private bool debugMode = false;
        
        [Header("Hot Reload Settings")]
        [SerializeField] private bool enableHotReload = true;
        [SerializeField] private KeyCode reloadKey = KeyCode.F5;
        [SerializeField] private KeyCode toggleDebugKey = KeyCode.F12;
        [SerializeField] private float reloadCheckInterval = 1.0f;
        
        [Header("Performance")]
        [SerializeField] private int maxConcurrentLoads = 3;
        [SerializeField] private float modUpdateInterval = 0f;
        
        [Header("UI Settings")]
        [SerializeField] private Canvas uiCanvas;
        [SerializeField] private bool createUICanvas = true;
        
        [Header("Runtime Info")]
        [SerializeField] private int loadedModsCount = 0;
        [SerializeField] private List<string> loadedModIds = new List<string>();
        #endregion

        #region Private Fields
        private ModManagerCore modManagerCore;
        private ModManager modManager;
        private ModUIFactory uiFactory;
        private UnityObjectFactory objectFactory;
        private UnityLogger logger;
        private UnityPathProvider pathProvider;
        private IEventBus eventBus;
        private SecurityManager securityManager;
        private bool isInitialized;
        private float lastReloadCheck = 0;
        #endregion

        #region Properties
        /// <summary>
        /// 获取模组管理器核心
        /// </summary>
        public ModManagerCore ModManagerCore => modManagerCore;
        
        /// <summary>
        /// 获取Unity模组管理器
        /// </summary>
        public ModManager ModManager => modManager;
        
        /// <summary>
        /// 获取UI工厂
        /// </summary>
        public ModUIFactory UIFactory => uiFactory;
        
        /// <summary>
        /// 获取对象工厂
        /// </summary>
        public UnityObjectFactory ObjectFactory => objectFactory;
        
        /// <summary>
        /// 获取事件总线
        /// </summary>
        public IEventBus EventBus => eventBus;
        
        /// <summary>
        /// 获取日志记录器
        /// </summary>
        public ILogger Logger => logger;
        
        /// <summary>
        /// 获取是否已初始化
        /// </summary>
        public bool IsInitialized => isInitialized;
        #endregion

        #region Unity Lifecycle
        void Awake()
        {
            // 设置单例
            if (instance != null && instance != this)
            {
                Debug.LogWarning("[ModSystemController] Multiple instances detected, destroying duplicate");
                Destroy(gameObject);
                return;
            }
            
            instance = this;
            DontDestroyOnLoad(gameObject);
            
            // 初始化系统
            InitializeSystem();
        }
        
        async void Start()
        {
            if (autoLoadMods && isInitialized)
            {
                await LoadAllMods();
            }
        }
        
        void Update()
        {
            if (!isInitialized) return;
            
            // 热键处理
            if (enableHotReload && Input.GetKeyDown(reloadKey))
            {
                ReloadMods();
            }
            
            if (Input.GetKeyDown(toggleDebugKey))
            {
                debugMode = !debugMode;
                logger.Log($"Debug mode: {debugMode}");
            }
            
            // 自动重载检查（开发模式）
            #if UNITY_EDITOR
            if (enableHotReload && debugMode && Time.time - lastReloadCheck > reloadCheckInterval)
            {
                lastReloadCheck = Time.time;
                CheckForModChanges();
            }
            #endif
            
            // 更新模组
            if (modUpdateInterval <= 0 || Time.frameCount % Mathf.Max(1, Mathf.RoundToInt(modUpdateInterval * 60)) == 0)
            {
                modManagerCore?.UpdateMods(Time.deltaTime);
            }
        }
        
        void OnApplicationPause(bool pauseStatus)
        {
            if (!isInitialized) return;
            
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
            if (!isInitialized) return;
            
            logger?.Log("[ModSystemController] Application quitting, unloading mods");
            UnloadAllMods();
        }
        
        void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
            
            // 清理资源
            Cleanup();
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
                // 创建基础组件
                logger = new UnityLogger("[ModSystem] ");
                pathProvider = new UnityPathProvider(modsPath, configPath);
                eventBus = new EventBus(logger);
                
                // 创建安全管理器
                var securityConfig = LoadSecurityConfig();
                securityManager = new SecurityManager(securityConfig, logger);
                
                // 创建核心管理器
                modManagerCore = new ModManagerCore(logger, pathProvider, eventBus);
                
                // 创建Unity层工厂
                CreateFactories();
                
                // 创建Unity层管理器
                modManager = gameObject.AddComponent<ModManager>();
                modManager.Initialize(modManagerCore, uiFactory, objectFactory);
                
                // 订阅事件
                SubscribeToEvents();
                
                // 更新运行时信息
                UpdateRuntimeInfo();
                
                isInitialized = true;
                logger.Log("System initialized successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ModSystemController] Failed to initialize: {ex}");
                isInitialized = false;
            }
        }
        
        /// <summary>
        /// 创建工厂实例
        /// </summary>
        private void CreateFactories()
        {
            // 创建或查找UI画布
            if (createUICanvas && uiCanvas == null)
            {
                var canvasObj = new GameObject("ModSystemCanvas");
                canvasObj.transform.SetParent(transform);
                uiCanvas = canvasObj.AddComponent<Canvas>();
                uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                uiCanvas.sortingOrder = 100;
                
                // 添加必要的UI组件
                canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
                canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            }
            
            // 创建工厂
            if (uiCanvas != null)
            {
                uiFactory = new ModUIFactory(uiCanvas, logger);
            }
            
            objectFactory = new UnityObjectFactory(logger);
        }
        
        /// <summary>
        /// 订阅系统事件
        /// </summary>
        private void SubscribeToEvents()
        {
            eventBus.Subscribe<ModLoadedEvent>(OnModLoaded);
            eventBus.Subscribe<ModUnloadedEvent>(OnModUnloaded);
            eventBus.Subscribe<ModErrorEvent>(OnModError);
        }
        
        /// <summary>
        /// 加载安全配置
        /// </summary>
        private SecurityConfig LoadSecurityConfig()
        {
            var configPath = Path.Combine(pathProvider.GetConfigPath(), "security.json");
            
            if (File.Exists(configPath))
            {
                try
                {
                    var json = File.ReadAllText(configPath);
                    return Newtonsoft.Json.JsonConvert.DeserializeObject<SecurityConfig>(json);
                }
                catch (Exception ex)
                {
                    logger?.LogError($"Failed to load security config: {ex.Message}");
                }
            }
            
            // 返回默认配置
            return new SecurityConfig
            {
                RequireSignedMods = false,
                AllowedModPaths = new[] { pathProvider.GetModsPath() },
                MaxModSize = 100 * 1024 * 1024 // 100MB
            };
        }
        #endregion

        #region Mod Loading
        /// <summary>
        /// 加载所有模组
        /// </summary>
        private async Task LoadAllMods()
        {
            logger.Log("Loading all mods...");
            
            try
            {
                // 加载内置模组
                await LoadBuiltInMods();
                
                // 加载开发中的模组
                var modsPath = pathProvider.GetModsPath();
                if (Directory.Exists(modsPath))
                {
                    var modDirs = Directory.GetDirectories(modsPath);
                    logger.Log($"Found {modDirs.Length} mod directories");
                    
                    // 限制并发加载
                    var loadTasks = new List<Task>();
                    var semaphore = new System.Threading.SemaphoreSlim(maxConcurrentLoads);
                    
                    foreach (var modDir in modDirs)
                    {
                        var task = LoadModWithConcurrencyControl(modDir, semaphore);
                        loadTasks.Add(task);
                    }
                    
                    await Task.WhenAll(loadTasks);
                }
                else
                {
                    logger.LogWarning($"Mods directory not found: {modsPath}");
                    Directory.CreateDirectory(modsPath);
                }
                
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
        /// 使用并发控制加载模组
        /// </summary>
        private async Task LoadModWithConcurrencyControl(string modDir, System.Threading.SemaphoreSlim semaphore)
        {
            await semaphore.WaitAsync();
            
            try
            {
                if (enableHotReload)
                {
                    await modManager.LoadAndActivateMod(modDir);
                }
                else
                {
                    await modManagerCore.LoadMod(modDir);
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Failed to load mod from {modDir}: {ex.Message}");
            }
            finally
            {
                semaphore.Release();
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
        
        /// <summary>
        /// 重载所有模组
        /// </summary>
        private async void ReloadMods()
        {
            logger.Log("Reloading all mods...");
            
            // 保存当前加载的模组列表
            var currentMods = modManagerCore.GetLoadedMods()
                .Select(m => m.LoadedMod.RootPath)
                .ToList();
            
            // 卸载所有模组
            if (modManager != null)
            {
                modManager.UnloadAllMods();
            }
            
            // 等待一帧，确保对象被销毁
            await Task.Yield();
            
            // 重新加载所有模组
            foreach (var modPath in currentMods)
            {
                try
                {
                    await modManager.LoadAndActivateMod(modPath);
                }
                catch (Exception ex)
                {
                    logger.LogError($"Failed to reload mod from {modPath}: {ex.Message}");
                }
            }
            
            // 更新运行时信息
            UpdateRuntimeInfo();
            
            logger.Log("Mods reloaded");
        }
        
        /// <summary>
        /// 检查模组变化（可选实现）
        /// </summary>
        private void CheckForModChanges()
        {
            // 这里可以实现文件监控，检测DLL变化自动重载
            // 为了简单起见，暂时不实现
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
        /// 更新运行时信息
        /// </summary>
        private void UpdateRuntimeInfo()
        {
            var loadedMods = modManagerCore.GetLoadedMods().ToList();
            loadedModsCount = loadedMods.Count;
            loadedModIds = loadedMods.Select(m => m.LoadedMod.Manifest.id).ToList();
        }
        #endregion

        #region Event Handlers
        /// <summary>
        /// 处理模组加载事件
        /// </summary>
        private void OnModLoaded(ModLoadedEvent e)
        {
            logger.Log($"Mod loaded: {e.ModName} v{e.Version}");
            UpdateRuntimeInfo();
        }
        
        /// <summary>
        /// 处理模组卸载事件
        /// </summary>
        private void OnModUnloaded(ModUnloadedEvent e)
        {
            logger.Log($"Mod unloaded: {e.ModId}");
            UpdateRuntimeInfo();
        }
        
        /// <summary>
        /// 处理模组错误事件
        /// </summary>
        private void OnModError(ModErrorEvent e)
        {
            logger.LogError($"Mod error in {e.ModId}: {e.Message}");
        }
        #endregion

        #region Public API
        /// <summary>
        /// 手动加载指定模组
        /// </summary>
        public async Task<bool> LoadMod(string modPath)
        {
            try
            {
                if (enableHotReload)
                {
                    await modManager.LoadAndActivateMod(modPath);
                }
                else
                {
                    await modManagerCore.LoadMod(modPath);
                }
                
                UpdateRuntimeInfo();
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError($"Failed to load mod: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 卸载指定模组
        /// </summary>
        public void UnloadMod(string modId)
        {
            modManager.UnloadMod(modId);
            UpdateRuntimeInfo();
        }
        
        /// <summary>
        /// 获取已加载的模组数量
        /// </summary>
        public int GetLoadedModCount()
        {
            return loadedModsCount;
        }
        
        /// <summary>
        /// 获取已加载的模组ID列表
        /// </summary>
        public List<string> GetLoadedModIds()
        {
            return new List<string>(loadedModIds);
        }
        
        /// <summary>
        /// 获取模组信息
        /// </summary>
        public ModInstance GetModInfo(string modId)
        {
            return modManagerCore.GetModInstance(modId);
        }
        
        /// <summary>
        /// 发布全局事件
        /// </summary>
        public void PublishEvent<T>(T eventData) where T : IModEvent
        {
            eventBus.Publish(eventData);
        }
        
        /// <summary>
        /// 切换调试模式
        /// </summary>
        public void ToggleDebugMode()
        {
            debugMode = !debugMode;
            logger.Log($"Debug mode: {debugMode}");
        }
        #endregion

        #region Cleanup
        /// <summary>
        /// 清理资源
        /// </summary>
        private void Cleanup()
        {
            if (eventBus != null)
            {
                eventBus.UnsubscribeAll(this);
            }
            
            // 其他清理操作
        }
        #endregion
    }
}