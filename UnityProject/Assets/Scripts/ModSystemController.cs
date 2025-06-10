// ===================================================================
// ModSystemController.cs - 简化的Unity层实现
// Unity平台特定的实现，不包含任何UI预置
// ===================================================================

namespace ModSystem.Unity
{
    using UnityEngine;
    using ModSystem.Core;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Linq;
    using System.IO;
    using ILogger = ModSystem.Core.ILogger;
    using System;

    #region Unity实现

    /// <summary>
    /// Unity日志实现
    /// </summary>
    public class UnityLogger : ILogger
    {
        private readonly string prefix;

        public UnityLogger(string prefix = "")
        {
            this.prefix = prefix;
        }

        public void Log(string message) => Debug.Log($"{prefix}{message}");
        public void LogWarning(string message) => Debug.LogWarning($"{prefix}{message}");
        public void LogError(string message) => Debug.LogError($"{prefix}{message}");
    }

    /// <summary>
    /// Unity路径提供器
    /// </summary>
    public class UnityPathProvider : IPathProvider
    {
        public string GetModsPath()
        {
            #if UNITY_EDITOR
            return Path.Combine(Application.streamingAssetsPath, "Mods");
            #else
            return Path.Combine(Application.persistentDataPath, "Mods");
            #endif
        }

        public string GetConfigPath()
        {
            return Path.Combine(Application.streamingAssetsPath, "ModConfigs");
        }
    }

    #endregion

    #region 主控制器

    /// <summary>
    /// 模组系统控制器 - Unity主入口（极简版本）
    /// </summary>
    [AddComponentMenu("ModSystem/Mod System Controller")]
    public class ModSystemController : MonoBehaviour
    {
        [Header("配置")]
        [SerializeField] private bool autoLoadMods = true;
        [SerializeField] private bool debugMode = true;
        [SerializeField] private float reloadCheckInterval = 1.0f;

        [Header("热键设置")]
        [SerializeField] private KeyCode reloadKey = KeyCode.F5;
        [SerializeField] private KeyCode toggleDebugKey = KeyCode.F12;

        [Header("运行时信息")]
        [SerializeField] private int loadedModsCount = 0;
        [SerializeField] private List<string> loadedModIds = new List<string>();

        private ModManagerCore core;
        private ModManager modManager;
        private ILogger logger;
        private IPathProvider pathProvider;
        private float lastReloadCheck = 0;

        #region Unity生命周期

        void Awake()
        {
            // 确保单例
            if (FindObjectsOfType<ModSystemController>().Length > 1)
            {
                Debug.LogWarning("[ModSystem] Multiple ModSystemController instances found!");
                Destroy(gameObject);
                return;
            }

            // 初始化系统
            InitializeSystem();
        }

        async void Start()
        {
            if (autoLoadMods)
            {
                await LoadAllMods();
            }
        }

        void Update()
        {
            // 热键处理
            if (Input.GetKeyDown(reloadKey))
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
            if (debugMode && Time.time - lastReloadCheck > reloadCheckInterval)
            {
                lastReloadCheck = Time.time;
                CheckForModChanges();
            }
            #endif
        }

        void OnDestroy()
        {
            if (modManager != null)
            {
                modManager.UnloadAllMods();
            }
        }

        void OnApplicationPause(bool pauseStatus)
        {
            if (debugMode)
                logger.Log($"Application pause: {pauseStatus}");
        }

        void OnApplicationQuit()
        {
            logger?.Log("[ModSystem] Application quitting");
        }

        #endregion

        #region 初始化

        private void InitializeSystem()
        {
            logger = new UnityLogger("[ModSystem] ");
            pathProvider = new UnityPathProvider();
            
            // 初始化核心
            core = new ModManagerCore(logger, pathProvider);
            
            // 初始化Unity层管理器
            modManager = gameObject.AddComponent<ModManager>();
            modManager.Initialize(core);

            if (debugMode)
            {
                logger.Log("Controller initialized");
                logger.Log($"Mods path: {pathProvider.GetModsPath()}");
                logger.Log($"Config path: {pathProvider.GetConfigPath()}");
            }
        }

        #endregion

        #region 模组加载

        private async Task LoadAllMods()
        {
            var modsPath = pathProvider.GetModsPath();
            
            if (!Directory.Exists(modsPath))
            {
                logger.LogWarning($"Mods directory not found: {modsPath}");
                Directory.CreateDirectory(modsPath);
                return;
            }

            var modDirs = Directory.GetDirectories(modsPath);
            logger.Log($"Found {modDirs.Length} mod directories");

            loadedModIds.Clear();
            loadedModsCount = 0;

            foreach (var modDir in modDirs)
            {
                try
                {
                    await modManager.LoadAndActivateMod(modDir);
                    
                    var modId = Path.GetFileName(modDir);
                    loadedModIds.Add(modId);
                    loadedModsCount++;
                }
                catch (System.Exception ex)
                {
                    logger.LogError($"Failed to load mod from {modDir}: {ex.Message}");
                }
            }

            logger.Log($"Successfully loaded {loadedModsCount} mods");
        }

        private async void ReloadMods()
        {
            logger.Log("Reloading all mods...");
            
            // 卸载所有模组
            if (modManager != null)
            {
                modManager.UnloadAllMods();
            }
            
            // 等待一帧，确保对象被销毁
            await Task.Yield();
            
            // 重新加载
            await LoadAllMods();
            
            logger.Log("Mods reloaded");
        }

        private void CheckForModChanges()
        {
            // 这里可以实现文件监控，检测DLL变化自动重载
            // 为了简单起见，暂时不实现
        }

        #endregion

        #region 公共API

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
        /// 手动加载指定模组
        /// </summary>
        public async Task LoadMod(string modPath)
        {
            try
            {
                await modManager.LoadAndActivateMod(modPath);
                var modId = Path.GetFileName(modPath);
                if (!loadedModIds.Contains(modId))
                {
                    loadedModIds.Add(modId);
                    loadedModsCount++;
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Failed to load mod: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 卸载指定模组
        /// </summary>
        public void UnloadMod(string modId)
        {
            modManager.UnloadMod(modId);
            if (loadedModIds.Remove(modId))
            {
                loadedModsCount--;
            }
        }

        /// <summary>
        /// 发布全局事件
        /// </summary>
        public void PublishEvent<T>(T eventData) where T : IModEvent
        {
            core.EventBus.Publish(eventData);
        }

        #endregion
    }

    #endregion

    #region ModManager

    /// <summary>
    /// Unity模组管理器（极简版本，不包含UI相关代码）
    /// </summary>
    public class ModManager : MonoBehaviour
    {
        private ModManagerCore core;
        private Dictionary<string, ModUnityInstance> unityInstances;
        private Transform modsContainer;

        public void Initialize(ModManagerCore core)
        {
            this.core = core;
            unityInstances = new Dictionary<string, ModUnityInstance>();
            
            // 创建模组容器
            GameObject container = new GameObject("Mods");
            container.transform.SetParent(transform);
            modsContainer = container.transform;
        }

        public async Task LoadAndActivateMod(string modPath)
        {
            Debug.Log($"[ModManager] Loading mod from: {modPath}");
            
            // 使用ReloadModAsync来支持热重载
            var instance = await core.ReloadModAsync(modPath);
            var modId = instance.LoadedMod.Manifest.id;
            
            Debug.Log($"[ModManager] Mod loaded: {instance.LoadedMod.Manifest.name}");
            
            // 如果已存在Unity实例，先清理
            if (unityInstances.ContainsKey(modId))
            {
                Debug.Log($"[ModManager] Cleaning up existing instance for mod: {modId}");
                DestroyUnityInstance(modId);
            }
            
            // 创建新的Unity实例
            var unityInstance = CreateUnityInstance(instance);
            unityInstances[modId] = unityInstance;
            
            // 创建行为
            Debug.Log($"[ModManager] Creating behaviour for class: {instance.LoadedMod.Manifest.main_class}");
            var behaviour = core.CreateModBehaviour(instance);
            
            if (behaviour != null)
            {
                Debug.Log($"[ModManager] Behaviour created successfully: {behaviour.GetType().FullName}");
                
                // 创建上下文 - 简化版本，不包含UI工厂
                var context = new ModContext
                {
                    ModId = instance.LoadedMod.Manifest.id,
                    EventBus = core.EventBus,
                    Logger = new UnityLogger()
                };
                
                // 初始化模组
                behaviour.OnInitialize(context);
                behaviour.OnEnable();
                
                // 如果支持热重载，调用OnAfterReload
                if (behaviour is IReloadable reloadable)
                {
                    reloadable.OnAfterReload();
                }
                
                unityInstance.Behaviour = behaviour;
                Debug.Log($"[ModManager] Mod activated: {instance.LoadedMod.Manifest.id}");
            }
            else
            {
                Debug.LogError($"[ModManager] Failed to create behaviour for {instance.LoadedMod.Manifest.main_class}");
            }
        }

        public void UnloadMod(string modId)
        {
            if (unityInstances.ContainsKey(modId))
            {
                // 通过核心卸载
                core.UnloadMod(modId);
                
                // 销毁Unity实例
                DestroyUnityInstance(modId);
            }
        }

        public void UnloadAllMods()
        {
            Debug.Log("[ModManager] Unloading all mods...");
            
            // 先通知所有模组即将卸载
            foreach (var kvp in unityInstances.ToList())
            {
                var modId = kvp.Key;
                var unityInstance = kvp.Value;
                
                if (unityInstance.Behaviour != null)
                {
                    // 如果支持热重载，先调用OnBeforeReload
                    if (unityInstance.Behaviour is IReloadable reloadable)
                    {
                        try
                        {
                            reloadable.OnBeforeReload();
                        }
                        catch (System.Exception ex)
                        {
                            Debug.LogError($"[ModManager] Error in OnBeforeReload for {modId}: {ex.Message}");
                        }
                    }
                }
                
                // 通过核心卸载模组
                core.UnloadMod(modId);
                
                // 销毁Unity实例
                DestroyUnityInstance(modId);
            }
            
            // 清空字典
            unityInstances.Clear();
            
            Debug.Log("[ModManager] All mods unloaded");
        }
        
        private ModUnityInstance CreateUnityInstance(ModInstance instance)
        {
            var modId = instance.LoadedMod.Manifest.id;
            var modObj = new GameObject($"Mod_{modId}_v{instance.LoadedMod.LoadVersion}");
            modObj.transform.SetParent(modsContainer);
            
            var unityInstance = new ModUnityInstance
            {
                GameObject = modObj,
                ModInstance = instance
            };
            
            Debug.Log($"[ModManager] Created Unity instance for mod: {modId}");
            
            return unityInstance;
        }

        private void DestroyUnityInstance(string modId)
        {
            if (unityInstances.TryGetValue(modId, out var unityInstance))
            {
                // 销毁GameObject
                if (unityInstance.GameObject != null)
                {
                    Destroy(unityInstance.GameObject);
                    Debug.Log($"[ModManager] Destroyed GameObject for mod: {modId}");
                }
            }
        }

        void OnDestroy()
        {
            // 确保所有模组都被正确卸载
            UnloadAllMods();
        }
    }

    /// <summary>
    /// 模组的Unity实例数据（简化版本）
    /// </summary>
    public class ModUnityInstance
    {
        public GameObject GameObject { get; set; }
        public ModInstance ModInstance { get; set; }
        public IMod Behaviour { get; set; }
    }

    #endregion
}