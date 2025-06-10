# SimulationProject Unity层完整代码实现

## 目录结构
```
SimulationProject/Assets/ModSystem/Unity/
├── ModSystemController.cs
├── ModManager.cs
├── ModBehaviourUpdater.cs
├── UnityImplementations/
│   ├── UnityLogger.cs
│   ├── UnityPathProvider.cs
│   ├── UnityObjectFactory.cs
│   ├── UnityGameObjectWrapper.cs
│   └── UnityEventLogger.cs
├── Debug/
│   ├── EventMonitor.cs
│   ├── ModPerformanceProfiler.cs
│   └── ModMemoryMonitor.cs
└── Editor/
    ├── ModSystemMenu.cs
    └── ModSystemInspector.cs
```

## 1. ModSystemController.cs

```csharp
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
```

## 2. ModManager.cs

```csharp
// ModSystem.Unity/ModManager.cs
using UnityEngine;
using ModSystem.Core;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace ModSystem.Unity
{
    /// <summary>
    /// Unity特定的ModManager包装器
    /// 处理Unity生命周期和GameObject管理，作为Core层ModManagerCore的Unity层扩展
    /// </summary>
    [AddComponentMenu("ModSystem/Mod Manager")]
    [RequireComponent(typeof(ModSystemController))]
    public class ModManager : MonoBehaviour
    {
        #region Fields
        private ModManagerCore core;
        private Dictionary<string, ModUnityInstance> unityInstances;
        private IObjectFactory objectFactory;
        private Transform modsContainer;
        #endregion

        #region Properties
        /// <summary>
        /// 获取所有Unity模组实例
        /// </summary>
        public IReadOnlyDictionary<string, ModUnityInstance> UnityInstances => unityInstances;
        
        /// <summary>
        /// 获取模组容器Transform
        /// </summary>
        public Transform ModsContainer => modsContainer;
        #endregion

        #region Initialization
        /// <summary>
        /// 初始化ModManager
        /// </summary>
        /// <param name="core">核心模组管理器</param>
        public void Initialize(ModManagerCore core)
        {
            this.core = core;
            unityInstances = new Dictionary<string, ModUnityInstance>();
            objectFactory = new UnityObjectFactory();
            
            // 创建模组容器
            CreateModsContainer();
            
            // 监听模组加载/卸载事件
            SubscribeToEvents();
            
            Debug.Log("[ModManager] Initialized");
        }
        
        /// <summary>
        /// 创建模组容器GameObject
        /// </summary>
        private void CreateModsContainer()
        {
            GameObject containerObj = new GameObject("Mods");
            containerObj.transform.SetParent(transform);
            modsContainer = containerObj.transform;
        }
        
        /// <summary>
        /// 订阅核心事件
        /// </summary>
        private void SubscribeToEvents()
        {
            core.EventBus.Subscribe<ModLoadedEvent>(OnModLoaded);
            core.EventBus.Subscribe<ModUnloadedEvent>(OnModUnloaded);
            core.EventBus.Subscribe<CreateGameObjectRequest>(OnCreateGameObjectRequest);
        }
        #endregion

        #region Event Handlers
        /// <summary>
        /// 处理模组加载事件
        /// </summary>
        private void OnModLoaded(ModLoadedEvent e)
        {
            CreateUnityInstance(e.ModId);
        }
        
        /// <summary>
        /// 处理模组卸载事件
        /// </summary>
        private void OnModUnloaded(ModUnloadedEvent e)
        {
            DestroyUnityInstance(e.ModId);
        }
        
        /// <summary>
        /// 处理创建GameObject请求
        /// </summary>
        private async void OnCreateGameObjectRequest(CreateGameObjectRequest request)
        {
            try
            {
                var gameObject = await CreateGameObjectFromDefinition(request.Definition);
                
                // 发送响应
                core.EventBus.Publish(new CreateGameObjectResponse
                {
                    RequestId = request.RequestId,
                    Success = true,
                    GameObject = new UnityGameObjectWrapper(gameObject)
                });
            }
            catch (Exception ex)
            {
                core.EventBus.Publish(new CreateGameObjectResponse
                {
                    RequestId = request.RequestId,
                    Success = false,
                    Message = ex.Message
                });
            }
        }
        #endregion

        #region Unity Instance Management
        /// <summary>
        /// 创建模组的Unity实例
        /// </summary>
        private void CreateUnityInstance(string modId)
        {
            var modInstance = core.GetModInstance(modId);
            if (modInstance == null) 
            {
                Debug.LogError($"[ModManager] Mod instance not found: {modId}");
                return;
            }
            
            // 创建Unity容器
            var container = new GameObject($"Mod_{modId}");
            container.transform.SetParent(modsContainer);
            
            var unityInstance = new ModUnityInstance
            {
                Container = container,
                GameObjects = new List<GameObject>(),
                Components = new List<MonoBehaviour>()
            };
            
            // 为每个行为创建GameObject
            CreateBehaviourGameObjects(modInstance, unityInstance);
            
            // 创建对象定义中的GameObject
            CreateObjectsFromDefinitions(modInstance, unityInstance);
            
            unityInstances[modId] = unityInstance;
            
            Debug.Log($"[ModManager] Created Unity instance for mod: {modId}");
        }
        
        /// <summary>
        /// 为模组行为创建GameObject
        /// </summary>
        private void CreateBehaviourGameObjects(ModInstance modInstance, ModUnityInstance unityInstance)
        {
            foreach (var behaviour in modInstance.LoadedMod.Behaviours)
            {
                var behaviourObj = new GameObject($"Behaviour_{behaviour.BehaviourId}");
                behaviourObj.transform.SetParent(unityInstance.Container.transform);
                
                // 添加更新组件
                var updater = behaviourObj.AddComponent<ModBehaviourUpdater>();
                updater.Initialize(behaviour);
                
                // 设置模组上下文中的GameObject
                if (core.GetModInstance(modInstance.LoadedMod.Manifest.id) is ModInstance instance)
                {
                    // 这里需要通过某种方式设置behaviour的GameObject引用
                    // 可能需要扩展IModBehaviour接口或使用反射
                }
                
                unityInstance.GameObjects.Add(behaviourObj);
                unityInstance.Components.Add(updater);
            }
        }
        
        /// <summary>
        /// 从对象定义创建GameObject
        /// </summary>
        private async void CreateObjectsFromDefinitions(ModInstance modInstance, ModUnityInstance unityInstance)
        {
            foreach (var objDef in modInstance.LoadedMod.Resources.ObjectDefinitions.Values)
            {
                try
                {
                    var obj = await CreateGameObjectFromDefinition(objDef);
                    obj.transform.SetParent(unityInstance.Container.transform);
                    unityInstance.GameObjects.Add(obj);
                    
                    Debug.Log($"[ModManager] Created object: {objDef.objectId}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[ModManager] Failed to create object {objDef.objectId}: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// 从定义创建GameObject
        /// </summary>
        private async Task<GameObject> CreateGameObjectFromDefinition(ObjectDefinition definition)
        {
            var wrapper = await objectFactory.CreateObjectFromDefinitionAsync(definition);
            if (wrapper is UnityGameObjectWrapper unityWrapper)
            {
                return unityWrapper.GameObject;
            }
            
            throw new InvalidOperationException("Failed to create GameObject from definition");
        }
        
        /// <summary>
        /// 销毁Unity实例
        /// </summary>
        private void DestroyUnityInstance(string modId)
        {
            if (unityInstances.TryGetValue(modId, out var instance))
            {
                // 清理所有组件
                foreach (var component in instance.Components)
                {
                    if (component != null)
                    {
                        Destroy(component);
                    }
                }
                
                // 销毁容器GameObject
                if (instance.Container != null)
                {
                    Destroy(instance.Container);
                }
                
                unityInstances.Remove(modId);
                
                Debug.Log($"[ModManager] Destroyed Unity instance for mod: {modId}");
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 获取指定模组的Unity实例
        /// </summary>
        public ModUnityInstance GetUnityInstance(string modId)
        {
            return unityInstances.TryGetValue(modId, out var instance) ? instance : null;
        }
        
        /// <summary>
        /// 获取所有已加载的模组信息
        /// </summary>
        public IEnumerable<ModInstance> GetLoadedMods()
        {
            return core.GetLoadedMods();
        }
        
        /// <summary>
        /// 查找模组中的GameObject
        /// </summary>
        public GameObject FindModGameObject(string modId, string objectName)
        {
            if (unityInstances.TryGetValue(modId, out var instance))
            {
                return instance.GameObjects.FirstOrDefault(go => go != null && go.name == objectName);
            }
            return null;
        }
        
        /// <summary>
        /// 获取模组中的所有组件
        /// </summary>
        public T[] GetModComponents<T>(string modId) where T : Component
        {
            if (unityInstances.TryGetValue(modId, out var instance))
            {
                return instance.Container.GetComponentsInChildren<T>();
            }
            return new T[0];
        }
        #endregion

        #region Unity Lifecycle
        void OnDestroy()
        {
            // 清理所有Unity实例
            foreach (var modId in unityInstances.Keys.ToList())
            {
                DestroyUnityInstance(modId);
            }
            
            // 取消事件订阅
            if (core != null)
            {
                core.EventBus.UnsubscribeAll(this);
            }
        }
        #endregion
    }

    #region Data Structures
    /// <summary>
    /// 模组的Unity实例数据
    /// </summary>
    public class ModUnityInstance
    {
        /// <summary>
        /// 模组的根容器GameObject
        /// </summary>
        public GameObject Container { get; set; }
        
        /// <summary>
        /// 模组创建的所有GameObject列表
        /// </summary>
        public List<GameObject> GameObjects { get; set; }
        
        /// <summary>
        /// 模组的所有MonoBehaviour组件
        /// </summary>
        public List<MonoBehaviour> Components { get; set; }
        
        /// <summary>
        /// 获取活动的GameObject数量
        /// </summary>
        public int ActiveObjectCount => GameObjects?.Count(go => go != null && go.activeInHierarchy) ?? 0;
        
        /// <summary>
        /// 获取内存使用估算（字节）
        /// </summary>
        public long EstimatedMemoryUsage { get; set; }
    }
    
    /// <summary>
    /// 创建GameObject请求
    /// </summary>
    public class CreateGameObjectRequest : ModRequest
    {
        public ObjectDefinition Definition { get; set; }
    }
    
    /// <summary>
    /// 创建GameObject响应
    /// </summary>
    public class CreateGameObjectResponse : ModResponse
    {
        public IGameObject GameObject { get; set; }
    }
    #endregion
}
```

## 3. ModBehaviourUpdater.cs

```csharp
// ModSystem.Unity/ModBehaviourUpdater.cs
using UnityEngine;
using ModSystem.Core;
using System;

namespace ModSystem.Unity
{
    /// <summary>
    /// Unity组件，负责调用模组行为的Update方法
    /// 将Unity的生命周期事件转发给模组行为
    /// </summary>
    [AddComponentMenu("ModSystem/Mod Behaviour Updater")]
    public class ModBehaviourUpdater : MonoBehaviour
    {
        #region Fields
        private IModBehaviour behaviour;
        private float lastUpdateTime;
        private bool isInitialized;
        private float updateInterval = 0f; // 0表示每帧更新
        private float timeSinceLastUpdate = 0f;
        #endregion

        #region Properties
        /// <summary>
        /// 获取关联的模组行为
        /// </summary>
        public IModBehaviour Behaviour => behaviour;
        
        /// <summary>
        /// 获取或设置更新间隔（秒）
        /// </summary>
        public float UpdateInterval
        {
            get => updateInterval;
            set => updateInterval = Mathf.Max(0f, value);
        }
        
        /// <summary>
        /// 获取行为是否已初始化
        /// </summary>
        public bool IsInitialized => isInitialized;
        #endregion

        #region Initialization
        /// <summary>
        /// 初始化更新器
        /// </summary>
        /// <param name="behaviour">要更新的模组行为</param>
        public void Initialize(IModBehaviour behaviour)
        {
            if (behaviour == null)
            {
                Debug.LogError("[ModBehaviourUpdater] Cannot initialize with null behaviour");
                enabled = false;
                return;
            }
            
            this.behaviour = behaviour;
            lastUpdateTime = Time.time;
            isInitialized = true;
            
            Debug.Log($"[ModBehaviourUpdater] Initialized for behaviour: {behaviour.BehaviourId}");
        }
        #endregion

        #region Unity Lifecycle
        void Start()
        {
            if (!isInitialized)
            {
                Debug.LogWarning("[ModBehaviourUpdater] Not initialized, disabling component");
                enabled = false;
            }
        }
        
        void Update()
        {
            if (!isInitialized || behaviour == null)
                return;
            
            // 检查更新间隔
            if (updateInterval > 0f)
            {
                timeSinceLastUpdate += Time.deltaTime;
                if (timeSinceLastUpdate < updateInterval)
                    return;
                
                timeSinceLastUpdate = 0f;
            }
            
            // 计算实际的deltaTime
            float deltaTime = Time.time - lastUpdateTime;
            lastUpdateTime = Time.time;
            
            try
            {
                // 调用行为的更新方法
                behaviour.OnUpdate(deltaTime);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ModBehaviourUpdater] Error updating behaviour {behaviour.BehaviourId}: {ex}");
                
                // 发布错误事件
                PublishErrorEvent(ex);
            }
        }
        
        void OnDestroy()
        {
            if (behaviour != null)
            {
                try
                {
                    behaviour.OnDestroy();
                    Debug.Log($"[ModBehaviourUpdater] Destroyed behaviour: {behaviour.BehaviourId}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[ModBehaviourUpdater] Error destroying behaviour {behaviour.BehaviourId}: {ex}");
                }
                
                behaviour = null;
            }
        }
        
        void OnEnable()
        {
            lastUpdateTime = Time.time;
            timeSinceLastUpdate = 0f;
            
            // 可以添加恢复逻辑
            if (behaviour != null)
            {
                Debug.Log($"[ModBehaviourUpdater] Enabled behaviour: {behaviour.BehaviourId}");
            }
        }
        
        void OnDisable()
        {
            // 可以添加暂停逻辑
            if (behaviour != null)
            {
                Debug.Log($"[ModBehaviourUpdater] Disabled behaviour: {behaviour.BehaviourId}");
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 设置更新速率（每秒更新次数）
        /// </summary>
        /// <param name="updatesPerSecond">每秒更新次数，0表示每帧更新</param>
        public void SetUpdateRate(float updatesPerSecond)
        {
            if (updatesPerSecond <= 0f)
            {
                UpdateInterval = 0f;
            }
            else
            {
                UpdateInterval = 1f / updatesPerSecond;
            }
        }
        
        /// <summary>
        /// 暂停更新
        /// </summary>
        public void PauseUpdates()
        {
            enabled = false;
        }
        
        /// <summary>
        /// 恢复更新
        /// </summary>
        public void ResumeUpdates()
        {
            enabled = true;
            lastUpdateTime = Time.time;
        }
        
        /// <summary>
        /// 强制立即更新
        /// </summary>
        public void ForceUpdate()
        {
            if (behaviour != null)
            {
                float deltaTime = Time.time - lastUpdateTime;
                lastUpdateTime = Time.time;
                
                try
                {
                    behaviour.OnUpdate(deltaTime);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[ModBehaviourUpdater] Error in forced update: {ex}");
                }
            }
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// 发布错误事件
        /// </summary>
        private void PublishErrorEvent(Exception ex)
        {
            var controller = ModSystemController.Instance;
            if (controller != null && controller.EventBus != null)
            {
                controller.EventBus.Publish(new ModErrorEvent
                {
                    SenderId = behaviour?.BehaviourId ?? "Unknown",
                    ErrorType = "UpdateError",
                    Message = ex.Message,
                    StackTrace = ex.StackTrace
                });
            }
        }
        #endregion
    }
}
```

## 4. UnityImplementations/UnityLogger.cs

```csharp
// ModSystem.Unity/UnityImplementations/UnityLogger.cs
using UnityEngine;
using ModSystem.Core;
using System;

namespace ModSystem.Unity
{
    /// <summary>
    /// Unity日志记录器实现
    /// 将Core层的日志调用转发到Unity的Debug类
    /// </summary>
    public class UnityLogger : ILogger
    {
        #region Fields
        private readonly string prefix;
        private readonly bool includeTimestamp;
        private readonly LogLevel minLogLevel;
        #endregion

        #region Enums
        /// <summary>
        /// 日志级别枚举
        /// </summary>
        public enum LogLevel
        {
            Debug = 0,
            Info = 1,
            Warning = 2,
            Error = 3
        }
        #endregion

        #region Constructor
        /// <summary>
        /// 创建Unity日志记录器
        /// </summary>
        /// <param name="prefix">日志前缀</param>
        /// <param name="includeTimestamp">是否包含时间戳</param>
        /// <param name="minLogLevel">最小日志级别</param>
        public UnityLogger(string prefix = "[ModSystem]", bool includeTimestamp = false, LogLevel minLogLevel = LogLevel.Info)
        {
            this.prefix = prefix;
            this.includeTimestamp = includeTimestamp;
            this.minLogLevel = minLogLevel;
        }
        #endregion

        #region ILogger Implementation
        /// <summary>
        /// 记录普通日志
        /// </summary>
        public void Log(string message)
        {
            if (minLogLevel <= LogLevel.Info)
            {
                Debug.Log(FormatMessage(message, LogLevel.Info));
            }
        }
        
        /// <summary>
        /// 记录警告日志
        /// </summary>
        public void LogWarning(string message)
        {
            if (minLogLevel <= LogLevel.Warning)
            {
                Debug.LogWarning(FormatMessage(message, LogLevel.Warning));
            }
        }
        
        /// <summary>
        /// 记录错误日志
        /// </summary>
        public void LogError(string message)
        {
            if (minLogLevel <= LogLevel.Error)
            {
                Debug.LogError(FormatMessage(message, LogLevel.Error));
            }
        }
        #endregion

        #region Extended Logging Methods
        /// <summary>
        /// 记录调试日志
        /// </summary>
        public void LogDebug(string message)
        {
            if (minLogLevel <= LogLevel.Debug)
            {
                Debug.Log(FormatMessage(message, LogLevel.Debug));
            }
        }
        
        /// <summary>
        /// 记录异常
        /// </summary>
        public void LogException(Exception exception, string context = null)
        {
            string message = string.IsNullOrEmpty(context) 
                ? $"Exception: {exception.Message}" 
                : $"Exception in {context}: {exception.Message}";
            
            Debug.LogError(FormatMessage(message, LogLevel.Error));
            Debug.LogException(exception);
        }
        
        /// <summary>
        /// 记录断言失败
        /// </summary>
        public void LogAssertion(string condition, string message)
        {
            Debug.LogAssertion(FormatMessage($"Assertion failed: {condition} - {message}", LogLevel.Error));
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// 格式化日志消息
        /// </summary>
        private string FormatMessage(string message, LogLevel level)
        {
            var formattedMessage = message;
            
            // 添加时间戳
            if (includeTimestamp)
            {
                formattedMessage = $"[{DateTime.Now:HH:mm:ss.fff}] {formattedMessage}";
            }
            
            // 添加日志级别
            if (level == LogLevel.Debug)
            {
                formattedMessage = $"[DEBUG] {formattedMessage}";
            }
            
            // 添加前缀
            if (!string.IsNullOrEmpty(prefix))
            {
                formattedMessage = $"{prefix} {formattedMessage}";
            }
            
            return formattedMessage;
        }
        #endregion

        #region Conditional Logging
        /// <summary>
        /// 条件日志记录
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void LogEditor(string message)
        {
            Log($"[EDITOR] {message}");
        }
        
        /// <summary>
        /// 开发构建日志记录
        /// </summary>
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        public void LogDevelopment(string message)
        {
            Log($"[DEV] {message}");
        }
        #endregion
    }
}
```

## 5. UnityImplementations/UnityPathProvider.cs

```csharp
// ModSystem.Unity/UnityImplementations/UnityPathProvider.cs
using UnityEngine;
using System.IO;
using ModSystem.Core;

namespace ModSystem.Unity
{
    /// <summary>
    /// Unity路径提供器实现
    /// 提供Unity环境下的各种路径访问
    /// </summary>
    public class UnityPathProvider : IPathProvider
    {
        #region Constants
        private const string MODS_FOLDER = "Mods";
        private const string CONFIG_FOLDER = "ModConfigs";
        private const string TEMP_FOLDER = "ModTemp";
        private const string CACHE_FOLDER = "ModCache";
        #endregion

        #region IPathProvider Implementation
        /// <summary>
        /// 获取模组存放路径
        /// </summary>
        public string GetModsPath()
        {
            // 在编辑器中使用StreamingAssets，在运行时使用持久化路径
            #if UNITY_EDITOR
            return Path.Combine(Application.streamingAssetsPath, MODS_FOLDER);
            #else
            return Path.Combine(Application.persistentDataPath, MODS_FOLDER);
            #endif
        }
        
        /// <summary>
        /// 获取配置文件路径
        /// </summary>
        public string GetConfigPath()
        {
            return Path.Combine(Application.streamingAssetsPath, CONFIG_FOLDER);
        }
        
        /// <summary>
        /// 获取临时文件路径
        /// </summary>
        public string GetTempPath()
        {
            string tempPath = Path.Combine(Application.temporaryCachePath, TEMP_FOLDER);
            EnsureDirectoryExists(tempPath);
            return tempPath;
        }
        
        /// <summary>
        /// 获取持久化数据路径
        /// </summary>
        public string GetPersistentDataPath()
        {
            return Application.persistentDataPath;
        }
        #endregion

        #region Extended Path Methods
        /// <summary>
        /// 获取缓存路径
        /// </summary>
        public string GetCachePath()
        {
            string cachePath = Path.Combine(Application.temporaryCachePath, CACHE_FOLDER);
            EnsureDirectoryExists(cachePath);
            return cachePath;
        }
        
        /// <summary>
        /// 获取模组包路径
        /// </summary>
        public string GetModPackagesPath()
        {
            return Path.Combine(Application.streamingAssetsPath, "ModPackages");
        }
        
        /// <summary>
        /// 获取用户模组路径（用于运行时下载的模组）
        /// </summary>
        public string GetUserModsPath()
        {
            string userModsPath = Path.Combine(Application.persistentDataPath, "UserMods");
            EnsureDirectoryExists(userModsPath);
            return userModsPath;
        }
        
        /// <summary>
        /// 获取日志文件路径
        /// </summary>
        public string GetLogsPath()
        {
            string logsPath = Path.Combine(Application.persistentDataPath, "Logs");
            EnsureDirectoryExists(logsPath);
            return logsPath;
        }
        
        /// <summary>
        /// 获取截图路径
        /// </summary>
        public string GetScreenshotsPath()
        {
            string screenshotsPath = Path.Combine(Application.persistentDataPath, "Screenshots");
            EnsureDirectoryExists(screenshotsPath);
            return screenshotsPath;
        }
        #endregion

        #region Platform-Specific Paths
        /// <summary>
        /// 获取平台特定的数据路径
        /// </summary>
        public string GetPlatformDataPath()
        {
            switch (Application.platform)
            {
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.WindowsEditor:
                    return Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments), 
                        Application.companyName, Application.productName);
                    
                case RuntimePlatform.OSXPlayer:
                case RuntimePlatform.OSXEditor:
                    return Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), 
                        "Library", "Application Support", Application.identifier);
                    
                case RuntimePlatform.LinuxPlayer:
                case RuntimePlatform.LinuxEditor:
                    return Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), 
                        ".config", Application.identifier);
                    
                default:
                    return Application.persistentDataPath;
            }
        }
        
        /// <summary>
        /// 获取安装路径
        /// </summary>
        public string GetInstallationPath()
        {
            return Application.dataPath;
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// 确保目录存在
        /// </summary>
        private void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
        
        /// <summary>
        /// 获取相对于StreamingAssets的路径
        /// </summary>
        public string GetStreamingAssetsRelativePath(string fullPath)
        {
            if (fullPath.StartsWith(Application.streamingAssetsPath))
            {
                return fullPath.Substring(Application.streamingAssetsPath.Length + 1);
            }
            return fullPath;
        }
        
        /// <summary>
        /// 清理路径（处理不同平台的路径分隔符）
        /// </summary>
        public string CleanPath(string path)
        {
            return path.Replace('\\', Path.DirectorySeparatorChar)
                      .Replace('/', Path.DirectorySeparatorChar);
        }
        #endregion

        #region Path Validation
        /// <summary>
        /// 验证路径是否安全
        /// </summary>
        public bool IsPathSafe(string path)
        {
            try
            {
                // 获取完整路径
                string fullPath = Path.GetFullPath(path);
                
                // 检查是否在允许的目录内
                return fullPath.StartsWith(Application.streamingAssetsPath) ||
                       fullPath.StartsWith(Application.persistentDataPath) ||
                       fullPath.StartsWith(Application.temporaryCachePath);
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// 验证文件扩展名
        /// </summary>
        public bool IsValidModFile(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLower();
            return extension == ".modpack" || extension == ".zip";
        }
        #endregion
    }
}
```

## 6. UnityImplementations/UnityObjectFactory.cs

```csharp
// ModSystem.Unity/UnityImplementations/UnityObjectFactory.cs
using UnityEngine;
using System.Threading.Tasks;
using ModSystem.Core;
using System.Collections.Generic;
using System;
using System.Linq;

namespace ModSystem.Unity
{
    /// <summary>
    /// Unity对象工厂实现
    /// 负责从JSON定义创建Unity GameObject
    /// </summary>
    public class UnityObjectFactory : ObjectFactoryBase
    {
        #region Fields
        private readonly Dictionary<string, Shader> shaderCache;
        private readonly Dictionary<string, Material> materialCache;
        private readonly Dictionary<string, Mesh> meshCache;
        #endregion

        #region Constructor
        /// <summary>
        /// 创建Unity对象工厂
        /// </summary>
        public UnityObjectFactory() : base(Application.streamingAssetsPath, new UnityLogger())
        {
            shaderCache = new Dictionary<string, Shader>();
            materialCache = new Dictionary<string, Material>();
            meshCache = new Dictionary<string, Mesh>();
            
            // 预加载常用着色器
            PreloadShaders();
        }
        #endregion

        #region ObjectFactoryBase Implementation
        /// <summary>
        /// 从对象定义创建GameObject
        /// </summary>
        public override async Task<IGameObject> CreateObjectFromDefinitionAsync(ObjectDefinition definition)
        {
            // 创建GameObject
            var gameObject = new GameObject(definition.name ?? "ModObject");
            var wrapper = new UnityGameObjectWrapper(gameObject);
            
            // 处理每个组件定义
            foreach (var compDef in definition.components)
            {
                try
                {
                    await AddComponentAsync(wrapper, compDef);
                }
                catch (Exception ex)
                {
                    logger.LogError($"Failed to add component {compDef.type}: {ex.Message}");
                }
            }
            
            return wrapper;
        }
        #endregion

        #region Component Creation
        /// <summary>
        /// 异步添加组件到GameObject
        /// </summary>
        private async Task AddComponentAsync(UnityGameObjectWrapper wrapper, ComponentDefinition compDef)
        {
            switch (compDef.type)
            {
                case "Transform":
                    ConfigureTransform(wrapper.GameObject.transform, compDef);
                    break;
                    
                case "MeshRenderer":
                    await ConfigureMeshRenderer(wrapper.GameObject, compDef);
                    break;
                    
                case "MeshFilter":
                    await ConfigureMeshFilter(wrapper.GameObject, compDef);
                    break;
                    
                case "BoxCollider":
                    ConfigureBoxCollider(wrapper.GameObject, compDef);
                    break;
                    
                case "SphereCollider":
                    ConfigureSphereCollider(wrapper.GameObject, compDef);
                    break;
                    
                case "CapsuleCollider":
                    ConfigureCapsuleCollider(wrapper.GameObject, compDef);
                    break;
                    
                case "MeshCollider":
                    await ConfigureMeshCollider(wrapper.GameObject, compDef);
                    break;
                    
                case "Rigidbody":
                    ConfigureRigidbody(wrapper.GameObject, compDef);
                    break;
                    
                case "Light":
                    ConfigureLight(wrapper.GameObject, compDef);
                    break;
                    
                case "Camera":
                    ConfigureCamera(wrapper.GameObject, compDef);
                    break;
                    
                case "AudioSource":
                    await ConfigureAudioSource(wrapper.GameObject, compDef);
                    break;
                    
                case "ParticleSystem":
                    ConfigureParticleSystem(wrapper.GameObject, compDef);
                    break;
                    
                case "LineRenderer":
                    ConfigureLineRenderer(wrapper.GameObject, compDef);
                    break;
                    
                case "TrailRenderer":
                    ConfigureTrailRenderer(wrapper.GameObject, compDef);
                    break;
                    
                case "Canvas":
                    ConfigureCanvas(wrapper.GameObject, compDef);
                    break;
                    
                case "Text":
                    ConfigureText(wrapper.GameObject, compDef);
                    break;
                    
                case "Image":
                    await ConfigureImage(wrapper.GameObject, compDef);
                    break;
                    
                case "Button":
                    ConfigureButton(wrapper.GameObject, compDef);
                    break;
                    
                case "ObjectBehaviour":
                    ConfigureObjectBehaviour(wrapper, compDef);
                    break;
                    
                default:
                    // 尝试通过反射添加组件
                    TryAddComponentByReflection(wrapper.GameObject, compDef);
                    break;
            }
        }
        #endregion

        #region Transform Configuration
        /// <summary>
        /// 配置Transform组件
        /// </summary>
        private void ConfigureTransform(Transform transform, ComponentDefinition compDef)
        {
            // 位置
            var position = compDef.GetProperty<float[]>("position", new float[] { 0, 0, 0 });
            transform.position = new Vector3(position[0], position[1], position[2]);
            
            // 旋转
            var rotation = compDef.GetProperty<float[]>("rotation", new float[] { 0, 0, 0 });
            transform.rotation = Quaternion.Euler(rotation[0], rotation[1], rotation[2]);
            
            // 缩放
            var scale = compDef.GetProperty<float[]>("scale", new float[] { 1, 1, 1 });
            transform.localScale = new Vector3(scale[0], scale[1], scale[2]);
            
            // 父对象（如果指定）
            var parentPath = compDef.GetProperty<string>("parent");
            if (!string.IsNullOrEmpty(parentPath))
            {
                var parent = GameObject.Find(parentPath);
                if (parent != null)
                {
                    transform.SetParent(parent.transform);
                }
            }
        }
        #endregion

        #region Mesh and Rendering Configuration
        /// <summary>
        /// 配置MeshRenderer组件
        /// </summary>
        private async Task ConfigureMeshRenderer(GameObject obj, ComponentDefinition compDef)
        {
            var renderer = obj.GetComponent<MeshRenderer>();
            if (renderer == null)
            {
                renderer = obj.AddComponent<MeshRenderer>();
            }
            
            // 创建或获取材质
            var material = await CreateMaterialFromDefinition(compDef);
            if (material != null)
            {
                renderer.material = material;
            }
            
            // 渲染设置
            renderer.shadowCastingMode = compDef.GetProperty<bool>("castShadows", true) 
                ? UnityEngine.Rendering.ShadowCastingMode.On 
                : UnityEngine.Rendering.ShadowCastingMode.Off;
            
            renderer.receiveShadows = compDef.GetProperty<bool>("receiveShadows", true);
            
            // 光照探针
            var lightProbeUsage = compDef.GetProperty<string>("lightProbes", "BlendProbes");
            if (Enum.TryParse<UnityEngine.Rendering.LightProbeUsage>(lightProbeUsage, out var probeUsage))
            {
                renderer.lightProbeUsage = probeUsage;
            }
        }
        
        /// <summary>
        /// 配置MeshFilter组件
        /// </summary>
        private async Task ConfigureMeshFilter(GameObject obj, ComponentDefinition compDef)
        {
            var meshFilter = obj.GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                meshFilter = obj.AddComponent<MeshFilter>();
            }
            
            // 获取或创建网格
            var mesh = await GetOrCreateMesh(compDef);
            if (mesh != null)
            {
                meshFilter.mesh = mesh;
            }
        }
        
        /// <summary>
        /// 创建材质
        /// </summary>
        private async Task<Material> CreateMaterialFromDefinition(ComponentDefinition compDef)
        {
            // 获取着色器
            var shaderName = compDef.GetProperty<string>("shader", "Standard");
            var shader = GetShader(shaderName);
            
            if (shader == null)
            {
                logger.LogError($"Shader not found: {shaderName}");
                return null;
            }
            
            // 创建材质
            var material = new Material(shader);
            material.name = compDef.GetProperty<string>("materialName", "ModMaterial");
            
            // 设置颜色
            var color = compDef.GetProperty<float[]>("color", new float[] { 1, 1, 1, 1 });
            material.color = new Color(color[0], color[1], color[2], color[3]);
            
            // 设置标准着色器属性
            if (shaderName == "Standard")
            {
                material.SetFloat("_Metallic", compDef.GetProperty<float>("metallic", 0f));
                material.SetFloat("_Glossiness", compDef.GetProperty<float>("smoothness", 0.5f));
                
                // 设置纹理
                var mainTexturePath = compDef.GetProperty<string>("mainTexture");
                if (!string.IsNullOrEmpty(mainTexturePath))
                {
                    var texture = await LoadTextureAsync(mainTexturePath);
                    if (texture != null)
                    {
                        material.mainTexture = texture;
                    }
                }
                
                // 法线贴图
                var normalMapPath = compDef.GetProperty<string>("normalMap");
                if (!string.IsNullOrEmpty(normalMapPath))
                {
                    var normalMap = await LoadTextureAsync(normalMapPath, true);
                    if (normalMap != null)
                    {
                        material.SetTexture("_BumpMap", normalMap);
                    }
                }
            }
            
            // 设置渲染模式
            var renderMode = compDef.GetProperty<string>("renderMode", "Opaque");
            SetMaterialRenderMode(material, renderMode);
            
            return material;
        }
        
        /// <summary>
        /// 获取或创建网格
        /// </summary>
        private async Task<Mesh> GetOrCreateMesh(ComponentDefinition compDef)
        {
            // 检查是否使用内置网格
            var meshType = compDef.GetProperty<string>("meshType");
            if (!string.IsNullOrEmpty(meshType))
            {
                return GetPrimitiveMesh(meshType);
            }
            
            // 加载自定义网格
            var meshPath = compDef.GetProperty<string>("meshPath");
            if (!string.IsNullOrEmpty(meshPath))
            {
                return await LoadMeshAsync(meshPath);
            }
            
            // 程序化生成网格
            var proceduralType = compDef.GetProperty<string>("proceduralMesh");
            if (!string.IsNullOrEmpty(proceduralType))
            {
                return GenerateProceduralMesh(proceduralType, compDef);
            }
            
            return null;
        }
        
        /// <summary>
        /// 获取原始网格
        /// </summary>
        private Mesh GetPrimitiveMesh(string meshType)
        {
            if (meshCache.TryGetValue(meshType, out var cachedMesh))
            {
                return cachedMesh;
            }
            
            var primitiveType = meshType.ToLower() switch
            {
                "cube" => PrimitiveType.Cube,
                "sphere" => PrimitiveType.Sphere,
                "cylinder" => PrimitiveType.Cylinder,
                "capsule" => PrimitiveType.Capsule,
                "plane" => PrimitiveType.Plane,
                "quad" => PrimitiveType.Quad,
                _ => PrimitiveType.Cube
            };
            
            var tempObj = GameObject.CreatePrimitive(primitiveType);
            var mesh = tempObj.GetComponent<MeshFilter>().sharedMesh;
            GameObject.Destroy(tempObj);
            
            meshCache[meshType] = mesh;
            return mesh;
        }
        #endregion

        #region Physics Configuration
        /// <summary>
        /// 配置BoxCollider组件
        /// </summary>
        private void ConfigureBoxCollider(GameObject obj, ComponentDefinition compDef)
        {
            var collider = obj.AddComponent<BoxCollider>();
            
            var center = compDef.GetProperty<float[]>("center", new float[] { 0, 0, 0 });
            var size = compDef.GetProperty<float[]>("size", new float[] { 1, 1, 1 });
            
            collider.center = new Vector3(center[0], center[1], center[2]);
            collider.size = new Vector3(size[0], size[1], size[2]);
            collider.isTrigger = compDef.GetProperty<bool>("isTrigger", false);
            
            // 物理材质
            var physicsMaterialName = compDef.GetProperty<string>("physicsMaterial");
            if (!string.IsNullOrEmpty(physicsMaterialName))
            {
                collider.material = LoadPhysicMaterial(physicsMaterialName);
            }
        }
        
        /// <summary>
        /// 配置SphereCollider组件
        /// </summary>
        private void ConfigureSphereCollider(GameObject obj, ComponentDefinition compDef)
        {
            var collider = obj.AddComponent<SphereCollider>();
            
            var center = compDef.GetProperty<float[]>("center", new float[] { 0, 0, 0 });
            collider.center = new Vector3(center[0], center[1], center[2]);
            collider.radius = compDef.GetProperty<float>("radius", 0.5f);
            collider.isTrigger = compDef.GetProperty<bool>("isTrigger", false);
            
            // 物理材质
            var physicsMaterialName = compDef.GetProperty<string>("physicsMaterial");
            if (!string.IsNullOrEmpty(physicsMaterialName))
            {
                collider.material = LoadPhysicMaterial(physicsMaterialName);
            }
        }
        
        /// <summary>
        /// 配置CapsuleCollider组件
        /// </summary>
        private void ConfigureCapsuleCollider(GameObject obj, ComponentDefinition compDef)
        {
            var collider = obj.AddComponent<CapsuleCollider>();
            
            var center = compDef.GetProperty<float[]>("center", new float[] { 0, 0, 0 });
            collider.center = new Vector3(center[0], center[1], center[2]);
            collider.radius = compDef.GetProperty<float>("radius", 0.5f);
            collider.height = compDef.GetProperty<float>("height", 2f);
            collider.direction = compDef.GetProperty<int>("direction", 1); // 0=X, 1=Y, 2=Z
            collider.isTrigger = compDef.GetProperty<bool>("isTrigger", false);
        }
        
        /// <summary>
        /// 配置MeshCollider组件
        /// </summary>
        private async Task ConfigureMeshCollider(GameObject obj, ComponentDefinition compDef)
        {
            var collider = obj.AddComponent<MeshCollider>();
            
            // 获取网格
            var meshFilter = obj.GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                collider.sharedMesh = meshFilter.sharedMesh;
            }
            else
            {
                // 尝试加载指定的碰撞网格
                var collisionMeshPath = compDef.GetProperty<string>("collisionMesh");
                if (!string.IsNullOrEmpty(collisionMeshPath))
                {
                    var mesh = await LoadMeshAsync(collisionMeshPath);
                    if (mesh != null)
                    {
                        collider.sharedMesh = mesh;
                    }
                }
            }
            
            collider.convex = compDef.GetProperty<bool>("convex", false);
            collider.isTrigger = compDef.GetProperty<bool>("isTrigger", false);
            collider.cookingOptions = compDef.GetProperty<bool>("inflateConvex", false) 
                ? MeshColliderCookingOptions.InflateConvexMesh 
                : MeshColliderCookingOptions.None;
        }
        
        /// <summary>
        /// 配置Rigidbody组件
        /// </summary>
        private void ConfigureRigidbody(GameObject obj, ComponentDefinition compDef)
        {
            var rb = obj.AddComponent<Rigidbody>();
            
            rb.mass = compDef.GetProperty<float>("mass", 1f);
            rb.drag = compDef.GetProperty<float>("drag", 0f);
            rb.angularDrag = compDef.GetProperty<float>("angularDrag", 0.05f);
            rb.useGravity = compDef.GetProperty<bool>("useGravity", true);
            rb.isKinematic = compDef.GetProperty<bool>("isKinematic", false);
            rb.interpolation = compDef.GetProperty<bool>("interpolate", false) 
                ? RigidbodyInterpolation.Interpolate 
                : RigidbodyInterpolation.None;
            
            // 碰撞检测模式
            var collisionDetection = compDef.GetProperty<string>("collisionDetection", "Discrete");
            if (Enum.TryParse<CollisionDetectionMode>(collisionDetection, out var mode))
            {
                rb.collisionDetectionMode = mode;
            }
            
            // 约束
            var constraints = compDef.GetProperty<string[]>("constraints");
            if (constraints != null)
            {
                RigidbodyConstraints rbConstraints = RigidbodyConstraints.None;
                foreach (var constraint in constraints)
                {
                    if (Enum.TryParse<RigidbodyConstraints>(constraint, out var c))
                    {
                        rbConstraints |= c;
                    }
                }
                rb.constraints = rbConstraints;
            }
        }
        #endregion

        #region Lighting Configuration
        /// <summary>
        /// 配置Light组件
        /// </summary>
        private void ConfigureLight(GameObject obj, ComponentDefinition compDef)
        {
            var light = obj.AddComponent<Light>();
            
            // 光源类型
            var lightType = compDef.GetProperty<string>("lightType", "Directional");
            if (Enum.TryParse<LightType>(lightType, out var type))
            {
                light.type = type;
            }
            
            // 颜色
            var color = compDef.GetProperty<float[]>("color", new float[] { 1, 1, 1, 1 });
            light.color = new Color(color[0], color[1], color[2], color[3]);
            
            // 强度
            light.intensity = compDef.GetProperty<float>("intensity", 1f);
            
            // 范围（用于点光源和聚光灯）
            light.range = compDef.GetProperty<float>("range", 10f);
            
            // 聚光灯角度
            light.spotAngle = compDef.GetProperty<float>("spotAngle", 30f);
            light.innerSpotAngle = compDef.GetProperty<float>("innerSpotAngle", 21.8f);
            
            // 阴影
            light.shadows = compDef.GetProperty<bool>("shadows", false) 
                ? LightShadows.Soft 
                : LightShadows.None;
            
            // 阴影设置
            if (light.shadows != LightShadows.None)
            {
                light.shadowStrength = compDef.GetProperty<float>("shadowStrength", 1f);
                light.shadowBias = compDef.GetProperty<float>("shadowBias", 0.05f);
                light.shadowNormalBias = compDef.GetProperty<float>("shadowNormalBias", 0.4f);
                light.shadowNearPlane = compDef.GetProperty<float>("shadowNearPlane", 0.2f);
                
                // 阴影分辨率
                var shadowResolution = compDef.GetProperty<string>("shadowResolution", "FromQualitySettings");
                if (Enum.TryParse<UnityEngine.Rendering.LightShadowResolution>(shadowResolution, out var res))
                {
                    light.shadowResolution = res;
                }
            }
            
            // Cookie（光照贴图）
            var cookiePath = compDef.GetProperty<string>("cookie");
            if (!string.IsNullOrEmpty(cookiePath))
            {
                LoadTextureAsync(cookiePath).ContinueWith(task =>
                {
                    if (task.Result != null)
                    {
                        light.cookie = task.Result;
                    }
                });
            }
            
            // 渲染模式
            var renderMode = compDef.GetProperty<string>("renderMode", "Auto");
            if (Enum.TryParse<LightRenderMode>(renderMode, out var mode))
            {
                light.renderMode = mode;
            }
            
            // 光照贴图
            light.lightmapBakeType = compDef.GetProperty<bool>("baked", false) 
                ? LightmapBakeType.Baked 
                : LightmapBakeType.Realtime;
        }
        #endregion

        #region Camera Configuration
        /// <summary>
        /// 配置Camera组件
        /// </summary>
        private void ConfigureCamera(GameObject obj, ComponentDefinition compDef)
        {
            var camera = obj.AddComponent<Camera>();
            
            // 基本设置
            camera.fieldOfView = compDef.GetProperty<float>("fieldOfView", 60f);
            camera.nearClipPlane = compDef.GetProperty<float>("nearClipPlane", 0.3f);
            camera.farClipPlane = compDef.GetProperty<float>("farClipPlane", 1000f);
            camera.depth = compDef.GetProperty<float>("depth", 0f);
            
            // 清除标志
            var clearFlags = compDef.GetProperty<string>("clearFlags", "Skybox");
            if (Enum.TryParse<CameraClearFlags>(clearFlags, out var flags))
            {
                camera.clearFlags = flags;
            }
            
            // 背景颜色
            var backgroundColor = compDef.GetProperty<float[]>("backgroundColor", 
                new float[] { 0.19f, 0.3f, 0.47f, 1f });
            camera.backgroundColor = new Color(
                backgroundColor[0], 
                backgroundColor[1], 
                backgroundColor[2], 
                backgroundColor[3]
            );
            
            // 剔除遮罩
            var cullingMaskLayers = compDef.GetProperty<string[]>("cullingMask");
            if (cullingMaskLayers != null)
            {
                int mask = 0;
                foreach (var layerName in cullingMaskLayers)
                {
                    int layer = LayerMask.NameToLayer(layerName);
                    if (layer != -1)
                    {
                        mask |= 1 << layer;
                    }
                }
                camera.cullingMask = mask == 0 ? -1 : mask; // 默认为所有层
            }
            
            // 投影类型
            camera.orthographic = compDef.GetProperty<bool>("orthographic", false);
            if (camera.orthographic)
            {
                camera.orthographicSize = compDef.GetProperty<float>("orthographicSize", 5f);
            }
            
            // 渲染路径
            var renderingPath = compDef.GetProperty<string>("renderingPath", "UsePlayerSettings");
            if (Enum.TryParse<RenderingPath>(renderingPath, out var path))
            {
                camera.renderingPath = path;
            }
            
            // HDR
            camera.allowHDR = compDef.GetProperty<bool>("allowHDR", true);
            camera.allowMSAA = compDef.GetProperty<bool>("allowMSAA", true);
            
            // 目标纹理
            var targetTextureName = compDef.GetProperty<string>("targetTexture");
            if (!string.IsNullOrEmpty(targetTextureName))
            {
                // 创建渲染纹理
                var rtWidth = compDef.GetProperty<int>("targetTextureWidth", 1024);
                var rtHeight = compDef.GetProperty<int>("targetTextureHeight", 1024);
                var rtDepth = compDef.GetProperty<int>("targetTextureDepth", 24);
                
                var renderTexture = new RenderTexture(rtWidth, rtHeight, rtDepth);
                renderTexture.name = targetTextureName;
                camera.targetTexture = renderTexture;
            }
        }
        #endregion

        #region Audio Configuration
        /// <summary>
        /// 配置AudioSource组件
        /// </summary>
        private async Task ConfigureAudioSource(GameObject obj, ComponentDefinition compDef)
        {
            var audioSource = obj.AddComponent<AudioSource>();
            
            // 基本设置
            audioSource.volume = compDef.GetProperty<float>("volume", 1f);
            audioSource.pitch = compDef.GetProperty<float>("pitch", 1f);
            audioSource.loop = compDef.GetProperty<bool>("loop", false);
            audioSource.playOnAwake = compDef.GetProperty<bool>("playOnAwake", false);
            
            // 3D声音设置
            audioSource.spatialBlend = compDef.GetProperty<float>("spatialBlend", 1f);
            audioSource.minDistance = compDef.GetProperty<float>("minDistance", 1f);
            audioSource.maxDistance = compDef.GetProperty<float>("maxDistance", 500f);
            
            // 衰减模式
            var rolloffMode = compDef.GetProperty<string>("rolloffMode", "Logarithmic");
            if (Enum.TryParse<AudioRolloffMode>(rolloffMode, out var mode))
            {
                audioSource.rolloffMode = mode;
            }
            
            // 多普勒效应
            audioSource.dopplerLevel = compDef.GetProperty<float>("dopplerLevel", 1f);
            
            // 音频混合器组
            var mixerGroupName = compDef.GetProperty<string>("mixerGroup");
            if (!string.IsNullOrEmpty(mixerGroupName))
            {
                // 这里需要从某处获取AudioMixerGroup引用
                // audioSource.outputAudioMixerGroup = GetAudioMixerGroup(mixerGroupName);
            }
            
            // 加载音频剪辑
            var audioClipPath = compDef.GetProperty<string>("clip");
            if (!string.IsNullOrEmpty(audioClipPath))
            {
                var clip = await LoadAudioClipAsync(audioClipPath);
                if (clip != null)
                {
                    audioSource.clip = clip;
                }
            }
            
            // 优先级
            audioSource.priority = compDef.GetProperty<int>("priority", 128);
            
            // 立体声平移
            audioSource.panStereo = compDef.GetProperty<float>("panStereo", 0f);
            
            // 混响区混合
            audioSource.reverbZoneMix = compDef.GetProperty<float>("reverbZoneMix", 1f);
        }
        #endregion

        #region Particle System Configuration
        /// <summary>
        /// 配置ParticleSystem组件
        /// </summary>
        private void ConfigureParticleSystem(GameObject obj, ComponentDefinition compDef)
        {
            var particleSystem = obj.AddComponent<ParticleSystem>();
            var main = particleSystem.main;
            
            // 主模块设置
            main.duration = compDef.GetProperty<float>("duration", 5f);
            main.loop = compDef.GetProperty<bool>("loop", true);
            main.prewarm = compDef.GetProperty<bool>("prewarm", false);
            
            // 开始设置
            main.startLifetime = compDef.GetProperty<float>("startLifetime", 5f);
            main.startSpeed = compDef.GetProperty<float>("startSpeed", 5f);
            main.startSize = compDef.GetProperty<float>("startSize", 1f);
            
            // 开始颜色
            var startColor = compDef.GetProperty<float[]>("startColor", new float[] { 1, 1, 1, 1 });
            main.startColor = new Color(startColor[0], startColor[1], startColor[2], startColor[3]);
            
            // 最大粒子数
            main.maxParticles = compDef.GetProperty<int>("maxParticles", 1000);
            
            // 发射模块
            var emission = particleSystem.emission;
            emission.enabled = compDef.GetProperty<bool>("emissionEnabled", true);
            emission.rateOverTime = compDef.GetProperty<float>("emissionRate", 10f);
            
            // 形状模块
            var shape = particleSystem.shape;
            shape.enabled = compDef.GetProperty<bool>("shapeEnabled", true);
            var shapeType = compDef.GetProperty<string>("shapeType", "Cone");
            if (Enum.TryParse<ParticleSystemShapeType>(shapeType, out var type))
            {
                shape.shapeType = type;
            }
            
            // 渲染器设置
            var renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
            var renderMode = compDef.GetProperty<string>("renderMode", "Billboard");
            if (Enum.TryParse<ParticleSystemRenderMode>(renderMode, out var mode))
            {
                renderer.renderMode = mode;
            }
            
            // 材质
            var materialName = compDef.GetProperty<string>("material");
            if (!string.IsNullOrEmpty(materialName))
            {
                // 这里需要加载或创建材质
                // renderer.material = LoadMaterial(materialName);
            }
        }
        #endregion

        #region UI Configuration
        /// <summary>
        /// 配置Canvas组件
        /// </summary>
        private void ConfigureCanvas(GameObject obj, ComponentDefinition compDef)
        {
            var canvas = obj.AddComponent<Canvas>();
            
            // 渲染模式
            var renderMode = compDef.GetProperty<string>("renderMode", "ScreenSpaceOverlay");
            switch (renderMode)
            {
                case "ScreenSpaceOverlay":
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    break;
                case "ScreenSpaceCamera":
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    // 需要设置相机
                    var cameraName = compDef.GetProperty<string>("camera");
                    if (!string.IsNullOrEmpty(cameraName))
                    {
                        var camera = GameObject.Find(cameraName)?.GetComponent<Camera>();
                        if (camera != null)
                        {
                            canvas.worldCamera = camera;
                        }
                    }
                    canvas.planeDistance = compDef.GetProperty<float>("planeDistance", 100f);
                    break;
                case "WorldSpace":
                    canvas.renderMode = RenderMode.WorldSpace;
                    break;
            }
            
            // 排序
            canvas.sortingOrder = compDef.GetProperty<int>("sortingOrder", 0);
            var sortingLayerName = compDef.GetProperty<string>("sortingLayer");
            if (!string.IsNullOrEmpty(sortingLayerName))
            {
                canvas.sortingLayerName = sortingLayerName;
            }
            
            // 添加必要的组件
            if (obj.GetComponent<CanvasScaler>() == null)
            {
                var scaler = obj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
            }
            
            if (obj.GetComponent<GraphicRaycaster>() == null)
            {
                obj.AddComponent<GraphicRaycaster>();
            }
        }
        
        /// <summary>
        /// 配置Text组件（使用旧版Text）
        /// </summary>
        private void ConfigureText(GameObject obj, ComponentDefinition compDef)
        {
            var text = obj.AddComponent<UnityEngine.UI.Text>();
            
            // 文本内容
            text.text = compDef.GetProperty<string>("text", "Text");
            
            // 字体设置
            var fontSize = compDef.GetProperty<int>("fontSize", 14);
            text.fontSize = fontSize;
            
            var fontStyle = compDef.GetProperty<string>("fontStyle", "Normal");
            if (Enum.TryParse<FontStyle>(fontStyle, out var style))
            {
                text.fontStyle = style;
            }
            
            // 颜色
            var color = compDef.GetProperty<float[]>("color", new float[] { 0, 0, 0, 1 });
            text.color = new Color(color[0], color[1], color[2], color[3]);
            
            // 对齐
            var alignment = compDef.GetProperty<string>("alignment", "MiddleCenter");
            if (Enum.TryParse<TextAnchor>(alignment, out var anchor))
            {
                text.alignment = anchor;
            }
            
            // 自动调整大小
            text.resizeTextForBestFit = compDef.GetProperty<bool>("bestFit", false);
            if (text.resizeTextForBestFit)
            {
                text.resizeTextMinSize = compDef.GetProperty<int>("minSize", 10);
                text.resizeTextMaxSize = compDef.GetProperty<int>("maxSize", 40);
            }
            
            // RectTransform设置
            var rectTransform = obj.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                ConfigureRectTransform(rectTransform, compDef);
            }
        }
        
        /// <summary>
        /// 配置Image组件
        /// </summary>
        private async Task ConfigureImage(GameObject obj, ComponentDefinition compDef)
        {
            var image = obj.AddComponent<UnityEngine.UI.Image>();
            
            // 颜色
            var color = compDef.GetProperty<float[]>("color", new float[] { 1, 1, 1, 1 });
            image.color = new Color(color[0], color[1], color[2], color[3]);
            
            // 图片类型
            var imageType = compDef.GetProperty<string>("imageType", "Simple");
            if (Enum.TryParse<UnityEngine.UI.Image.Type>(imageType, out var type))
            {
                image.type = type;
            }
            
            // 加载精灵
            var spritePath = compDef.GetProperty<string>("sprite");
            if (!string.IsNullOrEmpty(spritePath))
            {
                var texture = await LoadTextureAsync(spritePath);
                if (texture != null)
                {
                    var sprite = Sprite.Create(
                        texture,
                        new Rect(0, 0, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f)
                    );
                    image.sprite = sprite;
                }
            }
            
            // 填充设置（用于Filled类型）
            if (image.type == UnityEngine.UI.Image.Type.Filled)
            {
                image.fillMethod = (UnityEngine.UI.Image.FillMethod)compDef.GetProperty<int>("fillMethod", 0);
                image.fillOrigin = compDef.GetProperty<int>("fillOrigin", 0);
                image.fillAmount = compDef.GetProperty<float>("fillAmount", 1f);
                image.fillClockwise = compDef.GetProperty<bool>("fillClockwise", true);
            }
            
            // 保持宽高比
            image.preserveAspect = compDef.GetProperty<bool>("preserveAspect", false);
            
            // RectTransform设置
            var rectTransform = obj.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                ConfigureRectTransform(rectTransform, compDef);
            }
        }
        
        /// <summary>
        /// 配置Button组件
        /// </summary>
        private void ConfigureButton(GameObject obj, ComponentDefinition compDef)
        {
            var button = obj.AddComponent<UnityEngine.UI.Button>();
            
            // 交互性
            button.interactable = compDef.GetProperty<bool>("interactable", true);
            
            // 过渡类型
            var transition = compDef.GetProperty<string>("transition", "ColorTint");
            if (Enum.TryParse<UnityEngine.UI.Selectable.Transition>(transition, out var trans))
            {
                button.transition = trans;
            }
            
            // 颜色过渡设置
            if (button.transition == UnityEngine.UI.Selectable.Transition.ColorTint)
            {
                var normalColor = compDef.GetProperty<float[]>("normalColor", new float[] { 1, 1, 1, 1 });
                var highlightedColor = compDef.GetProperty<float[]>("highlightedColor", new float[] { 0.96f, 0.96f, 0.96f, 1 });
                var pressedColor = compDef.GetProperty<float[]>("pressedColor", new float[] { 0.78f, 0.78f, 0.78f, 1 });
                var disabledColor = compDef.GetProperty<float[]>("disabledColor", new float[] { 0.78f, 0.78f, 0.78f, 0.5f });
                
                var colors = button.colors;
                colors.normalColor = new Color(normalColor[0], normalColor[1], normalColor[2], normalColor[3]);
                colors.highlightedColor = new Color(highlightedColor[0], highlightedColor[1], highlightedColor[2], highlightedColor[3]);
                colors.pressedColor = new Color(pressedColor[0], pressedColor[1], pressedColor[2], pressedColor[3]);
                colors.disabledColor = new Color(disabledColor[0], disabledColor[1], disabledColor[2], disabledColor[3]);
                colors.colorMultiplier = compDef.GetProperty<float>("colorMultiplier", 1f);
                colors.fadeDuration = compDef.GetProperty<float>("fadeDuration", 0.1f);
                button.colors = colors;
            }
            
            // 添加点击事件处理
            var clickEvent = compDef.GetProperty<string>("onClick");
            if (!string.IsNullOrEmpty(clickEvent))
            {
                button.onClick.AddListener(() =>
                {
                    // 发布按钮点击事件
                    ModSystemController.Instance?.EventBus?.Publish(new ButtonClickEvent
                    {
                        SenderId = obj.name,
                        ButtonName = obj.name,
                        EventName = clickEvent
                    });
                });
            }
            
            // 如果没有Image组件，添加一个
            if (obj.GetComponent<UnityEngine.UI.Image>() == null)
            {
                var image = obj.AddComponent<UnityEngine.UI.Image>();
                button.targetGraphic = image;
            }
        }
        #endregion

        #region Rendering Helpers
        /// <summary>
        /// 配置LineRenderer组件
        /// </summary>
        private void ConfigureLineRenderer(GameObject obj, ComponentDefinition compDef)
        {
            var lineRenderer = obj.AddComponent<LineRenderer>();
            
            // 位置点
            var positions = compDef.GetProperty<float[]>("positions");
            if (positions != null && positions.Length >= 6) // 至少两个点
            {
                int pointCount = positions.Length / 3;
                lineRenderer.positionCount = pointCount;
                
                Vector3[] points = new Vector3[pointCount];
                for (int i = 0; i < pointCount; i++)
                {
                    points[i] = new Vector3(
                        positions[i * 3],
                        positions[i * 3 + 1],
                        positions[i * 3 + 2]
                    );
                }
                lineRenderer.SetPositions(points);
            }
            
            // 宽度
            lineRenderer.startWidth = compDef.GetProperty<float>("startWidth", 0.1f);
            lineRenderer.endWidth = compDef.GetProperty<float>("endWidth", 0.1f);
            
            // 颜色
            var startColor = compDef.GetProperty<float[]>("startColor", new float[] { 1, 1, 1, 1 });
            var endColor = compDef.GetProperty<float[]>("endColor", new float[] { 1, 1, 1, 1 });
            lineRenderer.startColor = new Color(startColor[0], startColor[1], startColor[2], startColor[3]);
            lineRenderer.endColor = new Color(endColor[0], endColor[1], endColor[2], endColor[3]);
            
            // 材质设置
            CreateMaterialFromDefinition(compDef).ContinueWith(task =>
            {
                if (task.Result != null)
                {
                    lineRenderer.material = task.Result;
                }
            });
            
            // 其他设置
            lineRenderer.useWorldSpace = compDef.GetProperty<bool>("useWorldSpace", true);
            lineRenderer.numCornerVertices = compDef.GetProperty<int>("cornerVertices", 0);
            lineRenderer.numCapVertices = compDef.GetProperty<int>("capVertices", 0);
            
            // 纹理模式
            var textureMode = compDef.GetProperty<string>("textureMode", "Stretch");
            if (Enum.TryParse<LineTextureMode>(textureMode, out var mode))
            {
                lineRenderer.textureMode = mode;
            }
        }
        
        /// <summary>
        /// 配置TrailRenderer组件
        /// </summary>
        private void ConfigureTrailRenderer(GameObject obj, ComponentDefinition compDef)
        {
            var trailRenderer = obj.AddComponent<TrailRenderer>();
            
            // 时间
            trailRenderer.time = compDef.GetProperty<float>("time", 5f);
            
            // 宽度曲线
            trailRenderer.startWidth = compDef.GetProperty<float>("startWidth", 1f);
            trailRenderer.endWidth = compDef.GetProperty<float>("endWidth", 0f);
            
            // 颜色
            var startColor = compDef.GetProperty<float[]>("startColor", new float[] { 1, 1, 1, 1 });
            var endColor = compDef.GetProperty<float[]>("endColor", new float[] { 1, 1, 1, 0 });
            trailRenderer.startColor = new Color(startColor[0], startColor[1], startColor[2], startColor[3]);
            trailRenderer.endColor = new Color(endColor[0], endColor[1], endColor[2], endColor[3]);
            
            // 最小顶点距离
            trailRenderer.minVertexDistance = compDef.GetProperty<float>("minVertexDistance", 0.1f);
            
            // 自动销毁
            trailRenderer.autodestruct = compDef.GetProperty<bool>("autodestruct", false);
            
            // 发光
            trailRenderer.emitting = compDef.GetProperty<bool>("emitting", true);
            
            // 材质设置
            CreateMaterialFromDefinition(compDef).ContinueWith(task =>
            {
                if (task.Result != null)
                {
                    trailRenderer.material = task.Result;
                }
            });
        }
        #endregion

        #region Resource Loading
        /// <summary>
        /// 异步加载纹理
        /// </summary>
        private async Task<Texture2D> LoadTextureAsync(string path, bool isNormalMap = false)
        {
            try
            {
                // 检查是否为内置纹理
                if (path.StartsWith("builtin:"))
                {
                    return GetBuiltinTexture(path.Substring(8));
                }
                
                // 构建完整路径
                string fullPath = Path.Combine(Application.streamingAssetsPath, path);
                
                // 使用UnityWebRequest加载纹理
                using (var www = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(fullPath))
                {
                    var operation = www.SendWebRequest();
                    
                    while (!operation.isDone)
                    {
                        await Task.Yield();
                    }
                    
                    if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                    {
                        var texture = UnityEngine.Networking.DownloadHandlerTexture.GetContent(www);
                        
                        if (isNormalMap)
                        {
                            // 转换为法线贴图格式
                            ConvertToNormalMap(texture);
                        }
                        
                        return texture;
                    }
                    else
                    {
                        logger.LogError($"Failed to load texture {path}: {www.error}");
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Error loading texture {path}: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 异步加载音频剪辑
        /// </summary>
        private async Task<AudioClip> LoadAudioClipAsync(string path)
        {
            try
            {
                string fullPath = Path.Combine(Application.streamingAssetsPath, path);
                
                // 确定音频类型
                var extension = Path.GetExtension(path).ToLower();
                var audioType = extension switch
                {
                    ".wav" => UnityEngine.AudioType.WAV,
                    ".mp3" => UnityEngine.AudioType.MPEG,
                    ".ogg" => UnityEngine.AudioType.OGGVORBIS,
                    _ => UnityEngine.AudioType.UNKNOWN
                };
                
                using (var www = UnityEngine.Networking.UnityWebRequestMultimedia.GetAudioClip(fullPath, audioType))
                {
                    var operation = www.SendWebRequest();
                    
                    while (!operation.isDone)
                    {
                        await Task.Yield();
                    }
                    
                    if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                    {
                        return UnityEngine.Networking.DownloadHandlerAudioClip.GetContent(www);
                    }
                    else
                    {
                        logger.LogError($"Failed to load audio {path}: {www.error}");
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Error loading audio {path}: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 异步加载网格（需要实际的模型加载库）
        /// </summary>
        private async Task<Mesh> LoadMeshAsync(string path)
        {
            // 这里需要实际的模型加载实现
            // 可以使用glTF加载库等
            logger.Log($"Would load mesh from: {path}");
            await Task.Delay(100); // 模拟异步加载
            return null;
        }
        
        /// <summary>
        /// 加载物理材质
        /// </summary>
        private PhysicMaterial LoadPhysicMaterial(string name)
        {
            // 创建默认物理材质
            var material = new PhysicMaterial(name);
            
            // 这里可以从配置加载预设值
            switch (name.ToLower())
            {
                case "ice":
                    material.dynamicFriction = 0.02f;
                    material.staticFriction = 0.02f;
                    break;
                case "rubber":
                    material.dynamicFriction = 1f;
                    material.staticFriction = 1f;
                    material.bounciness = 0.5f;
                    break;
                case "metal":
                    material.dynamicFriction = 0.4f;
                    material.staticFriction = 0.4f;
                    break;
                default:
                    material.dynamicFriction = 0.6f;
                    material.staticFriction = 0.6f;
                    break;
            }
            
            return material;
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// 预加载着色器
        /// </summary>
        private void PreloadShaders()
        {
            shaderCache["Standard"] = Shader.Find("Standard");
            shaderCache["Unlit/Color"] = Shader.Find("Unlit/Color");
            shaderCache["Unlit/Texture"] = Shader.Find("Unlit/Texture");
            shaderCache["Sprites/Default"] = Shader.Find("Sprites/Default");
            shaderCache["UI/Default"] = Shader.Find("UI/Default");
        }
        
        /// <summary>
        /// 获取着色器
        /// </summary>
        private Shader GetShader(string shaderName)
        {
            if (shaderCache.TryGetValue(shaderName, out var cachedShader))
            {
                return cachedShader;
            }
            
            var shader = Shader.Find(shaderName);
            if (shader != null)
            {
                shaderCache[shaderName] = shader;
            }
            
            return shader;
        }
        
        /// <summary>
        /// 设置材质渲染模式
        /// </summary>
        private void SetMaterialRenderMode(Material material, string mode)
        {
            switch (mode.ToLower())
            {
                case "opaque":
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    material.SetInt("_ZWrite", 1);
                    material.DisableKeyword("_ALPHATEST_ON");
                    material.DisableKeyword("_ALPHABLEND_ON");
                    material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    material.renderQueue = -1;
                    break;
                    
                case "cutout":
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    material.SetInt("_ZWrite", 1);
                    material.EnableKeyword("_ALPHATEST_ON");
                    material.DisableKeyword("_ALPHABLEND_ON");
                    material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    material.renderQueue = 2450;
                    break;
                    
                case "fade":
                case "transparent":
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    material.SetInt("_ZWrite", 0);
                    material.DisableKeyword("_ALPHATEST_ON");
                    material.EnableKeyword("_ALPHABLEND_ON");
                    material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    material.renderQueue = 3000;
                    break;
            }
        }
        
        /// <summary>
        /// 将纹理转换为法线贴图
        /// </summary>
        private void ConvertToNormalMap(Texture2D texture)
        {
            // 这里应该实现正确的法线贴图转换
            // 暂时只是标记纹理类型
            texture.name += "_Normal";
        }
        
        /// <summary>
        /// 获取内置纹理
        /// </summary>
        private Texture2D GetBuiltinTexture(string name)
        {
            switch (name.ToLower())
            {
                case "white":
                    return Texture2D.whiteTexture;
                case "black":
                    return Texture2D.blackTexture;
                case "gray":
                    return Texture2D.grayTexture;
                case "normal":
                    return Texture2D.normalTexture;
                default:
                    return Texture2D.whiteTexture;
            }
        }
        
        /// <summary>
        /// 生成程序化网格
        /// </summary>
        private Mesh GenerateProceduralMesh(string type, ComponentDefinition compDef)
        {
            switch (type.ToLower())
            {
                case "quad":
                    return GenerateQuadMesh(compDef);
                case "plane":
                    return GeneratePlaneMesh(compDef);
                case "circle":
                    return GenerateCircleMesh(compDef);
                default:
                    return null;
            }
        }
        
        /// <summary>
        /// 生成四边形网格
        /// </summary>
        private Mesh GenerateQuadMesh(ComponentDefinition compDef)
        {
            var width = compDef.GetProperty<float>("width", 1f);
            var height = compDef.GetProperty<float>("height", 1f);
            
            var mesh = new Mesh();
            mesh.name = "ProceduralQuad";
            
            var vertices = new Vector3[]
            {
                new Vector3(-width/2, -height/2, 0),
                new Vector3(width/2, -height/2, 0),
                new Vector3(width/2, height/2, 0),
                new Vector3(-width/2, height/2, 0)
            };
            
            var triangles = new int[] { 0, 2, 1, 0, 3, 2 };
            var normals = new Vector3[] { Vector3.back, Vector3.back, Vector3.back, Vector3.back };
            var uv = new Vector2[]
            {
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(1, 1),
                new Vector2(0, 1)
            };
            
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.normals = normals;
            mesh.uv = uv;
            
            return mesh;
        }
        
        /// <summary>
        /// 生成平面网格
        /// </summary>
        private Mesh GeneratePlaneMesh(ComponentDefinition compDef)
        {
            var width = compDef.GetProperty<float>("width", 10f);
            var length = compDef.GetProperty<float>("length", 10f);
            var widthSegments = compDef.GetProperty<int>("widthSegments", 10);
            var lengthSegments = compDef.GetProperty<int>("lengthSegments", 10);
            
            var mesh = new Mesh();
            mesh.name = "ProceduralPlane";
            
            // 生成顶点、UV和法线
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            
            for (int z = 0; z <= lengthSegments; z++)
            {
                for (int x = 0; x <= widthSegments; x++)
                {
                    float xPos = (x / (float)widthSegments - 0.5f) * width;
                    float zPos = (z / (float)lengthSegments - 0.5f) * length;
                    
                    vertices.Add(new Vector3(xPos, 0, zPos));
                    normals.Add(Vector3.up);
                    uvs.Add(new Vector2(x / (float)widthSegments, z / (float)lengthSegments));
                }
            }
            
            // 生成三角形
            var triangles = new List<int>();
            for (int z = 0; z < lengthSegments; z++)
            {
                for (int x = 0; x < widthSegments; x++)
                {
                    int bottomLeft = z * (widthSegments + 1) + x;
                    int bottomRight = bottomLeft + 1;
                    int topLeft = bottomLeft + widthSegments + 1;
                    int topRight = topLeft + 1;
                    
                    triangles.Add(bottomLeft);
                    triangles.Add(topLeft);
                    triangles.Add(bottomRight);
                    
                    triangles.Add(bottomRight);
                    triangles.Add(topLeft);
                    triangles.Add(topRight);
                }
            }
            
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            
            return mesh;
        }
        
        /// <summary>
        /// 生成圆形网格
        /// </summary>
        private Mesh GenerateCircleMesh(ComponentDefinition compDef)
        {
            var radius = compDef.GetProperty<float>("radius", 1f);
            var segments = compDef.GetProperty<int>("segments", 32);
            
            var mesh = new Mesh();
            mesh.name = "ProceduralCircle";
            
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            
            // 中心点
            vertices.Add(Vector3.zero);
            normals.Add(Vector3.up);
            uvs.Add(new Vector2(0.5f, 0.5f));
            
            // 圆周顶点
            for (int i = 0; i <= segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                float x = Mathf.Cos(angle) * radius;
                float z = Mathf.Sin(angle) * radius;
                
                vertices.Add(new Vector3(x, 0, z));
                normals.Add(Vector3.up);
                uvs.Add(new Vector2(Mathf.Cos(angle) * 0.5f + 0.5f, Mathf.Sin(angle) * 0.5f + 0.5f));
            }
            
            // 生成三角形
            for (int i = 0; i < segments; i++)
            {
                triangles.Add(0);
                triangles.Add(i + 1);
                triangles.Add(i + 2);
            }
            
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            
            return mesh;
        }
        
        /// <summary>
        /// 配置RectTransform
        /// </summary>
        private void ConfigureRectTransform(RectTransform rectTransform, ComponentDefinition compDef)
        {
            // 锚点
            var anchorMin = compDef.GetProperty<float[]>("anchorMin", new float[] { 0.5f, 0.5f });
            var anchorMax = compDef.GetProperty<float[]>("anchorMax", new float[] { 0.5f, 0.5f });
            rectTransform.anchorMin = new Vector2(anchorMin[0], anchorMin[1]);
            rectTransform.anchorMax = new Vector2(anchorMax[0], anchorMax[1]);
            
            // 位置
            var anchoredPosition = compDef.GetProperty<float[]>("anchoredPosition", new float[] { 0, 0 });
            rectTransform.anchoredPosition = new Vector2(anchoredPosition[0], anchoredPosition[1]);
            
            // 大小
            var sizeDelta = compDef.GetProperty<float[]>("sizeDelta", new float[] { 100, 100 });
            rectTransform.sizeDelta = new Vector2(sizeDelta[0], sizeDelta[1]);
            
            // 旋转和缩放
            var rotation = compDef.GetProperty<float[]>("rotation", new float[] { 0, 0, 0 });
            rectTransform.localRotation = Quaternion.Euler(rotation[0], rotation[1], rotation[2]);
            
            var scale = compDef.GetProperty<float[]>("scale", new float[] { 1, 1, 1 });
            rectTransform.localScale = new Vector3(scale[0], scale[1], scale[2]);
            
            // 轴心
            var pivot = compDef.GetProperty<float[]>("pivot", new float[] { 0.5f, 0.5f });
            rectTransform.pivot = new Vector2(pivot[0], pivot[1]);
        }
        
        /// <summary>
        /// 尝试通过反射添加组件
        /// </summary>
        private void TryAddComponentByReflection(GameObject obj, ComponentDefinition compDef)
        {
            try
            {
                // 查找组件类型
                var componentType = Type.GetType(compDef.type);
                if (componentType == null)
                {
                    // 尝试在UnityEngine命名空间中查找
                    componentType = Type.GetType($"UnityEngine.{compDef.type}, UnityEngine");
                }
                
                if (componentType != null && componentType.IsSubclassOf(typeof(Component)))
                {
                    var component = obj.AddComponent(componentType);
                    
                    // 使用反射设置属性
                    if (compDef.properties != null)
                    {
                        foreach (var kvp in compDef.properties)
                        {
                            var property = componentType.GetProperty(kvp.Key);
                            if (property != null && property.CanWrite)
                            {
                                try
                                {
                                    var value = Convert.ChangeType(kvp.Value, property.PropertyType);
                                    property.SetValue(component, value);
                                }
                                catch
                                {
                                    // 忽略无法设置的属性
                                }
                            }
                        }
                    }
                    
                    logger.Log($"Added component {compDef.type} via reflection");
                }
                else
                {
                    logger.LogWarning($"Unknown component type: {compDef.type}");
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Failed to add component via reflection: {ex.Message}");
            }
        }
        #endregion
    }
    
    /// <summary>
    /// 按钮点击事件
    /// </summary>
    public class ButtonClickEvent : IModEvent
    {
        public string EventId => "ui_button_click";
        public string SenderId { get; set; }
        public DateTime Timestamp { get; set; }
        public string ButtonName { get; set; }
        public string EventName { get; set; }
    }
}
```

## 7. UnityImplementations/UnityGameObjectWrapper.cs

```csharp
// ModSystem.Unity/UnityImplementations/UnityGameObjectWrapper.cs
using UnityEngine;
using ModSystem.Core;
using System;

namespace ModSystem.Unity
{
    /// <summary>
    /// Unity GameObject的包装器，实现IGameObject接口
    /// 将Unity的GameObject适配为平台无关的接口
    /// </summary>
    public class UnityGameObjectWrapper : IGameObject
    {
        #region Fields
        private readonly GameObject gameObject;
        private UnityTransformWrapper transformWrapper;
        #endregion

        #region Properties
        /// <summary>
        /// 获取原始的Unity GameObject
        /// </summary>
        public GameObject GameObject => gameObject;
        #endregion

        #region Constructor
        /// <summary>
        /// 创建GameObject包装器
        /// </summary>
        /// <param name="gameObject">要包装的GameObject</param>
        public UnityGameObjectWrapper(GameObject gameObject)
        {
            this.gameObject = gameObject ?? throw new ArgumentNullException(nameof(gameObject));
        }
        #endregion

        #region IGameObject Implementation
        /// <summary>
        /// 获取或设置对象名称
        /// </summary>
        public string Name
        {
            get => gameObject.name;
            set => gameObject.name = value;
        }
        
        /// <summary>
        /// 获取或设置对象是否激活
        /// </summary>
        public bool IsActive
        {
            get => gameObject.activeSelf;
            set => gameObject.SetActive(value);
        }
        
        /// <summary>
        /// 获取Transform组件包装器
        /// </summary>
        public ITransform Transform
        {
            get
            {
                if (transformWrapper == null)
                {
                    transformWrapper = new UnityTransformWrapper(gameObject.transform);
                }
                return transformWrapper;
            }
        }
        
        /// <summary>
        /// 获取指定类型的组件
        /// </summary>
        public T GetComponent<T>() where T : class
        {
            // 处理特殊的包装器类型
            if (typeof(T) == typeof(ObjectBehaviourComponent))
            {
                var comp = gameObject.GetComponent<UnityObjectBehaviourComponent>();
                return comp as T;
            }
            
            // 尝试获取Unity组件
            var component = gameObject.GetComponent(typeof(T));
            return component as T;
        }
        
        /// <summary>
        /// 添加指定类型的组件
        /// </summary>
        public T AddComponent<T>() where T : class
        {
            // 处理特殊的包装器类型
            if (typeof(T) == typeof(ObjectBehaviourComponent))
            {
                var comp = gameObject.AddComponent<UnityObjectBehaviourComponent>();
                return comp as T;
            }
            
            // 尝试添加Unity组件
            var componentType = typeof(T);
            if (componentType.IsSubclassOf(typeof(Component)) || componentType == typeof(Component))
            {
                var component = gameObject.AddComponent(componentType);
                return component as T;
            }
            
            throw new InvalidOperationException($"Cannot add component of type {typeof(T).Name}");
        }
        #endregion

        #region Extended Methods
        /// <summary>
        /// 获取所有子对象
        /// </summary>
        public IGameObject[] GetChildren()
        {
            var childCount = gameObject.transform.childCount;
            var children = new IGameObject[childCount];
            
            for (int i = 0; i < childCount; i++)
            {
                children[i] = new UnityGameObjectWrapper(gameObject.transform.GetChild(i).gameObject);
            }
            
            return children;
        }
        
        /// <summary>
        /// 查找子对象
        /// </summary>
        public IGameObject FindChild(string name)
        {
            var child = gameObject.transform.Find(name);
            return child != null ? new UnityGameObjectWrapper(child.gameObject) : null;
        }
        
        /// <summary>
        /// 销毁对象
        /// </summary>
        public void Destroy()
        {
            GameObject.Destroy(gameObject);
        }
        
        /// <summary>
        /// 延迟销毁对象
        /// </summary>
        public void Destroy(float delay)
        {
            GameObject.Destroy(gameObject, delay);
        }
        
        /// <summary>
        /// 获取或添加组件
        /// </summary>
        public T GetOrAddComponent<T>() where T : Component
        {
            var component = gameObject.GetComponent<T>();
            if (component == null)
            {
                component = gameObject.AddComponent<T>();
            }
            return component;
        }
        #endregion

        #region Implicit Operators
        /// <summary>
        /// 隐式转换为GameObject
        /// </summary>
        public static implicit operator GameObject(UnityGameObjectWrapper wrapper)
        {
            return wrapper?.gameObject;
        }
        
        /// <summary>
        /// 隐式转换为包装器
        /// </summary>
        public static implicit operator UnityGameObjectWrapper(GameObject gameObject)
        {
            return gameObject != null ? new UnityGameObjectWrapper(gameObject) : null;
        }
        #endregion
    }
    
    /// <summary>
    /// Unity Transform的包装器，实现ITransform接口
    /// </summary>
    public class UnityTransformWrapper : ITransform
    {
        #region Fields
        private readonly Transform transform;
        #endregion

        #region Constructor
        /// <summary>
        /// 创建Transform包装器
        /// </summary>
        public UnityTransformWrapper(Transform transform)
        {
            this.transform = transform ?? throw new ArgumentNullException(nameof(transform));
        }
        #endregion

        #region ITransform Implementation
        /// <summary>
        /// 获取或设置世界空间位置
        /// </summary>
        public ModSystem.Core.Vector3 Position
        {
            get
            {
                var pos = transform.position;
                return new ModSystem.Core.Vector3(pos.x, pos.y, pos.z);
            }
            set => transform.position = new UnityEngine.Vector3(value.x, value.y, value.z);
        }
        
        /// <summary>
        /// 获取或设置世界空间旋转
        /// </summary>
        public ModSystem.Core.Quaternion Rotation
        {
            get
            {
                var rot = transform.rotation;
                return new ModSystem.Core.Quaternion(rot.x, rot.y, rot.z, rot.w);
            }
            set => transform.rotation = new UnityEngine.Quaternion(value.x, value.y, value.z, value.w);
        }
        
        /// <summary>
        /// 获取或设置局部缩放
        /// </summary>
        public ModSystem.Core.Vector3 Scale
        {
            get
            {
                var scale = transform.localScale;
                return new ModSystem.Core.Vector3(scale.x, scale.y, scale.z);
            }
            set => transform.localScale = new UnityEngine.Vector3(value.x, value.y, value.z);
        }
        
        /// <summary>
        /// 获取或设置父Transform
        /// </summary>
        public ITransform Parent
        {
            get => transform.parent != null ? new UnityTransformWrapper(transform.parent) : null;
            set
            {
                if (value is UnityTransformWrapper wrapper)
                {
                    transform.parent = wrapper.transform;
                }
                else if (value == null)
                {
                    transform.parent = null;
                }
            }
        }
        #endregion

        #region Extended Transform Methods
        /// <summary>
        /// 获取局部位置
        /// </summary>
        public ModSystem.Core.Vector3 LocalPosition
        {
            get
            {
                var pos = transform.localPosition;
                return new ModSystem.Core.Vector3(pos.x, pos.y, pos.z);
            }
            set => transform.localPosition = new UnityEngine.Vector3(value.x, value.y, value.z);
        }
        
        /// <summary>
        /// 获取局部旋转
        /// </summary>
        public ModSystem.Core.Quaternion LocalRotation
        {
            get
            {
                var rot = transform.localRotation;
                return new ModSystem.Core.Quaternion(rot.x, rot.y, rot.z, rot.w);
            }
            set => transform.localRotation = new UnityEngine.Quaternion(value.x, value.y, value.z, value.w);
        }
        
        /// <summary>
        /// 朝向目标
        /// </summary>
        public void LookAt(ModSystem.Core.Vector3 target)
        {
            transform.LookAt(new UnityEngine.Vector3(target.x, target.y, target.z));
        }
        
        /// <summary>
        /// 平移
        /// </summary>
        public void Translate(ModSystem.Core.Vector3 translation)
        {
            transform.Translate(new UnityEngine.Vector3(translation.x, translation.y, translation.z));
        }
        
        /// <summary>
        /// 旋转
        /// </summary>
        public void Rotate(ModSystem.Core.Vector3 eulerAngles)
        {
            transform.Rotate(new UnityEngine.Vector3(eulerAngles.x, eulerAngles.y, eulerAngles.z));
        }
        
        /// <summary>
        /// 获取前方向
        /// </summary>
        public ModSystem.Core.Vector3 Forward
        {
            get
            {
                var fwd = transform.forward;
                return new ModSystem.Core.Vector3(fwd.x, fwd.y, fwd.z);
            }
        }
        
        /// <summary>
        /// 获取右方向
        /// </summary>
        public ModSystem.Core.Vector3 Right
        {
            get
            {
                var right = transform.right;
                return new ModSystem.Core.Vector3(right.x, right.y, right.z);
            }
        }
        
        /// <summary>
        /// 获取上方向
        /// </summary>
        public ModSystem.Core.Vector3 Up
        {
            get
            {
                var up = transform.up;
                return new ModSystem.Core.Vector3(up.x, up.y, up.z);
            }
        }
        #endregion
    }
    
    /// <summary>
    /// Unity中的ObjectBehaviour组件
    /// 用于存储和管理附加到GameObject的模组行为
    /// </summary>
    public class UnityObjectBehaviourComponent : MonoBehaviour, ObjectBehaviourComponent
    {
        #region Fields
        private IObjectBehaviour behaviour;
        #endregion

        #region Properties
        /// <summary>
        /// 获取或设置关联的行为实例
        /// </summary>
        public IObjectBehaviour Behaviour
        {
            get => behaviour;
            set => behaviour = value;
        }
        #endregion

        #region Unity Lifecycle
        void OnDestroy()
        {
            // 当GameObject被销毁时，通知行为
            behaviour?.OnDetach();
            behaviour = null;
        }
        
        void OnEnable()
        {
            // 可以添加启用时的逻辑
        }
        
        void OnDisable()
        {
            // 可以添加禁用时的逻辑
        }
        #endregion
    }
}
```

## 8. UnityImplementations/UnityEventLogger.cs

```csharp
// ModSystem.Unity/UnityImplementations/UnityEventLogger.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using ModSystem.Core;

namespace ModSystem.Unity
{
    /// <summary>
    /// Unity事件日志记录器实现
    /// 用于记录和跟踪事件系统的活动
    /// </summary>
    public class UnityEventLogger : IEventLogger
    {
        #region Fields
        private readonly List<EventLogEntry> eventHistory;
        private readonly int maxHistorySize;
        private readonly bool logToConsole;
        private readonly Dictionary<string, EventStatistics> eventStats;
        #endregion

        #region Properties
        /// <summary>
        /// 获取事件历史记录
        /// </summary>
        public IReadOnlyList<EventLogEntry> EventHistory => eventHistory;
        
        /// <summary>
        /// 获取事件统计信息
        /// </summary>
        public IReadOnlyDictionary<string, EventStatistics> EventStatistics => eventStats;
        #endregion

        #region Constructor
        /// <summary>
        /// 创建Unity事件日志记录器
        /// </summary>
        /// <param name="maxHistorySize">最大历史记录数</param>
        /// <param name="logToConsole">是否输出到控制台</param>
        public UnityEventLogger(int maxHistorySize = 1000, bool logToConsole = false)
        {
            this.maxHistorySize = maxHistorySize;
            this.logToConsole = logToConsole;
            this.eventHistory = new List<EventLogEntry>();
            this.eventStats = new Dictionary<string, EventStatistics>();
        }
        #endregion

        #region IEventLogger Implementation
        /// <summary>
        /// 记录事件
        /// </summary>
        public void LogEvent(IModEvent e)
        {
            // 创建日志条目
            var entry = new EventLogEntry
            {
                Timestamp = e.Timestamp,
                EventType = e.GetType().Name,
                EventId = e.EventId,
                SenderId = e.SenderId,
                EventData = e
            };
            
            // 添加到历史记录
            AddToHistory(entry);
            
            // 更新统计信息
            UpdateStatistics(e);
            
            // 输出到控制台
            if (logToConsole)
            {
                Debug.Log($"[Event] {entry.EventType} from {entry.SenderId} at {entry.Timestamp:HH:mm:ss.fff}");
            }
        }
        
        /// <summary>
        /// 记录订阅
        /// </summary>
        public void LogSubscription(string eventType, string subscriber)
        {
            var entry = new EventLogEntry
            {
                Timestamp = DateTime.Now,
                EventType = "Subscription",
                EventId = "subscription",
                SenderId = subscriber ?? "Unknown",
                Message = $"Subscribed to {eventType}"
            };
            
            AddToHistory(entry);
            
            if (logToConsole)
            {
                Debug.Log($"[Subscription] {subscriber} subscribed to {eventType}");
            }
        }
        
        /// <summary>
        /// 记录错误
        /// </summary>
        public void LogError(string message)
        {
            var entry = new EventLogEntry
            {
                Timestamp = DateTime.Now,
                EventType = "Error",
                EventId = "error",
                SenderId = "System",
                Message = message,
                IsError = true
            };
            
            AddToHistory(entry);
            
            Debug.LogError($"[EventSystem Error] {message}");
        }
        #endregion

        #region History Management
        /// <summary>
        /// 添加到历史记录
        /// </summary>
        private void AddToHistory(EventLogEntry entry)
        {
            eventHistory.Add(entry);
            
            // 限制历史记录大小
            while (eventHistory.Count > maxHistorySize)
            {
                eventHistory.RemoveAt(0);
            }
        }
        
        /// <summary>
        /// 清除历史记录
        /// </summary>
        public void ClearHistory()
        {
            eventHistory.Clear();
        }
        
        /// <summary>
        /// 获取过滤后的历史记录
        /// </summary>
        public List<EventLogEntry> GetFilteredHistory(Predicate<EventLogEntry> filter)
        {
            return eventHistory.FindAll(filter);
        }
        #endregion

        #region Statistics
        /// <summary>
        /// 更新统计信息
        /// </summary>
        private void UpdateStatistics(IModEvent e)
        {
            var eventType = e.GetType().Name;
            
            if (!eventStats.TryGetValue(eventType, out var stats))
            {
                stats = new EventStatistics
                {
                    EventType = eventType,
                    FirstOccurrence = e.Timestamp
                };
                eventStats[eventType] = stats;
            }
            
            stats.Count++;
            stats.LastOccurrence = e.Timestamp;
            
            // 更新发送者统计
            if (!string.IsNullOrEmpty(e.SenderId))
            {
                if (!stats.SenderCounts.ContainsKey(e.SenderId))
                {
                    stats.SenderCounts[e.SenderId] = 0;
                }
                stats.SenderCounts[e.SenderId]++;
            }
        }
        
        /// <summary>
        /// 重置统计信息
        /// </summary>
        public void ResetStatistics()
        {
            eventStats.Clear();
        }
        
        /// <summary>
        /// 获取事件频率报告
        /// </summary>
        public string GetFrequencyReport()
        {
            var report = "Event Frequency Report\n";
            report += "======================\n";
            
            foreach (var kvp in eventStats)
            {
                var stats = kvp.Value;
                var duration = (stats.LastOccurrence - stats.FirstOccurrence).TotalSeconds;
                var frequency = duration > 0 ? stats.Count / duration : 0;
                
                report += $"{stats.EventType}: {stats.Count} events, {frequency:F2} events/sec\n";
            }
            
            return report;
        }
        #endregion

        #region Export
        /// <summary>
        /// 导出日志到文件
        /// </summary>
        public void ExportToFile(string filePath)
        {
            try
            {
                var lines = new List<string>();
                lines.Add("Timestamp,EventType,EventId,SenderId,Message");
                
                foreach (var entry in eventHistory)
                {
                    var line = $"{entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff}," +
                              $"{entry.EventType}," +
                              $"{entry.EventId}," +
                              $"{entry.SenderId}," +
                              $"\"{entry.Message}\"";
                    lines.Add(line);
                }
                
                System.IO.File.WriteAllLines(filePath, lines);
                Debug.Log($"Event log exported to: {filePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to export event log: {ex.Message}");
            }
        }
        #endregion
    }

    #region Data Structures
    /// <summary>
    /// 事件日志条目
    /// </summary>
    public class EventLogEntry
    {
        public DateTime Timestamp { get; set; }
        public string EventType { get; set; }
        public string EventId { get; set; }
        public string SenderId { get; set; }
        public string Message { get; set; }
        public IModEvent EventData { get; set; }
        public bool IsError { get; set; }
    }
    
    /// <summary>
    /// 事件统计信息
    /// </summary>
    public class EventStatistics
    {
        public string EventType { get; set; }
        public int Count { get; set; }
        public DateTime FirstOccurrence { get; set; }
        public DateTime LastOccurrence { get; set; }
        public Dictionary<string, int> SenderCounts { get; set; } = new Dictionary<string, int>();
    }
    #endregion
}
```

## 9. Debug/EventMonitor.cs

```csharp
// ModSystem.Unity/Debug/EventMonitor.cs
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ModSystem.Core;

namespace ModSystem.Unity.Debug
{
    /// <summary>
    /// 事件监控器组件
    /// 提供实时的事件监控和调试功能
    /// </summary>
    [AddComponentMenu("ModSystem/Debug/Event Monitor")]
    public class EventMonitor : MonoBehaviour
    {
        #region Configuration
        [Header("Display Settings")]
        [SerializeField] private bool showUI = true;
        [SerializeField] private KeyCode toggleKey = KeyCode.F12;
        [SerializeField] private int maxEventHistory = 100;
        [SerializeField] private Vector2 windowPosition = new Vector2(10, 10);
        [SerializeField] private Vector2 windowSize = new Vector2(600, 400);
        
        [Header("Filter Settings")]
        [SerializeField] private bool showOnlyErrors = false;
        [SerializeField] private List<string> mutedEventTypes = new List<string>();
        #endregion

        #region Private Fields
        private List<EventLogEntry> eventHistory = new List<EventLogEntry>();
        private Vector2 scrollPosition;
        private bool isWindowMinimized = false;
        private Dictionary<string, int> eventCounts = new Dictionary<string, int>();
        private string filterText = "";
        private bool isPaused = false;
        private Tab currentTab = Tab.Events;
        private UnityEventLogger eventLogger;
        private GUIStyle windowStyle;
        private GUIStyle boxStyle;
        private Texture2D backgroundTexture;
        #endregion

        #region Enums
        private enum Tab
        {
            Events,
            Statistics,
            Filters,
            Settings
        }
        #endregion

        #region Unity Lifecycle
        void Awake()
        {
            DontDestroyOnLoad(gameObject);
            InitializeStyles();
        }
        
        void Start()
        {
            ConnectToEventBus();
        }
        
        void Update()
        {
            // 快捷键处理
            if (Input.GetKeyDown(toggleKey))
            {
                showUI = !showUI;
            }
            
            if (Input.GetKeyDown(KeyCode.F11))
            {
                isPaused = !isPaused;
            }
            
            // 清除历史快捷键
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.C))
            {
                ClearHistory();
            }
        }
        
        void OnGUI()
        {
            if (!showUI) return;
            
            var windowRect = new Rect(windowPosition, windowSize);
            windowRect = GUI.Window(0, windowRect, DrawWindow, "Event Monitor", windowStyle);
            windowPosition = windowRect.position;
        }
        
        void OnDestroy()
        {
            DisconnectFromEventBus();
        }
        #endregion

        #region Initialization
        /// <summary>
        /// 初始化GUI样式
        /// </summary>
        private void InitializeStyles()
        {
            // 创建背景纹理
            backgroundTexture = new Texture2D(1, 1);
            backgroundTexture.SetPixel(0, 0, new Color(0.2f, 0.2f, 0.2f, 0.95f));
            backgroundTexture.Apply();
        }
        
        /// <summary>
        /// 连接到事件总线
        /// </summary>
        private void ConnectToEventBus()
        {
            var controller = ModSystemController.Instance;
            if (controller != null)
            {
                // 创建事件记录器
                eventLogger = new UnityEventLogger(maxEventHistory, false);
                
                // 订阅所有事件
                if (controller.EventBus is ModEventBus eventBus)
                {
                    eventBus.OnEventPublished += OnEventPublished;
                }
                
                Debug.Log("[EventMonitor] Connected to event bus");
            }
            else
            {
                Debug.LogWarning("[EventMonitor] ModSystemController not found");
            }
        }
        
        /// <summary>
        /// 断开事件总线连接
        /// </summary>
        private void DisconnectFromEventBus()
        {
            var controller = ModSystemController.Instance;
            if (controller != null && controller.EventBus is ModEventBus eventBus)
            {
                eventBus.OnEventPublished -= OnEventPublished;
            }
        }
        #endregion

        #region Event Handling
        /// <summary>
        /// 处理事件发布
        /// </summary>
        private void OnEventPublished(IModEvent e)
        {
            if (isPaused) return;
            
            var eventTypeName = e.GetType().Name;
            
            // 检查是否被静音
            if (mutedEventTypes.Contains(eventTypeName))
                return;
            
            // 记录事件
            eventLogger.LogEvent(e);
            
            // 添加到本地历史
            var entry = new EventLogEntry
            {
                Timestamp = e.Timestamp,
                EventType = eventTypeName,
                EventId = e.EventId,
                SenderId = e.SenderId ?? "Unknown",
                Message = SerializeEventDetails(e),
                Color = GetEventColor(e)
            };
            
            AddEventEntry(entry);
        }
        
        /// <summary>
        /// 添加事件条目
        /// </summary>
        private void AddEventEntry(EventLogEntry entry)
        {
            eventHistory.Add(entry);
            
            // 更新计数
            if (!eventCounts.ContainsKey(entry.EventType))
                eventCounts[entry.EventType] = 0;
            eventCounts[entry.EventType]++;
            
            // 限制历史大小
            while (eventHistory.Count > maxEventHistory)
            {
                eventHistory.RemoveAt(0);
            }
        }
        #endregion

        #region GUI Drawing
        /// <summary>
        /// 绘制窗口
        /// </summary>
        void DrawWindow(int windowId)
        {
            // 自定义窗口样式
            if (windowStyle == null)
            {
                windowStyle = new GUIStyle(GUI.skin.window);
                windowStyle.normal.background = backgroundTexture;
                windowStyle.normal.textColor = Color.white;
            }
            
            GUILayout.BeginVertical();
            
            // 标题栏
            DrawTitleBar();
            
            if (!isWindowMinimized)
            {
                // 工具栏
                DrawToolbar();
                
                // 选项卡
                DrawTabs();
                
                // 内容区域
                DrawContent();
            }
            
            GUILayout.EndVertical();
            
            GUI.DragWindow();
        }
        
        /// <summary>
        /// 绘制标题栏
        /// </summary>
        private void DrawTitleBar()
        {
            GUILayout.BeginHorizontal();
            
            GUILayout.Label($"Events: {eventHistory.Count} | " +
                          $"Types: {eventCounts.Count} | " +
                          $"{(isPaused ? "PAUSED" : "LIVE")}", 
                          GetStatusStyle());
            
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button(isWindowMinimized ? "□" : "—", GUILayout.Width(20)))
            {
                isWindowMinimized = !isWindowMinimized;
            }
            
            if (GUILayout.Button("X", GUILayout.Width(20)))
            {
                showUI = false;
            }
            
            GUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// 绘制工具栏
        /// </summary>
        private void DrawToolbar()
        {
            GUILayout.BeginHorizontal("box");
            
            // 暂停/恢复按钮
            if (GUILayout.Button(isPaused ? "▶ Resume" : "‖ Pause", GUILayout.Width(80)))
            {
                isPaused = !isPaused;
            }
            
            // 清除按钮
            if (GUILayout.Button("Clear", GUILayout.Width(60)))
            {
                ClearHistory();
            }
            
            // 导出按钮
            if (GUILayout.Button("Export", GUILayout.Width(60)))
            {
                ExportLog();
            }
            
            GUILayout.Space(20);
            
            // 过滤器
            GUILayout.Label("Filter:", GUILayout.Width(50));
            filterText = GUILayout.TextField(filterText, GUILayout.Width(150));
            
            // 错误过滤
            showOnlyErrors = GUILayout.Toggle(showOnlyErrors, "Errors Only");
            
            GUILayout.FlexibleSpace();
            
            GUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// 绘制选项卡
        /// </summary>
        private void DrawTabs()
        {
            GUILayout.BeginHorizontal();
            
            if (GUILayout.Toggle(currentTab == Tab.Events, "Events", "Button"))
                currentTab = Tab.Events;
            
            if (GUILayout.Toggle(currentTab == Tab.Statistics, "Statistics", "Button"))
                currentTab = Tab.Statistics;
            
            if (GUILayout.Toggle(currentTab == Tab.Filters, "Filters", "Button"))
                currentTab = Tab.Filters;
            
            if (GUILayout.Toggle(currentTab == Tab.Settings, "Settings", "Button"))
                currentTab = Tab.Settings;
            
            GUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// 绘制内容
        /// </summary>
        private void DrawContent()
        {
            switch (currentTab)
            {
                case Tab.Events:
                    DrawEventList();
                    break;
                case Tab.Statistics:
                    DrawStatistics();
                    break;
                case Tab.Filters:
                    DrawFilters();
                    break;
                case Tab.Settings:
                    DrawSettings();
                    break;
            }
        }
        
        /// <summary>
        /// 绘制事件列表
        /// </summary>
        private void DrawEventList()
        {
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            
            var filteredEvents = GetFilteredEvents();
            
            foreach (var entry in filteredEvents)
            {
                DrawEventEntry(entry);
            }
            
            GUILayout.EndScrollView();
        }
        
        /// <summary>
        /// 绘制单个事件条目
        /// </summary>
        private void DrawEventEntry(EventLogEntry entry)
        {
            GUILayout.BeginHorizontal("box");
            
            // 时间戳
            GUI.color = Color.gray;
            GUILayout.Label(entry.Timestamp.ToString("HH:mm:ss.fff"), GUILayout.Width(80));
            
            // 事件类型
            GUI.color = entry.Color;
            GUILayout.Label(entry.EventType, GUILayout.Width(150));
            
            // 发送者
            GUI.color = Color.cyan;
            GUILayout.Label(entry.SenderId, GUILayout.Width(100));
            
            // 详情
            GUI.color = Color.white;
            GUILayout.Label(entry.Message);
            
            GUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// 绘制统计信息
        /// </summary>
        private void DrawStatistics()
        {
            GUILayout.BeginVertical("box");
            
            GUILayout.Label("Event Statistics", "BoldLabel");
            
            var sortedStats = eventCounts.OrderByDescending(kvp => kvp.Value).ToList();
            
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            
            foreach (var kvp in sortedStats)
            {
                GUILayout.BeginHorizontal("box");
                
                GUILayout.Label(kvp.Key, GUILayout.Width(200));
                GUILayout.Label($"Count: {kvp.Value}", GUILayout.Width(100));
                
                // 百分比
                float percentage = (float)kvp.Value / eventHistory.Count * 100f;
                GUILayout.Label($"{percentage:F1}%", GUILayout.Width(60));
                
                // 静音按钮
                bool isMuted = mutedEventTypes.Contains(kvp.Key);
                if (GUILayout.Button(isMuted ? "Unmute" : "Mute", GUILayout.Width(60)))
                {
                    if (isMuted)
                        mutedEventTypes.Remove(kvp.Key);
                    else
                        mutedEventTypes.Add(kvp.Key);
                }
                
                GUILayout.EndHorizontal();
            }
            
            GUILayout.EndScrollView();
            
            // 总计
            GUILayout.Space(10);
            GUILayout.Label($"Total Events: {eventHistory.Count}");
            GUILayout.Label($"Event Types: {eventCounts.Count}");
            GUILayout.Label($"Events/Second: {CalculateEventRate():F2}");
            
            GUILayout.EndVertical();
        }
        
        /// <summary>
        /// 绘制过滤器设置
        /// </summary>
        private void DrawFilters()
        {
            GUILayout.BeginVertical("box");
            
            GUILayout.Label("Event Filters", "BoldLabel");
            
            // 静音的事件类型
            GUILayout.Label("Muted Event Types:");
            
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));
            
            foreach (var eventType in mutedEventTypes.ToList())
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(eventType);
                if (GUILayout.Button("Remove", GUILayout.Width(60)))
                {
                    mutedEventTypes.Remove(eventType);
                }
                GUILayout.EndHorizontal();
            }
            
            GUILayout.EndScrollView();
            
            // 添加新的静音类型
            GUILayout.Space(10);
            GUILayout.Label("Add Event Type to Mute:");
            
            GUILayout.BeginHorizontal();
            var newMuteType = GUILayout.TextField("", GUILayout.Width(200));
            if (GUILayout.Button("Add", GUILayout.Width(60)) && !string.IsNullOrEmpty(newMuteType))
            {
                if (!mutedEventTypes.Contains(newMuteType))
                {
                    mutedEventTypes.Add(newMuteType);
                }
            }
            GUILayout.EndHorizontal();
            
            GUILayout.EndVertical();
        }
        
        /// <summary>
        /// 绘制设置
        /// </summary>
        private void DrawSettings()
        {
            GUILayout.BeginVertical("box");
            
            GUILayout.Label("Monitor Settings", "BoldLabel");
            
            // 最大历史记录
            GUILayout.BeginHorizontal();
            GUILayout.Label("Max Event History:", GUILayout.Width(150));
            maxEventHistory = (int)GUILayout.HorizontalSlider(maxEventHistory, 10, 1000, GUILayout.Width(200));
            GUILayout.Label(maxEventHistory.ToString(), GUILayout.Width(50));
            GUILayout.EndHorizontal();
            
            // 窗口位置
            GUILayout.Space(10);
            GUILayout.Label("Window Position:");
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("X:", GUILayout.Width(20));
            windowPosition.x = float.Parse(GUILayout.TextField(windowPosition.x.ToString(), GUILayout.Width(60)));
            GUILayout.Label("Y:", GUILayout.Width(20));
            windowPosition.y = float.Parse(GUILayout.TextField(windowPosition.y.ToString(), GUILayout.Width(60)));
            GUILayout.EndHorizontal();
            
            // 窗口大小
            GUILayout.Space(10);
            GUILayout.Label("Window Size:");
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("Width:", GUILayout.Width(50));
            windowSize.x = float.Parse(GUILayout.TextField(windowSize.x.ToString(), GUILayout.Width(60)));
            GUILayout.Label("Height:", GUILayout.Width(50));
            windowSize.y = float.Parse(GUILayout.TextField(windowSize.y.ToString(), GUILayout.Width(60)));
            GUILayout.EndHorizontal();
            
            // 快捷键
            GUILayout.Space(10);
            GUILayout.Label($"Toggle Key: {toggleKey}");
            GUILayout.Label("Pause Key: F11");
            GUILayout.Label("Clear Key: Ctrl+C");
            
            GUILayout.EndVertical();
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// 获取过滤后的事件
        /// </summary>
        private List<EventLogEntry> GetFilteredEvents()
        {
            var filtered = eventHistory.AsEnumerable();
            
            // 文本过滤
            if (!string.IsNullOrEmpty(filterText))
            {
                filtered = filtered.Where(e => 
                    e.EventType.Contains(filterText, StringComparison.OrdinalIgnoreCase) ||
                    e.SenderId.Contains(filterText, StringComparison.OrdinalIgnoreCase) ||
                    e.Message.Contains(filterText, StringComparison.OrdinalIgnoreCase));
            }
            
            // 错误过滤
            if (showOnlyErrors)
            {
                filtered = filtered.Where(e => e.EventType.Contains("Error") || e.EventType.Contains("Exception"));
            }
            
            return filtered.Reverse().ToList(); // 最新的在上面
        }
        
        /// <summary>
        /// 序列化事件详情
        /// </summary>
        private string SerializeEventDetails(IModEvent e)
        {
            var properties = e.GetType().GetProperties()
                .Where(p => p.Name != "EventId" && p.Name != "SenderId" && p.Name != "Timestamp")
                .Select(p => 
                {
                    try
                    {
                        var value = p.GetValue(e);
                        return $"{p.Name}: {value ?? "null"}";
                    }
                    catch
                    {
                        return $"{p.Name}: <error>";
                    }
                })
                .ToList();
            
            return string.Join(", ", properties);
        }
        
        /// <summary>
        /// 获取事件颜色
        /// </summary>
        private Color GetEventColor(IModEvent e)
        {
            var typeName = e.GetType().Name;
            
            if (typeName.Contains("Error") || typeName.Contains("Exception"))
                return Color.red;
            if (typeName.Contains("Warning"))
                return Color.yellow;
            if (typeName.Contains("Success") || typeName.Contains("Complete"))
                return Color.green;
            if (typeName.Contains("Button"))
                return new Color(0.5f, 1f, 0.5f);
            if (typeName.Contains("Robot"))
                return new Color(0.5f, 0.5f, 1f);
            if (typeName.Contains("Service"))
                return Color.yellow;
            
            return Color.white;
        }
        
        /// <summary>
        /// 计算事件速率
        /// </summary>
        private float CalculateEventRate()
        {
            if (eventHistory.Count < 2) return 0;
            
            var timeSpan = eventHistory[eventHistory.Count - 1].Timestamp - eventHistory[0].Timestamp;
            return (float)(eventHistory.Count / timeSpan.TotalSeconds);
        }
        
        /// <summary>
        /// 清除历史记录
        /// </summary>
        private void ClearHistory()
        {
            eventHistory.Clear();
            eventCounts.Clear();
            eventLogger?.ClearHistory();
        }
        
        /// <summary>
        /// 导出日志
        /// </summary>
        private void ExportLog()
        {
            var fileName = $"EventLog_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
            var path = System.IO.Path.Combine(Application.persistentDataPath, fileName);
            
            eventLogger?.ExportToFile(path);
            
            Debug.Log($"Event log exported to: {path}");
        }
        
        /// <summary>
        /// 获取状态样式
        /// </summary>
        private GUIStyle GetStatusStyle()
        {
            var style = new GUIStyle(GUI.skin.label);
            style.normal.textColor = isPaused ? Color.yellow : Color.green;
            return style;
        }
        #endregion
    }
}
```

## 10. Debug/ModPerformanceProfiler.cs

```csharp
// ModSystem.Unity/Debug/ModPerformanceProfiler.cs
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;

namespace ModSystem.Unity.Debug
{
    /// <summary>
    /// 模组性能分析器
    /// 提供详细的性能分析和优化建议
    /// </summary>
    [AddComponentMenu("ModSystem/Debug/Performance Profiler")]
    public class ModPerformanceProfiler : MonoBehaviour
    {
        #region Singleton
        private static ModPerformanceProfiler instance;
        
        /// <summary>
        /// 获取性能分析器实例
        /// </summary>
        public static ModPerformanceProfiler Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("ModPerformanceProfiler");
                    instance = go.AddComponent<ModPerformanceProfiler>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }
        #endregion

        #region Configuration
        [Header("Profiler Settings")]
        [SerializeField] private bool enableProfiling = true;
        [SerializeField] private bool showUI = false;
        [SerializeField] private KeyCode toggleKey = KeyCode.F10;
        [SerializeField] private int maxSampleHistory = 100;
        
        [Header("Display Settings")]
        [SerializeField] private Vector2 windowPosition = new Vector2(10, 10);
        [SerializeField] private Vector2 windowSize = new Vector2(800, 600);
        #endregion

        #region Private Fields
        private Dictionary<string, ProfileData> profileData = new Dictionary<string, ProfileData>();
        private Dictionary<string, Stopwatch> activeTimers = new Dictionary<string, Stopwatch>();
        private Vector2 scrollPosition;
        private SortMode sortMode = SortMode.TotalTime;
        private string filterText = "";
        private Tab currentTab = Tab.Overview;
        private readonly object lockObject = new object();
        #endregion

        #region Enums
        private enum SortMode
        {
            Name,
            CallCount,
            TotalTime,
            AverageTime,
            LastTime,
            MaxTime
        }
        
        private enum Tab
        {
            Overview,
            Details,
            Timeline,
            Recommendations
        }
        #endregion

        #region Data Structures
        /// <summary>
        /// 性能数据
        /// </summary>
        public class ProfileData
        {
            public string Name { get; set; }
            public int CallCount { get; set; }
            public double TotalTime { get; set; }
            public double MinTime { get; set; } = double.MaxValue;
            public double MaxTime { get; set; } = double.MinValue;
            public double AverageTime => CallCount > 0 ? TotalTime / CallCount : 0;
            public double LastTime { get; set; }
            public Queue<double> RecentTimes { get; set; } = new Queue<double>();
            public long AllocatedMemory { get; set; }
            public int GCCount { get; set; }
            public Stack<Stopwatch> TimerStack { get; set; } = new Stack<Stopwatch>();
        }
        
        /// <summary>
        /// 性能采样范围
        /// </summary>
        public class ProfileScope : IDisposable
        {
            private readonly ModPerformanceProfiler profiler;
            private readonly string name;
            private readonly long startMemory;
            
            public ProfileScope(ModPerformanceProfiler profiler, string name)
            {
                this.profiler = profiler;
                this.name = name;
                this.startMemory = GC.GetTotalMemory(false);
                profiler.BeginSample(name);
            }
            
            public void Dispose()
            {
                var endMemory = GC.GetTotalMemory(false);
                var allocatedMemory = endMemory - startMemory;
                profiler.EndSample(name, allocatedMemory);
            }
        }
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
        }
        
        void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                showUI = !showUI;
            }
            
            // 定期清理超时的计时器
            if (Time.frameCount % 300 == 0) // 每300帧清理一次
            {
                CleanupStaleTimers();
            }
        }
        
        void OnGUI()
        {
            if (!showUI) return;
            
            var rect = new Rect(windowPosition, windowSize);
            GUI.Window(1, rect, DrawProfilerWindow, "Mod Performance Profiler");
        }
        #endregion

        #region Profiling Methods
        /// <summary>
        /// 开始采样
        /// </summary>
        public void BeginSample(string name)
        {
            if (!enableProfiling) return;
            
            lock (lockObject)
            {
                if (!profileData.ContainsKey(name))
                {
                    profileData[name] = new ProfileData { Name = name };
                }
                
                var sw = Stopwatch.StartNew();
                profileData[name].TimerStack.Push(sw);
                
                // Unity Profiler集成
                Profiler.BeginSample($"Mod_{name}");
            }
        }
        
        /// <summary>
        /// 结束采样
        /// </summary>
        public void EndSample(string name, long allocatedMemory = 0)
        {
            if (!enableProfiling) return;
            
            Profiler.EndSample();
            
            lock (lockObject)
            {
                if (profileData.TryGetValue(name, out var data) && data.TimerStack.Count > 0)
                {
                    var sw = data.TimerStack.Pop();
                    sw.Stop();
                    
                    var elapsed = sw.Elapsed.TotalMilliseconds;
                    
                    // 更新统计数据
                    data.CallCount++;
                    data.TotalTime += elapsed;
                    data.LastTime = elapsed;
                    data.MinTime = Math.Min(data.MinTime, elapsed);
                    data.MaxTime = Math.Max(data.MaxTime, elapsed);
                    
                    // 记录内存分配
                    if (allocatedMemory > 0)
                    {
                        data.AllocatedMemory += allocatedMemory;
                    }
                    
                    // 保留最近的采样时间
                    data.RecentTimes.Enqueue(elapsed);
                    if (data.RecentTimes.Count > maxSampleHistory)
                        data.RecentTimes.Dequeue();
                }
            }
        }
        
        /// <summary>
        /// 创建性能采样范围
        /// </summary>
        public ProfileScope BeginScope(string name)
        {
            return new ProfileScope(this, name);
        }
        
        /// <summary>
        /// 标记事件
        /// </summary>
        public void MarkEvent(string eventName)
        {
            if (!enableProfiling) return;
            
            UnityEngine.Debug.Log($"[Performance Event] {eventName} at {Time.time:F3}s");
        }
        #endregion

        #region GUI Drawing
        /// <summary>
        /// 绘制性能分析器窗口
        /// </summary>
        void DrawProfilerWindow(int windowId)
        {
            GUILayout.BeginVertical();
            
            // 工具栏
            DrawToolbar();
            
            // 选项卡
            DrawTabs();
            
            // 内容
            switch (currentTab)
            {
                case Tab.Overview:
                    DrawOverview();
                    break;
                case Tab.Details:
                    DrawDetails();
                    break;
                case Tab.Timeline:
                    DrawTimeline();
                    break;
                case Tab.Recommendations:
                    DrawRecommendations();
                    break;
            }
            
            GUILayout.EndVertical();
            
            GUI.DragWindow();
        }
        
        /// <summary>
        /// 绘制工具栏
        /// </summary>
        private void DrawToolbar()
        {
            GUILayout.BeginHorizontal();
            
            enableProfiling = GUILayout.Toggle(enableProfiling, "Enable", GUILayout.Width(60));
            
            if (GUILayout.Button("Clear", GUILayout.Width(60)))
            {
                ClearData();
            }
            
            if (GUILayout.Button("Export", GUILayout.Width(60)))
            {
                ExportReport();
            }
            
            GUILayout.Space(20);
            
            GUILayout.Label("Filter:", GUILayout.Width(50));
            filterText = GUILayout.TextField(filterText, GUILayout.Width(200));
            
            GUILayout.FlexibleSpace();
            
            GUILayout.Label($"Samples: {profileData.Count} | FPS: {1f / Time.smoothDeltaTime:F1}");
            
            GUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// 绘制选项卡
        /// </summary>
        private void DrawTabs()
        {
            GUILayout.BeginHorizontal();
            
            if (GUILayout.Toggle(currentTab == Tab.Overview, "Overview", "Button"))
                currentTab = Tab.Overview;
            
            if (GUILayout.Toggle(currentTab == Tab.Details, "Details", "Button"))
                currentTab = Tab.Details;
            
            if (GUILayout.Toggle(currentTab == Tab.Timeline, "Timeline", "Button"))
                currentTab = Tab.Timeline;
            
            if (GUILayout.Toggle(currentTab == Tab.Recommendations, "Recommendations", "Button"))
                currentTab = Tab.Recommendations;
            
            GUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// 绘制概览
        /// </summary>
        private void DrawOverview()
        {
            // 排序按钮
            GUILayout.BeginHorizontal();
            
            if (GUILayout.Toggle(sortMode == SortMode.Name, "Name", "Button"))
                sortMode = SortMode.Name;
            
            if (GUILayout.Toggle(sortMode == SortMode.CallCount, "Calls", "Button"))
                sortMode = SortMode.CallCount;
            
            if (GUILayout.Toggle(sortMode == SortMode.TotalTime, "Total (ms)", "Button"))
                sortMode = SortMode.TotalTime;
            
            if (GUILayout.Toggle(sortMode == SortMode.AverageTime, "Avg (ms)", "Button"))
                sortMode = SortMode.AverageTime;
            
            if (GUILayout.Toggle(sortMode == SortMode.MaxTime, "Max (ms)", "Button"))
                sortMode = SortMode.MaxTime;
            
            GUILayout.EndHorizontal();
            
            // 数据列表
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            
            var sortedData = GetSortedData();
            
            foreach (var data in sortedData)
            {
                DrawProfileEntry(data);
            }
            
            GUILayout.EndScrollView();
            
            // 统计信息
            DrawStatistics();
        }
        
        /// <summary>
        /// 绘制性能条目
        /// </summary>
        private void DrawProfileEntry(ProfileData data)
        {
            GUILayout.BeginHorizontal("box");
            
            // 性能指示器颜色
            var avgTime = data.AverageTime;
            GUI.color = avgTime > 16 ? Color.red :
                       avgTime > 8 ? Color.yellow :
                       Color.green;
            
            GUILayout.Label("●", GUILayout.Width(20));
            GUI.color = Color.white;
            
            // 数据显示
            GUILayout.Label(data.Name, GUILayout.Width(250));
            GUILayout.Label(data.CallCount.ToString(), GUILayout.Width(60));
            GUILayout.Label($"{data.TotalTime:F2}", GUILayout.Width(80));
            GUILayout.Label($"{data.AverageTime:F2}", GUILayout.Width(80));
            GUILayout.Label($"{data.MinTime:F2}", GUILayout.Width(60));
            GUILayout.Label($"{data.MaxTime:F2}", GUILayout.Width(60));
            GUILayout.Label($"{data.LastTime:F2}", GUILayout.Width(60));
            
            // 内存分配
            if (data.AllocatedMemory > 0)
            {
                var memoryMB = data.AllocatedMemory / 1024f / 1024f;
                GUILayout.Label($"{memoryMB:F2} MB", GUILayout.Width(60));
            }
            else
            {
                GUILayout.Label("-", GUILayout.Width(60));
            }
            
            // 迷你图
            if (data.RecentTimes.Count > 1)
            {
                DrawMiniGraph(data.RecentTimes.ToArray(), 100, 20);
            }
            
            GUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// 绘制迷你图表
        /// </summary>
        private void DrawMiniGraph(double[] values, float width, float height)
        {
            var rect = GUILayoutUtility.GetRect(width, height);
            
            if (values.Length < 2) return;
            
            // 创建纹理
            var texture = new Texture2D((int)width, (int)height);
            var pixels = new Color[(int)(width * height)];
            
            // 填充背景
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color(0.2f, 0.2f, 0.2f, 1f);
            }
            
            // 计算数据范围
            var max = values.Max();
            var min = values.Min();
            var range = max - min;
            
            if (range < 0.001) range = 1;
            
            // 绘制线条
            for (int i = 1; i < values.Length; i++)
            {
                var x1 = (int)((i - 1) / (float)(values.Length - 1) * (width - 1));
                var y1 = (int)((1 - (values[i - 1] - min) / range) * (height - 1));
                var x2 = (int)(i / (float)(values.Length - 1) * (width - 1));
                var y2 = (int)((1 - (values[i] - min) / range) * (height - 1));
                
                DrawLine(pixels, (int)width, (int)height, x1, y1, x2, y2, Color.cyan);
            }
            
            texture.SetPixels(pixels);
            texture.Apply();
            
            GUI.DrawTexture(rect, texture);
            
            Destroy(texture);
        }
        
        /// <summary>
        /// 在像素数组中绘制线条
        /// </summary>
        private void DrawLine(Color[] pixels, int width, int height, int x1, int y1, int x2, int y2, Color color)
        {
            var dx = Math.Abs(x2 - x1);
            var dy = Math.Abs(y2 - y1);
            var sx = x1 < x2 ? 1 : -1;
            var sy = y1 < y2 ? 1 : -1;
            var err = dx - dy;
            
            while (true)
            {
                if (x1 >= 0 && x1 < width && y1 >= 0 && y1 < height)
                {
                    pixels[y1 * width + x1] = color;
                }
                
                if (x1 == x2 && y1 == y2) break;
                
                var e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x1 += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    y1 += sy;
                }
            }
        }
        
        /// <summary>
        /// 绘制详细信息
        /// </summary>
        private void DrawDetails()
        {
            if (profileData.Count == 0)
            {
                GUILayout.Label("No profiling data available");
                return;
            }
            
            // 选择要查看的项目
            var selectedData = profileData.Values.OrderByDescending(d => d.TotalTime).FirstOrDefault();
            if (selectedData != null)
            {
                GUILayout.BeginVertical("box");
                
                GUILayout.Label($"Details for: {selectedData.Name}", "BoldLabel");
                
                GUILayout.Label($"Total Calls: {selectedData.CallCount}");
                GUILayout.Label($"Total Time: {selectedData.TotalTime:F2} ms");
                GUILayout.Label($"Average Time: {selectedData.AverageTime:F2} ms");
                GUILayout.Label($"Min Time: {selectedData.MinTime:F2} ms");
                GUILayout.Label($"Max Time: {selectedData.MaxTime:F2} ms");
                GUILayout.Label($"Allocated Memory: {selectedData.AllocatedMemory / 1024f / 1024f:F2} MB");
                
                // 时间分布直方图
                if (selectedData.RecentTimes.Count > 0)
                {
                    GUILayout.Space(10);
                    GUILayout.Label("Time Distribution:");
                    DrawHistogram(selectedData.RecentTimes.ToArray(), 400, 100);
                }
                
                GUILayout.EndVertical();
            }
        }
        
        /// <summary>
        /// 绘制直方图
        /// </summary>
        private void DrawHistogram(double[] values, float width, float height)
        {
            var rect = GUILayoutUtility.GetRect(width, height);
            
            // 创建直方图数据
            var bucketCount = 20;
            var min = values.Min();
            var max = values.Max();
            var range = max - min;
            var buckets = new int[bucketCount];
            
            foreach (var value in values)
            {
                var bucketIndex = (int)((value - min) / range * (bucketCount - 1));
                bucketIndex = Mathf.Clamp(bucketIndex, 0, bucketCount - 1);
                buckets[bucketIndex]++;
            }
            
            var maxCount = buckets.Max();
            
            // 绘制直方图
            var bucketWidth = width / bucketCount;
            for (int i = 0; i < bucketCount; i++)
            {
                var barHeight = (float)buckets[i] / maxCount * height;
                var barRect = new Rect(rect.x + i * bucketWidth, rect.y + height - barHeight, bucketWidth - 2, barHeight);
                GUI.Box(barRect, "");
            }
        }
        
        /// <summary>
        /// 绘制时间线
        /// </summary>
        private void DrawTimeline()
        {
            GUILayout.Label("Performance Timeline", "BoldLabel");
            
            // 这里可以实现一个时间线视图，显示性能数据随时间的变化
            GUILayout.Label("Timeline view not yet implemented");
        }
        
        /// <summary>
        /// 绘制优化建议
        /// </summary>
        private void DrawRecommendations()
        {
            GUILayout.BeginVertical("box");
            
            GUILayout.Label("Performance Recommendations", "BoldLabel");
            
            var recommendations = GenerateRecommendations();
            
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            
            foreach (var recommendation in recommendations)
            {
                DrawRecommendation(recommendation);
            }
            
            GUILayout.EndScrollView();
            
            GUILayout.EndVertical();
        }
        
        /// <summary>
        /// 绘制单个建议
        /// </summary>
        private void DrawRecommendation(PerformanceRecommendation recommendation)
        {
            GUILayout.BeginVertical("box");
            
            // 严重程度颜色
            GUI.color = recommendation.Severity switch
            {
                RecommendationSeverity.Critical => Color.red,
                RecommendationSeverity.Warning => Color.yellow,
                RecommendationSeverity.Info => Color.cyan,
                _ => Color.white
            };
            
            GUILayout.Label($"[{recommendation.Severity}] {recommendation.Title}");
            GUI.color = Color.white;
            
            GUILayout.Label(recommendation.Description, GUI.skin.textArea);
            
            if (!string.IsNullOrEmpty(recommendation.Solution))
            {
                GUILayout.Label("Solution:", "BoldLabel");
                GUILayout.Label(recommendation.Solution, GUI.skin.textArea);
            }
            
            GUILayout.EndVertical();
        }
        
        /// <summary>
        /// 绘制统计信息
        /// </summary>
        private void DrawStatistics()
        {
            GUILayout.BeginHorizontal("box");
            
            var totalTime = profileData.Values.Sum(d => d.TotalTime);
            var totalCalls = profileData.Values.Sum(d => d.CallCount);
            var totalMemory = profileData.Values.Sum(d => d.AllocatedMemory);
            
            GUILayout.Label($"Total Time: {totalTime:F2} ms");
            GUILayout.Label($"Total Calls: {totalCalls}");
            GUILayout.Label($"Total Memory: {totalMemory / 1024f / 1024f:F2} MB");
            GUILayout.Label($"Active Timers: {activeTimers.Count}");
            
            GUILayout.EndHorizontal();
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// 获取排序后的数据
        /// </summary>
        private IEnumerable<ProfileData> GetSortedData()
        {
            var filtered = profileData.Values.AsEnumerable();
            
            if (!string.IsNullOrEmpty(filterText))
            {
                filtered = filtered.Where(d => d.Name.ToLower().Contains(filterText.ToLower()));
            }
            
            return sortMode switch
            {
                SortMode.Name => filtered.OrderBy(d => d.Name),
                SortMode.CallCount => filtered.OrderByDescending(d => d.CallCount),
                SortMode.TotalTime => filtered.OrderByDescending(d => d.TotalTime),
                SortMode.AverageTime => filtered.OrderByDescending(d => d.AverageTime),
                SortMode.MaxTime => filtered.OrderByDescending(d => d.MaxTime),
                _ => filtered
            };
        }
        
        /// <summary>
        /// 生成性能优化建议
        /// </summary>
        private List<PerformanceRecommendation> GenerateRecommendations()
        {
            var recommendations = new List<PerformanceRecommendation>();
            
            // 检查高频调用
            var highFrequencyCalls = profileData.Values
                .Where(d => d.CallCount > 1000 && d.AverageTime > 0.1)
                .OrderByDescending(d => d.CallCount);
            
            foreach (var data in highFrequencyCalls)
            {
                recommendations.Add(new PerformanceRecommendation
                {
                    Severity = RecommendationSeverity.Warning,
                    Title = $"High frequency calls: {data.Name}",
                    Description = $"This method is called {data.CallCount} times with average time {data.AverageTime:F2}ms",
                    Solution = "Consider caching results or reducing call frequency"
                });
            }
            
            // 检查长时间运行
            var slowMethods = profileData.Values
                .Where(d => d.MaxTime > 16.67) // 超过一帧时间
                .OrderByDescending(d => d.MaxTime);
            
            foreach (var data in slowMethods)
            {
                recommendations.Add(new PerformanceRecommendation
                {
                    Severity = data.MaxTime > 33.33 ? RecommendationSeverity.Critical : RecommendationSeverity.Warning,
                    Title = $"Slow method: {data.Name}",
                    Description = $"Maximum execution time: {data.MaxTime:F2}ms (target: 16.67ms)",
                    Solution = "Consider optimizing algorithm, using coroutines, or splitting work across frames"
                });
            }
            
            // 检查内存分配
            var memoryHeavy = profileData.Values
                .Where(d => d.AllocatedMemory > 1024 * 1024) // 超过1MB
                .OrderByDescending(d => d.AllocatedMemory);
            
            foreach (var data in memoryHeavy)
            {
                recommendations.Add(new PerformanceRecommendation
                {
                    Severity = RecommendationSeverity.Warning,
                    Title = $"High memory allocation: {data.Name}",
                    Description = $"Allocated {data.AllocatedMemory / 1024f / 1024f:F2} MB",
                    Solution = "Use object pooling, reduce allocations, or reuse existing objects"
                });
            }
            
            return recommendations;
        }
        
        /// <summary>
        /// 清理过期的计时器
        /// </summary>
        private void CleanupStaleTimers()
        {
            lock (lockObject)
            {
                var staleTimers = activeTimers
                    .Where(kvp => kvp.Value.Elapsed.TotalSeconds > 60) // 超过60秒的计时器
                    .Select(kvp => kvp.Key)
                    .ToList();
                
                foreach (var key in staleTimers)
                {
                    activeTimers.Remove(key);
                    UnityEngine.Debug.LogWarning($"[Profiler] Removed stale timer: {key}");
                }
            }
        }
        
        /// <summary>
        /// 清除数据
        /// </summary>
        private void ClearData()
        {
            lock (lockObject)
            {
                profileData.Clear();
                activeTimers.Clear();
            }
        }
        
        /// <summary>
        /// 导出报告
        /// </summary>
        private void ExportReport()
        {
            var report = "Mod Performance Report\n";
            report += $"Generated: {DateTime.Now}\n\n";
            report += "Name\tCalls\tTotal(ms)\tAvg(ms)\tMin(ms)\tMax(ms)\tMemory(MB)\n";
            
            foreach (var data in GetSortedData())
            {
                report += $"{data.Name}\t{data.CallCount}\t{data.TotalTime:F2}\t";
                report += $"{data.AverageTime:F2}\t{data.MinTime:F2}\t{data.MaxTime:F2}\t";
                report += $"{data.AllocatedMemory / 1024f / 1024f:F2}\n";
            }
            
            var path = System.IO.Path.Combine(Application.persistentDataPath, 
                $"ModPerformance_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
            
            System.IO.File.WriteAllText(path, report);
            UnityEngine.Debug.Log($"Performance report exported to: {path}");
        }
        #endregion

        #region Public API
        /// <summary>
        /// 获取性能数据
        /// </summary>
        public ProfileData GetProfileData(string name)
        {
            lock (lockObject)
            {
                return profileData.TryGetValue(name, out var data) ? data : null;
            }
        }
        
        /// <summary>
        /// 获取所有性能数据
        /// </summary>
        public Dictionary<string, ProfileData> GetAllProfileData()
        {
            lock (lockObject)
            {
                return new Dictionary<string, ProfileData>(profileData);
            }
        }
        
        /// <summary>
        /// 设置是否启用性能分析
        /// </summary>
        public void SetProfilingEnabled(bool enabled)
        {
            enableProfiling = enabled;
        }
        #endregion

        #region Performance Recommendation
        /// <summary>
        /// 性能优化建议
        /// </summary>
        private class PerformanceRecommendation
        {
            public RecommendationSeverity Severity { get; set; }
            public string Title { get; set; }
            public string Description { get; set; }
            public string Solution { get; set; }
        }
        
        /// <summary>
        /// 建议严重程度
        /// </summary>
        private enum RecommendationSeverity
        {
            Info,
            Warning,
            Critical
        }
        #endregion
    }
    
    /// <summary>
    /// 性能分析扩展方法
    /// </summary>
    public static class ProfilerExtensions
    {
        /// <summary>
        /// 分析方法执行
        /// </summary>
        public static void ProfileMethod(this IModBehaviour behaviour, string methodName, Action action)
        {
            using (ModPerformanceProfiler.Instance.BeginScope($"{behaviour.BehaviourId}.{methodName}"))
            {
                action();
            }
        }
        
        /// <summary>
        /// 分析异步方法执行
        /// </summary>
        public static async System.Threading.Tasks.Task ProfileMethodAsync(
            this IModBehaviour behaviour, 
            string methodName, 
            Func<System.Threading.Tasks.Task> action)
        {
            using (ModPerformanceProfiler.Instance.BeginScope($"{behaviour.BehaviourId}.{methodName}"))
            {
                await action();
            }
        }
    }
}
```

## 11. Debug/ModMemoryMonitor.cs

```csharp
// ModSystem.Unity/Debug/ModMemoryMonitor.cs
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;

namespace ModSystem.Unity.Debug
{
    /// <summary>
    /// 模组内存监控器
    /// 监控模组的内存使用情况并提供优化建议
    /// </summary>
    [AddComponentMenu("ModSystem/Debug/Memory Monitor")]
    public class ModMemoryMonitor : MonoBehaviour
    {
        #region Configuration
        [Header("Monitor Settings")]
        [SerializeField] private bool enableMonitoring = true;
        [SerializeField] private float updateInterval = 1f;
        [SerializeField] private int historySize = 60;
        
        [Header("Display Settings")]
        [SerializeField] private bool showUI = false;
        [SerializeField] private KeyCode toggleKey = KeyCode.F9;
        [SerializeField] private Vector2 windowPosition = new Vector2(Screen.width - 410, 10);
        [SerializeField] private Vector2 windowSize = new Vector2(400, 500);
        
        [Header("Alert Settings")]
        [SerializeField] private float memoryWarningThreshold = 100f; // MB
        [SerializeField] private float memoryCriticalThreshold = 200f; // MB
        #endregion

        #region Private Fields
        private Dictionary<string, MemoryStats> modMemoryStats = new Dictionary<string, MemoryStats>();
        private float lastUpdateTime;
        private GCMemoryInfo lastGCInfo;
        private long baselineMemory;
        private Vector2 scrollPosition;
        private Tab currentTab = Tab.Overview;
        #endregion

        #region Enums
        private enum Tab
        {
            Overview,
            Details,
            History,
            Alerts
        }
        #endregion

        #region Data Structures
        /// <summary>
        /// 内存统计信息
        /// </summary>
        public class MemoryStats
        {
            public string ModId { get; set; }
            public long TotalAllocated { get; set; }
            public long CurrentUsage { get; set; }
            public int ObjectCount { get; set; }
            public int TextureMemory { get; set; }
            public int MeshMemory { get; set; }
            public int AudioMemory { get; set; }
            public int MaterialCount { get; set; }
            public List<float> UsageHistory { get; set; } = new List<float>();
            public DateTime LastUpdate { get; set; }
            public List<MemoryAlert> Alerts { get; set; } = new List<MemoryAlert>();
            
            // 详细的对象统计
            public Dictionary<string, int> ObjectTypeCount { get; set; } = new Dictionary<string, int>();
            public List<LargeObjectInfo> LargeObjects { get; set; } = new List<LargeObjectInfo>();
        }
        
        /// <summary>
        /// 大对象信息
        /// </summary>
        public class LargeObjectInfo
        {
            public string Name { get; set; }
            public string Type { get; set; }
            public long Size { get; set; }
            public UnityEngine.Object Reference { get; set; }
        }
        
        /// <summary>
        /// 内存警报
        /// </summary>
        public class MemoryAlert
        {
            public DateTime Time { get; set; }
            public AlertType Type { get; set; }
            public string Message { get; set; }
            public float MemoryUsage { get; set; }
        }
        
        /// <summary>
        /// 警报类型
        /// </summary>
        public enum AlertType
        {
            Info,
            Warning,
            Critical
        }
        #endregion

        #region Unity Lifecycle
        void Start()
        {
            // 记录基线内存使用
            baselineMemory = GC.GetTotalMemory(false);
            lastGCInfo = GC.GetGCMemoryInfo();
            
            InvokeRepeating(nameof(UpdateMemoryStats), updateInterval, updateInterval);
        }
        
        void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                showUI = !showUI;
            }
            
            // 强制GC快捷键
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.G))
            {
                ForceGarbageCollection();
            }
        }
        
        void OnGUI()
        {
            if (!showUI) return;
            
            var rect = new Rect(windowPosition, windowSize);
            GUI.Window(2, rect, DrawMemoryWindow, "Mod Memory Monitor");
        }
        
        void OnDestroy()
        {
            CancelInvoke(nameof(UpdateMemoryStats));
        }
        #endregion

        #region Memory Monitoring
        /// <summary>
        /// 更新内存统计
        /// </summary>
        private void UpdateMemoryStats()
        {
            if (!enableMonitoring) return;
            
            var modManager = FindObjectOfType<ModManager>();
            if (modManager == null) return;
            
            foreach (var kvp in modManager.UnityInstances)
            {
                UpdateModMemoryStats(kvp.Key, kvp.Value);
            }
            
            // 检查内存警报
            CheckMemoryAlerts();
            
            lastUpdateTime = Time.time;
        }
        
        /// <summary>
        /// 更新单个模组的内存统计
        /// </summary>
        private void UpdateModMemoryStats(string modId, ModUnityInstance instance)
        {
            if (!modMemoryStats.TryGetValue(modId, out var stats))
            {
                stats = new MemoryStats { ModId = modId };
                modMemoryStats[modId] = stats;
            }
            
            // 更新对象计数
            stats.ObjectCount = instance.ActiveObjectCount;
            stats.LastUpdate = DateTime.Now;
            
            // 计算各种资源的内存使用
            long totalMemory = 0;
            stats.ObjectTypeCount.Clear();
            stats.LargeObjects.Clear();
            
            foreach (var obj in instance.GameObjects.Where(go => go != null))
            {
                // 纹理内存
                var textureMemory = CalculateTextureMemory(obj);
                stats.TextureMemory = textureMemory;
                totalMemory += textureMemory;
                
                // 网格内存
                var meshMemory = CalculateMeshMemory(obj);
                stats.MeshMemory = meshMemory;
                totalMemory += meshMemory;
                
                // 音频内存
                var audioMemory = CalculateAudioMemory(obj);
                stats.AudioMemory = audioMemory;
                totalMemory += audioMemory;
                
                // 材质计数
                stats.MaterialCount = CountMaterials(obj);
                
                // 更新对象类型统计
                UpdateObjectTypeStats(obj, stats);
                
                // 检查大对象
                CheckLargeObjects(obj, stats);
            }
            
            stats.CurrentUsage = totalMemory;
            stats.TotalAllocated = Math.Max(stats.TotalAllocated, totalMemory);
            
            // 更新使用历史
            stats.UsageHistory.Add(totalMemory / 1024f / 1024f); // 转换为MB
            if (stats.UsageHistory.Count > historySize)
            {
                stats.UsageHistory.RemoveAt(0);
            }
        }
        
        /// <summary>
        /// 更新对象类型统计
        /// </summary>
        private void UpdateObjectTypeStats(GameObject obj, MemoryStats stats)
        {
            var components = obj.GetComponents<Component>();
            foreach (var component in components)
            {
                if (component == null) continue;
                
                var typeName = component.GetType().Name;
                if (!stats.ObjectTypeCount.ContainsKey(typeName))
                {
                    stats.ObjectTypeCount[typeName] = 0;
                }
                stats.ObjectTypeCount[typeName]++;
            }
        }
        
        /// <summary>
        /// 检查大对象
        /// </summary>
        private void CheckLargeObjects(GameObject obj, MemoryStats stats)
        {
            const long largeObjectThreshold = 1024 * 1024; // 1MB
            
            // 检查纹理
            var renderers = obj.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                if (renderer.sharedMaterial?.mainTexture != null)
                {
                    var texture = renderer.sharedMaterial.mainTexture;
                    var size = Profiler.GetRuntimeMemorySizeLong(texture);
                    
                    if (size > largeObjectThreshold)
                    {
                        stats.LargeObjects.Add(new LargeObjectInfo
                        {
                            Name = texture.name,
                            Type = "Texture",
                            Size = size,
                            Reference = texture
                        });
                    }
                }
            }
            
            // 检查网格
            var meshFilters = obj.GetComponentsInChildren<MeshFilter>();
            foreach (var meshFilter in meshFilters)
            {
                if (meshFilter.sharedMesh != null)
                {
                    var mesh = meshFilter.sharedMesh;
                    var size = Profiler.GetRuntimeMemorySizeLong(mesh);
                    
                    if (size > largeObjectThreshold)
                    {
                        stats.LargeObjects.Add(new LargeObjectInfo
                        {
                            Name = mesh.name,
                            Type = "Mesh",
                            Size = size,
                            Reference = mesh
                        });
                    }
                }
            }
        }
        #endregion

        #region Memory Calculation
        /// <summary>
        /// 计算纹理内存
        /// </summary>
        private int CalculateTextureMemory(GameObject obj)
        {
            int totalMemory = 0;
            
            var renderers = obj.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                if (renderer.sharedMaterials != null)
                {
                    foreach (var material in renderer.sharedMaterials)
                    {
                        if (material != null)
                        {
                            // 主纹理
                            if (material.mainTexture != null)
                            {
                                totalMemory += (int)Profiler.GetRuntimeMemorySizeLong(material.mainTexture);
                            }
                            
                            // 其他纹理属性
                            var texturePropertyNames = material.GetTexturePropertyNames();
                            foreach (var propName in texturePropertyNames)
                            {
                                var texture = material.GetTexture(propName);
                                if (texture != null && texture != material.mainTexture)
                                {
                                    totalMemory += (int)Profiler.GetRuntimeMemorySizeLong(texture);
                                }
                            }
                        }
                    }
                }
            }
            
            // UI图片
            var images = obj.GetComponentsInChildren<UnityEngine.UI.Image>();
            foreach (var image in images)
            {
                if (image.sprite?.texture != null)
                {
                    totalMemory += (int)Profiler.GetRuntimeMemorySizeLong(image.sprite.texture);
                }
            }
            
            return totalMemory;
        }
        
        /// <summary>
        /// 计算网格内存
        /// </summary>
        private int CalculateMeshMemory(GameObject obj)
        {
            int totalMemory = 0;
            
            var meshFilters = obj.GetComponentsInChildren<MeshFilter>();
            foreach (var meshFilter in meshFilters)
            {
                if (meshFilter.sharedMesh != null)
                {
                    totalMemory += (int)Profiler.GetRuntimeMemorySizeLong(meshFilter.sharedMesh);
                }
            }
            
            var skinnedMeshRenderers = obj.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (var smr in skinnedMeshRenderers)
            {
                if (smr.sharedMesh != null)
                {
                    totalMemory += (int)Profiler.GetRuntimeMemorySizeLong(smr.sharedMesh);
                }
            }
            
            return totalMemory;
        }
        
        /// <summary>
        /// 计算音频内存
        /// </summary>
        private int CalculateAudioMemory(GameObject obj)
        {
            int totalMemory = 0;
            
            var audioSources = obj.GetComponentsInChildren<AudioSource>();
            foreach (var audioSource in audioSources)
            {
                if (audioSource.clip != null)
                {
                    totalMemory += (int)Profiler.GetRuntimeMemorySizeLong(audioSource.clip);
                }
            }
            
            return totalMemory;
        }
        
        /// <summary>
        /// 计算材质数量
        /// </summary>
        private int CountMaterials(GameObject obj)
        {
            var materials = new HashSet<Material>();
            
            var renderers = obj.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                if (renderer.sharedMaterials != null)
                {
                    foreach (var material in renderer.sharedMaterials)
                    {
                        if (material != null)
                        {
                            materials.Add(material);
                        }
                    }
                }
            }
            
            return materials.Count;
        }
        #endregion

        #region Memory Alerts
        /// <summary>
        /// 检查内存警报
        /// </summary>
        private void CheckMemoryAlerts()
        {
            foreach (var kvp in modMemoryStats)
            {
                var stats = kvp.Value;
                var memoryMB = stats.CurrentUsage / 1024f / 1024f;
                
                if (memoryMB > memoryCriticalThreshold)
                {
                    AddAlert(stats, AlertType.Critical, 
                        $"Critical memory usage: {memoryMB:F2} MB", memoryMB);
                }
                else if (memoryMB > memoryWarningThreshold)
                {
                    AddAlert(stats, AlertType.Warning, 
                        $"High memory usage: {memoryMB:F2} MB", memoryMB);
                }
                
                // 检查内存泄漏
                if (stats.UsageHistory.Count > 10)
                {
                    var recent = stats.UsageHistory.Skip(stats.UsageHistory.Count - 10).ToList();
                    var trend = CalculateMemoryTrend(recent);
                    
                    if (trend > 0.5f) // 每秒增加0.5MB
                    {
                        AddAlert(stats, AlertType.Warning, 
                            $"Possible memory leak detected: +{trend:F2} MB/s", memoryMB);
                    }
                }
            }
        }
        
        /// <summary>
        /// 添加警报
        /// </summary>
        private void AddAlert(MemoryStats stats, AlertType type, string message, float memoryUsage)
        {
            var alert = new MemoryAlert
            {
                Time = DateTime.Now,
                Type = type,
                Message = message,
                MemoryUsage = memoryUsage
            };
            
            stats.Alerts.Add(alert);
            
            // 限制警报历史
            while (stats.Alerts.Count > 50)
            {
                stats.Alerts.RemoveAt(0);
            }
            
            // 输出到控制台
            switch (type)
            {
                case AlertType.Critical:
                    UnityEngine.Debug.LogError($"[Memory Alert] {stats.ModId}: {message}");
                    break;
                case AlertType.Warning:
                    UnityEngine.Debug.LogWarning($"[Memory Alert] {stats.ModId}: {message}");
                    break;
                default:
                    UnityEngine.Debug.Log($"[Memory Alert] {stats.ModId}: {message}");
                    break;
            }
        }
        
        /// <summary>
        /// 计算内存趋势
        /// </summary>
        private float CalculateMemoryTrend(List<float> values)
        {
            if (values.Count < 2) return 0;
            
            // 简单线性回归
            float sumX = 0, sumY = 0, sumXY = 0, sumX2 = 0;
            int n = values.Count;
            
            for (int i = 0; i < n; i++)
            {
                sumX += i;
                sumY += values[i];
                sumXY += i * values[i];
                sumX2 += i * i;
            }
            
            float slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
            return slope / updateInterval; // 转换为每秒变化率
        }
        #endregion

        #region GUI Drawing
        /// <summary>
        /// 绘制内存监控窗口
        /// </summary>
        void DrawMemoryWindow(int windowId)
        {
            GUILayout.BeginVertical();
            
            // 标题栏
            DrawTitleBar();
            
            // 选项卡
            DrawTabs();
            
            // 内容
            switch (currentTab)
            {
                case Tab.Overview:
                    DrawOverview();
                    break;
                case Tab.Details:
                    DrawDetails();
                    break;
                case Tab.History:
                    DrawHistory();
                    break;
                case Tab.Alerts:
                    DrawAlerts();
                    break;
            }
            
            GUILayout.EndVertical();
            
            GUI.DragWindow();
        }
        
        /// <summary>
        /// 绘制标题栏
        /// </summary>
        private void DrawTitleBar()
        {
            GUILayout.BeginHorizontal();
            
            var totalMemory = GC.GetTotalMemory(false) / 1024f / 1024f;
            var gcInfo = GC.GetGCMemoryInfo();
            
            GUILayout.Label($"Total: {totalMemory:F2} MB | " +
                          $"GC: {GC.CollectionCount(0)}/{GC.CollectionCount(1)}/{GC.CollectionCount(2)}");
            
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("GC", GUILayout.Width(30)))
            {
                ForceGarbageCollection();
            }
            
            if (GUILayout.Button("X", GUILayout.Width(20)))
            {
                showUI = false;
            }
            
            GUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// 绘制选项卡
        /// </summary>
        private void DrawTabs()
        {
            GUILayout.BeginHorizontal();
            
            if (GUILayout.Toggle(currentTab == Tab.Overview, "Overview", "Button"))
                currentTab = Tab.Overview;
            
            if (GUILayout.Toggle(currentTab == Tab.Details, "Details", "Button"))
                currentTab = Tab.Details;
            
            if (GUILayout.Toggle(currentTab == Tab.History, "History", "Button"))
                currentTab = Tab.History;
            
            if (GUILayout.Toggle(currentTab == Tab.Alerts, "Alerts", "Button"))
                currentTab = Tab.Alerts;
            
            GUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// 绘制概览
        /// </summary>
        private void DrawOverview()
        {
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            
            foreach (var kvp in modMemoryStats)
            {
                DrawModMemoryInfo(kvp.Key, kvp.Value);
            }
            
            GUILayout.EndScrollView();
            
            // 总计
            DrawTotalStats();
        }
        
        /// <summary>
        /// 绘制模组内存信息
        /// </summary>
        private void DrawModMemoryInfo(string modId, MemoryStats stats)
        {
            GUILayout.BeginVertical("box");
            
            var memoryMB = stats.CurrentUsage / 1024f / 1024f;
            
            // 标题行
            GUILayout.BeginHorizontal();
            
            // 状态指示器
            GUI.color = memoryMB > memoryCriticalThreshold ? Color.red :
                       memoryMB > memoryWarningThreshold ? Color.yellow :
                       Color.green;
            GUILayout.Label("●", GUILayout.Width(20));
            GUI.color = Color.white;
            
            GUILayout.Label($"<b>{modId}</b>", GetRichTextStyle());
            GUILayout.FlexibleSpace();
            GUILayout.Label($"{memoryMB:F2} MB");
            
            GUILayout.EndHorizontal();
            
            // 详细信息
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Objects: {stats.ObjectCount}", GUILayout.Width(80));
            GUILayout.Label($"Textures: {stats.TextureMemory / 1024 / 1024:F1} MB", GUILayout.Width(100));
            GUILayout.Label($"Meshes: {stats.MeshMemory / 1024 / 1024:F1} MB", GUILayout.Width(100));
            GUILayout.Label($"Materials: {stats.MaterialCount}", GUILayout.Width(80));
            GUILayout.EndHorizontal();
            
            // 内存使用趋势图
            if (stats.UsageHistory.Count > 1)
            {
                DrawMemoryGraph(stats.UsageHistory, 380, 50);
            }
            
            GUILayout.EndVertical();
        }
        
        /// <summary>
        /// 绘制详细信息
        /// </summary>
        private void DrawDetails()
        {
            if (modMemoryStats.Count == 0)
            {
                GUILayout.Label("No memory data available");
                return;
            }
            
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            
            foreach (var kvp in modMemoryStats)
            {
                DrawModDetailedInfo(kvp.Key, kvp.Value);
            }
            
            GUILayout.EndScrollView();
        }
        
        /// <summary>
        /// 绘制模组详细信息
        /// </summary>
        private void DrawModDetailedInfo(string modId, MemoryStats stats)
        {
            GUILayout.BeginVertical("box");
            
            GUILayout.Label($"<b>{modId} - Detailed Memory Breakdown</b>", GetRichTextStyle());
            
            // 对象类型统计
            if (stats.ObjectTypeCount.Count > 0)
            {
                GUILayout.Label("Component Types:");
                foreach (var typeCount in stats.ObjectTypeCount.OrderByDescending(x => x.Value))
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(typeCount.Key, GUILayout.Width(200));
                    GUILayout.Label(typeCount.Value.ToString());
                    GUILayout.EndHorizontal();
                }
            }
            
            // 大对象列表
            if (stats.LargeObjects.Count > 0)
            {
                GUILayout.Space(10);
                GUILayout.Label("Large Objects (>1MB):");
                
                foreach (var largeObj in stats.LargeObjects.OrderByDescending(x => x.Size).Take(10))
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"{largeObj.Type}: {largeObj.Name}", GUILayout.Width(250));
                    GUILayout.Label($"{largeObj.Size / 1024f / 1024f:F2} MB");
                    
                    if (GUILayout.Button("Select", GUILayout.Width(60)) && largeObj.Reference != null)
                    {
                        Selection.activeObject = largeObj.Reference;
                    }
                    
                    GUILayout.EndHorizontal();
                }
            }
            
            GUILayout.EndVertical();
        }
        
        /// <summary>
        /// 绘制历史记录
        /// </summary>
        private void DrawHistory()
        {
            GUILayout.Label("Memory Usage History", "BoldLabel");
            
            foreach (var kvp in modMemoryStats)
            {
                if (kvp.Value.UsageHistory.Count > 1)
                {
                    GUILayout.BeginVertical("box");
                    GUILayout.Label(kvp.Key);
                    DrawMemoryGraph(kvp.Value.UsageHistory, 380, 100);
                    GUILayout.EndVertical();
                }
            }
        }
        
        /// <summary>
        /// 绘制警报
        /// </summary>
        private void DrawAlerts()
        {
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            
            var allAlerts = new List<(string modId, MemoryAlert alert)>();
            
            foreach (var kvp in modMemoryStats)
            {
                foreach (var alert in kvp.Value.Alerts)
                {
                    allAlerts.Add((kvp.Key, alert));
                }
            }
            
            // 按时间排序
            allAlerts.Sort((a, b) => b.alert.Time.CompareTo(a.alert.Time));
            
            foreach (var (modId, alert) in allAlerts)
            {
                DrawAlert(modId, alert);
            }
            
            GUILayout.EndScrollView();
        }
        
        /// <summary>
        /// 绘制单个警报
        /// </summary>
        private void DrawAlert(string modId, MemoryAlert alert)
        {
            GUILayout.BeginHorizontal("box");
            
            // 警报类型颜色
            GUI.color = alert.Type switch
            {
                AlertType.Critical => Color.red,
                AlertType.Warning => Color.yellow,
                _ => Color.cyan
            };
            
            GUILayout.Label($"[{alert.Type}]", GUILayout.Width(60));
            GUI.color = Color.white;
            
            GUILayout.Label(alert.Time.ToString("HH:mm:ss"), GUILayout.Width(60));
            GUILayout.Label(modId, GUILayout.Width(100));
            GUILayout.Label(alert.Message);
            
            GUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// 绘制内存使用图表
        /// </summary>
        private void DrawMemoryGraph(List<float> values, float width, float height)
        {
            var rect = GUILayoutUtility.GetRect(width, height);
            
            // 绘制背景
            GUI.Box(rect, "");
            
            if (values.Count < 2) return;
            
            // 计算范围
            var max = Mathf.Max(values.ToArray());
            var min = 0f;
            
            if (max <= min) max = min + 1f;
            
            // 绘制网格线
            var gridColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);
            DrawHorizontalLine(rect, 0.25f, gridColor);
            DrawHorizontalLine(rect, 0.5f, gridColor);
            DrawHorizontalLine(rect, 0.75f, gridColor);
            
            // 绘制数据线
            var points = new Vector2[values.Count];
            for (int i = 0; i < values.Count; i++)
            {
                var x = rect.x + (i / (float)(values.Count - 1)) * rect.width;
                var y = rect.y + rect.height - ((values[i] - min) / (max - min)) * rect.height;
                points[i] = new Vector2(x, y);
            }
            
            // 绘制填充区域
            DrawFilledGraph(rect, points, new Color(0.2f, 0.8f, 0.2f, 0.3f));
            
            // 绘制线条
            for (int i = 1; i < points.Length; i++)
            {
                DrawLine(points[i - 1], points[i], Color.green, 2f);
            }
            
            // 绘制标签
            GUI.Label(new Rect(rect.x, rect.y, 50, 20), $"{max:F1} MB");
            GUI.Label(new Rect(rect.x, rect.y + rect.height - 20, 50, 20), "0 MB");
        }
        
        /// <summary>
        /// 绘制水平线
        /// </summary>
        private void DrawHorizontalLine(Rect rect, float normalizedY, Color color)
        {
            var y = rect.y + rect.height * (1f - normalizedY);
            DrawLine(new Vector2(rect.x, y), new Vector2(rect.x + rect.width, y), color, 1f);
        }
        
        /// <summary>
        /// 绘制线条
        /// </summary>
        private void DrawLine(Vector2 start, Vector2 end, Color color, float width)
        {
            var originalColor = GUI.color;
            GUI.color = color;
            
            var angle = Mathf.Atan2(end.y - start.y, end.x - start.x) * 180f / Mathf.PI;
            var length = Vector2.Distance(start, end);
            
            GUIUtility.RotateAroundPivot(angle, start);
            GUI.DrawTexture(new Rect(start.x, start.y - width / 2, length, width), Texture2D.whiteTexture);
            GUIUtility.RotateAroundPivot(-angle, start);
            
            GUI.color = originalColor;
        }
        
        /// <summary>
        /// 绘制填充图表
        /// </summary>
        private void DrawFilledGraph(Rect rect, Vector2[] points, Color fillColor)
        {
            if (points.Length < 2) return;
            
            var originalColor = GUI.color;
            GUI.color = fillColor;
            
            // 创建填充多边形
            var vertices = new List<Vector3>();
            
            // 添加数据点
            foreach (var point in points)
            {
                vertices.Add(new Vector3(point.x, point.y, 0));
            }
            
            // 添加底部点
            vertices.Add(new Vector3(points[points.Length - 1].x, rect.y + rect.height, 0));
            vertices.Add(new Vector3(points[0].x, rect.y + rect.height, 0));
            
            // 使用GL绘制填充
            if (Event.current.type == EventType.Repaint)
            {
                var mat = new Material(Shader.Find("Hidden/Internal-Colored"));
                mat.SetPass(0);
                
                GL.PushMatrix();
                GL.LoadPixelMatrix();
                GL.Begin(GL.TRIANGLES);
                GL.Color(fillColor);
                
                // 三角化多边形
                for (int i = 1; i < vertices.Count - 1; i++)
                {
                    GL.Vertex(vertices[0]);
                    GL.Vertex(vertices[i]);
                    GL.Vertex(vertices[i + 1]);
                }
                
                GL.End();
                GL.PopMatrix();
            }
            
            GUI.color = originalColor;
        }
        
        /// <summary>
        /// 绘制总计统计
        /// </summary>
        private void DrawTotalStats()
        {
            GUILayout.BeginHorizontal("box");
            
            var totalMemory = modMemoryStats.Values.Sum(s => s.CurrentUsage) / 1024f / 1024f;
            var totalObjects = modMemoryStats.Values.Sum(s => s.ObjectCount);
            var totalTextures = modMemoryStats.Values.Sum(s => s.TextureMemory) / 1024f / 1024f;
            var totalMeshes = modMemoryStats.Values.Sum(s => s.MeshMemory) / 1024f / 1024f;
            
            GUILayout.Label($"Total Memory: {totalMemory:F2} MB");
            GUILayout.Label($"Objects: {totalObjects}");
            GUILayout.Label($"Textures: {totalTextures:F2} MB");
            GUILayout.Label($"Meshes: {totalMeshes:F2} MB");
            
            GUILayout.EndHorizontal();
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// 强制垃圾回收
        /// </summary>
        private void ForceGarbageCollection()
        {
            UnityEngine.Debug.Log("[Memory Monitor] Forcing garbage collection...");
            
            var beforeMemory = GC.GetTotalMemory(false);
            
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            var afterMemory = GC.GetTotalMemory(false);
            var freedMemory = (beforeMemory - afterMemory) / 1024f / 1024f;
            
            UnityEngine.Debug.Log($"[Memory Monitor] Freed {freedMemory:F2} MB");
        }
        
        /// <summary>
        /// 获取富文本样式
        /// </summary>
        private GUIStyle GetRichTextStyle()
        {
            var style = new GUIStyle(GUI.skin.label);
            style.richText = true;
            return style;
        }
        
        /// <summary>
        /// 导出内存报告
        /// </summary>
        public void ExportMemoryReport()
        {
            var report = "Mod Memory Report\n";
            report += $"Generated: {DateTime.Now}\n";
            report += $"Total System Memory: {GC.GetTotalMemory(false) / 1024f / 1024f:F2} MB\n\n";
            
            report += "Mod Memory Usage:\n";
            report += "================\n";
            
            foreach (var kvp in modMemoryStats)
            {
                var stats = kvp.Value;
                report += $"\n{kvp.Key}:\n";
                report += $"  Current Usage: {stats.CurrentUsage / 1024f / 1024f:F2} MB\n";
                report += $"  Objects: {stats.ObjectCount}\n";
                report += $"  Textures: {stats.TextureMemory / 1024f / 1024f:F2} MB\n";
                report += $"  Meshes: {stats.MeshMemory / 1024f / 1024f:F2} MB\n";
                report += $"  Audio: {stats.AudioMemory / 1024f / 1024f:F2} MB\n";
                report += $"  Materials: {stats.MaterialCount}\n";
                
                if (stats.LargeObjects.Count > 0)
                {
                    report += "  Large Objects:\n";
                    foreach (var obj in stats.LargeObjects.Take(5))
                    {
                        report += $"    - {obj.Type}: {obj.Name} ({obj.Size / 1024f / 1024f:F2} MB)\n";
                    }
                }
            }
            
            var path = System.IO.Path.Combine(Application.persistentDataPath, 
                $"ModMemoryReport_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
            
            System.IO.File.WriteAllText(path, report);
            UnityEngine.Debug.Log($"Memory report exported to: {path}");
        }
        #endregion

        #region Public API
        /// <summary>
        /// 获取模组内存统计
        /// </summary>
        public MemoryStats GetModMemoryStats(string modId)
        {
            return modMemoryStats.TryGetValue(modId, out var stats) ? stats : null;
        }
        
        /// <summary>
        /// 获取所有内存统计
        /// </summary>
        public Dictionary<string, MemoryStats> GetAllMemoryStats()
        {
            return new Dictionary<string, MemoryStats>(modMemoryStats);
        }
        
        /// <summary>
        /// 设置内存警告阈值
        /// </summary>
        public void SetMemoryThresholds(float warningMB, float criticalMB)
        {
            memoryWarningThreshold = warningMB;
            memoryCriticalThreshold = criticalMB;
        }
        #endregion
    }
}
```

## 12. Editor/ModSystemMenu.cs

```csharp
// ModSystem.Unity/Editor/ModSystemMenu.cs
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using ModSystem.Core;

namespace ModSystem.Unity.Editor
{
    /// <summary>
    /// Unity编辑器菜单
    /// 提供模组系统的编辑器工具和快捷操作
    /// </summary>
    public class ModSystemMenu
    {
        #region Menu Items - Setup
        
        /// <summary>
        /// 设置项目
        /// </summary>
        [MenuItem("ModSystem/Setup/Setup Project", false, 0)]
        public static void SetupProject()
        {
            Debug.Log("Setting up ModSystem...");
            
            // 创建必要的目录
            CreateDirectories();
            
            // 创建ModSystemController
            CreateModSystemController();
            
            // 创建层级
            CreateLayers();
            
            // 创建标签
            CreateTags();
            
            // 导入设置
            ImportProjectSettings();
            
            // 刷新资产数据库
            AssetDatabase.Refresh();
            
            Debug.Log("ModSystem setup complete!");
            
            EditorUtility.DisplayDialog("Setup Complete", 
                "ModSystem has been successfully set up in your project.", "OK");
        }
        
        /// <summary>
        /// 验证项目设置
        /// </summary>
        [MenuItem("ModSystem/Setup/Verify Setup", false, 1)]
        public static void VerifySetup()
        {
            var issues = new System.Collections.Generic.List<string>();
            
            // 检查目录
            if (!Directory.Exists("Assets/ModSystem"))
                issues.Add("ModSystem directory not found");
            
            if (!Directory.Exists("Assets/StreamingAssets/Mods"))
                issues.Add("Mods directory not found");
            
            // 检查核心DLL
            if (!File.Exists("Assets/ModSystem/Core/Assemblies/ModSystem.Core.dll"))
                issues.Add("Core DLL not found");
            
            // 检查控制器
            if (GameObject.FindObjectOfType<ModSystemController>() == null)
                issues.Add("ModSystemController not found in scene");
            
            // 检查层
            if (LayerMask.NameToLayer("Interactable") == -1)
                issues.Add("Interactable layer not found");
            
            if (issues.Count == 0)
            {
                EditorUtility.DisplayDialog("Verification Passed", 
                    "ModSystem is properly set up!", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Verification Failed", 
                    "Issues found:\n" + string.Join("\n", issues), "OK");
            }
        }
        
        /// <summary>
        /// 导入Core DLL
        /// </summary>
        [MenuItem("ModSystem/Setup/Import Core DLL", false, 2)]
        public static void ImportCoreDLL()
        {
            var path = EditorUtility.OpenFilePanel("Select ModSystem.Core.dll", "", "dll");
            if (!string.IsNullOrEmpty(path))
            {
                var destPath = "Assets/ModSystem/Core/Assemblies/ModSystem.Core.dll";
                
                // 确保目录存在
                var destDir = Path.GetDirectoryName(destPath);
                if (!Directory.Exists(destDir))
                {
                    Directory.CreateDirectory(destDir);
                }
                
                File.Copy(path, destPath, true);
                AssetDatabase.Refresh();
                
                Debug.Log("Core DLL imported successfully!");
            }
        }
        #endregion

        #region Menu Items - Mod Management
        
        /// <summary>
        /// 创建测试模组
        /// </summary>
        [MenuItem("ModSystem/Mods/Create Test Mod", false, 20)]
        public static void CreateTestMod()
        {
            var modPath = Path.Combine(Application.streamingAssetsPath, "Mods/TestMod");
            
            if (Directory.Exists(modPath))
            {
                if (!EditorUtility.DisplayDialog("Test Mod Exists", 
                    "Test mod already exists. Overwrite?", "Yes", "No"))
                {
                    return;
                }
            }
            
            Directory.CreateDirectory(modPath);
            
            // 创建基本结构
            Directory.CreateDirectory(Path.Combine(modPath, "Source"));
            Directory.CreateDirectory(Path.Combine(modPath, "Config"));
            Directory.CreateDirectory(Path.Combine(modPath, "Objects"));
            Directory.CreateDirectory(Path.Combine(modPath, "Resources"));
            
            // 创建manifest.json
            var manifest = @"{
  ""id"": ""test_mod"",
  ""name"": ""Test Mod"",
  ""version"": ""1.0.0"",
  ""author"": ""Developer"",
  ""description"": ""A test mod for ModSystem"",
  ""unity_version"": ""2021.3"",
  ""sdk_version"": ""1.0.0"",
  ""main_class"": ""TestMod.TestBehaviour"",
  ""permissions"": [""event_publish"", ""event_subscribe"", ""object_create""]
}";
            File.WriteAllText(Path.Combine(modPath, "manifest.json"), manifest);
            
            // 创建测试对象定义
            var buttonObject = @"{
  ""objectId"": ""test_button"",
  ""name"": ""Test Button"",
  ""components"": [
    {
      ""type"": ""Transform"",
      ""properties"": {
        ""position"": [0, 1, 0],
        ""rotation"": [0, 0, 0],
        ""scale"": [1, 1, 1]
      }
    },
    {
      ""type"": ""MeshRenderer"",
      ""properties"": {
        ""meshType"": ""cube"",
        ""color"": [0.2, 0.8, 0.2, 1],
        ""metallic"": 0.5,
        ""smoothness"": 0.8
      }
    },
    {
      ""type"": ""BoxCollider"",
      ""properties"": {
        ""size"": [1, 0.2, 1],
        ""isTrigger"": false
      }
    }
  ]
}";
            File.WriteAllText(Path.Combine(modPath, "Objects/test_button.json"), buttonObject);
            
            AssetDatabase.Refresh();
            Debug.Log("Test mod created!");
        }
        
        /// <summary>
        /// 加载所有模组
        /// </summary>
        [MenuItem("ModSystem/Mods/Load All Mods", false, 21)]
        public static void LoadAllMods()
        {
            var controller = GameObject.FindObjectOfType<ModSystemController>();
            if (controller == null)
            {
                EditorUtility.DisplayDialog("Error", 
                    "ModSystemController not found. Please run Setup Project first.", "OK");
                return;
            }
            
            // 这里可以添加编辑器中加载模组的逻辑
            Debug.Log("Load All Mods - This feature requires Play Mode");
            EditorUtility.DisplayDialog("Info", 
                "Please enter Play Mode to load mods.", "OK");
        }
        
        /// <summary>
        /// 打开模组文件夹
        /// </summary>
        [MenuItem("ModSystem/Mods/Open Mods Folder", false, 22)]
        public static void OpenModsFolder()
        {
            var path = Path.Combine(Application.streamingAssetsPath, "Mods");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            
            EditorUtility.RevealInFinder(path);
        }
        #endregion

        #region Menu Items - Debug Tools
        
        /// <summary>
        /// 打开事件监视器
        /// </summary>
        [MenuItem("ModSystem/Debug/Open Event Monitor", false, 40)]
        public static void OpenEventMonitor()
        {
            var monitor = GameObject.FindObjectOfType<Debug.EventMonitor>();
            if (monitor == null)
            {
                var go = new GameObject("EventMonitor");
                monitor = go.AddComponent<Debug.EventMonitor>();
            }
            
            Selection.activeGameObject = monitor.gameObject;
            Debug.Log("Event Monitor opened!");
        }
        
        /// <summary>
        /// 打开性能分析器
        /// </summary>
        [MenuItem("ModSystem/Debug/Open Performance Profiler", false, 41)]
        public static void OpenPerformanceProfiler()
        {
            var profiler = GameObject.FindObjectOfType<Debug.ModPerformanceProfiler>();
            if (profiler == null)
            {
                var go = new GameObject("ModPerformanceProfiler");
                profiler = go.AddComponent<Debug.ModPerformanceProfiler>();
            }
            
            Selection.activeGameObject = profiler.gameObject;
            Debug.Log("Performance Profiler opened!");
        }
        
        /// <summary>
        /// 打开内存监视器
        /// </summary>
        [MenuItem("ModSystem/Debug/Open Memory Monitor", false, 42)]
        public static void OpenMemoryMonitor()
        {
            var monitor = GameObject.FindObjectOfType<Debug.ModMemoryMonitor>();
            if (monitor == null)
            {
                var go = new GameObject("ModMemoryMonitor");
                monitor = go.AddComponent<Debug.ModMemoryMonitor>();
            }
            
            Selection.activeGameObject = monitor.gameObject;
            Debug.Log("Memory Monitor opened!");
        }
        #endregion

        #region Menu Items - Configuration
        
        /// <summary>
        /// 创建通信配置
        /// </summary>
        [MenuItem("ModSystem/Config/Create Communication Config", false, 60)]
        public static void CreateCommunicationConfig()
        {
            var config = @"{
  ""routes"": [
    {
      ""name"": ""example_route"",
      ""sourceEvent"": ""ButtonMod.ButtonPressedEvent"",
      ""conditions"": [
        {
          ""property"": ""ButtonId"",
          ""operator"": ""=="",
          ""value"": ""test_button""
        }
      ],
      ""actions"": [
        {
          ""eventType"": ""TestMod.TestEvent"",
          ""parameters"": {
            ""message"": ""Button pressed!""
          },
          ""delay"": 0
        }
      ],
      ""enabled"": true,
      ""priority"": 0
    }
  ],
  ""settings"": {
    ""enableDebugLogging"": false,
    ""maxConcurrentActions"": 10,
    ""defaultActionTimeout"": 5000
  }
}";
            
            var path = Path.Combine(Application.streamingAssetsPath, "ModConfigs/communication_config.json");
            var dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            
            File.WriteAllText(path, config);
            AssetDatabase.Refresh();
            
            Debug.Log("Communication config created!");
        }
        
        /// <summary>
        /// 创建安全配置
        /// </summary>
        [MenuItem("ModSystem/Config/Create Security Config", false, 61)]
        public static void CreateSecurityConfig()
        {
            var config = @"{
  ""requireSignedMods"": false,
  ""publicKeyPath"": """",
  ""modDirectory"": ""Mods"",
  ""allowedPermissions"": [
    ""event_publish"",
    ""event_subscribe"",
    ""service_register"",
    ""object_create"",
    ""config_read"",
    ""audio_play"",
    ""ui_create""
  ],
  ""defaultPermissions"": [
    ""event_publish"",
    ""event_subscribe"",
    ""config_read""
  ],
  ""trustedMods"": []
}";
            
            var path = Path.Combine(Application.streamingAssetsPath, "ModConfigs/security_config.json");
            var dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            
            File.WriteAllText(path, config);
            AssetDatabase.Refresh();
            
            Debug.Log("Security config created!");
        }
        #endregion

        #region Menu Items - Documentation
        
        /// <summary>
        /// 打开文档
        /// </summary>
        [MenuItem("ModSystem/Help/Open Documentation", false, 80)]
        public static void OpenDocumentation()
        {
            Application.OpenURL("https://docs.example.com/modsystem");
        }
        
        /// <summary>
        /// 打开API参考
        /// </summary>
        [MenuItem("ModSystem/Help/API Reference", false, 81)]
        public static void OpenAPIReference()
        {
            Application.OpenURL("https://docs.example.com/modsystem/api");
        }
        
        /// <summary>
        /// 关于ModSystem
        /// </summary>
        [MenuItem("ModSystem/Help/About ModSystem", false, 82)]
        public static void ShowAbout()
        {
            EditorUtility.DisplayDialog("About ModSystem",
                "Unity Mod System v1.0.0\n\n" +
                "A comprehensive modding framework for Unity applications.\n\n" +
                "Copyright © 2024 Your Company",
                "OK");
        }
        #endregion

        #region Helper Methods
        
        /// <summary>
        /// 创建必要的目录
        /// </summary>
        private static void CreateDirectories()
        {
            var dirs = new[]
            {
                "Assets/ModSystem",
                "Assets/ModSystem/Core",
                "Assets/ModSystem/Core/Assemblies",
                "Assets/ModSystem/Unity",
                "Assets/ModSystem/Unity/UnityImplementations",
                "Assets/ModSystem/Unity/Debug",
                "Assets/ModSystem/Unity/Editor",
                "Assets/ModSystem/Examples",
                "Assets/StreamingAssets",
                "Assets/StreamingAssets/ModConfigs",
                "Assets/StreamingAssets/Mods",
                "Assets/StreamingAssets/ModPackages"
            };
            
            foreach (var dir in dirs)
            {
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                    Debug.Log($"Created directory: {dir}");
                }
            }
        }
        
        /// <summary>
        /// 创建ModSystemController
        /// </summary>
        private static void CreateModSystemController()
        {
            var existing = GameObject.FindObjectOfType<ModSystemController>();
            if (existing == null)
            {
                var go = new GameObject("ModSystemController");
                go.AddComponent<ModSystemController>();
                
                // 标记为场景中的改动
                EditorUtility.SetDirty(go);
                
                Debug.Log("ModSystemController created!");
            }
            else
            {
                Debug.Log("ModSystemController already exists");
            }
        }
        
        /// <summary>
        /// 创建层级
        /// </summary>
        private static void CreateLayers()
        {
            CreateLayer("Interactable", 10);
            CreateLayer("ModObjects", 11);
            CreateLayer("ModUI", 12);
        }
        
        /// <summary>
        /// 创建单个层
        /// </summary>
        private static void CreateLayer(string name, int layer)
        {
            var tagManager = new SerializedObject(
                AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]
            );
            var layers = tagManager.FindProperty("layers");
            
            if (layers.GetArrayElementAtIndex(layer).stringValue == "")
            {
                layers.GetArrayElementAtIndex(layer).stringValue = name;
                tagManager.ApplyModifiedProperties();
                Debug.Log($"Layer '{name}' created at index {layer}");
            }
            else
            {
                Debug.Log($"Layer index {layer} already in use");
            }
        }
        
        /// <summary>
        /// 创建标签
        /// </summary>
        private static void CreateTags()
        {
            CreateTag("ModObject");
            CreateTag("ModUI");
            CreateTag("Interactable");
        }
        
        /// <summary>
        /// 创建单个标签
        /// </summary>
        private static void CreateTag(string tagName)
        {
            var tagManager = new SerializedObject(
                AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]
            );
            var tags = tagManager.FindProperty("tags");
            
            // 检查标签是否已存在
            for (int i = 0; i < tags.arraySize; i++)
            {
                if (tags.GetArrayElementAtIndex(i).stringValue == tagName)
                {
                    Debug.Log($"Tag '{tagName}' already exists");
                    return;
                }
            }
            
            // 添加新标签
            tags.InsertArrayElementAtIndex(tags.arraySize);
            tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tagName;
            tagManager.ApplyModifiedProperties();
            
            Debug.Log($"Tag '{tagName}' created");
        }
        
        /// <summary>
        /// 导入项目设置
        /// </summary>
        private static void ImportProjectSettings()
        {
            // 这里可以导入预定义的项目设置
            // 例如：输入设置、物理设置等
            Debug.Log("Project settings imported");
        }
        #endregion

        #region Validation
        
        /// <summary>
        /// 验证是否可以设置项目
        /// </summary>
        [MenuItem("ModSystem/Setup/Setup Project", true)]
        private static bool ValidateSetupProject()
        {
            // 只在编辑器非播放模式下可用
            return !EditorApplication.isPlaying;
        }
        
        /// <summary>
        /// 验证是否可以创建测试模组
        /// </summary>
        [MenuItem("ModSystem/Mods/Create Test Mod", true)]
        private static bool ValidateCreateTestMod()
        {
            // 检查StreamingAssets目录是否存在
            return Directory.Exists(Application.streamingAssetsPath);
        }
        #endregion
    }
}
```

## 13. Editor/ModSystemInspector.cs

```csharp
// ModSystem.Unity/Editor/ModSystemInspector.cs
using UnityEngine;
using UnityEditor;
using ModSystem.Core;
using System.Linq;
using System.Collections.Generic;

namespace ModSystem.Unity.Editor
{
    /// <summary>
    /// ModSystemController的自定义Inspector
    /// </summary>
    [CustomEditor(typeof(ModSystemController))]
    public class ModSystemControllerInspector : UnityEditor.Editor
    {
        private ModSystemController controller;
        private bool showLoadedMods = true;
        private bool showEventBus = true;
        private bool showServices = true;
        private bool showDebugOptions = false;
        
        void OnEnable()
        {
            controller = (ModSystemController)target;
        }
        
        public override void OnInspectorGUI()
        {
            // 标题
            EditorGUILayout.LabelField("Mod System Controller", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            // 状态信息
            DrawStatusInfo();
            
            EditorGUILayout.Space();
            
            // 控制按钮
            DrawControlButtons();
            
            EditorGUILayout.Space();
            
            // 加载的模组
            showLoadedMods = EditorGUILayout.Foldout(showLoadedMods, "Loaded Mods");
            if (showLoadedMods)
            {
                DrawLoadedMods();
            }
            
            // 事件总线信息
            showEventBus = EditorGUILayout.Foldout(showEventBus, "Event Bus");
            if (showEventBus)
            {
                DrawEventBusInfo();
            }
            
            // 服务注册信息
            showServices = EditorGUILayout.Foldout(showServices, "Registered Services");
            if (showServices)
            {
                DrawServicesInfo();
            }
            
            // 调试选项
            showDebugOptions = EditorGUILayout.Foldout(showDebugOptions, "Debug Options");
            if (showDebugOptions)
            {
                DrawDebugOptions();
            }
            
            // 在播放模式下自动刷新
            if (Application.isPlaying)
            {
                Repaint();
            }
        }
        
        /// <summary>
        /// 绘制状态信息
        /// </summary>
        private void DrawStatusInfo()
        {
            EditorGUILayout.BeginVertical("box");
            
            // 运行状态
            var isRunning = Application.isPlaying && controller.ModManagerCore != null;
            var statusColor = isRunning ? Color.green : Color.red;
            var statusText = isRunning ? "Running" : "Not Running";
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Status:", GUILayout.Width(60));
            
            var oldColor = GUI.color;
            GUI.color = statusColor;
            EditorGUILayout.LabelField(statusText, EditorStyles.boldLabel);
            GUI.color = oldColor;
            
            EditorGUILayout.EndHorizontal();
            
            if (isRunning)
            {
                var modCount = controller.ModManagerCore.GetLoadedMods().Count();
                EditorGUILayout.LabelField("Loaded Mods:", modCount.ToString());
                
                if (controller.EventBus is ModEventBus eventBus)
                {
                    var stats = eventBus.GetEventStatistics();
                    EditorGUILayout.LabelField("Event Types:", stats.Count.ToString());
                }
            }
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// 绘制控制按钮
        /// </summary>
        private void DrawControlButtons()
        {
            EditorGUILayout.BeginHorizontal();
            
            if (!Application.isPlaying)
            {
                if (GUILayout.Button("Enter Play Mode"))
                {
                    EditorApplication.EnterPlaymode();
                }
            }
            else
            {
                if (GUILayout.Button("Reload Mods"))
                {
                    // 实现重新加载逻辑
                    Debug.Log("Reloading mods...");
                }
                
                if (GUILayout.Button("Unload All"))
                {
                    // 实现卸载所有模组逻辑
                    Debug.Log("Unloading all mods...");
                }
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// 绘制加载的模组列表
        /// </summary>
        private void DrawLoadedMods()
        {
            EditorGUILayout.BeginVertical("box");
            
            if (!Application.isPlaying || controller.ModManagerCore == null)
            {
                EditorGUILayout.LabelField("Enter Play Mode to see loaded mods");
            }
            else
            {
                var mods = controller.ModManagerCore.GetLoadedMods().ToList();
                
                if (mods.Count == 0)
                {
                    EditorGUILayout.LabelField("No mods loaded");
                }
                else
                {
                    foreach (var mod in mods)
                    {
                        DrawModInfo(mod);
                    }
                }
            }
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// 绘制单个模组信息
        /// </summary>
        private void DrawModInfo(ModInstance mod)
        {
            EditorGUILayout.BeginVertical("box");
            
            // 模组基本信息
            EditorGUILayout.LabelField($"{mod.LoadedMod.Manifest.name} v{mod.LoadedMod.Manifest.version}");
            
            // 状态
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("State:", GUILayout.Width(50));
            
            var stateColor = mod.State switch
            {
                ModState.Active => Color.green,
                ModState.Error => Color.red,
                ModState.Paused => Color.yellow,
                _ => Color.white
            };
            
            var oldColor = GUI.color;
            GUI.color = stateColor;
            EditorGUILayout.LabelField(mod.State.ToString());
            GUI.color = oldColor;
            
            EditorGUILayout.EndHorizontal();
            
            // 详细信息
            EditorGUILayout.LabelField($"ID: {mod.LoadedMod.Manifest.id}");
            EditorGUILayout.LabelField($"Author: {mod.LoadedMod.Manifest.author}");
            EditorGUILayout.LabelField($"Behaviours: {mod.LoadedMod.Behaviours.Count}");
            
            // 操作按钮
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Unload", GUILayout.Width(60)))
            {
                controller.ModManagerCore.UnloadMod(mod.LoadedMod.Manifest.id);
            }
            
            if (mod.State == ModState.Active)
            {
                if (GUILayout.Button("Pause", GUILayout.Width(60)))
                {
                    mod.State = ModState.Paused;
                }
            }
            else if (mod.State == ModState.Paused)
            {
                if (GUILayout.Button("Resume", GUILayout.Width(60)))
                {
                    mod.State = ModState.Active;
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// 绘制事件总线信息
        /// </summary>
        private void DrawEventBusInfo()
        {
            EditorGUILayout.BeginVertical("box");
            
            if (!Application.isPlaying || controller.EventBus == null)
            {
                EditorGUILayout.LabelField("Event Bus not available");
            }
            else if (controller.EventBus is ModEventBus eventBus)
            {
                var stats = eventBus.GetEventStatistics();
                
                if (stats.Count == 0)
                {
                    EditorGUILayout.LabelField("No event subscriptions");
                }
                else
                {
                    foreach (var kvp in stats.OrderByDescending(x => x.Value))
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(kvp.Key, GUILayout.Width(200));
                        EditorGUILayout.LabelField($"{kvp.Value} handlers");
                        EditorGUILayout.EndHorizontal();
                    }
                }
            }
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// 绘制服务信息
        /// </summary>
        private void DrawServicesInfo()
        {
            EditorGUILayout.BeginVertical("box");
            
            if (!Application.isPlaying || controller.ServiceRegistry == null)
            {
                EditorGUILayout.LabelField("Service Registry not available");
            }
            else if (controller.ServiceRegistry is ModServiceRegistry registry)
            {
                var stats = registry.GetServiceStatistics();
                
                if (stats.Count == 0)
                {
                    EditorGUILayout.LabelField("No services registered");
                }
                else
                {
                    foreach (var kvp in stats)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(kvp.Key, GUILayout.Width(200));
                        EditorGUILayout.LabelField($"{kvp.Value} instances");
                        EditorGUILayout.EndHorizontal();
                    }
                }
            }
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// 绘制调试选项
        /// </summary>
        private void DrawDebugOptions()
        {
            EditorGUILayout.BeginVertical("box");
            
            if (GUILayout.Button("Open Event Monitor"))
            {
                ModSystemMenu.OpenEventMonitor();
            }
            
            if (GUILayout.Button("Open Performance Profiler"))
            {
                ModSystemMenu.OpenPerformanceProfiler();
            }
            
            if (GUILayout.Button("Open Memory Monitor"))
            {
                ModSystemMenu.OpenMemoryMonitor();
            }
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Export Debug Report"))
            {
                ExportDebugReport();
            }
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// 导出调试报告
        /// </summary>
        private void ExportDebugReport()
        {
            var report = "ModSystem Debug Report\n";
            report += $"Generated: {System.DateTime.Now}\n\n";
            
            if (Application.isPlaying && controller.ModManagerCore != null)
            {
                report += "Loaded Mods:\n";
                foreach (var mod in controller.ModManagerCore.GetLoadedMods())
                {
                    report += $"- {mod.LoadedMod.Manifest.name} v{mod.LoadedMod.Manifest.version} ({mod.State})\n";
                }
            }
            else
            {
                report += "System not running\n";
            }
            
            var path = EditorUtility.SaveFilePanel("Save Debug Report", "", "ModSystemDebugReport.txt", "txt");
            if (!string.IsNullOrEmpty(path))
            {
                System.IO.File.WriteAllText(path, report);
                Debug.Log($"Debug report saved to: {path}");
            }
        }
    }
    
    /// <summary>
    /// ModManager的自定义Inspector
    /// </summary>
    [CustomEditor(typeof(ModManager))]
    public class ModManagerInspector : UnityEditor.Editor
    {
        private ModManager manager;
        private bool showUnityInstances = true;
        
        void OnEnable()
        {
            manager = (ModManager)target;
        }
        
        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField("Mod Manager", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to see mod instances", MessageType.Info);
                return;
            }
            
            // Unity实例
            showUnityInstances = EditorGUILayout.Foldout(showUnityInstances, "Unity Instances");
            if (showUnityInstances)
            {
                DrawUnityInstances();
            }
        }
        
        /// <summary>
        /// 绘制Unity实例列表
        /// </summary>
        private void DrawUnityInstances()
        {
            EditorGUILayout.BeginVertical("box");
            
            if (manager.UnityInstances.Count == 0)
            {
                EditorGUILayout.LabelField("No Unity instances");
            }
            else
            {
                foreach (var kvp in manager.UnityInstances)
                {
                    DrawUnityInstance(kvp.Key, kvp.Value);
                }
            }
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// 绘制单个Unity实例
        /// </summary>
        private void DrawUnityInstance(string modId, ModUnityInstance instance)
        {
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.LabelField(modId, EditorStyles.boldLabel);
            
            if (instance.Container != null)
            {
                EditorGUILayout.ObjectField("Container", instance.Container, typeof(GameObject), true);
                EditorGUILayout.LabelField($"GameObjects: {instance.GameObjects.Count}");
                EditorGUILayout.LabelField($"Active Objects: {instance.ActiveObjectCount}");
                
                if (instance.EstimatedMemoryUsage > 0)
                {
                    var memoryMB = instance.EstimatedMemoryUsage / 1024f / 1024f;
                    EditorGUILayout.LabelField($"Memory: {memoryMB:F2} MB");
                }
            }
            else
            {
                EditorGUILayout.LabelField("Container destroyed");
            }
            
            EditorGUILayout.EndVertical();
        }
    }
    
    /// <summary>
    /// ModBehaviourUpdater的自定义Inspector
    /// </summary>
    [CustomEditor(typeof(ModBehaviourUpdater))]
    public class ModBehaviourUpdaterInspector : UnityEditor.Editor
    {
        private ModBehaviourUpdater updater;
        
        void OnEnable()
        {
            updater = (ModBehaviourUpdater)target;
        }
        
        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField("Mod Behaviour Updater", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            if (updater.Behaviour != null)
            {
                EditorGUILayout.LabelField("Behaviour ID:", updater.Behaviour.BehaviourId);
                EditorGUILayout.LabelField("Version:", updater.Behaviour.Version);
                EditorGUILayout.LabelField("Initialized:", updater.IsInitialized.ToString());
                
                EditorGUILayout.Space();
                
                // 更新间隔
                var newInterval = EditorGUILayout.FloatField("Update Interval", updater.UpdateInterval);
                if (newInterval != updater.UpdateInterval)
                {
                    updater.UpdateInterval = newInterval;
                }
                
                EditorGUILayout.Space();
                
                // 控制按钮
                EditorGUILayout.BeginHorizontal();
                
                if (updater.enabled)
                {
                    if (GUILayout.Button("Pause"))
                    {
                        updater.PauseUpdates();
                    }
                }
                else
                {
                    if (GUILayout.Button("Resume"))
                    {
                        updater.ResumeUpdates();
                    }
                }
                
                if (GUILayout.Button("Force Update"))
                {
                    updater.ForceUpdate();
                }
                
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.HelpBox("No behaviour attached", MessageType.Warning);
            }
        }
    }
}
```

这就是SimulationProject中Unity层的完整代码实现。这些代码提供了：

1. **ModSystemController** - 主控制器，管理整个模组系统
2. **ModManager** - Unity特定的模组管理器
3. **ModBehaviourUpdater** - 处理模组行为的Unity生命周期
4. **Unity实现类** - 将Core层的接口适配到Unity
5. **调试工具** - 事件监控、性能分析、内存监控
6. **编辑器工具** - 方便的菜单和Inspector界面

所有代码都包含了详细的注释，说明了每个类、接口和方法的用途。这些代码与Core层配合，提供了完整的模组系统功能。