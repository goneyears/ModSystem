/ UnityProject/Assets/Scripts/ModManager.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using ModSystem.Core;

namespace ModSystem.Unity
{
    /// <summary>
    /// Unity模组管理器
    /// 负责创建和管理模组的Unity实例
    /// </summary>
    [AddComponentMenu("ModSystem/Mod Manager")]
    public class ModManager : MonoBehaviour
    {
        #region Fields
        private ModManagerCore core;
        private ModUIFactory uiFactory;
        private UnityObjectFactory objectFactory;
        private Dictionary<string, ModUnityInstance> unityInstances;
        private Transform modsContainer;
        #endregion

        #region Properties
        /// <summary>
        /// 获取核心管理器
        /// </summary>
        public ModManagerCore Core => core;
        
        /// <summary>
        /// 获取UI工厂
        /// </summary>
        public ModUIFactory UIFactory => uiFactory;
        
        /// <summary>
        /// 获取对象工厂
        /// </summary>
        public UnityObjectFactory ObjectFactory => objectFactory;
        #endregion

        #region Initialization
        /// <summary>
        /// 初始化模组管理器
        /// </summary>
        public void Initialize(ModManagerCore core, ModUIFactory uiFactory = null, UnityObjectFactory objectFactory = null)
        {
            this.core = core ?? throw new ArgumentNullException(nameof(core));
            this.uiFactory = uiFactory;
            this.objectFactory = objectFactory;
            
            unityInstances = new Dictionary<string, ModUnityInstance>();
            
            // 创建模组容器
            CreateModsContainer();
            
            // 订阅事件
            SubscribeToEvents();
            
            Debug.Log("[ModManager] Initialized");
        }
        
        /// <summary>
        /// 创建模组容器GameObject
        /// </summary>
        private void CreateModsContainer()
        {
            var containerObj = new GameObject("ModsContainer");
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
            var loadVersion = modInstance.LoadedMod.LoadVersion;
            var container = new GameObject($"Mod_{modId}_v{loadVersion}");
            container.transform.SetParent(modsContainer);
            
            var unityInstance = new ModUnityInstance
            {
                Container = container,
                GameObjects = new List<GameObject>(),
                Components = new List<MonoBehaviour>(),
                ModInstance = modInstance,
                ModBehaviour = modInstance.ModBehaviour // 支持IMod接口
            };
            
            // 为每个IModBehaviour行为创建GameObject
            CreateBehaviourGameObjects(modInstance, unityInstance);
            
            // 如果模组实现了IMod接口，创建对应的包装器
            if (modInstance.LoadedMod.ModInstance != null)
            {
                CreateModBehaviourWrapper(modInstance, unityInstance);
            }
            
            // 创建对象定义中的GameObject
            CreateObjectsFromDefinitions(modInstance, unityInstance);
            
            unityInstances[modId] = unityInstance;
            
            Debug.Log($"[ModManager] Created Unity instance for mod: {modId} (v{loadVersion})");
        }
        
        /// <summary>
        /// 为IModBehaviour行为创建GameObject
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
        /// 为IMod创建行为包装器
        /// </summary>
        private void CreateModBehaviourWrapper(ModInstance modInstance, ModUnityInstance unityInstance)
        {
            if (modInstance.LoadedMod.ModInstance != null)
            {
                var wrapperObj = new GameObject($"ModWrapper_{modInstance.LoadedMod.Manifest.id}");
                wrapperObj.transform.SetParent(unityInstance.Container.transform);
                
                // 添加IMod包装器组件
                var wrapper = wrapperObj.AddComponent<ModBehaviourWrapper>();
                wrapper.Initialize(modInstance.LoadedMod.ModInstance, modInstance);
                
                unityInstance.GameObjects.Add(wrapperObj);
                unityInstance.Components.Add(wrapper);
                
                Debug.Log($"[ModManager] Created wrapper for IMod: {modInstance.LoadedMod.Manifest.id}");
            }
        }
        
        /// <summary>
        /// 从对象定义创建GameObject
        /// </summary>
        private async void CreateObjectsFromDefinitions(ModInstance modInstance, ModUnityInstance unityInstance)
        {
            if (objectFactory == null)
            {
                Debug.LogWarning("[ModManager] ObjectFactory not available, skipping object creation");
                return;
            }
            
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
            if (objectFactory == null)
            {
                throw new InvalidOperationException("ObjectFactory not initialized");
            }
            
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
                // 通知IMod即将销毁（如果支持热重载）
                if (instance.ModBehaviour is IReloadable reloadable)
                {
                    try
                    {
                        reloadable.OnBeforeReload();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[ModManager] Error in OnBeforeReload for {modId}: {ex.Message}");
                    }
                }
                
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

        #region Hot Reload Support
        /// <summary>
        /// 支持热重载的模组加载和激活
        /// </summary>
        public async Task LoadAndActivateMod(string modPath)
        {
            Debug.Log($"[ModManager] Loading and activating mod from: {modPath}");
            
            try
            {
                // 使用支持热重载的加载方法
                var instance = await core.ReloadModAsync(modPath);
                if (instance == null)
                {
                    Debug.LogError($"[ModManager] Failed to load mod from: {modPath}");
                    return;
                }
                
                var modId = instance.LoadedMod.Manifest.id;
                
                Debug.Log($"[ModManager] Mod loaded: {instance.LoadedMod.Manifest.name}");
                
                // 如果已存在Unity实例，先清理
                if (unityInstances.ContainsKey(modId))
                {
                    Debug.Log($"[ModManager] Cleaning up existing instance for mod: {modId}");
                    DestroyUnityInstance(modId);
                }
                
                // 创建新的Unity实例
                CreateUnityInstance(modId);
                
                // 如果模组使用IMod接口，需要创建行为并初始化
                if (instance.LoadedMod.ModInstance == null && !string.IsNullOrEmpty(instance.LoadedMod.Manifest.main_class))
                {
                    Debug.Log($"[ModManager] Creating behaviour for class: {instance.LoadedMod.Manifest.main_class}");
                    var behaviour = core.CreateModBehaviour(instance);
                    
                    if (behaviour != null)
                    {
                        Debug.Log($"[ModManager] Behaviour created successfully: {behaviour.GetType().FullName}");
                        
                        // 创建模组上下文
                        var context = new ModContext
                        {
                            ModId = instance.LoadedMod.Manifest.id,
                            EventBus = core.EventBus,
                            Logger = new UnityLogger(),
                            // 如果有UI工厂，添加到上下文
                            UIFactory = uiFactory
                        };
                        
                        // 初始化模组
                        behaviour.OnInitialize(context);
                        behaviour.OnEnable();
                        
                        // 如果支持热重载，调用OnAfterReload
                        if (behaviour is IReloadable reloadable)
                        {
                            reloadable.OnAfterReload();
                        }
                        
                        instance.ModBehaviour = behaviour;
                        Debug.Log($"[ModManager] Mod activated: {instance.LoadedMod.Manifest.id}");
                    }
                    else
                    {
                        Debug.LogError($"[ModManager] Failed to create behaviour for {instance.LoadedMod.Manifest.main_class}");
                    }
                }
                
                Debug.Log($"[ModManager] Mod {modId} loaded and activated successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ModManager] Error loading mod: {ex.Message}\n{ex.StackTrace}");
            }
        }
        
        /// <summary>
        /// 卸载指定模组
        /// </summary>
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
        
        /// <summary>
        /// 卸载所有模组
        /// </summary>
        public void UnloadAllMods()
        {
            Debug.Log("[ModManager] Unloading all mods...");
            
            // 先通知所有模组即将卸载
            foreach (var kvp in unityInstances.ToList())
            {
                var modId = kvp.Key;
                var unityInstance = kvp.Value;
                
                if (unityInstance.ModBehaviour != null)
                {
                    // 如果支持热重载，先调用OnBeforeReload
                    if (unityInstance.ModBehaviour is IReloadable reloadable)
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
    /// 模组的Unity实例数据（扩展支持IMod）
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
        /// 模组实例引用
        /// </summary>
        public ModInstance ModInstance { get; set; }
        
        /// <summary>
        /// IMod行为引用（用于反射模组）
        /// </summary>
        public IMod ModBehaviour { get; set; }
        
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